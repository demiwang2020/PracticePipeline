using Helper;
using PubsuiteStaticTestLib.DbClassContext;
using PubsuiteStaticTestLib.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.UpdateHelper
{
    public partial class ApplicabilityRulesBuilder
    {
        private static string BuildIsInstallableRuleForRedistPatch(int arch, int releaseType, int sku, WorkItemHelper tfsObject, TApplicabilityRules rules)
        {
            using (var db = new WUSAFXDbContext())
            {
                string rule = db.TPropertyMappings.Where(p => p.ID == rules.IsInstallableRuleID).Single().MappedContent;

                return String.Format("<pub:IsInstallable>{0}</pub:IsInstallable>", rule);
            }
        }

        // Is-Installed rules is like:
        // <Or>
        //  <And>
        //      <Or> Product codes </Or>
        //      File versions
        //  </And>
        // </Or>
        private static string BuildIsInstalledRuleForRedistPatch(int arch, int releaseType, int sku, WorkItemHelper tfsObject, TApplicabilityRules rules)
        {
            using (var db = new WUSAFXDbContext())
            {
                // get product codes nodes
                string productCodes = db.TPropertyMappings.Where(p => p.ID == rules.IsInstalledRuleID).Single().MappedContent;

                // get version nodes
                string versionNodes = BuildRedistPayloadFileList(arch, sku, tfsObject);

                return String.Format("<pub:IsInstalled><lar:Or xmlns:lar=\"http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules\"><lar:And><lar:Or>{0}</lar:Or>{1}</lar:And></lar:Or></pub:IsInstalled>",
                                    productCodes, versionNodes);
            }
        }

        private static string GetMSPName(WorkItemHelper tfsObject, int arch)
        {
            string[] temp = tfsObject.GetPatchName((Architecture)arch).Split(new char[] { '-' });

            if(temp[0].Equals("NDP46", StringComparison.InvariantCultureIgnoreCase))
                temp[0] = "NDP47";

            return String.Format("{0}-{1}.msp", temp[0], temp[1]);
        }

        private static string[] GetRedistExtractedFiles(int arch, WorkItemHelper tfsObject)
        {
            string wsdLocation = tfsObject.GetProppedLocation((Architecture)arch);
            if (String.IsNullOrEmpty(wsdLocation))
                throw new Exception("Package Propped location is empty");
            if (!Directory.Exists(wsdLocation))
                throw new Exception("Package Propped location does not exist: " + wsdLocation);

            string mspName = GetMSPName(tfsObject, arch);
            wsdLocation = Path.Combine(wsdLocation, "EXPANDED_PACKAGE", mspName);
            if (!Directory.Exists(wsdLocation))
                throw new Exception("Package Propped location does not exist: " + wsdLocation);

            return Directory.GetFiles(wsdLocation, "*", SearchOption.AllDirectories);
        }

        private static string GetTargetMSIPath(int arch, WorkItemHelper tfsObject)
        {
            using (var db = new WUSAFXDbContext())
            {
                int sku = db.TNetSkus.Where(p => p.SKU == tfsObject.SKU).Single().ID;

                return db.TMSIs.Where(p => p.SKU == sku && p.Arch == arch).Single().MSIPath;
            }
        }

        //private static string GetRedistActualFileName(string msiPath, string originalName)
        //{
        //    return MsiUtils.GetActualFileName(originalName, msiPath);
        //}

        //private static Dictionary<string, string> GetPayloadFilesInfo(string[] files, string targetMSIPath)
        //{
        //    Dictionary<string, string> dictFileAndVersion = new Dictionary<string, string>();

        //    foreach (string file in files)
        //    {
        //        FileVersionInfo myFvi = FileVersionInfo.GetVersionInfo(file);
                
        //        // skip non-versioned files
        //        if (String.IsNullOrEmpty(myFvi.FileVersion))
        //        {
        //            continue;
        //        }

        //        string name = GetRedistActualFileName(targetMSIPath, Path.GetFileName(file));

        //        // skip CRT files because applicability doesn't include them
        //        if (name.Equals("msvcp120_clr0400.dll", StringComparison.InvariantCultureIgnoreCase) || name.Equals("msvcr120_clr0400.dll", StringComparison.InvariantCultureIgnoreCase))
        //            continue;

        //        if (!dictFileAndVersion.ContainsKey(name))
        //        {
        //            string version = String.Format("{0}.{1}.{2}.{3}", myFvi.FileMajorPart, myFvi.FileMinorPart, myFvi.FileBuildPart, myFvi.FilePrivatePart);
        //            dictFileAndVersion.Add(name, version);
        //        }
        //    }

        //    return dictFileAndVersion;
        //}

        private static string BuildRedistPayloadFileList(int arch, int sku, WorkItemHelper tfsObject)
        {
            DtgpatchtestContext dtgDB = new DtgpatchtestContext();
            WUSAFXDbContext wuDB = new WUSAFXDbContext();
            StringBuilder sb = new StringBuilder();

            try
            {
                //1. find out WSD extract location
                //string[] files = GetRedistExtractedFiles(arch, tfsObject);

                //string targetMSIPath = GetTargetMSIPath(arch, tfsObject);
                //Dictionary<string, string> dictFileAndVersion = GetPayloadFilesInfo(files, targetMSIPath);
                Dictionary<string, string> dictFileAndVersion = GetFilesFromReleaseMan(arch, tfsObject);
                AddAdditionalFiles(dictFileAndVersion, sku);
                

                //2. for each file
                //  a. Get actual file name
                //  b. Query file locations from Scorpion DB
                //  c. Build a rule according to the format
                int id = _dictSku[sku];
                string splevel = "RTM";
                switch (sku)
                {
                    case 1:
                        splevel = "SP2";
                        break;

                    case 2:
                        splevel = "SP2";
                        break;

                    case 12:
                        splevel = "SP1";
                        break;
                }

                foreach (KeyValuePair<string, string> kv in dictFileAndVersion)
                {
                    if (IsFile2ExecludeInRedist(kv.Key))
                        continue;
                    
                    var records = dtgDB.FileLocations.Where(p => p.CPUID == arch && p.ProductID == id && p.ProductSPLevel == splevel && p.FileName.Equals(kv.Key, StringComparison.InvariantCultureIgnoreCase));

                    foreach (var record in records)
                    {
                        // skip assembly files
                        if (record.FileLocation.Contains("\\GAC_"))
                            continue;

                        string fullPath = Path.Combine(record.FileLocation, record.FileName).Replace("%windir%\\system32", "%system%").Replace("%windir%\\syswow64", "%syswow%");

                        //Build rule to check file version
                        foreach (var csidl in wuDB.TCsidls)
                        {
                            if (fullPath.StartsWith(csidl.Value))
                            {
                                sb.AppendFormat("<bar:FileVersion Path=\"{0}\" Comparison=\"GreaterThanOrEqualTo\" Version=\"{1}\" Csidl=\"{2}\" xmlns:bar=\"http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules\" />",
                                    fullPath.Substring(csidl.Value.Length), //path
                                    kv.Value, //version
                                    csidl.ID);

                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                dtgDB.Dispose();
                wuDB.Dispose();
            }

            return sb.ToString();
        }

        private static bool IsFile2ExecludeInRedist(string fileName)
        {
            if (fileName == "msvcp120_clr0400.dll" ||
                fileName == "msvcr120_clr0400.dll" ||
                fileName == "dfshim.dll.mui" ||
                fileName == "dw20.exe" ||
                fileName == "sbscmp10.dll" ||
                fileName == "system.resources.resourcemanager.dll" ||
                fileName == "vsversion.dll" ||
                fileName == "wpffontcache_v0400.exe" ||
                fileName == "wpffontcache_v0400.exe.mui")
                return true;

            return false;
        }

        /// <summary>
        /// Hardcode some exceptions between RM and pubsuite (RM misses some files)
        /// </summary>
        private static void AddAdditionalFiles(Dictionary<string, string> dictFileAndVersion, int sku)
        {
            if (dictFileAndVersion.ContainsKey("penimc2_v0400.dll"))
            {
                string version = dictFileAndVersion["penimc2_v0400.dll"].ToString();

                dictFileAndVersion["penimc_v0400.dll"] = version;
                dictFileAndVersion["penimc.dll"] = "1" + version;
            }

            if (sku == 4) // 4.5.2
            {
                if (dictFileAndVersion.ContainsKey("filetrackerui.dll")) // this should be a LCU
                {
                    string version = dictFileAndVersion["filetrackerui.dll"].ToString();

                    dictFileAndVersion["CvtResUI.dll"] = version;
                    dictFileAndVersion["cvtres.exe"] = version;
                    dictFileAndVersion["regtlibv12.exe"] = version;
                    dictFileAndVersion["NaturalLanguage6.dll"] = version;
                    dictFileAndVersion["NlsData0009.dll"] = version;
                    dictFileAndVersion["NlsLexicons0009.dll"] = version;
                }
            }
        }
    }
}
