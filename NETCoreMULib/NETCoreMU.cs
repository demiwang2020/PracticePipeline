using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETCoreMURuntimeLib.Maddog;
using ScorpionDAL;
using Helper;
using System.Data.Linq;

namespace NETCoreMURuntimeLib
{
    public class NETCoreMU
    {
        /// <summary>
        /// Kick off runtime test for a given .NET Core MU
        /// </summary>
        /// <param name="updateInfo">Information of the update</param>
        /// <returns>A list of run ID</returns>
        public static List<int> KickoffRuntimeTest(UpdateInfo updateInfo)
        {
            var targetBundles = GetTargetBundle(updateInfo);
            var scenarios = FilterMatrix(updateInfo);
            // sort scenarios by filter (target product)
            scenarios.Sort(delegate (TNETCoreMU_Matrix p1, TNETCoreMU_Matrix p2)
            {
                return String.Compare(p1.TargetBundle, p2.TargetBundle, StringComparison.Ordinal);
            });

            List<int> runIds = new List<int>();
            foreach (var scenario in scenarios)
            {      
                    runIds.Add(KickoffRuntimeTest(updateInfo, scenario, targetBundles));
            }

            return runIds;
        }


        public static Dictionary<string, TNETCoreMU_Bundle> GetTargetBundle(UpdateInfo updateInfo)
        {
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                var bundles = dbContext.TNETCoreMU_Bundle.Where(p => p.Release.Equals(updateInfo.ReleaseName));
                var targetBundles = new Dictionary<string, TNETCoreMU_Bundle>();
                foreach (var bundle in bundles)
                {
                    targetBundles.Add(bundle.ShortName, bundle);
                }
                return targetBundles;
            }
        }


        public static List<TNETCoreMU_Matrix> FilterMatrix(UpdateInfo updateInfo)
        {
            
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                //1. Find all bundles in DB
                var targetBundles = GetTargetBundle(updateInfo);

                //2. Search from test matrix to get matched scenario
                bool isServerUpdate = HelperMethods.IsServerUpdate(updateInfo.Title);
                Architecture arch = HelperMethods.DetectUpdateTargetArch(updateInfo.Title);
                List<TNETCoreMU_Matrix> scenarios = new List<TNETCoreMU_Matrix>();
                foreach (var scenario in dbContext.TNETCoreMU_Matrixes)
                {
                    if (updateInfo.ReleaseName.Contains(scenario.ReleaseFilter) &&
                               scenario.Active &&
                               targetBundles.ContainsKey(scenario.TargetBundle) &&
                               scenario.IsServer == isServerUpdate &&
                               (Architecture)scenario.OSArch == arch)
                    {
                        scenarios.Add(scenario);
                    }
                }
                return scenarios;
            }
        }



    /// <summary>
    /// Kick off one run for a scenario
    /// </summary>
    /// <param name="updateInfo"></param>
    /// <param name="scenario"></param>
    /// <param name="targetBundles"></param>
    /// <returns>Run ID</returns>
    public static int KickoffRuntimeTest(UpdateInfo updateInfo, TNETCoreMU_Matrix scenario, Dictionary<string, TNETCoreMU_Bundle> targetBundles)
        {
            try
            {
                using (var dbContext = new PatchTestDataClassDataContext())
                {
                    //a. OS id, OS image id
                    var osImage = dbContext.TOSImages.Where(p => p.OSImageID == scenario.WUOSID).Single();

                    //b. case
                    var caseInfo = dbContext.TMDQueryMappings.Where(p => p.ID == scenario.CaseQueryMappingID).Single();

                    Dictionary<string, string> caseSpecificData = HelperMethods.ParseCaseSpecificData(scenario.CaseSpecificData);

                    //c. Prerequistes
                    List<VerbDescriptor> prereqVebs = null;
                    if (!String.IsNullOrEmpty(scenario.Prerequistes))
                    {
                        string prerequistes = HelperMethods.ReploaceCaseSpecificData(scenario.Prerequistes, caseSpecificData);

                        prereqVebs = VerbDescHelper.ParseVerbsFromString(prerequistes);
                    }

                    //d. Validations
                    List<VerbDescriptor> validationVebs = null;
                    if (!String.IsNullOrEmpty(scenario.Validation))
                    {
                        string validationStr = HelperMethods.ReploaceCaseSpecificData(scenario.Validation, caseSpecificData);

                        validationVebs = VerbDescHelper.ParseVerbsFromString(validationStr);
                    }

                    // Get all MD install steps of a run
                    List<Maddog.MDPackage> mdPackages = GenerateMDInstallMgrSteps(updateInfo, scenario, prereqVebs, validationVebs, targetBundles, dbContext);

                    // Build Run title
                    string title = String.Format(".NET Core MU-{0}-{1}-{2}-{3} {4}-{5}",
                                                updateInfo.ReleaseName, targetBundles[scenario.TargetBundle].Name,
                                                caseInfo.TestcaseDesc, osImage.TO.OSName, (Architecture)scenario.OSArch,
                                                scenario.Comment);

                    MaddogHelper mdHelper = new MaddogHelper();
                    mdHelper.KickoffMaddogRun(mdPackages,
                                              title,
                                              osImage.MDOSID, osImage.TO,
                                              (int)osImage.MaddogOSImageID,
                                              caseInfo.QueryID, (Architecture)scenario.OSArch);

                    return mdHelper.RunID;
                }
            }
            catch (Exception ex)
            {
                return -1;
            }
        }


        #region Installation Steps of a run
        private static List<Maddog.MDPackage> GenerateMDInstallMgrSteps(UpdateInfo updateInfo,
                                                                        TNETCoreMU_Matrix scenario,
                                                                        List<VerbDescriptor> prereqVebs,
                                                                        List<VerbDescriptor> validationVebs,
                                                                        Dictionary<string, TNETCoreMU_Bundle> targetBundles,
                                                                        PatchTestDataClassDataContext dbContext)
        {
            List<Maddog.MDPackage> installSteps = new List<Maddog.MDPackage>();

            installSteps.Add(MDPackage.CreateEnvironmentVariablePackage("RunID", "[RunID]"));
            installSteps.Add(MDPackage.CreateCommandPackage(ConfigurationManager.AppSettings["NugetConfigPath"]));
            installSteps.Add(MDPackage.CreateCommandPackage(ConfigurationManager.AppSettings["NugetPackagePath"]));
            installSteps.Add(MDPackage.CreateCommandPackage(ConfigurationManager.AppSettings["EnableWUService"]));
            installSteps.Add(MDPackage.CreateCommandPackage(ConfigurationManager.AppSettings["DisableWarning"]));
            installSteps.Add(MDPackage.CreateImportRegFilePackage(ConfigurationManager.AppSettings["DisablePerSessionTempDir"], true));
            installSteps.Add(MDPackage.CreateCommandPackage(ConfigurationManager.AppSettings["NETCoreMU_CopyFiles"]));
            installSteps.Add(MDPackage.CreateImportRegFilePackage(ConfigurationManager.AppSettings["OnlyUseLatestCLR"], true));

            // Test parameter file
            string parasFilePath = GenerateTestParameterFile(updateInfo, scenario, validationVebs, targetBundles, dbContext);
            installSteps.Add(MDPackage.CreateEnvironmentVariablePackage("ParameterFile", parasFilePath));

            // Prerequistes of the scenario, which usually is an old .NET core or VS
            foreach (var verbObj in prereqVebs)
            {
                string value;
                installSteps.Add(VerbDescHelper.VerbObj2MDPackage(verbObj, dbContext, updateInfo.ReleaseName));
               if(verbObj.Tokens.TryGetValue("Command",out value))
                {
                    if(value.Contains("BlockMU"))
                        installSteps.Add(MDPackage.CreateEnvironmentVariablePackage("HaveBlockKey", "True"));
                }
            }

            return installSteps;
        }
        #endregion

        #region Parameter File of a run
        private static string GenerateTestParameterFile(UpdateInfo updateInfo,
                                                        TNETCoreMU_Matrix scenario,
                                                        List<VerbDescriptor> validationVebs,
                                                        Dictionary<string, TNETCoreMU_Bundle> targetBundles,
                                                        PatchTestDataClassDataContext dbContext)
        {
            // Create a folder to store parameter files
            // Folder structure is: Release\GUID\BUNDLE\index\
            string wuParameterRootPath = ConfigurationManager.AppSettings["NETCoreMUParameterRoot"];
            string destPath = Path.Combine(wuParameterRootPath, updateInfo.ReleaseName, updateInfo.BundleGUID, scenario.TargetBundle);

            // index, to avoid overwriting parameter file
            int index = 0;
            if (Directory.Exists(destPath))
                index = Directory.GetDirectories(destPath).Length;
            destPath = Path.Combine(destPath, index.ToString());
            Directory.CreateDirectory(destPath);

            // Generate install verification file
            Dictionary<string, string> expectProducts, unexpectProducts;
            VerbDescHelper.VerbObjects2NETCoreValidations(validationVebs, dbContext, updateInfo.ReleaseName, out expectProducts, out unexpectProducts);
            string installVerifyFilePath = GenerateInstallVerificationFile(destPath, expectProducts, unexpectProducts);

            // Generate uninstall scripts
            string uninstallScript = GenerateUninstallScript(Path.GetDirectoryName(destPath), scenario.TargetBundle, (Architecture)scenario.OSArch, targetBundles);

            string parameterFilePath = Path.Combine(destPath, "Parameters.txt");
            using (StreamWriter writer = new StreamWriter(parameterFilePath))
            {
                writer.WriteLine("Title={0}", updateInfo.Title);
                writer.WriteLine("BundleGUID={0}", updateInfo.BundleGUID);
                writer.WriteLine("InstallVerificationPath=" + ConfigurationManager.AppSettings["NETCoreMU_VVPath"]);
                writer.WriteLine("InstallVerificationParas={0}", installVerifyFilePath);
                writer.WriteLine("UninstallScript={0}", uninstallScript);
                writer.WriteLine("BrowseOnly=False");
                writer.WriteLine("AutoSelectOnWebSites=False");
            }

            return parameterFilePath;
        }

        private static string GenerateInstallVerificationFile(string destPath, Dictionary<string, string> expectProducts, Dictionary<string, string> unexpectProducts)
        {
            string installVerifyFilePath = System.IO.Path.Combine(destPath, "InstallVerification.txt");

            using (StreamWriter writer = new StreamWriter(installVerifyFilePath))
            {
                if (expectProducts.Count > 0)
                {
                    writer.WriteLine("[Expect]");
                    foreach (var prod in expectProducts)
                    {
                        writer.WriteLine("{0}={1}", prod.Key, prod.Value);
                    }
                }

                if (unexpectProducts.Count > 0)
                {
                    writer.WriteLine("[Unexpect]");
                    foreach (var prod in unexpectProducts)
                    {
                        writer.WriteLine("{0}={1}", prod.Key, prod.Value);
                    }
                }
            }

            return installVerifyFilePath;
        }

        private static string GenerateUninstallScript(string destPath, string bundleShortName, Architecture arch, Dictionary<string, TNETCoreMU_Bundle> targetBundles)
        {
            string scriptPath = Path.Combine(destPath, "uninstall.bat");

            // if the script already exists, no need to create it again
            if (File.Exists(scriptPath))
                return scriptPath;

            List<string> bundlePath = new List<string>();
            switch (arch)
            {
                case Architecture.X86:
                    bundlePath.Add(Path.Combine(targetBundles[bundleShortName].InstallerPath, targetBundles[bundleShortName].InstallerNameX86));
                    if (bundleShortName == "Hosting")
                    {
                        bundlePath.Add(Path.Combine(targetBundles["NETCoreRuntime"].InstallerPath, targetBundles["NETCoreRuntime"].InstallerNameX86));
                    }
                    break;

                case Architecture.AMD64:
                    bundlePath.Add(Path.Combine(targetBundles[bundleShortName].InstallerPath, targetBundles[bundleShortName].InstallerNameX86));
                    bundlePath.Add(Path.Combine(targetBundles[bundleShortName].InstallerPath, targetBundles[bundleShortName].InstallerNameX64));
                    if (bundleShortName == "Hosting")
                    {
                        bundlePath.Add(Path.Combine(targetBundles["NETCoreRuntime"].InstallerPath, targetBundles["NETCoreRuntime"].InstallerNameX86));
                        bundlePath.Add(Path.Combine(targetBundles["NETCoreRuntime"].InstallerPath, targetBundles["NETCoreRuntime"].InstallerNameX64));
                    }
                    break;

                case Architecture.ARM64:
                    bundlePath.Add(Path.Combine(targetBundles[bundleShortName].InstallerPath, targetBundles[bundleShortName].InstallerNameARM64));
                    break;

                default:
                    throw new NotSupportedException("Architecture " + arch.ToString() + " is not supported in .NET Core MU");
            }

            using (StreamWriter writer = new StreamWriter(scriptPath))
            {
                foreach (var bundle in bundlePath)
                    writer.WriteLine("call {0} /uninstall /quiet /norestart", bundle);

                writer.WriteLine();

                writer.Write("exit /B 3010");
            }

            return scriptPath;
        }

        #endregion

    }
}
