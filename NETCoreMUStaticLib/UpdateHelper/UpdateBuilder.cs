using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Helper;
using NETCoreMUStaticLib.DbClassContext;
using System.Linq.Expressions;
using NETCoreMUStaticLib.Model;
using System.Data;

namespace NETCoreMUStaticLib.UpdateHelper
{
    class UpdateBuilder
    {
        private static string _pubNodeWithNameSpaces;
        static UpdateBuilder()
        {
            using (var dbContext = new NetCoreWUSAFXDbContext())
            {
                string xmlNameSpace = dbContext.TNETCoreConfigMappings.Where(p => p.PropertyName == "PublishXmlNameSpaces").Single().Value;

                _pubNodeWithNameSpaces = "<pub:Update " + xmlNameSpace + ">";
            }
        }

        public static Update BuildUpdateFromPublishingXml(string xmlContent)
        {
            List<string> contents = new List<string>();

            string startNode = "<pub:Update>";
            string endNode = "</pub:Update>";
            int startIndex = 0;
            int endIndex = 0;

            while (endIndex >= 0 && (startIndex = xmlContent.IndexOf(startNode, endIndex)) > 0)
            {
                endIndex = xmlContent.IndexOf(endNode, startIndex);

                contents.Add(xmlContent.Substring(startIndex, endIndex - startIndex + endNode.Length));
            }

            //Create parent update
            Update parentUpdate = new Update(contents.Last().Replace("<pub:Update>", _pubNodeWithNameSpaces), true, true);

            //Create child updates and add them to parent update
            for (int i = 0; i < contents.Count - 1; ++i)
            {
                string xml = contents[i].Replace("<pub:Update>", _pubNodeWithNameSpaces);

                Update childUpdate = new Update(xml, false, true);

                parentUpdate.AddChildUpdate(childUpdate);
            }

            return parentUpdate;
        }

        public static Update BuildUpdateFromInputData(InnerData innerData)
        {
            return BuildParentUpdate(innerData);
        }

        private static Update BuildParentUpdate(InnerData innerData)
        {
            var db = new NetCoreWUSAFXDbContext();
            var dbDtg = new DtgpatchtestContext();

            try
            {
                StringBuilder sb = new StringBuilder();

                //Generate expected parent publishing xml
                sb.Append(_pubNodeWithNameSpaces);

                sb.AppendFormat("<pub:UpdateIdentity UpdateID=\"{0}\"/>", innerData.UpdateID);
                sb.Append("<pub:Properties />");

                string kbArticleID = innerData.Parameters.ContainsKey("KBArticleID") ?
                    innerData.Parameters["KBArticleID"] : 
                    db.TNETCoreConfigMappings.Where(p => p.PropertyName == "KBArticleID" && p.MajorRelease == innerData.MajorRelease).Single().Value;

                UpdateProperty updateProperties = new UpdateProperty();
                updateProperties.KBArticle = kbArticleID;
                UpdateBuilder.BuildUpdateProperties(innerData, updateProperties);

                string supportUrl = db.TNETCoreConfigMappings.Where(p => p.PropertyName == "SupportUrl" && p.MajorRelease == innerData.MajorRelease).Single().Value;
                string moreInfoUrl = db.TNETCoreConfigMappings.Where(p => p.PropertyName == "MoreInfoUrl" && p.MajorRelease == innerData.MajorRelease).Single().Value;

                string dbSettings = db.TNETCoreConfigMappings.Where(p => p.PropertyName == "LocalizedPropertiesCollection").Single().Value;
                string localizedProps = dbSettings.Replace("[#Title]", innerData.Title)
                                                  .Replace("[#Description]", innerData.Title)
                                                  .Replace("[#SupportUrl]", supportUrl)
                                                  .Replace("[#MoreInfoUrl]", moreInfoUrl);

                sb.Append(localizedProps);

                sb.Append("<pub:Relationships>");

                //Categories
                string categories = db.TNETCoreConfigMappings.Where(p => p.PropertyName == "Categories" && p.MajorRelease == innerData.MajorRelease).Single().Value;
                sb.Append(categories);

                //Prerequistes
                string prerequistes = db.TNETCoreConfigMappings.Where(p => p.PropertyName == "Prerequisites" &&
                                                             p.MajorRelease == innerData.MajorRelease &&
                                                             p.Arch == (int)innerData.Arch &&
                                                             p.IsServer == innerData.IsServerBundle).Single().Value;

                if (innerData.IsServerBundle && innerData.IsAUBundle)
                {
                    prerequistes = GenerateAUPrerequistesFromMU(prerequistes, innerData);
                }

                sb.Append(prerequistes);

                sb.Append("</pub:Relationships>");

                sb.Append("</pub:Update>");

                Update parentUpdate = new Update(sb.ToString(), true, false);
                parentUpdate.Properties = updateProperties;

                // Generate Child bundles
                var bundles = dbDtg.BundleInfos.Where(p => p.Release == innerData.ReleaseNumber).ToList();
                BuildChildBundles(parentUpdate, db, dbDtg, bundles, innerData.Arch, categories, prerequistes, innerData);

                return parentUpdate;
            }
            catch (Exception ex)
            {
                  throw;
            }
            finally
            {
                db.Dispose();
                dbDtg.Dispose();
            }
        }

        private static void BuildChildBundles(Update parentUpdate,
                                        NetCoreWUSAFXDbContext dbSafx,
                                        DtgpatchtestContext dbDtg,
                                        IEnumerable<TNETCoreMUBundle> bundles,
                                        Architecture arch,
                                        string categories,
                                        string prerequistes,
                                        InnerData data)
        {
            string rele = string.Empty;
            int num = 0;
            foreach (var bundle in bundles)
            {
                rele = bundle.Release.ToString();
                num = Convert.ToInt32(rele.Substring(0, 1));
                // Exclude bundles that are not supported on ARM64 platform
                if (arch == Architecture.ARM64 && String.IsNullOrEmpty(bundle.InstallerNameARM64))
                    continue;

                if (arch == Architecture.ARM64 && num < 8 && bundle.ShortName == "ASPNETCoreRuntime")
                    continue;

                Update childUpdate = BuildChildBundle(dbSafx, dbDtg, bundle, arch, categories, prerequistes, data);
                parentUpdate.AddChildUpdate(childUpdate);

                // create x86 bundle too
                if (arch == Architecture.AMD64 && bundle.Name != "Hosting")
                {
                    childUpdate = BuildChildBundle(dbSafx, dbDtg, bundle, Architecture.X86, categories, prerequistes, data);
                    parentUpdate.AddChildUpdate(childUpdate);
                }
            }
        }

        //private static void BuildChildBundles(Update parentUpdate,
        //                                       NetCoreWUSAFXDbContext dbSafx,
        //                                       DtgpatchtestContext dbDtg,
        //                                       IEnumerable<TNETCoreMUBundle> bundles,
        //                                       Architecture arch,
        //                                       string categories,
        //                                       string prerequistes,
        //                                       InnerData data)
        //{
        //    int prevReleaseMaxID = bundles.Min(p => p.ID);
        //    string rele = string.Empty;
        //    int num = 0;
        //    foreach (var bundle in bundles)
        //    {
        //        if(bundle.ShortName== "ASPNETCoreRuntime")
        //        {
        //            int ww = 0;
        //        }
        //        rele = bundle.Release.ToString();
        //        num = Convert.ToInt32(rele.Substring(0, 1));
        //        // Exclude bundles that are not supported on ARM64 platform
        //        if (arch == Architecture.ARM64 && 
        //            (String.IsNullOrEmpty(bundle.InstallerNameARM64) || bundle.ShortName == "DesktopRuntime" ))
        //            continue;

        //        if (arch == Architecture.ARM64 && num < 8 && bundle.ShortName == "ASPNETCoreRuntime")
        //            continue;

        //        Update childUpdate = BuildChildBundle(dbSafx, dbDtg, bundle, arch, prevReleaseMaxID, categories, prerequistes, data);
        //        parentUpdate.AddChildUpdate(childUpdate);

        //        // create x86 bundle too
        //        if (arch == Architecture.AMD64 && bundle.Name != "Hosting")
        //        {
        //            childUpdate = BuildChildBundle(dbSafx, dbDtg, bundle, Architecture.X86, prevReleaseMaxID, categories, prerequistes, data);
        //            parentUpdate.AddChildUpdate(childUpdate);
        //        }
        //    }
        //}

        private static Update BuildChildBundle(NetCoreWUSAFXDbContext dbSafx,
                                               DtgpatchtestContext dbDtg,
                                               TNETCoreMUBundle bundle,
                                               Architecture arch,
                                               ///int prevReleaseMaxID,
                                               string categories,
                                               string prerequistes,
                                               InnerData data)
        {
            StringBuilder sb = new StringBuilder();

            // Generate expected parent publishing xml
            sb.Append(_pubNodeWithNameSpaces);

            sb.Append("<pub:UpdateIdentity />");
            sb.Append("<pub:Properties />");

            // Build title
            string title = BuildChildBundleTitle(bundle, data, arch);
            string localzedCollection = dbSafx.TNETCoreConfigMappings.Where(p => p.PropertyName == "ChildLocalizedPropertiesCollection").Single().Value;
            localzedCollection = localzedCollection.Replace("[#Title]", title);
            sb.Append(localzedCollection);

            // Relationship
            sb.Append("<pub:Relationships>");

            sb.Append(categories);
            sb.Append(prerequistes);

            sb.Append("</pub:Relationships>");

            // Applicability
            //BuildChildBundleApplicability(dbDtg, bundle, arch, prevReleaseMaxID, data, sb);
            BuildChildBundleApplicability(dbDtg, bundle, arch, data, sb);

            // Files
            string installerName = null;
            switch (arch)
            {
                case Architecture.X86:
                    installerName = bundle.InstallerNameX86;
                    break;

                case Architecture.AMD64:
                    installerName = bundle.InstallerNameX64;
                    break;

                case Architecture.ARM64:
                    installerName = bundle.InstallerNameARM64;
                    break;
            }
            sb.AppendFormat("<pub:Files><pub:File FileName=\"{0}\" FileLocation=\"{1}\"/></pub:Files>",
                            installerName,
                            Path.Combine(bundle.InstallerPath, installerName));

            //HandlerSpecificData
            sb.AppendFormat("<pub:HandlerSpecificData xsi:type=\"cmd:CommandLineInstallation\"><cmd:InstallCommand DefaultResult=\"Failed\" RebootByDefault=\"false\" Program=\"{0}\" Arguments=\"/quiet /norestart\">" +
                            "<cmd:ReturnCode Code=\"0\" Result=\"Succeeded\" Reboot=\"false\"/>" +
                            "<cmd:ReturnCode Code=\"3010\" Result=\"Succeeded\" Reboot=\"true\"/>" +
                            "<cmd:ReturnCode Code=\"1641\" Result=\"Succeeded\" Reboot=\"true\"/>" +
                            "</cmd:InstallCommand></pub:HandlerSpecificData>", installerName);

            sb.Append("</pub:Update>");

            // create child update object
            Update childUpdate = new Update(sb.ToString(), false, false);

            return childUpdate;
        }

        private static string BuildChildBundleTitle(TNETCoreMUBundle bundle, InnerData data, Architecture arch)
        {
            string bundleName = null;
            string version = data.ReleaseNumber;
            if (bundle.ShortName.StartsWith("SDK"))
            {
                bundleName = "SDK";
                version = bundle.Name.Split(new char[] { ' ' })[1];
            }
            else
            {
                switch (bundle.ShortName)
                {
                    case "NETCoreRuntime":
                        bundleName = "Runtime";
                        break;

                    case "DesktopRuntime":
                        bundleName = "Desktop Runtime";
                        break;

                    case "ASPNETCoreRuntime":
                        bundleName = "ASP.NET Core";
                        break;

                    case "Hosting":
                        bundleName = "Hosting";
                        break;
                }
            }

            string netcoreName = version[0] < '5' ? ".NET Core" : ".NET";

            string title = String.Format("{0} {1} {2} {3} {4} for {5} {6}",
                                         data.ReleaseDate,
                                         netcoreName,
                                         version,
                                         bundleName,
                                         data.IsSecurityRelease ? "Security Update" : "Update",
                                         CommonHelper.Arch2String(arch).ToLower(),
                                         data.IsServerBundle ? "Server" : "Client");

            string release = bundle.Release.Substring(0, 3);
            if (bundleName == "Hosting")
                if (release == "6.0")
                {
                    if (arch == Architecture.X86)
                        title = title.Replace(" x86 ", " x86_x64 ");
                    else if (arch == Architecture.AMD64)
                        title = title.Replace(" x64 ", " x86_x64 ");
                }
                else
                {
                    if (arch == Architecture.X86)
                        title = title.Replace(" x86 ", " x86_x64_arm64 ");
                    else if (arch == Architecture.AMD64)
                        title = title.Replace(" x64 ", " x86_x64_arm64 ");
                    else if (arch == Architecture.ARM64)
                        title = title.Replace(" arm64 ", " x86_x64_arm64 ");
                }

            return title;
        }
   
        private static string GetBundleCodeFromBundleRecord(TNETCoreMUBundle bundle, Architecture arch)
        {
            switch (arch)
            {
                case Architecture.X86:
                    return bundle.BundleCodeX86;

                case Architecture.AMD64:
                    return bundle.BundleCodeX64;

                case Architecture.ARM64:
                    return bundle.BundleCodeARM64;

                default:
                    throw new NotSupportedException("Not supported arch code " + arch);
            }
        }

        private static void BuildChildBundleApplicability(DtgpatchtestContext dbDtg,
                                                  TNETCoreMUBundle bundle,
                                                  Architecture arch,
                                                  InnerData data,
                                                  StringBuilder sb)
        {
            var currentBundleCode = GetBundleCodeFromBundleRecord(bundle, arch);

            var previousBundles = dbDtg.BundleInfos
                .Where(bdl => bdl.ShortName == bundle.ShortName && bdl.Release.StartsWith(data.MajorRelease))
                .AsEnumerable()
                .Where(bdl =>
                {
                    var releaseParts = bdl.Release.Split('.');
                    var dataReleaseParts = data.ReleaseNumber.Split('.');
                    return !releaseParts.SequenceEqual(dataReleaseParts) &&
                            releaseParts.Length >= 3 && releaseParts.Length <= 4 && dataReleaseParts.Length >= 3 && dataReleaseParts.Length <= 4 &&
                            string.Compare(releaseParts[2], dataReleaseParts[2]) <= 0 &&
                            !(releaseParts.Length == 4 && dataReleaseParts.Length == 3 && string.Compare(releaseParts[2], dataReleaseParts[2]) == 0);
                })
                .ToList();

            sb.Append("<pub:ApplicabilityRules>");

            // Is-Installed rules
            sb.Append("<pub:IsInstalled>");
            sb.Append("<lar:And>");
            AppendBundleCodeDetection(sb, currentBundleCode);
            sb.Append("</lar:And>");
            sb.Append("</pub:IsInstalled>");

            // Is-Installable rules
            sb.Append("<pub:IsInstallable>");
            sb.Append("<lar:And>");

            sb.Append("<lar:Or>");

            foreach (var bc in previousBundles)
            {
                string rele = bc.Release.ToString();
                int num = Convert.ToInt32(rele.Substring(4, 1));
                int numT = Convert.ToInt32(rele.Substring(0, 1));
                if (arch == Architecture.ARM64 && numT == 8 && num < 6 && bundle.ShortName == "ASPNETCoreRuntime")
                {
                    continue;
                }
                AppendBundleCodeDetection(sb, GetBundleCodeFromBundleRecord(bc, arch));
            }

            sb.Append("</lar:Or>");
            sb.Append("</lar:And>");
            sb.Append("</pub:IsInstallable>");

            sb.Append("</pub:ApplicabilityRules>");

        }

        private static void AppendBundleCodeDetection(StringBuilder sb, string productCode)
        {
            sb.AppendFormat("<bar:RegKeyExists Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{0}\" RegType32=\"true\"/>", productCode);
        }

        private static void BuildUpdateProperties(InnerData data, UpdateProperty updateProperties)
        {
            if (data.IsSecurityRelease)
            {
                updateProperties.AU = true;
                updateProperties.Site = true;
                updateProperties.SUS = true;
                updateProperties.Catalog = true;
                updateProperties.AutoSelectOnWebSites = true;
                updateProperties.BrowseOnly = false;
                updateProperties.MsrcSeverity = "Important";
                updateProperties.UpdateClassification = "0FA1201D-4330-4FA8-8AE9-B877473B6441";
            }
            else
            {
                updateProperties.AU = true;
                updateProperties.Site = true;
                updateProperties.SUS = true;
                updateProperties.Catalog = true;
                updateProperties.AutoSelectOnWebSites = true;
                updateProperties.BrowseOnly = false;
                updateProperties.MsrcSeverity = String.Empty;
                updateProperties.UpdateClassification = "E6CF1350-C01B-414D-A61F-263D14D133B4";
            }

            if (data.IsServerBundle)
            {
                if (data.IsAUBundle)
                {
                    updateProperties.AU = true;
                    updateProperties.Site = true;
                    updateProperties.SUS = false;
                    updateProperties.Catalog = false;
                }
                else
                {
                    updateProperties.AU = false;
                    updateProperties.Site = false;
                    updateProperties.AutoSelectOnWebSites = false;
                }
            }

            using (var dbContext = new NetCoreWUSAFXDbContext())
            {
                string cveIDs = data.Parameters.ContainsKey("CveID") ? 
                    data.Parameters["CveID"] : 
                    dbContext.TNETCoreConfigMappings.Where(p => p.PropertyName == "CveID").Single().Value;

                updateProperties.CveIDs = cveIDs;
            }
        }

        private static string GenerateAUPrerequistesFromMU(string muPrerequistes, InnerData data)
        {
            using (var dbContext = new NetCoreWUSAFXDbContext())
            {
                string audetectoid = dbContext.TNETCoreConfigMappings.Where(p => p.PropertyName == "AUDetectoid" && p.MajorRelease == data.MajorRelease).First().Value;
                string mudetectoid = dbContext.TNETCoreConfigMappings.Where(p => p.PropertyName == "MUDetectoid" && p.MajorRelease == data.MajorRelease).First().Value;

                return muPrerequistes.Replace(mudetectoid, audetectoid);
            }
        }
    }
}
