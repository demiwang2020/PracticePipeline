using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Helper;
using System.Xml;

namespace NetFxSetupLibrary.ProductTesting
{
    public class NDP45x
    {
        #region Data Member
        public string Product { get; private set; }

        public string Dropserver { get; private set; }

        public string DevVer { get; private set; }

        public string Branch { get; private set; }

        public string Buildnumber { get; private set; }

        public string GDRVersion { get; private set; }

        public string LDRVersion { get; private set; }

        public int ReleaseNumber { get; private set; }

        public string VSVersionNumberMajor { get; private set; }

        public string FileName { get; private set; }

        public string FullRedistPath { get; private set; }

        public string FullRedistISVPath { get; private set; }

        public bool IsLDR { get; private set; }

        public string RRType { get; private set; }

        public List<string> PackageList { get; private set; }

        public bool IsPrivatePackage { get; private set; }

        public bool IsDualBranch { get; private set; }

        public string MTPackPath { get; private set; }

        public string SDKPath { get; private set; }

        public string BuildnumberFileFolder { get; private set; }

        public string FullRedistKBNumber { get; private set; }

        public string COBPath { get; private set; }

        #endregion

        #region Contructor

        public NDP45x()
        {
        }

        public NDP45x(string currentProduct)
        {
            Product = currentProduct;
            PackageList = new List<string>();
        }

        #endregion

        #region Public Method

        public bool GenerateNDP45xProduct(string currentProduct, string schema, string inputFile, string kbListFile)
        {
            Product product = new Product();
            product.Name = currentProduct;
            product.Schema = schema;
            product.Common = new ProductCommon { KBList = GetKBList(kbListFile) };
            product.Sku = GetSkuList(inputFile);
            if (string.IsNullOrEmpty(FullRedistPath))
            {
                throw new Exception("You must provide FullReidst package path whatever package you want to test!");
            }
            string netfxName = Path.GetFileName(FullRedistPath).Substring(3, 3);
            if (netfxName == "451" || netfxName == "452")
                IsDualBranch = true;
            else
            {
                IsDualBranch = false;
 //               IsLDR = false;
 //               RRType = "RU";
            }
            try
            {
                XMLHelper.XmlSerializeToFile(product, Path.Combine(Path.GetTempPath(), currentProduct + ".xml"), typeof(Product));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Serialize XML failed: {0}", ex.StackTrace));
            }
            return true;
        }

        public void GenerateNDP45xVariableFile(string destinationPath)
        {
            AnalyzeFullRedistPath();
            if (!GetBuildNumber())
            {
                throw new Exception("Could not generate variable files, could not get LDR/GDR build number.  NDP45x.cs GenerateNDP45xVariableFile(string destinationPath)");
            }
            if (IsDualBranch)
            {
                WriteVariableFile(Path.Combine(destinationPath, string.Format("Variables_{0}_LDR.txt", Product)), true);
                if (!IsLDR)
                    WriteVariableFile(Path.Combine(destinationPath, string.Format("Variables_{0}_GDR.txt", Product)), false);
            }
            else
            {
                if (IsLDR)
                {
                    WriteVariableFile(Path.Combine(destinationPath, string.Format("Variables_{0}_LDR.txt", Product)), true);
                }
                else
                {
                    WriteVariableFile(Path.Combine(destinationPath, string.Format("Variables_{0}_GDR.txt", Product)), false);
                }
            }
            if (PackageList.Contains("MTPackENU"))
                WriteMTPackVariableFile(Path.Combine(destinationPath, string.Format("Variables_{0}_MTPack.txt", Product)));
            if (PackageList.Contains("SDK"))
                WriteSDKVariableFile(Path.Combine(destinationPath, string.Format("Variables_{0}_SDK.txt", Product)));
        }

        public static List<string> GetRefreshRedistMatrixList(string rrType, string package)
        {
            List<string> matrixList = new List<string>();
            switch (package.ToUpperInvariant())
            {
                case "FULLREDIST":
                    if (rrType == "RU")
                    {
                        matrixList.Add("RefreshRedist_Downlevel");
                        matrixList.Add("RefreshRedist_Downlevel_LP");
                    }
                    else //matrix specially for redist HFR
                    {
                        matrixList.Add("RefreshRedist_HFR_Downlevel");
                    }  
                    break;
                case "FULLREDISTISV":
                    matrixList.Add("RefreshRedist_Win8");
                    matrixList.Add("RefreshRedist_WinBlue");
                    matrixList.Add("RefreshRedist_Win10A");
                    matrixList.Add("RefreshRedist_Win10B");
                    if (rrType.ToUpperInvariant() == "RU")
                    {
                        matrixList.Add("RefreshRedist_Downlevel");
                        matrixList.Add("RefreshRedist_Downlevel_LP");
                        matrixList.Add("RefreshRedist_Win8_LP");
                        matrixList.Add("RefreshRedist_WinBlue_LP");
                        matrixList.Add("RefreshRedist_Win10A_LP");
                        matrixList.Add("RefreshRedist_Win10B_LP");
                    }
                    else
                    {
                        matrixList.Add("RefreshRedist_HFR_Downlevel");
                    }
                    break;
                case "REFRESHREDISTMSU":
                    matrixList.Add("RefreshRedist_Win8");
                    if (rrType.ToUpperInvariant() == "RU")
                        matrixList.Add("RefreshRedist_Win8_LP");
                    break;
                case "REFRESHREDISTMSU_BLUE":
                    matrixList.Add("RefreshRedist_WinBlue");
                    if (rrType.ToUpperInvariant() == "RU")
                        matrixList.Add("RefreshRedist_WinBlue_LP");
                    break;
                case "REFRESHREDISTMSU_WIN10A":
                    matrixList.Add("RefreshRedist_Win10A");
                    if (rrType.ToUpperInvariant() == "RU")
                        matrixList.Add("RefreshRedist_Win10A_LP");
                    break;
                case "REFRESHREDISTMSU_WIN10B":
                    matrixList.Add("RefreshRedist_Win10B");
                    if (rrType.ToUpperInvariant() == "RU")
                        matrixList.Add("RefreshRedist_Win10B_LP");
                    break;
                case "WEBBOOTSTRAPPER":
                    if (rrType.ToUpperInvariant() == "RU")
                        matrixList.Add("RefreshRedist_Webbootstrapper");
                    else //For hotfix rollup
                    {
                        matrixList.Add("RefreshRedist_HFR_Downlevel");
                        matrixList.Add("RefreshRedist_Win8");
                        matrixList.Add("RefreshRedist_WinBlue");
                    }
                    break;
                case "DEVPACK":
                    matrixList.Add("RefreshRedist_DevPack");
                    break;
                case "MTPACKENU":
                    matrixList.Add("RefreshRedist_MTPack");
                    break;
                case "MTPACKCOREENU":
                    matrixList.Add("RefreshRedist_MTPackCore");
                    break;
                case "SDK":
                    matrixList.Add("RefreshRedist_SDK");
                    break;
            }
            return matrixList;
        }

        public void GenerateSAFXInputs(string inputFile)
        {
            FullRedistPath = string.Empty;
            FullRedistISVPath = string.Empty;
            using (StreamReader sr = new StreamReader(inputFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (String.IsNullOrEmpty(line))
                        continue;
                    string[] temp = line.Split('=');
                    if (temp.Length == 2 && !string.IsNullOrEmpty(temp[0].Trim()) && !string.IsNullOrEmpty(temp[1].Trim()))
                    {
                        switch (temp[0].Trim().ToUpperInvariant())
                        {
                            case "FULLREDIST":
                                FullRedistPath = ConvertValueToVariable(temp[1]);
                                break;
                            case "FULLREDISTISV":
                                FullRedistISVPath = ConvertValueToVariable(temp[1]);
                                break;
                            case "REFRESHREDISTRELEASETYPES":
                                RRType = temp[1].Trim().ToUpperInvariant();
                                switch (RRType)
                                {
                                    case "RU":
                                        IsLDR = false;
                                        break;
                                    case "HF":
                                    case "HFR":
                                        IsLDR = true;
                                        break;
                                    default:
                                        Console.WriteLine("Unknown RefreshRedistType");
                                        break;
                                }
                                break;
                            case "GDR":
                                GDRVersion = temp[1].Trim();
                                break;
                            case "LDR":
                                LDRVersion = temp[1].Trim();
                                break;
                            case "MTPACKENUBUNDLE":
                                MTPackPath = ConvertValueToVariable(temp[1]);
                                break;
                            case "SDKBUNDLE":
                                SDKPath = ConvertValueToVariable(temp[1]);
                                break;
                            case "FULLREDISTKBNUMBER":
                                FullRedistKBNumber = ConvertValueToVariable(temp[1]);
                                break;
                            case "CLICKONCEBOOTSTRAPPER":
                                COBPath = ConvertValueToVariable(temp[1]);
                                break;
                        }
                    }
                }
                IsPrivatePackage = !Regex.IsMatch(FullRedistPath, ConfigurationManager.AppSettings["NDP45xFullRedistPathRegexString"]);
                if (!IsPrivatePackage)
                {
                    AnalyzeFullRedistPath();
                    GetBuildNumber();
                }
            }
        }

        #endregion

        #region Private Method

        private void AnalyzeFullRedistPath()
        {
            string netfxName = Path.GetFileName(FullRedistPath).Substring(3, 3);
            if (netfxName == "451" || netfxName == "452")
                IsDualBranch = true;
            else
            {
                IsDualBranch = false;
//                IsLDR = false;
//                RRType = "RU";
            }
            string[] splitedPath = FullRedistPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            Dropserver = splitedPath[0];
            DevVer = splitedPath[2];
            Branch = splitedPath[3];
            Buildnumber = splitedPath[6];
        }

        private void WriteVariableFile(string path, bool isLDR)
        {
            string buildNumber = isLDR ? LDRVersion : GDRVersion;
            string[] splittedBuilNumbers = buildNumber.Split('.');
            string fileVersion = IsDualBranch ? "30319." + splittedBuilNumbers[1].TrimStart('0') : CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]);
            string netfxBuild, netfxSrvBuild;
            ConvertNetfxBuild(out netfxBuild, out netfxSrvBuild, buildNumber);
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.WriteLine("BRANCH={0}", Branch);
                sw.WriteLine("BUILDNUMBER={0}", buildNumber);
                sw.WriteLine("FILEVERSION={0}", fileVersion);
                if (Convert.ToDouble(buildNumber.Trim()) < 03621.00)
                {
                    sw.WriteLine("VSVER={0}.7.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                }
                else
                {
                    sw.WriteLine("VSVER={0}.8.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                }
                sw.WriteLine("NETFXBUILD={0}", netfxBuild);
                sw.WriteLine("RELEASENUMBER={0}", ReleaseNumber);
                if (!isLDR)
                {
                    //Used for DevPack
                    sw.WriteLine("MTBUILDNUMBER={0}", buildNumber);
                    sw.WriteLine("MTFILEVERSION={0}", fileVersion);
                    if (Convert.ToDouble(buildNumber.Trim()) < 03621.00)
                    {
                        sw.WriteLine("MTVSVER={0}.7.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                    }
                    else
                    {
                        sw.WriteLine("MTVSVER={0}.8.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                    }
                    sw.WriteLine("MTNETFXSRVCBUILD={0}", netfxSrvBuild);
                    sw.WriteLine("MTNETFXBUILD={0}", netfxBuild);
                    sw.WriteLine("MTRELEASENUMBER={0}", ReleaseNumber);
                    ConvertNetfxBuild(out netfxBuild, out netfxSrvBuild, buildNumber, "SDK");
                    sw.WriteLine("SDKBUILDNUMBER={0}", buildNumber);
                    sw.WriteLine("SDKFILEVERSION={0}", fileVersion);
                    if (Convert.ToDouble(buildNumber.Trim()) < 03621.00)
                    {
                        sw.WriteLine("SDKVSVER={0}.7.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                    }
                    else
                    {
                        sw.WriteLine("SDKVSVER={0}.8.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                    }
                    sw.WriteLine("SDKNETFXSRVCBUILD={0}", netfxSrvBuild);
                    sw.WriteLine("SDKNETFXBUILD={0}", netfxBuild);
                    sw.WriteLine("SDKRELEASENUMBER={0}", ReleaseNumber);
                }
            }
        }

        private void WriteMTPackVariableFile(string path)
        {
            string buildNumber = GDRVersion;
            string[] splittedBuilNumbers = buildNumber.Split('.');
            string fileVersion = IsDualBranch ? "30319." + splittedBuilNumbers[1].TrimStart('0') : CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]);
            string netfxBuild, netfxSrvBuild;
            ConvertNetfxBuild(out netfxBuild, out netfxSrvBuild, buildNumber);
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.WriteLine("MTBUILDNUMBER={0}", buildNumber);
                sw.WriteLine("MTFILEVERSION={0}", fileVersion);
                if(Convert.ToDouble(buildNumber.Trim()) < 03621.00)
                {
                    sw.WriteLine("MTVSVER={0}.7.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                }
                else
                {
                    sw.WriteLine("MTVSVER={0}.8.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                }
                sw.WriteLine("MTNETFXSRVCBUILD={0}", netfxSrvBuild);
                sw.WriteLine("MTNETFXBUILD={0}", netfxBuild);
                sw.WriteLine("MTRELEASENUMBER={0}", ReleaseNumber);
            }
        }

        private void WriteSDKVariableFile(string path)
        {
            string buildNumber = GDRVersion;
            string[] splittedBuilNumbers = buildNumber.Split('.');
            string fileVersion = IsDualBranch ? "30319." + splittedBuilNumbers[1].TrimStart('0') : CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]);
            string netfxBuild, netfxSrvBuild;
            ConvertNetfxBuild(out netfxBuild, out netfxSrvBuild, buildNumber, "SDK");
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.WriteLine("SDKBUILDNUMBER={0}", buildNumber);
                sw.WriteLine("SDKFILEVERSION={0}", fileVersion);
                if (Convert.ToDouble(buildNumber.Trim()) < 03621.00)
                {
                    sw.WriteLine("SDKVSVER={0}.7.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                }
                else
                {
                    sw.WriteLine("SDKVSVER={0}.8.{1}", VSVersionNumberMajor, CheckSpecialFileVersion(splittedBuilNumbers[0]) + '.' + CheckSpecialFileVersion(splittedBuilNumbers[1]));
                }
                sw.WriteLine("SDKNETFXSRVCBUILD={0}", netfxSrvBuild);
                sw.WriteLine("SDKNETFXBUILD={0}", netfxBuild);
                sw.WriteLine("SDKRELEASENUMBER={0}", ReleaseNumber);
            }
        }

        private ProductCommonKB[] GetKBList(string kbListFile)
        {
            List<ProductCommonKB> kbList = new List<ProductCommonKB>();
            using (StreamReader sr = new StreamReader(kbListFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (String.IsNullOrEmpty(line))
                        continue;
                    string[] temp = line.Split('=');
                    if (temp.Length == 2 && !string.IsNullOrEmpty(temp[0].Trim()) && !string.IsNullOrEmpty(temp[1].Trim()))
                    {
                        kbList.Add(new ProductCommonKB() { Name = temp[0].Trim(), Value = uint.Parse(temp[1].Trim()) });
                    }
                }
            }
            return kbList.ToArray();
        }

        private ProductSku[] GetSkuList(string inputFile)
        {
            #region Generate Packages

            List<ProductSkuPackage> RefreshRedistPackages = new List<ProductSkuPackage>();
            List<ProductSkuPackage> DevPackPackages = new List<ProductSkuPackage>();
            List<ProductSkuPackage> MTPackPackages = new List<ProductSkuPackage>();
            List<ProductSkuPackage> MTPackCorePackages = new List<ProductSkuPackage>();
            List<ProductSkuPackage> SDKPackages = new List<ProductSkuPackage>();

            using (StreamReader sr = new StreamReader(inputFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (String.IsNullOrEmpty(line))
                        continue;
                    string[] temp = line.Split('=');
                    if (temp.Length == 2 && !string.IsNullOrEmpty(temp[0].Trim()) && !string.IsNullOrEmpty(temp[1].Trim()))
                    {
                        switch (temp[0].Trim().ToUpperInvariant())
                        {
                            case "FULLREDIST":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "FullRedist", Path = ConvertValueToVariable(temp[1]) });
                                FullRedistPath = ConvertValueToVariable(temp[1]);
                                PackageList.Add("FullRedist");
                                break;
                            case "FULLREDISTLP":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "FullRedistLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "FULLREDISTISV":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "FullRedistISV", Path = ConvertValueToVariable(temp[1]) });
                                PackageList.Add("FullRedistISV");
                                break;
                            case "FULLREDISTISVLP":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "FullRedistISVLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "WEBBOOTSTRAPPER":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "Webbootstrapper", Path = ConvertValueToVariable(temp[1]) });
                                PackageList.Add("Webbootstrapper");
                                break;
                            case "REFRESHREDISTMSU":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU", Path = ConvertValueToVariable(temp[1], true) });
                                PackageList.Add("RefreshRedistMSU");
                                break;
                            case "REFRESHREDISTMSULP":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSULP", Path = ConvertValueToVariable(temp[1], true) });
                                break;
                            case "REFRESHREDISTMSU_BLUE":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU_Blue", Path = ConvertValueToVariable(temp[1], true) });
                                PackageList.Add("RefreshRedistMSU_Blue");
                                break;
                            case "REFRESHREDISTMSU_BLUELP":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU_BlueLP", Path = ConvertValueToVariable(temp[1], true) });
                                break;
                            case "REFRESHREDISTMSU_WIN10A":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU_Win10A", Path = ConvertValueToVariable(temp[1], true) });
                                PackageList.Add("RefreshRedistMSU_Win10A");
                                break;
                            case "REFRESHREDISTMSU_WIN10ALP":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU_Win10ALP", Path = ConvertValueToVariable(temp[1], true) });
                                break;
                            case "REFRESHREDISTMSU_WIN10B":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU_Win10B", Path = ConvertValueToVariable(temp[1], true) });
                                PackageList.Add("RefreshRedistMSU_Win10B");
                                break;
                            case "REFRESHREDISTMSU_WIN10BLP":
                                RefreshRedistPackages.Add(new ProductSkuPackage() { Name = "RefreshRedistMSU_Win10BLP", Path = ConvertValueToVariable(temp[1], true) });
                                break;
                            case "DEVPACK":
                                DevPackPackages.Add(new ProductSkuPackage() { Name = "DevPack", Path = ConvertValueToVariable(temp[1]) });
                                PackageList.Add("DevPack");
                                break;
                            case "DEVPACKLP":
                                DevPackPackages.Add(new ProductSkuPackage() { Name = "DevPackLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKENU":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackENU", Path = ConvertValueToVariable(temp[1]) });
                                PackageList.Add("MTPackENU");
                                break;
                            case "MTPACKENU45":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackENU45", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKLPENU":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackLPENU", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKLP":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCOREENU":
                                MTPackCorePackages.Add(new ProductSkuPackage() { Name = "MTPackCoreENU", Path = ConvertValueToVariable(temp[1]) });
                                PackageList.Add("MTPackCoreENU");
                                break;
                            case "MTPACKCORELPENU":
                                MTPackCorePackages.Add(new ProductSkuPackage() { Name = "MTPackCoreLPENU", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCORELP":
                                MTPackCorePackages.Add(new ProductSkuPackage() { Name = "MTPackCoreLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKENUBUNDLE":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackENUBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKLPENUBUNDLE":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackLPENUBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKLPBUNDLE":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackLPBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCUMULATIVELPENU":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackCumulativeLPENU", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCUMULATIVELP":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackCumulativeLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKDOCREDIRECTEDLPENU":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackDocRedirectedLPENU", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKDOCREDIRECTEDLP":
                                MTPackPackages.Add(new ProductSkuPackage() { Name = "MTPackDocRedirectedLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCOREENUBUNDLE":
                                MTPackCorePackages.Add(new ProductSkuPackage() { Name = "MTPackCoreENUBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCORELPENUBUNDLE":
                                MTPackCorePackages.Add(new ProductSkuPackage() { Name = "MTPackCoreLPENUBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "MTPACKCORELPBUNDLE":
                                MTPackCorePackages.Add(new ProductSkuPackage() { Name = "MTPackCoreLPBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "SDK":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "SDK", Path = ConvertValueToVariable(temp[1]) });
                                PackageList.Add("SDK");
                                break;
                            case "SDKLP":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "SDKLP", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "SDKBUNDLE":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "SDKBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "SDKLPBUNDLE":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "SDKLPBundle", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "SDK45":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "SDK45", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "SDKLP45":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "SDKLP45", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "CLICKONCEBOOTSTRAPPER":
                                SDKPackages.Add(new ProductSkuPackage() { Name = "ClickOnceBootstrapper", Path = ConvertValueToVariable(temp[1]) });
                                break;
                            case "REFRESHREDISTRELEASETYPES":
                                RRType = temp[1].Trim().ToUpperInvariant();
                                switch (RRType)
                                {
                                    case "RU":
                                        IsLDR = false;
                                        break;
                                    case "HF":
                                    case "HFR":
                                        IsLDR = true;
                                        break;
                                    default:
                                        Console.WriteLine("Unknown RefreshRedistType");
                                        break;
                                }
                                break;
                            case "BUILDNUMBERFILEFOLDER":
                                if (Directory.Exists(temp[1].Trim()))
                                    BuildnumberFileFolder = temp[1].Trim();
                                break;
                        }
                    }
                }
            }
            #endregion

            List<ProductSku> skuList = new List<ProductSku>();
            skuList.Add(new ProductSku() { Name = "RefreshRedist", Package = RefreshRedistPackages.ToArray() });
            skuList.Add(new ProductSku() { Name = "DevPack", Package = DevPackPackages.ToArray() });
            skuList.Add(new ProductSku() { Name = "MTPack", Package = MTPackPackages.ToArray() });
            skuList.Add(new ProductSku() { Name = "MTPackCore", Package = MTPackCorePackages.ToArray() });
            skuList.Add(new ProductSku() { Name = "SDK", Package = SDKPackages.ToArray() });
            return skuList.ToArray();
        }

        private string ConvertValueToVariable(string path, bool isMSU = false)
        {
            string convertedPath = path.ToLowerInvariant().Trim();
            if (convertedPath.Contains("jpn"))
            {
                convertedPath = convertedPath.Replace("jpn", "#[LANG]");
            }

            if (isMSU)
            {
                if (convertedPath.Contains("amd64"))
                {
                    convertedPath = convertedPath.Replace("amd64", "#(ARCHITECTURE)");
                }
                if (convertedPath.Contains("x64"))
                {
                    convertedPath = convertedPath.Replace("x64", "#(Arch)");
                }
                if (convertedPath.Contains("ja-jp"))
                {
                    convertedPath = convertedPath.Replace("ja-jp", "#[LANG4CHAR]");
                }
                Regex regex = new Regex(@"kb\w*");
                convertedPath = regex.Replace(convertedPath, "KB#(KBNumber)");
            }

            return convertedPath;
        }

        /// <summary>
        /// Get build number
        /// </summary>
        private bool GetBuildNumber()
        {
            string buildNumberFilePath = string.Format(@"\\{0}\drops\{1}\{2}\raw\{3}\binaries.x86ret\version\buildnumber.h", Dropserver, DevVer, Branch, Buildnumber);
            if (!string.IsNullOrEmpty(BuildnumberFileFolder))
            {
                buildNumberFilePath = Path.Combine(BuildnumberFileFolder, "buildnumber.h");
            }
            string vsVersionFilePath = Path.Combine(Path.GetDirectoryName(buildNumberFilePath), "vsversion_generated.h");
            if (IsDualBranch)
            {
                if (IsLDR)
                {
                    LDRVersion = ReadBuildNumberFile(buildNumberFilePath);
                    if (LDRVersion == null)
                        return false;
                }
                else
                {
                    GDRVersion = ReadBuildNumberFile(buildNumberFilePath);
                    LDRVersion = ReadBuildNumberFile(GetLDRBuildNumberFilePath());
                    if (GDRVersion == null || LDRVersion == null)
                        return false;
                }
                ReleaseNumber = (5 << 16) + int.Parse(LDRVersion.Split('.')[0]);
            }
            else
            {
                GDRVersion = ReadBuildNumberFile(buildNumberFilePath);
                if (IsLDR)
                    LDRVersion = GDRVersion;
                if (GDRVersion == null)
                    return false;
                string netVer = Regex.Match(Path.GetFileName(FullRedistPath), @"\d{2,3}", RegexOptions.None).Value;
                switch (netVer)
                { 
                    case "453":
                        ReleaseNumber = (5 << 16) + 53322 + int.Parse(GDRVersion.Split('.')[0]);
                        break;
                    //case "46":
                    //    ReleaseNumber = (6 << 16) + int.Parse(GDRVersion.Split('.')[0]);
                    //    break;
                    //case "461":
                    //    ReleaseNumber = (6 << 16) + int.Parse(GDRVersion.Split('.')[0]);
                    //    break;
                    //case "462":
                    //    ReleaseNumber = (6 << 16) + int.Parse(GDRVersion.Split('.')[0]);
                    //    break;
                    //case "47":
                    //    ReleaseNumber = (7 << 16) + int.Parse(GDRVersion.Split('.')[0]);
                    //    break;
                    default:
                        ReleaseNumber = (int.Parse(netVer.Substring(1,1)) << 16) + int.Parse(GDRVersion.Split('.')[0]);
                        break;
                }
                
            }
            VSVersionNumberMajor = ReadVSVersionFile(vsVersionFilePath);

            return true;
        }

        /// <summary>
        /// Read buildnumber.h to get build number
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string ReadBuildNumberFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception(string.Format("Buildnumber.h file doesn't exist in path {0}", filePath));
            }
            try
            {
                StreamReader sr = File.OpenText(filePath);
                string temp = string.Empty;
                Regex regex = new Regex(@"\d*\.\d*");
                while ((temp = sr.ReadLine()) != null)
                {
                    if (temp.ToUpperInvariant().Contains("#DEFINE BUILDNUMBERS_T"))
                    {
                        Match match = regex.Match(temp);
                        sr.Close();
                        if (match.Success)
                            return match.Value;
                        else
                            throw new Exception("Match build number failed");
                    }
                }
                throw new Exception("Failed to find BuildNumbers_T in buildnumber.h");
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error occurred when reading Buildnumber.h: {0}", ex.Message));
            }
        }

        private string ReadVSVersionFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception(string.Format("Vsversion_generated.h file doesn't exist in path {0}", filePath));
            }
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (String.IsNullOrEmpty(line))
                            continue;
                        if (line.ToUpperInvariant().StartsWith("#DEFINE VSVERSIONNUMBERMAJOR "))
                        {
                            return line.Substring(line.Trim().LastIndexOf(' ') + 1);
                        }
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error occurred when reading Vsversion_generated.h: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Get LDR build.config path through related GDR path
        /// </summary>
        /// <returns></returns>
        private string GetLDRBuildNumberFilePath()
        {
            string config = string.Format(@"\\{0}\drops\{1}\{2}\raw\{3}\binaries.x86ret\build.config", Dropserver, DevVer, Branch, Buildnumber);
            if (!File.Exists(config))
            {
                throw new Exception(string.Format("build.config file doesn't exist in path {0}", config));
            }
            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(config);
                XmlNamespaceManager namespaces = new XmlNamespaceManager(document.NameTable);
                namespaces.AddNamespace("ns", document.DocumentElement.NamespaceURI);
                XmlNode node = document.SelectSingleNode(@"/ns:BuildSettings/ns:SetupsBuild/ns:NamedStore/ns:Store", namespaces);
                return string.Format(@"{0}\binaries.x86ret\version\buildnumber.h", node.InnerText);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error occurred when reading build.config: {0}", ex.Message));
            }
        }

        private string CheckSpecialFileVersion(string splittedBuildNumber)
        {
            if (string.IsNullOrEmpty(splittedBuildNumber.TrimStart('0')))
                return "0";
            else
                return splittedBuildNumber.TrimStart('0');
        }

        private void ConvertNetfxBuild(out string netfxBuild, out string netfxSrvBuild, string buildNumber, string package = "")
        {
            string[] splittedBuilNumbers = buildNumber.Split('.');
            switch (Regex.Match(Path.GetFileName(FullRedistPath), @"\d{2,3}", RegexOptions.None).Value)
            {
                case "451":
                case "452":
                    netfxBuild = splittedBuilNumbers[0];
                    netfxSrvBuild = splittedBuilNumbers[0];
                    break;
                case "453":
                    netfxBuild = (int.Parse(splittedBuilNumbers[0].TrimStart('0')) + 53322).ToString();
                    netfxSrvBuild = (int.Parse(splittedBuilNumbers[0].TrimStart('0')) + 53322).ToString() + "." + splittedBuilNumbers[1];
                    break;
                //case "46":
                //    netfxBuild = splittedBuilNumbers[0];                    
                //    netfxSrvBuild = buildNumber;
                //    if (package.ToLowerInvariant() == "sdk")
                //        netfxSrvBuild = netfxBuild;
                //    break;
                //case "461":
                //    netfxBuild = splittedBuilNumbers[0];
                //    netfxSrvBuild = buildNumber;
                //    if (package.ToLowerInvariant() == "sdk")
                //        netfxSrvBuild = netfxBuild;
                //    break;
                //case "462":
                //    netfxBuild = splittedBuilNumbers[0];
                //    netfxSrvBuild = buildNumber;
                //    if (package.ToLowerInvariant() == "sdk")
                //        netfxSrvBuild = netfxBuild;
                //    break;
                //case "47":
                //    netfxBuild = splittedBuilNumbers[0];
                //    netfxSrvBuild = buildNumber;
                //    if (package.ToLowerInvariant() == "sdk")
                //        netfxSrvBuild = netfxBuild;
                //    break;
                default:
                    netfxBuild = splittedBuilNumbers[0];
                    netfxSrvBuild = buildNumber;
                    if (package.ToLowerInvariant() == "sdk")
                        netfxSrvBuild = netfxBuild;
                    break;
            }
        }

        #endregion
    }
}
