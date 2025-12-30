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
        private static string BuildIsInstallableRuleForCBSPatch(int arch, int releaseType, int sku, WorkItemHelper tfsObject, TApplicabilityRules rules)
        {
            using (var db = new WUSAFXDbContext())
            {
                string rule = db.TPropertyMappings.Where(p => p.ID == rules.IsInstallableRuleID).Single().MappedContent;

                return String.Format("<pub:IsInstallable>{0}</pub:IsInstallable>", rule);
            }
        }

        private static string BuildIsInstalledRuleForCBSPatch(int arch, int releaseType, int sku, WorkItemHelper tfsObject, TApplicabilityRules rules)
        {
            using (var db = new WUSAFXDbContext())
            {
                string content = db.TPropertyMappings.Where(p => p.ID == rules.IsInstalledRuleID).Single().MappedContent;

                if (releaseType == 3) // special code for security only updates
                {
                    bool b4PriorSKU = sku == 1 || sku == 2 || sku == 12;

                    string fileRule = BuildCBSPayloadFileList(arch, sku, b4PriorSKU, tfsObject);

                    content = String.Format("<lar:Or xmlns:lar=\"http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules\">{0}<lar:And>{1}</lar:And></lar:Or>",
                                            content,
                                            fileRule);
                }

                return String.Format("<pub:IsInstalled>{0}</pub:IsInstalled>", content);
            }
        }

        private static string BuildCBSPayloadFileList(int arch, int sku, bool b4PriorSKU, WorkItemHelper tfsObject)
        {
            DtgpatchtestContext dtgDB = new DtgpatchtestContext();
            WUSAFXDbContext wuDB = new WUSAFXDbContext();
            StringBuilder sb = new StringBuilder();

            try
            {
                //1. find out WSD extract location
                //List<string> files = GetCBSExtractedFiles(arch, tfsObject);

                //2. Get file version
                //Dictionary<string, string> dictFileAndVersion = GetPayloadFilesInfo(files);
                Dictionary<string, string> dictFileAndVersion = GetFilesFromReleaseMan(arch, tfsObject);

                //3. for each file
                //  a. Query file locations from Scorpion DB
                //  b. Build a rule according to the format
                int id = _dictSku[sku];
                foreach (KeyValuePair<string, string> kv in dictFileAndVersion)
                {
                    if (IsFile2ExecludeInCBS(kv.Key))
                        continue;

                    IQueryable<SANFileLocation> records = null;
                    if(id == 1 || id == 2 || id == 3)
                        records = dtgDB.FileLocations.Where(p => p.CPUID == arch && (p.ProductID == 1 && p.ProductSPLevel == "SP2" || p.ProductID == 2 && p.ProductSPLevel == "SP2" || p.ProductID == 3 && p.ProductSPLevel == "SP1") && p.FileName.Equals(kv.Key, StringComparison.InvariantCultureIgnoreCase));
                    else
                        records = dtgDB.FileLocations.Where(p => p.CPUID == arch && p.ProductID == id && p.ProductSPLevel == "RTM" && p.FileName.Equals(kv.Key, StringComparison.InvariantCultureIgnoreCase));

                    foreach (var record in records)
                    {
                        // skip assembly files
                        if (record.FileLocation.Contains("\\GAC_"))
                            continue;

                        string fullPath = Path.Combine(record.FileLocation, record.FileName);

                        //Build rule to check file version
                        foreach (var csidl in wuDB.TCsidls)
                        {
                            if (fullPath.StartsWith(csidl.Value))
                            {
                                string updatedPath = fullPath.Substring(csidl.Value.Length);

                                if (b4PriorSKU)
                                    sb.Append("<lar:Or>");
                                
                                sb.AppendFormat("<bar:FileVersion Path=\"{0}\" Comparison=\"GreaterThanOrEqualTo\" Version=\"{1}\" Csidl=\"{2}\" xmlns:bar=\"http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules\" />",
                                    updatedPath, //path
                                    kv.Value, //version
                                    csidl.ID);

                                if (b4PriorSKU)
                                {
                                    sb.Append("<lar:Not>");
                                    sb.AppendFormat("<bar:FileExists Path=\"{0}\" Csidl=\"{1}\" xmlns:bar=\"http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules\" />",
                                        updatedPath, //path
                                        csidl.ID);
                                    sb.Append("</lar:Not></lar:Or>");
                                }

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

        private static Dictionary<string, string> GetPayloadFilesInfo(List<string> files)
        {
            Dictionary<string, string> dictFileAndVersion = new Dictionary<string, string>();

            foreach (string file in files)
            {
                // skip resource files
                if (Path.GetFileName(file).ToLowerInvariant().Contains(".resources."))
                    continue;
                
                FileVersionInfo myFvi = FileVersionInfo.GetVersionInfo(file);

                // skip non-versioned files
                if (String.IsNullOrEmpty(myFvi.FileVersion))
                {
                    continue;
                }

                if (!dictFileAndVersion.ContainsKey(myFvi.OriginalFilename))
                {
                    string version = String.Format("{0}.{1}.{2}.{3}", myFvi.FileMajorPart, myFvi.FileMinorPart, myFvi.FileBuildPart, myFvi.FilePrivatePart);
                    dictFileAndVersion.Add(myFvi.OriginalFilename, version);
                }
            }

            return dictFileAndVersion;
        }

        private static List<string> GetCBSExtractedFiles(int arch, WorkItemHelper tfsObject)
        {   
            string wsdLocation = tfsObject.GetPatchLocation((Architecture)arch);
            if (!wsdLocation.StartsWith(@"\\winsehotfix"))
                wsdLocation = tfsObject.GetProppedLocation((Architecture)arch);

            if (String.IsNullOrEmpty(wsdLocation))
                throw new Exception("Package Propped location is empty");
            if (!Directory.Exists(wsdLocation))
                throw new Exception("Package Propped location does not exist: " + wsdLocation);

            string cabName = GetCabName(tfsObject, arch);
            wsdLocation = Path.Combine(wsdLocation, "EXPANDED_PACKAGE", cabName);
            if (!Directory.Exists(wsdLocation))
                throw new Exception("Package Propped location does not exist: " + wsdLocation);

            string[] subDirs = Directory.GetDirectories(wsdLocation);
            if (subDirs == null || subDirs.Length == 0)
                return null;

            List<string> listFiles = new List<string>();
            foreach (string dir in subDirs)
            {
                listFiles.AddRange(Directory.GetFiles(dir));
            }

            return listFiles;
        }

        private static string GetCabName(WorkItemHelper tfsObject, int arch)
        {
            return Path.GetFileNameWithoutExtension(tfsObject.GetPatchName((Architecture)arch));
        }

        private static bool IsFile2ExecludeInCBS(string fileName)
        {
            if (fileName == "msvcp120_clr0400.dll" ||
                fileName == "msvcr120_clr0400.dll" ||
                fileName == "presentationhost_v0400.dll.mui")
                return true;

            return false;
        }
    }
}
