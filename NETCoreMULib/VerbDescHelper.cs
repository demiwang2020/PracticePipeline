using Helper;
using NETCoreMURuntimeLib.Maddog;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMURuntimeLib
{
    class VerbDescHelper
    {
        public static char VEB_SEP_CHAR = '#';

        public static List<VerbDescriptor> ParseVerbsFromString(string vebString)
        {
            string[] splitVebString = vebString.Split(new char[] { VEB_SEP_CHAR });

            List<VerbDescriptor> vebObjs = new List<VerbDescriptor>();
            foreach (string singleVebStr in splitVebString)
            {
                vebObjs.Add(VerbDescriptor.ParseVerbFromString(singleVebStr));
            }

            return vebObjs;
        }

        #region Translate VerbDescriptor object to an MD package
        public static Maddog.MDPackage VerbObj2MDPackage(VerbDescriptor verbObj, PatchTestDataClassDataContext dbContext, string targetRelease)
        {
            switch (verbObj.Category)
            {
                case VerbCategory.NETCoreRelease:
                    return NETCoreReleaseVerb2MDPackage(verbObj, dbContext, targetRelease);

                case VerbCategory.CustomCommand:
                    return CommandVerb2MDPackage(verbObj);

                case VerbCategory.ImportRegistry:
                    throw new NotSupportedException("ImportRegistry verb is not implemented");

                case VerbCategory.Reboot:
                    return RebootVerb2MDPackage(verbObj);

                case VerbCategory.MDPackage:
                    return MDPackageVerb2MDPackage(verbObj);

                default:
                    throw new NotSupportedException();
            }
        }

        private static Maddog.MDPackage NETCoreReleaseVerb2MDPackage(VerbDescriptor verbObj, PatchTestDataClassDataContext dbContext, string targetRelease)
        {
            string release = verbObj.Tokens["Release"];
            string bundle = verbObj.Tokens["Bundle"];
            string action = verbObj.Tokens["Action"];
            string arch = verbObj.Tokens["Arch"];
            if (String.IsNullOrEmpty(release) ||
               String.IsNullOrEmpty(bundle) ||
               String.IsNullOrEmpty(action))
                throw new Exception("Invalid format of verb for .NET Core Bundle");

            if (action.Equals("VerifyInstall", StringComparison.OrdinalIgnoreCase) || 
                action.Equals("VerifyUninstall", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(action + " is not supported to translate to MD package");

            if (String.IsNullOrEmpty(arch))
                arch = "x86";

            release = ReplaceNETCoreToken(release, targetRelease);

            // query from DB for settings
            var record = dbContext.TNETCoreMU_Bundle.Where(p => p.Release.Equals(release) && p.ShortName.Equals(bundle)).Single();
            string packagePath = null;
            switch (arch.ToLower())
            {
                case "x86":
                    packagePath = record.InstallerNameX86;
                    break;

                case "x64":
                case "amd64":
                    packagePath = record.InstallerNameX64;
                    break;

                case "arm64":
                    packagePath = record.InstallerNameARM64;
                    break;

                default:
                    throw new NotSupportedException("Not supported arch " + arch);
            }

            packagePath = Path.Combine(record.InstallerPath, packagePath);

            string cmdline = String.Format("{0} {1} /quiet /norestart", packagePath, action.Equals("Install", StringComparison.OrdinalIgnoreCase) ? "/install" : "/uninstall");

            Maddog.MDPackage pkgRet = MDPackage.CreateCommandPackage(cmdline, true);
            pkgRet.Tokens.Add("SuccessfulExitCodes", "0;3010");

            return pkgRet;
        }

        private static Maddog.MDPackage CommandVerb2MDPackage(VerbDescriptor verbObj)
        {
            string args = verbObj.Tokens.ContainsKey("Args") ? verbObj.Tokens["Args"] : String.Empty;

            string cmdline = String.Format("{0} {1}", verbObj.Tokens["Command"], args);

            Maddog.MDPackage pkgRet = MDPackage.CreateCommandPackage(cmdline, true);

            if (verbObj.Tokens.ContainsKey("ExpectReturnCode"))
            {
                pkgRet.Tokens.Add("SuccessfulExitCodes", verbObj.Tokens["ExpectReturnCode"]);
            }

            return pkgRet;
        }

        private static Maddog.MDPackage RebootVerb2MDPackage(VerbDescriptor verbObj)
        {
            return MDPackage.CreateCommonPackage(MDPackage.ID_REBOOT);
        }

        private static Maddog.MDPackage MDPackageVerb2MDPackage(VerbDescriptor verbObj)
        {
            int packageId = Convert.ToInt32(verbObj.Tokens["MDPackage"]);

            Maddog.MDPackage pkgRet = MDPackage.CreateCommonPackage(packageId);

            foreach (var kv in verbObj.Tokens)
            {
                if (kv.Key == "MDPackage")
                    continue;

                pkgRet.Tokens.Add(kv.Key, kv.Value);
            }

            return pkgRet;
        }

        #endregion

        #region Translate VerbDescriptor object to input content of validation of .NET CORE
        public static void VerbObjects2NETCoreValidations(List<VerbDescriptor> verbObjects,
                                                    PatchTestDataClassDataContext dbContext,
                                                    string targetRelease,
                                                    out Dictionary<string, string> expectProducts,
                                                    out Dictionary<string, string> unexpectProducts)
        {
            expectProducts = new Dictionary<string, string>();
            unexpectProducts = new Dictionary<string, string>();

            foreach (var verbObj in verbObjects)
            {
                VerbObj2NETCoreValidation(verbObj, dbContext, targetRelease, ref expectProducts, ref unexpectProducts);
            }

            List<string> duplicated = new List<string>();
            foreach (var kv in unexpectProducts)
            {
                if (expectProducts.ContainsKey(kv.Key) && expectProducts[kv.Key] == kv.Value)
                    duplicated.Add(kv.Key);
            }
            foreach (var key in duplicated)
            {
                unexpectProducts.Remove(key);
            }
        }

        private static void VerbObj2NETCoreValidation(VerbDescriptor verbObj,
                                                    PatchTestDataClassDataContext dbContext,
                                                    string targetRelease,
                                                    ref Dictionary<string, string> expectProducts,
                                                    ref Dictionary<string, string> unexpectProducts)
        {
            if (verbObj.Category != VerbCategory.NETCoreRelease)
                throw new NotSupportedException(verbObj.Category.ToString() + "  is not supported to generate .NET Core validation");

            string release = verbObj.Tokens["Release"];
            string bundle = verbObj.Tokens["Bundle"];
            string action = verbObj.Tokens["Action"];
            string archStr = verbObj.Tokens["Arch"];
            if (archStr.ToLower() == "x64")
                archStr = "amd64";

            if (String.IsNullOrEmpty(release) ||
               String.IsNullOrEmpty(bundle) ||
               String.IsNullOrEmpty(action))
                throw new Exception("Invalid format of verb for .NET Core Bundle");

            if (action.Equals("Install", StringComparison.OrdinalIgnoreCase) ||
                action.Equals("Uninstall", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(action + " is not supported to generate .NET Core validation");

            Architecture arch = Architecture.X86;
            if (!Enum.TryParse<Architecture>(archStr, true, out arch))
                arch = Architecture.X86;

            var targetDict = action.Equals("VerifyUninstall", StringComparison.OrdinalIgnoreCase) ? unexpectProducts : expectProducts;

            // find out bundle(release) id
            release = ReplaceNETCoreToken(release, targetRelease);
            var bundleID = dbContext.TNETCoreMU_Bundle.Where(p => p.Release.Equals(release) && p.ShortName.Equals(bundle)).Single().ID;

            var msis = dbContext.TNETCoreMU_MSIs.Where(p => p.BundleID == bundleID && p.Arch == (int)arch);
            foreach (var msi in msis)
            {
                if(!targetDict.ContainsKey(msi.MSIName))
                    targetDict.Add(msi.MSIName, msi.ProductCode);
            }
        }
        #endregion

        private static string ReplaceNETCoreToken(string s, string targetRelease)
        {
            if(s.Contains("%TargetRelease%"))
                s = s.Replace("%TargetRelease%", targetRelease);

            return s;
        }
    }
}
