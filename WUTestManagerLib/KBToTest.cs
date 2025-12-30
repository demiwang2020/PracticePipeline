using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using ScorpionDAL;
using Helper;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System.Configuration;
//using System.Runtime.InteropServices;
namespace WUTestManagerLib
{
    public class SubKBInfo
    {
        public string TFSID { get; set; }
        public string KB { get; set; }
        public string ProductLayer { get; set; }
        public string VerificationFilePath { get; set; }
        public string PatchPath { get; set; }
    }

    public class KBToTest
    {
        private string id;
        private string kb;
        private string productlayer;
        private string title;
        private string guid;
        private Architecture targetarch;
        private string ss;
        private List<string> targetoss;
        private List<TWUOS> targetOSes;
        private Dictionary<string, SubKBInfo> subKBs;
        private Dictionary<string, string> otherProperties;
        private bool bWin10;

        public string KBValueForDisplay
        {
            get { return String.Format("{0} {1}", kb, targetarch); }
        }

        public string KB
        {
            get { return kb; }
        }

        public string Title
        {
            get { return title; }
        }
        public string GUID
        {
            get { return guid; }
        }
        public Architecture ARCH
        {
            get { return targetarch; }
        }

        public string ProductLayer
        {
            get { return productlayer; }
        }
        public List<TWUOS> TargetOSes
        {
            get { return targetOSes; }
        }
        public Dictionary<string, SubKBInfo> SubKBs
        {
            get { return subKBs; }
        }
        public Dictionary<string, string> OtherUpdateProperties
        {
            get { return otherProperties; }
        }

        public bool IsWin10Update
        {
            get { return this.bWin10; }
        }

        public KBToTest(string id, string kb, string productlayer, string title, string guid, string ss, string otherProperties)
        {
            targetoss = new List<string>();
            this.id = id;
            this.kb = kb.Replace("KB", "");
            this.productlayer = productlayer;
            this.title = title;
            this.guid = guid;
            this.ss = ss;
            this.bWin10 = title.Contains("Windows 10") ||
                            title.Contains("Server 2019") ||
                            title.Contains("Server 2016") ||
                            title.Contains("Windows Server, version 1909") ||
                            title.Contains("Windows Server, version 2004") ||
                            title.Contains("Microsoft server operating system version 21H2") ||
                            title.Contains("Windows 11");
            this.otherProperties = ParseUpdateProperyString(title,otherProperties);

            //pull the arch out of the title
            if (title.ToLower().Contains("x64"))
                targetarch = Architecture.AMD64;
            else if (title.ToLower().Contains("ia64") || title.ToLower().Contains("itanium"))
                targetarch = Architecture.IA64;
            else if (title.ToLower().Contains("windows rt "))
                targetarch = Architecture.ARM;
            else if (title.ToLower().Contains("windows 8.1 rt "))
                targetarch = Architecture.ARM;
            else if (title.ToLower().Contains("arm64"))
                targetarch = Architecture.ARM64;
            else
                targetarch = Architecture.X86;

            GetSubKBInfo();
            DetectTargetOSesViaTitle(title, targetarch);
        }

        private void GetSubKBInfo()
        {
            subKBs = new Dictionary<string, SubKBInfo>();
            List<WorkItemHelper> childTFSBugs = GetChildTFSBugs(id);

            // Not a parent KB
            if (childTFSBugs == null || childTFSBugs.Count == 0)
            {
                if (bWin10)
                    throw new Exception("Windows 10 updates without TFS ID is not supported for now");

                SubKBInfo subKb = new SubKBInfo();
                subKb.KB = kb;
                subKb.ProductLayer = productlayer;
                GetVerificationFilesForLegacyUpdates(subKb);
                subKBs.Add(kb, subKb);
            }
            else
            {
                productlayer = String.Empty; //Reset productlayer with the actual value from TFS

                foreach (WorkItemHelper wi in childTFSBugs)
                {
                    string sku = GetTargetSKUName(wi.SKU, Title);
                    string actualProdLayer = String.Format("{0} {1} {2}", sku, wi.ProductSPLevel, wi.PatchTechnology);

                    SubKBInfo subKb = new SubKBInfo();
                    subKb.TFSID = wi.ID.ToString();
                    subKb.KB = wi.KBNumber;
                    subKb.ProductLayer = actualProdLayer;
                    if (bWin10)
                    {
                        GetVerificationFilesForWin10Updates(subKb, wi, sku);
                    }
                    else
                    {
                        GetVerificationFilesForLegacyUpdates(subKb);
                        if (String.IsNullOrEmpty(subKb.VerificationFilePath))
                            GetVerificationFilesForWin10Updates(subKb, wi, sku);
                    }

                    if (String.IsNullOrEmpty(subKb.VerificationFilePath))
                        throw new Exception("Failed to get version verification file, please check legacy and Win10 extraction location");

                    subKBs.Add(subKb.KB, subKb);

                    if (productlayer == String.Empty)
                    {
                        productlayer = actualProdLayer;
                    }
                    else
                    {
                        productlayer = String.Format("{0}+{1}", productlayer, actualProdLayer);
                    }
                }
            }
        }

        private string GetTargetSKUName(string skuName, string updateTitle)
        {
            string[] sku = skuName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (sku.Length == 1)
                return skuName;

            //TFS 'SKU' field contains SKUs like '4.6/4.6.1', '4.5/4.5.1/4.5.2'. We get last (latest) SKU here
            for (int i = sku.Length - 1; i >= 0; --i)
            {
                if (updateTitle.Contains(sku[i]))
                    return sku[i];
            }

            return sku.Last();
        }

        //private static TFSCache _tfsCache = new TFSCache();
        //private static Connect2TFS.WorkItemBO QueryTFSWorkItem(int id)
        //{
        //    return _tfsCache.QueryTFSWorkItem(id);
        //}

        /// <summary>
        /// Get Child TFS bugs from tfsIDs, seperate with ',' or ';'
        /// </summary>
        private List<WorkItemHelper> GetChildTFSBugs(string tfsIDs)
        {
            if (String.IsNullOrEmpty(tfsIDs))
                return null;

            string[] ids = tfsIDs.Split(new char[] { ',', ';', '+' }, StringSplitOptions.RemoveEmptyEntries);
            List<WorkItemHelper> childBugs = new List<WorkItemHelper>();

            foreach (string id in ids)
            {
                int nId = Convert.ToInt32(id);
                childBugs.Add(new WorkItemHelper(WUTestManagerLib.TFSServerURI, nId));
            }

            return childBugs;
        }

        private void GetVerificationFilesForLegacyUpdates(SubKBInfo subKB)
        {
            string extractionlocaton = string.Format(@"\\DotNetPatchTest\F\SetupTest\ExtractLocation\KB{0}\", subKB.KB);
            //string extractionlocaton = string.Format(@"\\vsufile\workspace\current\setuptest\extractlocation\KB{0}\", subKB.KB);
            if (!Directory.Exists(extractionlocaton))
                return;
            //string extractionlocaton = string.Format(@"D:\Attachment\KB{0}\", subKB.KB);
            //UploadFiles uploadFiles = new UploadFiles();
            //uploadFiles.DownloadAllAttachments(Convert.ToInt32(subKB.TFSID), extractionlocaton);

            //search for the latest filelist for the arch
            DirectoryInfo extractiondir = new DirectoryInfo(extractionlocaton);
            FileInfo[] files = extractiondir.GetFiles(String.Format("FileVersion_{0}_GDR.txt", targetarch), SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                throw new Exception(String.Format("Could not find extraction file for parent KB{0}, sub KB{1} in location {2}.", kb, subKB.KB, extractionlocaton));
            }
            subKB.VerificationFilePath = files[files.Length - 1].FullName;

            DirectoryInfo patchDirectory = new DirectoryInfo(Path.GetDirectoryName(subKB.VerificationFilePath));
            var searchPatten = subKB.ProductLayer.ToUpper().Contains("CBS") ? "*.msu" : "*.exe";
            FileInfo[] setupfiles = patchDirectory.GetFiles(searchPatten, SearchOption.AllDirectories);

            if (setupfiles.Length == 0)
            {
                throw new Exception(String.Format("Could not find setup file for parent KB{0}, sub KB{1} in location {2}.", kb, subKB.KB, extractionlocaton));
            }

            subKB.PatchPath = setupfiles[setupfiles.Length - 1].FullName;
        }

        private void GetVerificationFilesForWin10Updates(SubKBInfo subKB, WorkItemHelper wi, string sku)
        {
            string patch = ConfigurationManager.AppSettings["PatchLocationHotfix"];
            string extractionlocaton = string.Format(@"\\DotNetPatchTest\F\Windows10Servicing\RunParameters\{0}\", subKB.KB);
            //string extractionlocaton = string.Format(@"\\clrdrop\ddrelqa\NetFXServicing\Windows10Servicing\RunParameters\{0}\", subKB.KB);
            bool flag = false;
            if (!Directory.Exists(extractionlocaton))
                return;
            //string[] directories = Directory.GetDirectories(extractionlocaton);
            DirectoryInfo extractiondir = new DirectoryInfo(extractionlocaton);

            FileInfo[] files = extractiondir.GetFiles(String.Format("FileVersion_{0}_{1}.txt", "NDP" + sku.Replace(".", string.Empty), targetarch), SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                throw new Exception(String.Format("Could not find extraction file for parent KB{0}, sub KB{1} in location {2}.", kb, subKB.KB, extractionlocaton));

            }
            var sortedFiles = files.OrderBy(s => int.Parse(Path.GetFileName(Path.GetDirectoryName(s.DirectoryName))));
            subKB.VerificationFilePath = sortedFiles.Last().FullName;
            //subKB.PatchPath = wi.GetPatchFullPath(ARCH);
            subKB.PatchPath = wi.GetPatchDownloadLocation(ARCH);
            subKB.PatchPath = subKB.PatchPath.Replace(ConfigurationManager.AppSettings["DownloadPath"], ConfigurationManager.AppSettings["PatchLocationHotfix"]);

            //change KB number because win10 patches have both 3.5 and 4.x payload
            if (bWin10)
                subKB.KB += "NDP" + sku.Replace(".", string.Empty);
        }

        private void DetectTargetOSesViaTitle(string title, Architecture arch)
        {
            #region define default os language/sp level/os name

            var osRTMLevel = "RTM";
            var osXPDefaultSPLevel = "SP3";
            var osXPName = "Windows XP";

            var os7DefaultSPLevel = "SP1";
            var os7Name = "Windows 7";

            var osVistaDefaultSPLevel = "SP2";
            var osVistaName = "Windows Vista";

            var os2003DefaultSPLevel = "SP2";
            var os2003Name = "Windows 2003 Server";

            var os2008DefaultSPLevel = "SP2";
            var os2008Name = "Windows Server 2008";

            var os2008R2DefaultSPLevel = "SP1";
            var os2008R2Name = "Windows Server 2008 R2";

            var os8Name = "Windows 8";
            var osBlueName = "Windows Blue";
            var os2012Name = "Windows Server 8";
            var os2012R2Name = "Windows Server Blue";
            var osRTName = "Windows RT";
            var osBlueRTName = "Windows 8.1 RT";

            var osRS5Name = "Windows 10 RS5";
            var os2019Name = "Windows Server 2019";

            //var osRS1Name = "Windows 10 RS1";
            var osRS1Name = "Windows 10 Enterprise 2016 LTSB";
            var os2016Name = "Windows Server 2016";

            var osRS2Name = "Windows 10 RS2";
            var osRS3Name = "Windows 10 RS3";
            var osRS4Name = "Windows 10 RS4";
            var osRS6Name = "Windows 10 RS6";
            var os19H2Name = "Windows 10 19H2";
            var os19H2ServerName = "Windows Server 19H2";

            var osRS3ServerName = "Windows Server RS3";
            var osRS4ServerName = "Windows Server RS4";
            var osRS6ServerName = "Windows Server RS6";

            var os20H1Name = "Windows 10 20H1";
            var os20H1ServerName = "Windows Server 20H1";


            var os20H2Name = "Windows 10 20H2";

            var os21H1Name = "Windows 10 21H1";
            var os21H2Name = "Windows 10 21H2";
            var os22H2Name = "Windows 10 22H2";

            var osServer2022 = "Windows Server 2022";
            var osServer23h2 = "Windows Server version 23H2";
            var osServer24h2 = "Windows Server version 24H2";

            var osWinSV21H2 = "Windows 10 SV21H2";
            var osWin1122H2 = "Windows 11 22H2";
            var osWin1123H2 = "Windows 11 23H2";
            var osWin1124H2 = "Windows 11 24H2";
            var osVNextName = "Windows Version Next";
            var osServerVNextName = "Windows Server Version Next";


            #endregion

            var shortOSCPUID = Convert.ToInt16(arch);
            var languageDefault = 1;
            targetOSes = new List<TWUOS>();
            title = title.ToUpper();

            var cutStartIndex = title.IndexOf(" ON ");
            if (cutStartIndex > 0)
            {
                title = title.Substring(cutStartIndex + 4);
            }
            else
            {
                cutStartIndex = title.IndexOf("FOR ");
                title = title.Substring(cutStartIndex + 4);
            }

            var cutEndIndex = title.Length - 1;

            //detect order by 'for','arch','kb', if detected any one of three then do substring
            //example titles like
            //1. Windows Vista SP2 and Windows Server 2008 SP2 for x64 (KB2931354)
            //2. Windows Vista SP2 and Windows Server 2008 SP2 x86 (KB2931354)
            //3. Windows 8.1 (KB2931358)
            //4. Microsoft .NET Framework 4.5.2 for Windows Vista (KB2901983)
            #region remove unuseful infors from title
            //var detectForIndex = title.IndexOf("FOR");
            var detectArchIndex = 0;
            var detectKBIndex = title.IndexOf("(KB");

            switch (arch)
            {
                case Architecture.AMD64:
                    {
                        detectArchIndex = title.IndexOf("X64");
                        break;
                    }
                case Architecture.IA64:
                    {
                        var tempIA64 = title.IndexOf("IA64");
                        var tempItanium = title.IndexOf("ITANIUM");
                        detectArchIndex = tempIA64 > tempItanium ? tempIA64 : tempItanium;
                        break;
                    }
                case Architecture.X86:
                    {
                        detectArchIndex = title.IndexOf("X86");
                        break;
                    }

                case Architecture.ARM64:
                    {
                        detectArchIndex = title.IndexOf("ARM64");
                        break;
                    }

                default:
                    detectArchIndex = 0;
                    break;
            }

            if (detectArchIndex > 0)
                cutEndIndex = detectArchIndex;
            else if (detectKBIndex > 0)
                cutEndIndex = detectKBIndex;

            var oses = title.Substring(0, cutEndIndex);
            #endregion

            //if title contains more than one os, it will join with 'AND' or ','
            //split by word 'AND' or ',', get the OSes in title
            string[] arrOSes = null;
            //if (title.Contains(',') || title.Contains("AND"))
            //    arrOSes = oses.Split(new string[] { ",", "AND" }, StringSplitOptions.None);
            if (title.Contains("FOR"))
                arrOSes = oses.Split(new string[] { "FOR" }, StringSplitOptions.None);
            else
                arrOSes = new string[] { oses };

            //foreach array of OSes, get right OS image ids
            //encapsulates into TargetOS class for display and kickoff use            
            foreach (var item in arrOSes)
            {
                #region Windows Server Version Next
                if (item.Contains("WINDOWS SERVER VERSION NEXT"))
                {
                    targetOSes.Add(GetTargetWUOS(osServerVNextName, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Windows Version Next
                if (item.Contains("WINDOWS VERSION NEXT"))
                {
                    targetOSes.Add(GetTargetWUOS(osVNextName, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Server 2022
                if (item.Contains("WINDOWS SERVER 2022") || item.Contains("MICROSOFT SERVER OPERATING SYSTEM VERSION 21H2"))
                {
                    targetOSes.Add(GetTargetWUOS(osServer2022, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Server 23h2
                if (item.Contains("SERVER VERSION 23H2") || item.Contains("MICROSOFT SERVER OPERATING SYSTEM, VERSION 23H2"))
                {
                    targetOSes.Add(GetTargetWUOS(osServer23h2, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Server 24h2
                if (item.Contains("SERVER VERSION 24H2") || item.Contains(" MICROSOFT SERVER OPERATING SYSTEM VERSION 24H2"))
                {
                    targetOSes.Add(GetTargetWUOS(osServer24h2, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Windows 11
                if (item.Contains("WINDOWS 11"))
                {
                    if (title.Contains("VERSION 22H2"))
                        targetOSes.Add(GetTargetWUOS(osWin1122H2, shortOSCPUID, osRTMLevel, languageDefault));
                    else if (title.Contains("VERSION 23H2"))
                        targetOSes.Add(GetTargetWUOS(osWin1123H2, shortOSCPUID, osRTMLevel, languageDefault));
                    else if (title.Contains("VERSION 24H2"))
                        targetOSes.Add(GetTargetWUOS(osWin1124H2, shortOSCPUID, osRTMLevel, languageDefault));
                    else
                        targetOSes.Add(GetTargetWUOS(osWinSV21H2, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Win10 21H2 and 22H2
                if (item.Contains("WINDOWS 10 VERSION 21H2"))
                {
                    targetOSes.Add(GetTargetWUOS(os21H2Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion
                #region Win10 21H2 and 22H2
                if (item.Contains("WINDOWS 10 VERSION 22H2"))
                {
                    targetOSes.Add(GetTargetWUOS(os22H2Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Win10 21H1
                if (item.Contains("WINDOWS 10 VERSION 21H1"))
                {
                    targetOSes.Add(GetTargetWUOS(os21H1Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 20H1
                if (item.Contains("VERSION 2004"))
                {
                    targetOSes.Add(GetTargetWUOS(os20H1Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 20H2
                if (item.Contains("VERSION 2009") || item.Contains("VERSION 20H2"))
                {
                    targetOSes.Add(GetTargetWUOS(os20H2Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 20H1 Server
                if (item.Contains("WINDOWS SERVER, VERSION 2004"))
                {
                    targetOSes.Add(GetTargetWUOS(os20H1ServerName, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS6 Server
                if (item.Contains("SERVER 2019 (1903)") || item.Contains("SERVER, VERSION 1903"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS6ServerName, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS5
                if (item.Contains("VERSION 1809"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS5Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Server 2019
                if (item.Contains("SERVER 2019"))
                {
                    targetOSes.Add(GetTargetWUOS(os2019Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS3 Server
                if (item.Contains("SERVER 2016 (1709)"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS3ServerName, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS4 Server
                if (item.Contains("SERVER 2016 (1803)"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS4ServerName, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS1
                if (item.Contains("VERSION 1607"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS1Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region Server 2016
                if (item.Contains("SERVER 2016"))
                {
                    targetOSes.Add(GetTargetWUOS(os2016Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS2
                if (item.Contains("VERSION 1703"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS2Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS3
                if (item.Contains("VERSION 1709"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS3Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS4
                if (item.Contains("VERSION 1803"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS4Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region RS6
                if (item.Contains("VERSION 1903"))
                {
                    targetOSes.Add(GetTargetWUOS(osRS6Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 19H2
                if (item.Contains("VERSION 1909"))
                {
                    if (title.Contains("WINDOWS SERVER, VERSION 1909"))
                    {
                        targetOSes.Add(GetTargetWUOS(os19H2ServerName, shortOSCPUID, osRTMLevel, languageDefault));
                    }
                    else
                    {
                        targetOSes.Add(GetTargetWUOS(os19H2Name, shortOSCPUID, osRTMLevel, languageDefault));
                    }

                    continue;
                }
                #endregion

                #region xp
                if (item.Contains("XP"))
                {
                    targetOSes.Add(GetTargetWUOS(osXPName, shortOSCPUID, osXPDefaultSPLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 2003
                if (item.Contains("2003"))
                {
                    targetOSes.Add(GetTargetWUOS(os2003Name, shortOSCPUID, os2003DefaultSPLevel, languageDefault));
                    continue;
                }
                #endregion

                #region vista
                if (item.Contains("VISTA"))
                {
                    targetOSes.Add(GetTargetWUOS(osVistaName, shortOSCPUID, osVistaDefaultSPLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 2008 & 2008 R2 RTM & 2008 R2 SP1
                if (item.Contains("2008"))
                {
                    if (item.Contains("R2"))
                    {
                        if (item.Contains(osRTMLevel))
                            targetOSes.Add(GetTargetWUOS(os2008R2Name, shortOSCPUID, osRTMLevel, languageDefault));
                        else
                            targetOSes.Add(GetTargetWUOS(os2008R2Name, shortOSCPUID, os2008R2DefaultSPLevel, languageDefault));
                    }
                    else
                    {
                        targetOSes.Add(GetTargetWUOS(os2008Name, shortOSCPUID, os2008DefaultSPLevel, languageDefault));
                    }

                    continue;
                }
                #endregion

                #region 7 SP1 & RTM
                if (item.Contains("WINDOWS 7"))
                {
                    if (item.Contains(osRTMLevel))
                        targetOSes.Add(GetTargetWUOS(os7Name, shortOSCPUID, osRTMLevel, languageDefault));
                    else
                        targetOSes.Add(GetTargetWUOS(os7Name, shortOSCPUID, os7DefaultSPLevel, languageDefault));
                    continue;
                }
                #endregion

                #region RT & 8.1 RT
                if (item.Contains(" RT ") && arch == Architecture.ARM)
                {
                    if (title.Contains("8.1") && shortOSCPUID == 4)
                        targetOSes.Add(GetTargetWUOS(osBlueRTName, shortOSCPUID, osRTMLevel, languageDefault));
                    else if (shortOSCPUID == 4)
                        targetOSes.Add(GetTargetWUOS(osRTName, shortOSCPUID, osRTMLevel, languageDefault));
                    continue;
                }
                #endregion

                #region 8 & blue
                if (item.Contains("WINDOWS 8"))
                {
                    if (item.Contains("WINDOWS 8.1"))
                        targetOSes.Add(GetTargetWUOS(osBlueName, shortOSCPUID, osRTMLevel, languageDefault));
                    else
                        targetOSes.Add(GetTargetWUOS(os8Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion

                #region 2012 & 2012 R2
                if (item.Contains("2012"))
                {
                    if (item.Contains("R2"))
                        targetOSes.Add(GetTargetWUOS(os2012R2Name, shortOSCPUID, osRTMLevel, languageDefault));
                    else
                        targetOSes.Add(GetTargetWUOS(os2012Name, shortOSCPUID, osRTMLevel, languageDefault));

                    continue;
                }
                #endregion
            }
        }

        private TWUOS GetTargetWUOS(string osName, short cpuID, string osSPLevel, int languageID)
        {
            TWUOS entity;
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                entity = db.TWUOS
                    .Where(p => p.OSName == osName
                        && p.OSCPUID == cpuID
                        && p.OSSPLevel == osSPLevel
                        && p.OSLanguageID == languageID)
                    .FirstOrDefault();
            }

            if (entity == null)
            {
                throw new Exception(
                    string.Format("Can't find target os in TWUOSes. OS name:{0}; cpu id:{1}; language id:{2}; os sp level:{3}"
                    , osName, cpuID.ToString(), languageID.ToString(), osSPLevel)
                 );
            }

            return entity;
        }

        /// <summary>
        /// Pause properties string
        /// example of string: prop1=value1;prop2=value2
        /// </summary>
        private Dictionary<string, string> ParseUpdateProperyString(string Title,string properties)
        {
            Dictionary<string, string> dictProp = DefaultUpdateOtherProperties();

            if (String.IsNullOrEmpty(properties))
                return dictProp;

            string[] temp = properties.Split(new char[] { ';', '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length <= 1)
                return dictProp;

            try
            {
                for (int i = 0; i < temp.Length; i += 2)
                {
                    dictProp[temp[i]] = temp[i + 1];
                }
            }
            catch
            { }

            if (dictProp.ContainsKey("ReleaseType"))
            {
                switch (dictProp["ReleaseType"])
                {
                    case "SecurityOnly":
                    case "Catalog(Security)":
                    case "Promotion":
                        dictProp["AutoSelectOnWebSites"] = "False";
                        break;

                    case "Preview":
                    case "Catalog(Preview)":
                        dictProp["AutoSelectOnWebSites"] = "False";
                        //if (IsWin10Update)
                        if (title.Contains("Preview") && (title.Contains("23H2") || title.Contains("24H2") || (title.Contains("Windows 11") && title.Contains("22H2"))))
                            dictProp["BrowseOnly"] = "True";
                        else
                            dictProp["BrowseOnly"] = "false";
                        break;
                }
            }

            return dictProp;
        }

        private Dictionary<string, string> DefaultUpdateOtherProperties()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            bool browseOnly = false;
            bool autoSelectOnWeb = true;

            if (Title.Contains("Security Only Update"))
                autoSelectOnWeb = false;
            if (Title.Contains("Preview of Quality Rollup"))
            {
                autoSelectOnWeb = false;
                if (!bWin10)
                    browseOnly = true;
            }

            //Default properties are for critical security update
            props.Add("BrowseOnly", browseOnly.ToString());
            props.Add("AutoSelectOnWebSites", autoSelectOnWeb.ToString());

            return props;
        }
    }
}
