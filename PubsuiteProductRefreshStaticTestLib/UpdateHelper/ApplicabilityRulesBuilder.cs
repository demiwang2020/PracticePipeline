using Microsoft.Test.DevDiv.SAFX.CommonLibraries.MSIAnalyzerLib;
using PubsuiteProductRefreshStaticTestLib.DbClassContext;
using PubsuiteProductRefreshStaticTestLib.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper;
using RMIntegration.RMService;

namespace PubsuiteProductRefreshStaticTestLib.UpdateHelper
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
            foreach (var f in patch.Files)
            {
                string fileName = f.FileName.ToLowerInvariant();
                
                if (!String.IsNullOrEmpty(f.FileVersion) && 
                    f.PatchArchitecture.Equals(strArch, StringComparison.InvariantCultureIgnoreCase) &&
                    !fileName.Contains("resources.dll") && 
                    !fileInfo.ContainsKey(fileName))
                {
                    fileInfo.Add(fileName, f.FileVersion);
                }
            }

            return fileInfo;
        }
    }
}
