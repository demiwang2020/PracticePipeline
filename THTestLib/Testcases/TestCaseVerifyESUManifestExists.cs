using Helper;
using HotFixLibrary;
using LogAnalyzer;
using LoggerLibrary;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using RMIntegration.RMService;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace THTestLib.Testcases
{
    class TestCaseVerifyESUManifestExists : TestCaseBase
    {
        private List<string> allRelatedWI;
        private Dictionary<string, Dictionary<Architecture, string>> patchs = new Dictionary<string, Dictionary<Architecture, string>>();
        private List<string> patchLocations;
        private List<string> localWinCloudPackages;
        private List<string> workItemId = new List<string>();
        private List<string> Arch = new List<string>();
        public TestCaseVerifyESUManifestExists(THTestObject testobj)
            : base(testobj)
        {
            if ((TestObject.TFSItem.OSInstalled.Equals("Windows 8.1") || TestObject.TFSItem.OSInstalled.Equals("Windows 8")) && !TestObject.TFSItem.Title.Contains("Product Refresh"))
            {
                allRelatedWI = testobj.GetAllRelatedWIByWI();
                allRelatedWI = allRelatedWI.Distinct().ToList();
                if (allRelatedWI.Count != 0)
                {
                    foreach (var WI in allRelatedWI)
                    {
                        Dictionary<Architecture, string> patchInformation = new Dictionary<Architecture, string>();
                        WorkItem workitem = Connect2TFS.Connect2TFS.GetWorkItem(int.Parse(WI), @"https://vstfdevdiv.corp.microsoft.com/DevDiv");
                        if (workitem["KB Article"].ToString() == workitem["LCU KB Article"].ToString() && workitem["SKU"].ToString().StartsWith("3"))
                        {
                            continue;
                        }
                        using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                        {
                            if (!String.IsNullOrEmpty(workitem["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(workitem["Drop Patch Location X64"].ToString()) ||
                                !String.IsNullOrEmpty(workitem["Drop Name"].ToString()) && !String.IsNullOrEmpty(workitem["Drop Patch Location"].ToString()))
                            {
                                patchLocations = testobj.DownloadPackages(workitem, true);
                            }
                            else
                            {

                                if (workitem["Windows Packaging ID"] == null)
                                {
                                    throw new Exception($"No DropName exists in work item, and the Windows Packaging ID value in work item {WI} is invalid.  Can't get packages.");
                                }
                                localWinCloudPackages = testobj.DownloadPackagesForJob(int.Parse(workitem["Windows Packaging ID"].ToString()), workitem["KB Article"].ToString());
                            }
                        }
                        foreach (Architecture arch in testobj.SupportedArchs)
                        {
                            string patchVersion = String.Empty;
                            string patchLocation = String.Empty;
                            Arch.Add(arch.ToString());
                            //patchLocation = GetLatestBuildPath(TFSItem.GetPatchLocation(arch), ref patchVersion);
                            patchLocation = testobj.TFSItem.GetPatchDownloadLocation(arch);
                            if (String.IsNullOrEmpty(patchLocation))
                            {
                                StaticLogWriter.Instance.logError(String.Format("Test blocked: {0} patch is expected but actually not available", arch.ToString()));
                            }
                            else
                            {
                                StaticLogWriter.Instance.logMessage(String.Format("{0} patch location: {1}", arch.ToString(), patchLocation));
                                StaticLogWriter.Instance.logMessage(string.Format("{0} patch version: {1} ", arch.ToString(), patchVersion));

                                patchInformation.Add(arch, patchLocation);
                            }
                        }
                        patchs.Add(WI, patchInformation);
                    }
                }
            }

        }
        public override bool RunTestCase()
        {
            // This case only applies to Windows 8.1
            if (!(TestObject.TFSItem.OSInstalled.Equals("Windows 8.1") || TestObject.TFSItem.OSInstalled.Equals("Windows 8")) || TestObject.TFSItem.Title.Contains("Product Refresh"))
                return true;

            //Create result table
            DataTable resultTable = HelperMethods.CreateDataTable("Verify ESU MainifestFile exist in the corresponding folder",
                new string[] { "Patch", "FilePath", "Version", "LCUVersion", "LCUWSDVersion", "Result" },
                new string[] { "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center#ResultCol=1" });

            //Creaet manifest Table
            DataTable table = HelperMethods.CreateDataTable("Verify ESU MainifestFile exist in the corresponding folder",
                new string[] { "ID", "Arch", "Component Version", "Sku", "ActiveLicenses", "ExtendedSecurityUpdates", "SKUs", "Arguments" });

            //Run test
            bool overallResult = true;

            bool result = false;


            // Verify the specified mainifest file exists
            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.Arch == Architecture.ARM || patch.Value.Arch == Architecture.ARM64)
                    continue;

                if (TestObject.TFSItem.Title.Contains("WSD"))
                {
                    result = ExecuteVerifyESUMainifest(patch.Value.ExtractLocation, patch.Value.Arch, resultTable);
                }
                else
                {
                    result = VerifySameOsDiffSku(patch.Value.Arch, table);
                }

                //resultTable.TableName = "Verify ESU MainifestFile exist in the corresponding folder";
                //TestObject.TestResults.ResultDetails.Add(resultTable);
                //TestObject.TestResults.ResultDetailSummaries.Add(result);
                overallResult &= result;
            }
            if (TestObject.TFSItem.Title.Contains("WSD"))
            {
                TestObject.TestResults.ResultDetails.Add(resultTable);
            }
            else
            {
                table.Columns.Remove("ID");
                TestObject.TestResults.ResultDetails.Add(table);
            }

            TestObject.TestResults.ResultDetailSummaries.Add(result);
            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }

        private string GetActualBinaries(WorkItem Item, WorkItemHelper helper, Architecture arch, string loc)
        {
            string ExtractLocation = string.Empty;

            if (String.IsNullOrEmpty(loc))
                return null;
            //Copy remote package to local and extract it
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {

                if (!String.IsNullOrEmpty(Item["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location X64"].ToString()) ||
                    !String.IsNullOrEmpty(Item["Drop Name"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location"].ToString()))
                {
                    ExtractLocation = helper.GetExpandPackage(arch);
                }
                else
                {
                    ExtractLocation = Extraction.ExtractPatchToPath(loc);
                }

            }


            return ExtractLocation;
        }

        private void AddInfoToTable(Architecture arch, DataTable table)
        {

            string TFSServerURI = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
            string Extractpath = string.Empty;
            foreach (var patch in patchs)
            {

                WorkItem item = Connect2TFS.Connect2TFS.GetWorkItem(Convert.ToInt32(patch.Key), TFSServerURI);
                WorkItemHelper helper = new WorkItemHelper(item);
                //foreach (var diffArch in patch.Value)
                //{
                Extractpath = GetActualBinaries(item, helper, arch, patchs[patch.Key][arch]);
                string[] files = Directory.GetFiles(Extractpath, "*.manifest");
                foreach (string f in files)
                {
                    if (Path.GetFileNameWithoutExtension(f).Contains("microsoft-windows-s..edsecurityupdatesai"))
                    {
                        XmlReader reader = XmlReader.Create(f);
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "ExtendedSecurityUpdatesAI")
                            {
                                DataRow row = table.NewRow();
                                row["ID"] = patch.Key;
                                row["Arch"] = arch;
                                row["Component Version"] = helper.ComponentVersion;
                                row["Sku"] = helper.SKU;
                                row["ActiveLicenses"] = reader.GetAttribute("ActiveLicenses");
                                row["ExtendedSecurityUpdates"] = reader.GetAttribute("ExtendedSecurityUpdates");
                                row["SKUs"] = reader.GetAttribute("SKUs");
                                row["arguments"] = reader.GetAttribute("arguments");
                                table.Rows.Add(row);
                            }
                        }
                    }
                }

                //}

            }
        }

        private bool VerifySameOsDiffSku(Architecture architecture, DataTable table)
        {
            bool result = true;
            string tempCon = string.Empty;
            List<string> content = new List<string>();
            List<string> component = new List<string>();
            AddInfoToTable(architecture, table);

            if (patchs.Count < 3)
            {
                workItemId.Add(table.AsEnumerable().Where(p => p["Sku"].ToString().Contains("2")).Select(p => p["ID"].ToString()).First());
                workItemId.Add(table.AsEnumerable().Where(p => p["Sku"].ToString().Contains("8")).Select(p => p["ID"].ToString()).First());
            }
            else
            {
                workItemId.Add(table.AsEnumerable().Where(p => p["Sku"].ToString().Contains("3")).Select(p => p["ID"].ToString()).First());
                workItemId.Add(table.AsEnumerable().Where(p => p["Sku"].ToString().Contains("2")).Select(p => p["ID"].ToString()).First());
                workItemId.Add(table.AsEnumerable().Where(p => p["Sku"].ToString().Contains("8")).Select(p => p["ID"].ToString()).First());
            }


            foreach (var item in workItemId)
            {

                component.Add(table.AsEnumerable().Where(p => p["ID"].ToString() == item).Select(p => p["Component Version"].ToString()).First());

                tempCon = string.Join("", table.AsEnumerable().Where(p => p["ID"].ToString() == item && p["Arch"].ToString() == architecture.ToString()).Select(p => p["ActiveLicenses"].ToString()));
                content.Add(tempCon);
                tempCon = string.Join("", table.AsEnumerable().Where(p => p["ID"].ToString() == item && p["Arch"].ToString() == architecture.ToString()).Select(p => p["ExtendedSecurityUpdates"].ToString()));
                content.Add(tempCon);
                tempCon = string.Join("", table.AsEnumerable().Where(p => p["ID"].ToString() == item && p["Arch"].ToString() == architecture.ToString()).Select(p => p["SKUs"].ToString()));
                content.Add(tempCon);
                tempCon = string.Join("", table.AsEnumerable().Where(p => p["ID"].ToString() == item && p["Arch"].ToString() == architecture.ToString()).Select(p => p["Arguments"].ToString()));
                content.Add(tempCon);


            }


            //Compare all content

            if (workItemId.Count == 3)
            {
                for (int i = 0; i < 4; i++)
                {
                    // 3.5 and 4.7.2
                    if (content[i].Equals(content[4 + i]))
                        result &= true;
                    else
                        result &= false;
                    //4.7.2 and 4.8
                    if (content[4 + i].Equals(content[8 + i]))
                        result &= true;
                    else
                        result &= false;
                    //3.5 and 4.8
                    if (content[i].Equals(content[8 + i]))
                        result &= true;
                    else
                        result &= false;

                }
            }
            else if (workItemId.Count == 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    // 4.8 and 4.7.2
                    if (content[i].Equals(content[4 + i]))
                        result &= true;
                    else
                        result &= false;
                }
            }
            else
            {
                result = false;
            }



            if (patchs.Count == 3)
            {
                if (component[0].Equals(component[1]) || component[0].Equals(component[2]) || component[1].Equals(component[2]))
                    result = false;
            }
            else if (patchs.Count == 2)
            {
                if (component[0].Equals(component[1]))
                    return false;
            }

            content.Clear();
            workItemId.Clear();


            return result;
        }


        private bool ExecuteVerifyESUMainifest(string extractPath, Architecture arch, DataTable resultTable)
        {

            bool ret = false;
            string version = "";
            string LCUVersion = "";

            DataRow row = resultTable.NewRow();
            row["Patch"] = arch.ToString();
            row["FilePath"] = "n/a";
            row["Version"] = "n/a";
            row["LCUVersion"] = "n/a";
            row["LCUWSDVersion"] = "n/a";
            row["Result"] = "Fail";

            string[] files = Directory.GetFiles(extractPath, "*.manifest");
            foreach (string f in files)
            {

                if (Path.GetFileNameWithoutExtension(f).Contains("microsoft-windows-s..edsecurityupdatesai"))
                {
                    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                    {
                        XmlReader reader = XmlReader.Create(f);
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "assemblyIdentity")
                            {
                                if (reader.GetAttribute("name").Contains("ExtendedSecurityUpdatesAI"))
                                {
                                    version = reader.GetAttribute("version");
                                    break;
                                }
                            }
                        }
                        string KBArticle = TestObject.TFSItem.KBNumber.ToString();
                        string LCUKBArticle = TestObject.TFSItem.LCUKBArticle.ToString();

                        var release = TestObject.TFSItem.Custom02.ToString();
                        //int month = int.Parse(release.Substring(5, 2));
                        //int lastMonth = month - 1;
                        //string previousMonth = lastMonth.ToString();
                        //if (lastMonth < 10)
                        //{
                        //    previousMonth = "0" + previousMonth;
                        //}

                        //var previousRelase = release.Substring(0, 5) + previousMonth + release.Substring(7, 2);

                        //int year = int.Parse(release.Substring(0, 4));
                        //int month = int.Parse(release.Substring(5, 2));
                        //int lastMonth = month - 1;
                        //if (lastMonth == 0)
                        //{
                        //    lastMonth = 12;
                        //    year = year - 1;
                        //}
                        //var previousRelase = year.ToString("D4") + "." + lastMonth.ToString("D2") + release.Substring(7, 2);

                        var previousRelase = "2023.11 B";
                        string ReleaseType = TestObject.TFSItem.Title.Contains("Monthly Rollup") ? "MR" : "SO";
                        string PackageType = "";
                        if (LCUKBArticle == "")
                        {
                            LCUVersion = dataContext.Win81WSDTests.Where(a =>
                            a.Release == previousRelase && a.Arch == arch.ToString() && a.ReleaseType == ReleaseType).First().ComponentVersion;
                        }
                        else if (LCUKBArticle == KBArticle)
                        {
                            PackageType = "ProductRefresh";
                            LCUVersion = dataContext.Win81WSDTests.Where(a =>
                            a.Release == previousRelase && a.PackageType == PackageType && a.Arch == arch.ToString()).First().ComponentVersion;
                        }
                        else
                        {
                            LCUVersion = dataContext.Win81WSDTests.Where(a =>
                               a.KB == LCUKBArticle && a.Arch == arch.ToString()).First().ComponentVersion;
                        }

                        var previousWSDVersion = dataContext.Win81WSDTests.Where(r => r.Release == previousRelase
                        && r.PackageType == "WSD"
                        && r.Arch == arch.ToString()).OrderByDescending(r => r.JobID).FirstOrDefault().ComponentVersion;

                        row["FilePath"] = Path.GetFileName(f);
                        row["Version"] = version;
                        row["LCUVersion"] = LCUVersion;
                        row["LCUWSDVersion"] = previousWSDVersion;

                        if (String.Compare(version, LCUVersion, true) > 0 && String.Compare(version, previousWSDVersion, true) > 0)
                        {
                            row["Result"] = "Pass";
                            ret = true;
                        }
                        //var recode = dataContext.Win81WSDTests.Where(a => a.KB == KBArticle && a.Arch == arch.ToString()&&a.JobID== TestObject.TFSItem.WindowsPackagingId.ToString()).FirstOrDefault();
                        var recode = dataContext.Win81WSDTests.Where(a => a.KB == KBArticle && a.Arch == arch.ToString() && a.ComponentVersion == version).FirstOrDefault();
                        if (recode == null)
                        {
                            Win81WSDTest w81Test = new Win81WSDTest();
                            w81Test.Release = TestObject.TFSItem.Custom02;
                            w81Test.PackageType = string.IsNullOrEmpty(PackageType) ? "DotNET" : "ProductRefresh";
                            w81Test.KB = TestObject.TFSItem.KBNumber;
                            w81Test.ComponentVersion = version;
                            w81Test.JobID = TestObject.TFSItem.WindowsPackagingId.ToString();
                            w81Test.ReleaseType = TestObject.TFSItem.Title.Contains("Monthly Rollup") ? "MR" : "SO";
                            w81Test.SKU = TestObject.TFSItem.SKU;
                            if (arch.ToString() == "AMD64")
                            {
                                w81Test.Arch = "AMD64";
                            }
                            else if (arch.ToString() == "X86")
                            {
                                w81Test.Arch = "x86";
                            }
                            dataContext.Win81WSDTests.InsertOnSubmit(w81Test);
                            dataContext.SubmitChanges();
                        }

                    }
                }
            }
            resultTable.Rows.Add(row);
            return ret;
        }
    }
}


