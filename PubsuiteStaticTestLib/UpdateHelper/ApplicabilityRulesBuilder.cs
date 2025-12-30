using PubsuiteStaticTestLib.DbClassContext;
using PubsuiteStaticTestLib.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper;
using RMIntegration.RMService;
using Microsoft.Test.DevDiv.SAFX.CommonLibraries.CBSAnalyzerLib;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace PubsuiteStaticTestLib.UpdateHelper
{
    public partial class ApplicabilityRulesBuilder
    {
        // Map sku id in WUSAFXDB to PatchTestDatabase
        private static Dictionary<int, int> _dictSku = null;

        public static string BuildApplicabilityRules(int osid, int arch, int releaseType, int sku, WorkItemHelper tfsObject)
        {
            if (_dictSku == null)
                CreateSKUMapping();
            
            using (var db = new WUSAFXDbContext())
            {                              
                // query for applicability rules that exactly match
                TApplicabilityRules rule = db.TApplicabilityRulesCollection.Where(p => p.OS == osid && p.Arch == arch && p.SKU == sku).FirstOrDefault();
                if (rule == null)
                    rule = db.TApplicabilityRulesCollection.First();

                string isInstalled = String.Empty, isInstallable = String.Empty;

                switch (tfsObject.PatchTechnology)
                {
                    case "MSI":
                        isInstallable = BuildIsInstallableRuleForRedistPatch(arch, releaseType, sku, tfsObject, rule);
                        isInstalled = BuildIsInstalledRuleForRedistPatch(arch, releaseType, sku, tfsObject, rule);
                        break;

                    case "CBS":
                        isInstallable = BuildIsInstallableRuleForCBSPatch(arch, releaseType, sku, tfsObject, rule);
                        isInstalled = BuildIsInstalledRuleForCBSPatch(arch, releaseType, sku, tfsObject, rule);
                        break;

                    case "OCM":
                        break;
                }

                if (!String.IsNullOrEmpty(isInstalled) && !String.IsNullOrEmpty(isInstallable))
                {
                    return String.Format("<pub:ApplicabilityRules>{0}{1}</pub:ApplicabilityRules>", isInstalled, isInstallable);
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        private static void CreateSKUMapping()
        {
            _dictSku = new Dictionary<int, int>();

            _dictSku.Add(1, 1); //2.0
            _dictSku.Add(2, 2); //3.0
            _dictSku.Add(12, 3); //3.5
            _dictSku.Add(3, 8); //4.0
            _dictSku.Add(4, 27); //4.5.2
            
            //4.6.x ~ 4.7.x
            _dictSku.Add(5, 38);
            _dictSku.Add(6, 38);
            _dictSku.Add(7, 38);
            _dictSku.Add(8, 38);
            _dictSku.Add(9, 38);
            _dictSku.Add(10, 38);
            _dictSku.Add(13, 39);
        }

        private static Dictionary<string, string> GetFilesFromReleaseMan(int arch, WorkItemHelper tfsObject)
        {
            RMIntegration.RMSvcMethods rm = new RMIntegration.RMSvcMethods(tfsObject.ID);
            rm.Populate();
            Patch patch = rm.PPatch;

            string strArch = String.Empty;
            switch (arch)
            { 
                case 1:
                    strArch = "x86";
                    break;

                case 2:
                    strArch = "x64";
                    break;

                case 3:
                    strArch = "IA64";
                    break;

                case 4:
                    strArch = "arm";
                    break;
            }
            Dictionary<string, string> fileInfo = new Dictionary<string, string>();
            string FileLocation = tfsObject.GetPatchFullPath((Architecture)arch).ToLowerInvariant();

            if (FileLocation.StartsWith(@"f:\packages"))
            {
                List<FileSpecificInfo> files = GetFilesFromMenifest(arch, tfsObject);
                foreach (var f in files)
                {
                    string fileName = f.FileName.ToLowerInvariant();
                    if (f.PatchArchitecture == "amd64")
                    {
                        f.PatchArchitecture = "x64";
                    }
                    if (!String.IsNullOrEmpty(f.FileVersion) &&
                        f.PatchArchitecture.Equals(strArch, StringComparison.InvariantCultureIgnoreCase) &&
                        !fileName.Contains("resources.dll") &&
                        !fileName.Equals("WorkflowServiceHostPerformanceCounters.dll", StringComparison.InvariantCultureIgnoreCase) &&
                        !fileInfo.ContainsKey(fileName))
                    {
                        fileInfo.Add(fileName, f.FileVersion);
                    }
                }
            }
            else
            {
                foreach (var f in patch.Files)
                {
                    string fileName = f.FileName.ToLowerInvariant();
                    if (!String.IsNullOrEmpty(f.FileVersion) &&
                        f.PatchArchitecture.Equals(strArch, StringComparison.InvariantCultureIgnoreCase) &&
                        !fileName.Contains("resources.dll") &&
                        !fileName.Equals("WorkflowServiceHostPerformanceCounters.dll", StringComparison.InvariantCultureIgnoreCase) &&
                        !fileInfo.ContainsKey(fileName))
                    {
                        fileInfo.Add(fileName, f.FileVersion);
                    }
                }


            }

            return fileInfo;
        }

        public static List<FileSpecificInfo> GetFilesFromMenifest(int arch, WorkItemHelper tfsObject)
        {
            List<FileSpecificInfo> items = new List<FileSpecificInfo>();
           
            CBSManifestAnalyzer analyzer = new CBSManifestAnalyzer();
            string FileLocation = tfsObject.GetPatchFullPath((Architecture)arch).ToLowerInvariant();
            if (FileLocation.StartsWith(@"f:\packages")) {
                FileLocation = FileLocation.Replace(@"f:\packages", @"\\DOTNETPATCHTEST\Packages");
            }
            string extractLocation = Extraction.ExtractPatchToPath(FileLocation);
            analyzer.ExtractionPath = extractLocation;
            analyzer.RunAnalyze();
            foreach (CBSManifest manifest in analyzer.Assemblies)
            {
                if (!CBSContentHelper.IsDotNetManifest(manifest))
                    continue;

                if (manifest.Files != null && manifest.Files.Count > 0)
                {
                    foreach (FileItem item in manifest.Files)
                    {
                        FileSpecificInfo fileSpecificInfo = new FileSpecificInfo();
                        fileSpecificInfo.FileVersion = GetFileVersionString(Path.Combine(extractLocation, manifest.Name, item.Name));
                        fileSpecificInfo.PatchArchitecture= manifest.Identity.ProcessorAchitecture;
                        fileSpecificInfo.FileName = item.Name.ToLowerInvariant();
                        items.Add(fileSpecificInfo);
                    }
                }
        }
            return items;

        }

        public static string GetFileVersionString(string filePath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                if (!String.IsNullOrEmpty(versionInfo.ProductVersion))
                {
                    return String.Format("{0}.{1}.{2}.{3}", versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
                }
            }
            catch
            {
            }

            return String.Empty;
        }
    }
}
