using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubsuiteStaticTestLib.Model;
using PubsuiteStaticTestLib.DbClassContext;
using Helper;
using System.Configuration;
using PubUtilManager;
using KeyVaultManagementLib;
using System.Text.RegularExpressions;

namespace PubsuiteStaticTestLib.UpdateHelper
{

    public class UpdateBuilder
    {
        public static Update QueryUpdateFromGUID(string guid)
        {
            //string clientId = ConfigurationManager.AppSettings["ManagedIdentityClientId"];
            string user = ConfigurationManager.AppSettings["ServiceAccountName2"];
            string password = KeyVaultAccess.GetGoFXKVSecret("VsulabServiceAccountPassword", GetManagedId());
            //string password = "6xQ63d5Q3n@Y#pnf9ktf";
            string endpoint = ConfigurationManager.AppSettings["PubsuiteName"];
            PubUtilClient client = new PubUtilClient(user, password, endpoint);
            string xmlPath = @"C:\Users\v-wxiaox\Desktop\0b4287c1-2b3a-4456-bae6-2cc906f47a59.out.xml";
            //string xmlPath= client.GetPublishingXML(guid, true);
            string xmlContent = String.Empty;
            if(xmlPath != null)
            {
                using (StreamReader sr = new StreamReader(xmlPath))
                {
                    xmlContent = sr.ReadToEnd();
                }
            }
            else
            {
                xmlPath = @"C:\Users\v-wxiaox\Desktop\0b4287c1-2b3a-4456-bae6-2cc906f47a59.out.xml";
                //xmlPath = client.GetPublishingXML(guid, true);
                using (StreamReader sr = new StreamReader(xmlPath))
                {
                    xmlContent = sr.ReadToEnd();
                }
            }

                return BuildUpdateFromPublishingXml(xmlContent);
        }

        public static string GetManagedId()
        {
            if (Regex.Match(Environment.GetEnvironmentVariable("COMPUTERNAME"), "DotNetPatchTest", RegexOptions.IgnoreCase).Success)
            {
                return ConfigurationManager.AppSettings["gofxservinfra01ManagedId"];
            }
            return string.Empty;
        }
        public static Update BuildUpdateFromPublishingXml(string xmlContent)
        {
            List<string> contents = new List<string>();

            string startNode = "<pub:Update xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\">";
            string endNode = "</pub:Update>";
            int startIndex = 0;
            int endIndex = 0;

            while (endIndex >= 0 && (startIndex = xmlContent.IndexOf(startNode, endIndex)) > 0)
            {
                endIndex = xmlContent.IndexOf(endNode, startIndex);

                contents.Add(xmlContent.Substring(startIndex, endIndex - startIndex + endNode.Length));
            }

            //Create parent update
            Update parentUpdate = new Update(contents.Last(), true, true);

            //Create child updates and add them to parent update
            for (int i = 0; i < contents.Count - 1; ++i)
            {
                Update childUpdate = new Update(contents[i], false, true);

                parentUpdate.AddChildUpdate(childUpdate);
            }

            return parentUpdate;
        }



        public static Update BuildExpectedUpdateFromInputData(InputData inputData)
        {
            using (var db = new WUSAFXDbContext())
            {
                //Get some basic info

                string os = OSDetector.ParseTargetOSFromUpdateTitle(inputData.Title);
                int osid = db.TOperatingSystems.Where(p => p.Name.Equals(os)).First().ID;

                int arch = ArchDetector.ParseArchFromUpdateTitle(inputData.Title);
                int releaseType = GetUpdateType(inputData);

                Update parentUpdate = BuildParentUpdate(inputData, osid, arch, releaseType);

                foreach (int tfsid in inputData.TFSIDs)
                {
                    Update childUpdate = BuildChildUpdate(inputData, osid, arch, releaseType, tfsid);
                    parentUpdate.AddChildUpdate(childUpdate);
                }

                List<Update> additionalChildUpdates = BuildFixedChildUpdate(osid, arch, releaseType, parentUpdate);
                if (additionalChildUpdates != null && additionalChildUpdates.Count > 0)
                {
                    foreach (Update fixedUpdate in additionalChildUpdates)
                        parentUpdate.AddChildUpdate(fixedUpdate);
                }

                return parentUpdate;
            }
        }

        private static Update BuildParentUpdate(InputData inputData, int osid, int arch, int releaseType)
        {
            using (var db = new WUSAFXDbContext())
            {
                UpdateProperty updateProperties = new UpdateProperty();
                BuildUpdateProperties(inputData, osid, arch, releaseType, updateProperties);

                StringBuilder sb = new StringBuilder();

                //Generate expected parent publishing xml
                sb.Append("<pub:Update xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\">");

                sb.AppendFormat("<pub:UpdateIdentity UpdateID=\"{0}\"/>", inputData.UpdateID);
                sb.Append("<pub:Properties />");

                //Title (in LocalizedPropertiesCollection)
                string updateDescription = String.Empty;
                if (ReleaseTypeDetector.IsSecurityRelease(releaseType))
                {
                    updateDescription = db.TPropertyMappings.Where(p => p.Name == "SecurityDescription").Single().MappedContent;
                }
                else
                {
                    updateDescription = db.TPropertyMappings.Where(p => p.Name == "Non-SecDescription").Single().MappedContent;
                }
                string dbSettings = db.TPropertyMappings.Where(p => p.Name == "LocalizedPropertiesCollection").Single().MappedContent;
                string localizedProps = dbSettings.Replace("[#Title]", inputData.Title)
                                                  .Replace("[#Description]", updateDescription)
                                                  .Replace("[#KBArticle]", inputData.KB);
                sb.Append(localizedProps);

                sb.Append("<pub:Relationships>");

                //Categories
                dbSettings = db.TCategoriesMappings.Where(p => p.OS == osid && p.CPU == arch).Single().Categories;
                sb.Append(dbSettings);

                //Prerequistes
                int parentSKUID = db.TNetSkus.Where(p => p.SKU == "Parent").Single().ID;
                dbSettings = db.TPrerequisitesMappings.Where(p => p.OS == osid && p.CPU == arch && p.SKU == parentSKUID).Single().Prerequisites;
                // Process prerequistes (some updates may have different settings from DB)
                dbSettings = ProcessPrerequistes(dbSettings, releaseType, osid, inputData,parentSKUID);

                sb.Append(dbSettings);

                sb.Append("</pub:Relationships>");

                sb.Append("</pub:Update>");

                Update parentUpdate = new Update(sb.ToString(), true, false);
                parentUpdate.Properties = updateProperties;

                return parentUpdate;
            }
        }

        private static Update BuildChildUpdate(InputData inputData, int osid, int arch, int releaseType, int tfsid)
        {
            using (var db = new WUSAFXDbContext())
            {
                StringBuilder sb = new StringBuilder();

                //Generate expected parent publishing xml
                sb.Append("<pub:Update xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\">");

                sb.Append("<pub:UpdateIdentity />");
                sb.Append("<pub:Properties />");

                //sb.Append("<pub:LocalizedPropertiesCollection />");
                WorkItemHelper tfsObject = new WorkItemHelper(TFSHelper.TFSURI, tfsid);
                string title = GetChildTitle(tfsObject.GetPatchName((Architecture)arch));
                string dbSettings = db.TPropertyMappings.Where(p => p.Name == "LocalizedPropertiesCollection").Single().MappedContent;
                string localizedProps = dbSettings.Replace("[#Title]", title)
                                                  .Replace("[#Description]", String.Empty)
                                                  .Replace("[#KBArticle]", String.Empty);
                sb.Append(localizedProps);

                sb.Append("<pub:Relationships>");

                //Categories
                dbSettings = db.TCategoriesMappings.Where(p => p.OS == osid && p.CPU == arch).Single().Categories;
                sb.Append(dbSettings);

                //Prerequistes
                int sku = db.TNetSkus.Where(p => p.SKU == tfsObject.SKU).Single().ID;
                var queryResult = db.TPrerequisitesMappings.Where(p => p.OS == osid && p.CPU == arch && p.SKU == sku).SingleOrDefault();
                if (queryResult != null)
                    dbSettings = queryResult.Prerequisites;
                else
                {
                    int parentSKUID = db.TNetSkus.Where(p => p.SKU == "Parent").Single().ID;
                    dbSettings = db.TPrerequisitesMappings.Where(p => p.OS == osid && p.CPU == arch && p.SKU == parentSKUID).Single().Prerequisites;
                }

                List<int> Os = new List<int>() { 1033, 1039, 1042 };
                List<string> Month = new List<string>() { "01", "04", "07", "10" };

                if (Os.Contains(osid))
                {
                    if (!(Month.Contains(inputData.Title.Substring(5, 2)) || inputData.ShipChannels.Contains("SiteAUSUSCatalog")) && !inputData.IsCatalogOnly)
                    {
                        dbSettings = dbSettings.Replace("</pub:Prerequisites>", "<pub:UpdateIdentity UpdateID=\"04259680-2dca-4c9d-b2e0-5d3e2d37e7c5\" /></pub:Prerequisites>");
                    }
                }
                //if (osid == 1033 || osid == 1039)
                //{
                //    if (inputData.IsCatalogOnly)
                //    {
                //        if (dbSettings.Contains("04259680-2dca-4c9d-b2e0-5d3e2d37e7c5"))
                //        {
                //            string reMove = "<pub:UpdateIdentity UpdateID=\"04259680-2dca-4c9d-b2e0-5d3e2d37e7c5\"/>";
                //            dbSettings = dbSettings.Replace(reMove, "");
                //        }
                //    }
                //}


                // Process prerequistes (some updates may have different settings from DB)
                dbSettings = ProcessPrerequistes(dbSettings, releaseType, osid, inputData,sku);

                sb.Append(dbSettings);

                sb.Append("</pub:Relationships>");

                //Applicability Rules
                string applicabilityRules = ApplicabilityRulesBuilder.BuildApplicabilityRules(osid, arch, releaseType, sku, tfsObject);
                if (!String.IsNullOrEmpty(applicabilityRules))
                    sb.Append(applicabilityRules);

                sb.Append("<pub:Files>");
                sb.AppendFormat("<pub:File FileLocation=\"{0}\" FileName=\"{1}\" />",
                                tfsObject.GetPatchFullPath((Architecture)arch).ToLowerInvariant(),
                                tfsObject.GetPatchName((Architecture)arch).ToLowerInvariant());
                sb.Append("</pub:Files>");

                //Install commands
                sb.Append(BuildInstallCommands(tfsObject, arch));

                sb.Append("</pub:Update>");

                // create child update object
                Update childUpdate = new Update(sb.ToString(), false, false);

                return childUpdate;
            }
        }

        public static int GetUpdateType(InputData data)
        {
            int releaseType;

            if (data.OtherProperties != null && data.OtherProperties.ContainsKey("ReleaseType"))
            {
                using (var db = new WUSAFXDbContext())
                {
                    string strType = data.OtherProperties["ReleaseType"];
                    var record = db.TReleaseTypes.Where(p => p.Name.Equals(strType)).FirstOrDefault();
                    if (record == null)
                        throw new NotSupportedException("Not supported release type: " + data.OtherProperties["ReleaseType"]);

                    releaseType = record.ID;
                }
            }
            else
            {
                releaseType = ReleaseTypeDetector.ParseReleaseTypeFromTitle(data.Title);
            }

            return releaseType;
        }

        private static void BuildUpdateProperties(InputData data, int osid, int arch, int releaseType, UpdateProperty updateProperties)
        {
            updateProperties.KBArticle = data.KB;

            string expectMsrcSeverity = "Important";
            if (data.OtherProperties != null && data.OtherProperties.ContainsKey("MsrcSeverity"))
                expectMsrcSeverity = data.OtherProperties["MsrcSeverity"];

            if (data.IsCatalogOnly)
            {
                updateProperties.IsCatalogOnly = true;
            }
            switch (releaseType)
            {
                // Security Update and Monthly Rollup
                case 1:
                case 2:
                    updateProperties.AU = true;
                    updateProperties.Site = true;
                    updateProperties.SUS = true;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = true;
                    updateProperties.BrowseOnly = false;
                    //updateProperties.MsrcSeverity = "Important";
                    updateProperties.MsrcSeverity = expectMsrcSeverity;
                    updateProperties.UpdateClassification = "0FA1201D-4330-4FA8-8AE9-B877473B6441";
                    break;

                // Security only
                case 3:
                    updateProperties.AU = false;
                    updateProperties.Site = false;
                    updateProperties.SUS = true;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = false;
                    updateProperties.BrowseOnly = false;
                    //updateProperties.MsrcSeverity = "Important";
                    updateProperties.MsrcSeverity = expectMsrcSeverity;
                    updateProperties.UpdateClassification = "0FA1201D-4330-4FA8-8AE9-B877473B6441";
                    break;

                // Preview of quality rollup
                case 4:
                    updateProperties.Site = true;
                    updateProperties.SUS = false;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = false;
                    updateProperties.BrowseOnly = true;
                    updateProperties.MsrcSeverity = String.Empty;
                    updateProperties.UpdateClassification = "CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83";
                    break;

                // CSA
                case 5:
                    updateProperties.Site = false;
                    updateProperties.SUS = true;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = false;
                    updateProperties.Csa = true;
                    updateProperties.BrowseOnly = false;
                    updateProperties.MsrcSeverity = expectMsrcSeverity;
                    updateProperties.UpdateClassification = "0FA1201D-4330-4FA8-8AE9-B877473B6441";
                    break;

                // Catalog(Security)
                case 6:
                    updateProperties.Site = false;
                    updateProperties.SUS = false;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = false;
                    updateProperties.Csa = false;
                    updateProperties.BrowseOnly = false;
                    //updateProperties.MsrcSeverity = "Important";
                    updateProperties.MsrcSeverity = expectMsrcSeverity;
                    updateProperties.UpdateClassification = "0FA1201D-4330-4FA8-8AE9-B877473B6441";
                    break;

                // Catalog(Preview)
                case 7:
                    updateProperties.Site = false;
                    updateProperties.SUS = false;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = false;
                    updateProperties.Csa = false;
                    updateProperties.BrowseOnly = false;
                    updateProperties.MsrcSeverity = String.Empty;
                    updateProperties.UpdateClassification = "CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83";
                    break;

                // Promotion
                case 8:
                    updateProperties.AU = false;
                    updateProperties.Site = true;
                    updateProperties.SUS = true;
                    updateProperties.Catalog = true;
                    updateProperties.AutoSelectOnWebSites = false;
                    updateProperties.BrowseOnly = false;
                    updateProperties.MsrcSeverity = String.Empty;
                    updateProperties.UpdateClassification = "CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83";
                    break;
            }

            if (OSDetector.IsWin10OS(osid)) //win10
            {
                updateProperties.BrowseOnly = false;
            }

            if ((osid == 1037 || osid >= 1042) && data.Title.Contains("Preview"))
            {
                updateProperties.BrowseOnly = true;
            }

            //if (arch == 4) // arm update
            //{
            //    updateProperties.SUS = false;
            //    updateProperties.Catalog = false;
            //}

            // hardcode some excepts
            if (osid == 1023 || // Win10 vNext Client
                osid == 1024 ||// Win10 vNext Server
                osid == 1027 // Azure Stack HCI, version 20H2
                )
            {
                updateProperties.SUS = false;
                updateProperties.Catalog = false;
            }

            if (data.OtherProperties.ContainsKey("Destination"))
            {
                string destination = data.OtherProperties["Destination"];

                if (!destination.Contains("WU"))
                {
                    updateProperties.AU = false;
                    updateProperties.Site = false;
                }
                if (!destination.Contains("SUS"))
                    updateProperties.SUS = false;
                if (!destination.Contains("Catalog"))
                    updateProperties.Catalog = false;
            }
        }

        private static List<Update> BuildFixedChildUpdate(int osid, int arch, int releaseType, Update parentUpdate)
        {
            using (var db = new WUSAFXDbContext())
            {
                // For 'Promotion', search 'MonthlyRollup' settings
                if (releaseType == 8)
                    releaseType = 2;

                var queryResult = db.TAdditionalChildUpdates.Where(p => p.OS == osid && p.Arch == arch && p.ReleaseType == releaseType).ToList();
                if (queryResult.Count > 0)
                {
                    List<Update> updates = new List<Update>();
                    foreach (var q in queryResult)
                    {
                        var fixedUpdateRecord = db.TFixedUpdates.Where(p => p.ID == q.ChildUpdateID).Single();

                        string fixedUpdateXml = BuildApplicabilityRulesForFixedChildUpdate(fixedUpdateRecord, parentUpdate);

                        Update update = new Update(fixedUpdateXml, false, false);

                        updates.Add(update);
                    }

                    return updates;
                }
                else
                {
                    return null;
                }
            }
        }

        private static string BuildApplicabilityRulesForFixedChildUpdate(TFixedUpdate fixedUpdateRec, Update parentUpdate)
        {
            string applicabilityRules = String.Empty;

            if (fixedUpdateRec.Description.StartsWith("msipatchregfix"))
            {
                //1. Find 4.5.2 child update
                Update childUpdate = parentUpdate.ChildUpdates.Where(p => p.Title.StartsWith("ndp45-")).FirstOrDefault();
                if (childUpdate != null)
                {
                    // Applicability rules should be same as 4.5.2 child update
                    applicabilityRules = childUpdate.ApplicabilityRules.OuterXml;
                }
            }

            return fixedUpdateRec.PublishingXML.Replace("[#ApplicabilityRules]", applicabilityRules)
                .Replace("[#Categories]", parentUpdate.Categories)
                .Replace("[#Prerequisites]", parentUpdate.Prerequisites.OuterXml);
        }

        private static string GetChildTitle(string patchName)
        {
            return System.IO.Path.GetFileNameWithoutExtension(patchName).ToLowerInvariant();
        }

        private static string BuildInstallCommands(WorkItemHelper tfsObject, int arch)
        {
            using (var db = new WUSAFXDbContext())
            {
                switch (tfsObject.PatchTechnology)
                {
                    case "MSI":
                        string commands = db.TPropertyMappings.Where(p => p.Name == "RedistInstallCommand").Single().MappedContent;
                        string arguments = String.Empty;
                        switch (tfsObject.SKU)
                        {
                            case "2.0":
                            case "3.0":
                            case "3.5":
                                arguments = "/q /norestart";
                                break;

                            case "4.0":
                                arguments = "/q /norestart /chainingpackage NETFX4WUKB";
                                break;

                            default:
                                arguments = "/q /norestart /chainingpackage NETFX45WUKB";
                                break;
                        }

                        return commands.Replace("[#PatchName]", tfsObject.GetPatchName((Architecture)arch)).Replace("[#Arguments]", arguments);

                    case "CBS":
                        return db.TPropertyMappings.Where(p => p.Name == "CBSInstallCommand").Single().MappedContent;

                    case "OCM":
                        return db.TPropertyMappings.Where(p => p.Name == "OCMInstallCommand").Single().MappedContent;

                    default:
                        throw new NotSupportedException("Unknown patch technology: " + tfsObject.PatchTechnology);
                }
            }
        }

        private static string ProcessPrerequistes(string prereqDB, int releaseType, int osid, InputData inputData,int SkuID)
        {
            //if (!ReleaseTypeDetector.IsSecurityRelease(releaseType) && OSDetector.IsWin10OS(osid))
            //{
            //    //Add WUfB detectoid for non-security win10 updates
            //    return prereqDB.Replace("</pub:Prerequisites>", "<pub:UpdateIdentity UpdateID=\"5671b1d0-eb3f-4259-b777-ae7aa53b51aa\" /></pub:Prerequisites>");
            //}
            
            if (inputData.Title.Contains("Cumulative Update Preview") && (osid == 1038 || osid == 1036))
            {
                //Add WUfB detectoid for non-security win10 updates
                return prereqDB.Replace("</pub:Prerequisites>", "<pub:UpdateIdentity UpdateID=\"5671b1d0-eb3f-4259-b777-ae7aa53b51aa\" /></pub:Prerequisites>");
            }
            else
            {
                return prereqDB;
            }

            
            
        }
    }
}
