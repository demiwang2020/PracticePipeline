using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data;
using ScorpionDAL;
using THTestLib.Testcases;
using LoggerLibrary;
using System.Configuration;
using NetFxSetupLibrary;
using Helper;
using NetFxSetupLibrary.Patch;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Xml;
using Product = NetFxSetupLibrary.Product;
using File = System.IO.File;
using NetFxServicing.DropHelperLib;
using NetFxServicing.LogInfoLib;
using KeyVaultManagementLib;
using THTestLib.GoFxService;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static Microsoft.TeamFoundation.Client.CommandLine.Options;
using System.Net.Http.Headers;
using HotFixLibrary;
using RMIntegration.RMService;
using System.Drawing;
namespace THTestLib
{
    class THTestObject
    {
        public THTestResults TestResults { get; private set; }
        public Dictionary<Architecture, PatchInformation> Patches { get; private set; }
        public Dictionary<string, Dictionary<string, string>> ExpectedBinariesVersions { get; private set; }
        public List<string> NotExpectedBinaryList { get; private set; }
        public WorkItemHelper TFSItem { get; private set; }
        public WorkItem Item { get; private set; }
        public bool IsTraditionalWin10LCU  //TH1~RS4 4.6.X/4.7.X is traditional LCU
        {
            get
            {
                if (TFSItem.OSInstalled == "Windows 10" && (!TFSItem.SKU.Equals("4.8")))
                {
                    switch (TFSItem.OSSPLevel)
                    {
                        case "RTM": //TH1
                        case "1511": //TH2
                        case "1607": //RS1
                        case "1703": //RS2
                        case "1709": //RS3
                        case "1803": //RS4
                            return true;
                    }
                }
                return false;
            }
        }
        public bool IsWindows10Patch { get { return Utility.CompareOS(TFSItem.OSInstalled, TFSItem.OSSPLevel, "Windows 10", "RTM") >= 0; } }

        public bool SimplePatch { get; private set; } //Indicates if a patch is LCU or not
        public bool IsProductRefresh { get; private set; }

        private string StrExpectedBinaries;
        private string StrExpectedBinaries1;
        private int _thTestID;
        private readonly string StrExpectedBinariesSample = "Binaries Affected:[(FileName1-Fileversion1),(FileName2-Fileversion2),(FileName3-Fileversion3)]";
        private string[] archs;
        private List<Architecture> _supportedArchs;
        /// <summary>
        /// Object to manage the downloading of packages from windows cloud
        /// </summary>
        private WinCloudPackage winCloudPackage;
        /// <summary>
        /// Local file path where packages from windows cloud will be downloaded
        /// </summary>
        private string localWinCloudPackagePath;
        /// <summary>
        /// List of local package files after download is complete
        /// </summary>
        private List<string> localWinCloudPackages;

        private string PackagePathBase = ConfigurationManager.AppSettings["DownloadPath"];

        public List<Architecture> SupportedArchs { get { return _supportedArchs; } }

        private List<string> packagePaths;
        public THTestObject(WorkItem item)
        {
            TFSItem = new WorkItemHelper(THTestProcess.TFSServerURI, item.Id);
            Item = item;
        }
        public void RunTest()
        {
            bool bStaticTestExecuted = false;
            bool bRuntimeKickedOff = false;
            try
            {
                if ((TFSItem.OSInstalled.Equals("Windows 8.1") && TFSItem.Title.Contains("LCU")) || (TFSItem.OSInstalled.Equals("Windows Server 2012 R2") && TFSItem.Title.Contains("LCU")))
                {
                    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                    {
                        if (!String.IsNullOrEmpty(Item["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location X64"].ToString()) ||
                            !String.IsNullOrEmpty(Item["Drop Name"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location"].ToString()))
                        {
                            packagePaths = DownloadPackages(Item, true);
                        }
                        else
                        {
                            packagePaths = DownloadPackagesForJob(TFSItem.WindowsPackagingId, TFSItem.KBNumber);
                        }

                        TTHTestRecord record = dataContext.TTHTestRecords.Where(r => r.TFSID == TFSItem.ID && r.Active
                        && r.X64PatchLocation == packagePaths[0] && r.TFSID == TFSItem.ID).SingleOrDefault();
                        if (record == null || !record.Active)
                        {
                            TestResults = new THTestResults();
                            bool Win81WSDTestResult = true;

                            List<Dictionary<string, string>> Win81WSDTestResultList = new List<Dictionary<string, string>>();
                            var release = TFSItem.Custom02.ToString();
                            Win81WSDTestResultList = Win81WSDTest();
                            DataTable dataTable = HelperMethods.CreateDataTable("ESU component verification",
                                new string[] { "Arch", "Current WSD Component Version", "Current .NET Component Version", "Previous WSD Component Version", "Result" });
                            foreach (Dictionary<string, string> map in Win81WSDTestResultList)
                            {
                                DataRow row = dataTable.NewRow();
                                if (map.TryGetValue("Arch", out string arch))
                                {
                                    row["Arch"] = arch;
                                }
                                if (map.TryGetValue("CurrentWSDVersion", out string cWSDVer))
                                {
                                    row["Current WSD Component Version"] = cWSDVer;
                                }
                                if (map.TryGetValue("CurrentDotNETVersion", out string cNetVer))
                                {
                                    row["Current .NET Component Version"] = cNetVer;
                                }
                                if (map.TryGetValue("PreviousWSDVersion", out string pWSDVer))
                                {
                                    row["Previous WSD Component Version"] = pWSDVer;
                                }
                                if (map.TryGetValue("Result", out string res))
                                {
                                    row["Result"] = res;
                                }
                                if (res == "Fail")
                                {
                                    Win81WSDTestResult = false;
                                }
                                dataTable.Rows.Add(row);
                            }
                            TestResults.ResultDetails.Add(dataTable);
                            TestResults.ResultDetailSummaries.Add(Win81WSDTestResult);
                            TestResults.Result = Win81WSDTestResult;

                            AddDBRecords(packagePaths[0]);

                            MailHelper helper = new MailHelper();
                            helper.SendStaticTestResultsMail(TFSItem, TestResults, null);
                            TTHTestLog testLog = new TTHTestLog();
                            testLog.IsStaticTest = true;
                            testLog.LogFilePath = HelperMethods.SaveTestLog(helper.MailContent, TFSItem.ID.ToString() + "_Static");
                            testLog.LogResult = TestResults.Result;
                            testLog.HasWarning = TestResults.HasWarning;
                            testLog.TimeStamp = DateTime.Now;

                            dataContext.TTHTestLogs.InsertOnSubmit(testLog);
                            dataContext.SubmitChanges();

                            TTHTestRecord thTestRec = dataContext.TTHTestRecords.Where(p => p.ID == _thTestID).SingleOrDefault();
                            thTestRec.StaticTestLog = testLog.ID;
                            thTestRec.LastModifiedDate = testLog.TimeStamp;

                            dataContext.SubmitChanges();
                        }
                        else
                        {
                            StaticLogWriter.Instance.logMessage("test have been tested");
                        }

                    }
                }
                else
                {
                    bStaticTestExecuted = RunStaticTests();
                    if (bStaticTestExecuted)
                    {
                        bRuntimeKickedOff = KickoffRuntimeTest();
                    }
                }

            }
            catch (Exception ex)
            {
                StaticLogWriter.Instance.logError(String.Format("Exception caught: {0}", ex.Message));
                StaticLogWriter.Instance.logMessage(ex.StackTrace);

                //Write exception to log
                StaticLogWriter.Instance.logError(ex.Message);
                StaticLogWriter.Instance.logMessage(ex.StackTrace);

                //Send out exception mail if necessary
                SendExceptionMail(ex);
                return;
            }
            finally
            {
                //DeleteExtractPath();
            }

            //Send result mail if no exception caught
            if (bStaticTestExecuted)
            {
                List<TTHTestRunInfo> lstRuntimeRuns = null;
                if (bRuntimeKickedOff)
                {
                    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                    {
                        lstRuntimeRuns = dataContext.TTHTestRunInfos.Where(r => r.THTestID == _thTestID).ToList();
                    }
                }

                MailHelper helper = new MailHelper();
                helper.SendStaticTestResultsMail(TFSItem, TestResults, lstRuntimeRuns);
                //Store static test result to DB
                using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                {
                    TTHTestLog testLog = new TTHTestLog();
                    testLog.IsStaticTest = true;
                    testLog.LogFilePath = HelperMethods.SaveTestLog(helper.MailContent, TFSItem.ID.ToString() + "_Static");
                    testLog.LogResult = TestResults.Result;
                    testLog.HasWarning = TestResults.HasWarning;
                    testLog.TimeStamp = DateTime.Now;

                    dataContext.TTHTestLogs.InsertOnSubmit(testLog);
                    dataContext.SubmitChanges();


                    TTHTestRecord thTestRec = dataContext.TTHTestRecords.Where(p => p.ID == _thTestID).SingleOrDefault();
                    thTestRec.StaticTestLog = testLog.ID;
                    thTestRec.LastModifiedDate = testLog.TimeStamp;

                    dataContext.SubmitChanges();
                }

                StaticLogWriter.Instance.logMessage("Test completed for TFS - " + TFSItem.ID.ToString());
            }
        }

        #region Private Methods

        private List<Dictionary<string, string>> Win81WSDTest()
        {
            List<Dictionary<string, string>> resultList = new List<Dictionary<string, string>>();
            //List<string> packagePaths = DownloadPackagesForJob(TFSItem.WindowsPackagingId);
            foreach (string packagePath in packagePaths)
            {
                string xmlPath = "";
                string version = "";
                string arch = "";

                if (packagePath.ToLower().Contains("x86"))
                {
                    xmlPath = Path.GetDirectoryName(packagePath) + "\\packagemetadata-x86.xml";
                    arch = "x86";
                }
                else if (packagePath.ToLower().Contains("x64"))
                {
                    xmlPath = Path.GetDirectoryName(packagePath) + "\\packagemetadata-x64.xml";
                    arch = "AMD64";
                }
                try
                {
                    XmlReader reader = XmlReader.Create(xmlPath);
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "assemblyIdentity")
                        {
                            if (reader.GetAttribute("name").Contains("ExtendedSecurityUpdatesAI"))
                            {
                                string serverURI = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
                                version = reader.GetAttribute("version");
                                UpdateComponentVersionInWI(version);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    StaticLogWriter.Instance.logMessage("Fail to parse file:" + xmlPath);
                    StaticLogWriter.Instance.logError(ex.Message);
                    StaticLogWriter.Instance.logMessage(ex.StackTrace);
                }
                InsertIntoWin81WSDTest(version, arch);
                StaticLogWriter.Instance.logMessage("insert wsd to db");
                resultList.Add(CompareVersion(arch, version));
            }
            return resultList;
        }
        public void UpdateComponentVersionInWI(string version)
        {
            string serverURI = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
            var tfs = Connect2TFS.Connect2TFS.GetWorkItem(TFSItem.ID, serverURI);
            var a = tfs["Windows Component Version"];
            if (tfs["Windows Component Version"] == "")
            {

                tfs["Windows Component Version"] = version;
                Connect2TFS.Connect2TFS.SaveWorkItem(tfs, serverURI);

            }


        }
        private List<Dictionary<string, string>> Win81WSDTest(string version)
        {
            archs = TFSItem.OSArchitecture.ToLowerInvariant().Split(new char[] { ';', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            List<Dictionary<string, string>> resultList = new List<Dictionary<string, string>>();
            foreach (string arch in archs)
            {
                if (arch != " ")
                {
                    resultList.Add(CompareVersion(arch, version));
                }
            }
            return resultList;
        }

        private Dictionary<string, string> CompareVersion(string arch, string version)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            //string releaseType = TFSItem.Title.Contains("Monthly Rollup") ? "MR" : "SO";
            string SKU = TFSItem.SKU;
            string release = TFSItem.Custom02.ToString();
            string LCUKBArticle = TFSItem.LCUKBArticle.ToString();
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                string currentDotNETVersionStr = "";
                string previousWSDVersionStr = "";
                int currentDotNETVersion = 0;
                int previousWSDVersion = 0;

                Win81WSDTest currentDotNETVersionResult = dataContext.Win81WSDTests.Where(r => r.SKU == SKU
                //&& r.ReleaseType == releaseType
                && r.Arch == arch
                && r.Release == release
                && r.PackageType == "DotNET").OrderByDescending(r => r.JobID).FirstOrDefault();

                //Win81WSDTest previousWSDVersionResult = dataContext.Win81WSDTests.Where(r => r.SKU == SKU
                ////&& r.ReleaseType == releaseType
                //&& r.Arch == arch
                //&& r.Release.Contains("7 B")
                //&& r.PackageType == "WSD").OrderByDescending(r => r.JobID).FirstOrDefault();
                Win81WSDTest previousWSDVersionResult = dataContext.Win81WSDTests.Where(r => r.KB == LCUKBArticle
                //&& r.ReleaseType == releaseType
                && r.Arch == arch).OrderByDescending(r => r.JobID).FirstOrDefault();

                if (currentDotNETVersionResult == null)
                {
                    StaticLogWriter.Instance.logMessage("Current .NET component version can't be found in DB");
                }
                else
                {
                    currentDotNETVersionStr = currentDotNETVersionResult.ComponentVersion;
                    currentDotNETVersion = int.Parse(currentDotNETVersionStr.Substring(currentDotNETVersionStr.LastIndexOf(".") + 1));
                }
                if (previousWSDVersionResult == null)
                {
                    StaticLogWriter.Instance.logMessage("Previous WSD component version can't be found in DB");
                }
                else
                {
                    previousWSDVersionStr = previousWSDVersionResult.ComponentVersion;
                    previousWSDVersion = int.Parse(previousWSDVersionStr.Substring(previousWSDVersionStr.LastIndexOf(".") + 1));
                }

                int currentWSDVersion = int.Parse(version.Substring(version.LastIndexOf(".") + 1));

                map.Add("Arch", arch);
                map.Add("CurrentWSDVersion", version);
                map.Add("CurrentDotNETVersion", currentDotNETVersionStr);
                map.Add("PreviousWSDVersion", previousWSDVersionStr);

                if (currentWSDVersion > currentDotNETVersion && currentDotNETVersion > previousWSDVersion)
                {
                    map.Add("Result", "Pass");
                }
                else
                {
                    map.Add("Result", "Fail");
                }
            }
            return map;
        }

        private void InsertIntoWin81WSDTest(string version, string arch)
        {

            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {

                Win81WSDTest w81Test = new Win81WSDTest();
                w81Test.Release = TFSItem.Custom02;
                w81Test.PackageType = "WSD";
                w81Test.KB = TFSItem.KBNumber;
                w81Test.ComponentVersion = version;
                w81Test.JobID = TFSItem.WindowsPackagingId.ToString();
                w81Test.ReleaseType = TFSItem.Title.Contains("Monthly Rollup") ? "MR" : "SO";
                w81Test.SKU = TFSItem.SKU;
                w81Test.Arch = arch;
                var recode = dataContext.Win81WSDTests.Where(a => a.ComponentVersion == version & a.KB == TFSItem.KBNumber & a.Arch == arch.ToString() && a.JobID == TFSItem.WindowsPackagingId.ToString()).FirstOrDefault();

                if (recode == null)
                {
                    dataContext.Win81WSDTests.InsertOnSubmit(w81Test);
                    dataContext.SubmitChanges();
                }

            }
        }

        #region Private Methods for static test
        private bool RunStaticTests()
        {
            //Step 1. Check TFS
            CheckTFSItem();
            //SignOffDetail signOff = new SignOffDetail();
            //if (!signOff.IfNeedToRun(new string[] { TFSItem.ID.ToString() }))
            //{
            //    throw new Exception(String.Format("Package version in drop, Trackit and WorkItem are different for {0}", TFSItem.ID.ToString()));
            //}

            if (TFSItem.OSInstalled == "Windows 8" || TFSItem.OSInstalled == "Windows 8.1")
            {
                bool statusResult = Check2K12and2K12R2WISmokeStatus();
                if (statusResult)
                {
                    StaticLogWriter.Instance.logMessage("All work items are in 'Smoke Test' state.");
                }
                else
                {
                    StaticLogWriter.Instance.logMessage("Not all work items are in 'Smoke Test' state.");
                }

            }
            List<string> patchLocations = new List<string>();
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                if (!String.IsNullOrEmpty(Item["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location X64"].ToString()) ||
                    !String.IsNullOrEmpty(Item["Drop Name"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location"].ToString()))
                {
                    patchLocations = DownloadPackages(Item, true);
                }
                else
                {
                    BuildLocalWinCloudPathDrop();
                }

            }


            //Step 2. Get x86/x64/arm64 package location
            Patches = new Dictionary<Architecture, PatchInformation>();

            foreach (Architecture arch in _supportedArchs)
            {
                string patchVersion = String.Empty;
                string patchLocation = String.Empty;

                //patchLocation = GetLatestBuildPath(TFSItem.GetPatchLocation(arch), ref patchVersion);
                patchLocation = TFSItem.GetPatchDownloadLocation(arch);
                if (String.IsNullOrEmpty(patchLocation))
                {
                    StaticLogWriter.Instance.logError(String.Format("Test blocked: {0} patch is expected but actually not available", arch.ToString()));
                    return false;
                }
                else
                {
                    StaticLogWriter.Instance.logMessage(String.Format("{0} patch location: {1}", arch.ToString(), patchLocation));
                    StaticLogWriter.Instance.logMessage(string.Format("{0} patch version: {1} ", arch.ToString(), patchVersion));

                    PatchInformation patchInfo = new PatchInformation();
                    patchInfo.Arch = arch;
                    patchInfo.PatchLocation = patchLocation;
                    patchInfo.PatchVersion = patchVersion;

                    Patches.Add(arch, patchInfo);
                }
            }

            //Check if patches have same patch version
            var versions = Patches.Values.Select(p => p.PatchVersion).Distinct();
            if (versions.Count() > 1)
            {
                StaticLogWriter.Instance.logMessage("Version numbers among different arch are different, some patches are not ready");
                return false;
            }

            //CHECK DB table to make sure if test is done before
            string flagLocation = _supportedArchs.Contains(Architecture.AMD64) ? Patches[Architecture.AMD64].PatchLocation : Patches[Architecture.X86].PatchLocation;
            //if (IsThisBuildTested(flagLocation))
            //{
            //    StaticLogWriter.Instance.logMessage("This build has been tested, return");
            //    return false;
            //}

            //Create results object and update it with actual patch locations
            TestResults = new THTestResults();

            //Step 3. Get expected files list
            GetExpectedBinariesAndVersions();

            // Get LCU patch
            GetLCUPatchPath();
            CheckLCUPatchValid();
            GetLCUExtractLocation();
            //Step 4. Get acutal binaries from x86 and x64 patches
            foreach (Architecture arch in _supportedArchs)
            {
                GetActualBinaries(Patches[arch]);
            }

            //Step 5. Run test cases
            List<TestCaseBase> allCases = TestCaseBase.GetAllTestCases(this);
            foreach (TestCaseBase testcase in allCases)
            {
                try
                {
                        testcase.RunTestCase();
                }
                catch (Exception ex) // record exception details but do not block other case running
                {
                    DataTable exceptionTable = HelperMethods.CreateDataTable("Exception caught when running case " + testcase.ToString() + ": " + ex.Message, new string[] { "Call Stack" });
                    DataRow row = exceptionTable.NewRow();
                    row[0] = ex.StackTrace;
                    exceptionTable.Rows.Add(row);

                    TestResults.ResultDetails.Add(exceptionTable);
                    TestResults.Result = false;
                    TestResults.ResultDetailSummaries.Add(false);
                }
            }

            //Generate a result summary table
            DataTable summaryTable = GenerateResultSummaryTable();
            TestResults.ResultDetails.Insert(0, summaryTable);
            TestResults.ResultDetailSummaries.Insert(0, TestResults.Result);

            //Step 6. Update DB to avoid this build to be tested again
            AddDBRecords(flagLocation);

            //Step 7. Update TFS with new location
            //UpdateTFSWithLatestPatchLocation();


            return true;
        }

        //private async Task<bool> Check2K12and2K12R2WISmokeStatus()
        private bool Check2K12and2K12R2WISmokeStatus()
        {
            //List<string> relatedWorkItemIds = await Task.Run(() => GetAllRelatedWIByWI());
            List<string> relatedWorkItemIds = GetAllRelatedWIByWI();
            bool allSmokeTest = true;
            foreach (var wiid in relatedWorkItemIds)
            {
                WorkItem workitem = Connect2TFS.Connect2TFS.GetWorkItem(int.Parse(wiid), @"https://vstfdevdiv.corp.microsoft.com/DevDiv");
                if (workitem["State"].ToString() != "Smoke Test")
                {
                    allSmokeTest = false;
                    break;
                }
            }
            return allSmokeTest;
        }
        public static string GetManagedId()
        {
            if (Regex.Match(Environment.GetEnvironmentVariable("COMPUTERNAME"), "DotNetPatchTest", RegexOptions.IgnoreCase).Success)
            {
                return ConfigurationManager.AppSettings["gofxservinfra01ManagedId"];
            }
            return string.Empty;
        }
        public List<string> GetAllRelatedWIByWI()
        {
            List<string> WIids = new List<string>();
            using (var client = new HttpClient())
            {
                string WIId = TFSItem.ID.ToString();
                string clientId = ConfigurationManager.AppSettings["ManagedIdentityClientId"];
                string pat = KeyVaultAccess.GetGoFXKVSecret("PATTokenToTFS", GetManagedId());
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(":" + pat)));

                string urlWithQuery = $"https://vstfdevdiv.corp.microsoft.com/DevDiv/DevDiv%20Servicing/_apis/wit/workItems/{WIId}?$expand=relations";
                HttpResponseMessage response = Task.Run(() => client.GetAsync(urlWithQuery)).Result;

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    string content = Task.Run(() => response.Content.ReadAsStringAsync()).Result;
                    JObject workItem = JObject.Parse(content);

                    // Get the System.Title value
                    string title = (string)workItem["fields"]["System.Title"];
                    string OSName = (string)workItem["fields"]["Microsoft.VSTS.Dogfood.Environment"];

                    JArray relations = (JArray)workItem["relations"];
                    List<string> relatedUrls = new List<string>();
                    foreach (JObject relation in relations)
                    {
                        if ((string)relation["rel"] == "System.LinkTypes.Related")
                        {
                            relatedUrls.Add((string)relation["url"]);
                        }
                    }
                    foreach (string url in relatedUrls)
                    {
                        HttpResponseMessage response1 = Task.Run(() => client.GetAsync(url)).Result;
                        if (response1.IsSuccessStatusCode)
                        {
                            string content1 = Task.Run(() => response1.Content.ReadAsStringAsync()).Result;
                            JObject workItem1 = JObject.Parse(content1);
                            string title1 = (string)workItem1["fields"]["System.Title"];
                            string OSName1 = (string)workItem1["fields"]["Microsoft.VSTS.Dogfood.Environment"];
                            if (title1.Contains("Parent") && OSName == OSName1)
                            {
                                int startIndex = url.LastIndexOf("workItems/") + "workItems/".Length;
                                string workItemId = url.Substring(startIndex);

                                workItemId = workItemId.Split(' ')[0];
                                workItemId = workItemId.Split('&')[0];
                                string parentUrl = $"https://vstfdevdiv.corp.microsoft.com/DevDiv/DevDiv%20Servicing/_apis/wit/workItems/{workItemId}?$expand=relations";
                                HttpResponseMessage response2 = Task.Run(() => client.GetAsync(parentUrl)).Result;
                                if (response2.IsSuccessStatusCode)
                                {
                                    string content2 = Task.Run(() => response2.Content.ReadAsStringAsync()).Result;
                                    JObject workItem2 = JObject.Parse(content2);
                                    JArray parentRelations = (JArray)workItem2["relations"];
                                    List<string> parentRelatedUrls = new List<string>();
                                    string WIID;
                                    foreach (JObject relation in parentRelations)
                                    {
                                        if ((string)relation["rel"] == "System.LinkTypes.Related")
                                        {
                                            parentRelatedUrls.Add((string)relation["url"]);
                                            int startIndex1 = relation["url"].ToString().LastIndexOf("workItems/") + "workItems/".Length;

                                            if (startIndex1 > "workItems/".Length)
                                            {
                                                int endIndex = relation["url"].ToString().IndexOf('/', startIndex1);
                                                if (endIndex == -1)
                                                {
                                                    endIndex = relation["url"].ToString().Length;
                                                }

                                                WIID = relation["url"].ToString().Substring(startIndex1, endIndex - startIndex1);
                                                WIids.Add(WIID);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return WIids;
        }
        /// <summary>
        /// new function to download patch Modify by jc
        /// </summary>
        public List<string> DownloadPackages(WorkItem item, bool flag)
        {
            List<string> packages = new List<string>();
            WorkItemHelper helper = new WorkItemHelper(item);
            int exitCode = 0;
            string localDownloadLocation = Path.Combine(ConfigurationManager.AppSettings["DownloadPath"], $"KB{helper.KBNumber}");
            string downloadRoots = string.Empty;
            if (!String.IsNullOrEmpty(item["Drop Patch Location"].ToString()))
            {
                packages.Add(Path.Combine(localDownloadLocation, $"{item["Drop Patch Location"].ToString()}\\{item["Patch Name"].ToString()}"));
            }
            if (!String.IsNullOrEmpty(item["Drop Patch Location X64"].ToString()))
            {
                packages.Add(Path.Combine(localDownloadLocation, $"{item["Drop Patch Location X64"].ToString()}\\{item["Patch Name X64"].ToString()}"));
            }
            if (!String.IsNullOrEmpty(item["Drop Patch Location ARM64"].ToString()))
            {
                packages.Add(Path.Combine(localDownloadLocation, $"{item["Drop Patch Location ARM64"].ToString()}\\{item["Patch Name ARM64"].ToString()}"));
            }
            DropHelper dropHelper = new DropHelper();
            //MsuDropObject msuDropObject = new MsuDropObject(item["Drop Name X64"].ToString());
            MsuDropObject msuDropObject = new MsuDropObject($"NetFxServicing/KB/{helper.KBNumber}");
            var jsonStr = dropHelper.GetDropInfoJsonStr(msuDropObject);
            List<DropInfo> drops = dropHelper.DeserializeJsonStr(jsonStr);
            drops.Sort((a, b) => b.CreatedDateUtc.CompareTo(a.CreatedDateUtc));

            if (helper.KBNumber == "5037929")
            {
                msuDropObject = new MsuDropObject(drops[1].Name);
            }
            else if (!flag)
            {
                string dro = FindLcu(drops);
                msuDropObject = new MsuDropObject(dro);
            }
            else
            {
                msuDropObject = new MsuDropObject(drops[0].Name);
            }

            if (dropHelper.DownloadPackage(msuDropObject, localDownloadLocation, $" -r {downloadRoots}", out exitCode))
            {
                foreach (string root in downloadRoots.Split(';'))
                {
                    packages.Add(Path.Combine(localDownloadLocation, root));
                }
            }
            else
            {
                throw new Exception($"Failed to download Patches from DevDiv cloud. Exit code: {exitCode}");
            }
            return packages;
        }

        /// <summary>
        /// find lcu drop
        /// </summary>
        /// <param name="drops"></param>
        /// <returns></returns>
        private string FindLcu(List<DropInfo> drops)
        {
            string dropName = drops[0].Name;
            string pattern = @"[0-9]+B";

            string month = dropName.Substring(dropName.IndexOf('.') + 1, 3);
            int num = 0;
            foreach (var dropInfo in drops)
            {
                //num = int.Parse(dropInfo.Name.Substring(dropInfo.Name.IndexOf('.')+1, 2));
                MatchCollection match = Regex.Matches(dropInfo.Name, pattern);
                if (match.Count > 0)
                {
                    if (month != match[0].Value)
                    {
                        dropName = dropInfo.Name;
                        break;
                    }
                }


            }
            return dropName;
        }

        /// <summary>
        /// Get all packages if it exist
        /// </summary>
        /// <param name="archList"></param>
        /// <returns></returns>
        private List<string> GetAllArch(List<string> archList)
        {
            if (TFSItem.GetPatchName(Architecture.AMD64).ToLower().Contains("x64"))
                archList.Add("amd64");
            if (TFSItem.GetPatchName(Architecture.X86).ToLower().Contains("x86"))
                archList.Add("x86");
            if (TFSItem.GetPatchName(Architecture.ARM64).ToLower().Contains("arm64"))
                archList.Add("arm64");
            return archList;
        }

        /// <summary>
        /// Check if TFS work item has everything that needed for test
        /// </summary>
        private void CheckTFSItem()
        {
            string[] archs = TFSItem.OSArchitecture.ToLowerInvariant().Split(new char[] { ';', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();

            _supportedArchs = new List<Architecture>();

            if (archs.Contains("x86") || archs.Contains("X86"))
                _supportedArchs.Add(Architecture.X86);
            if (archs.Contains("x64") || archs.Contains("X64") || archs.Contains("amd64") || archs.Contains("AMD64"))
                _supportedArchs.Add(Architecture.AMD64);
            if (archs.Contains("arm") || archs.Contains("ARM"))
                _supportedArchs.Add(Architecture.ARM);
            // IA64 is not supported now
            if (archs.Contains("arm64") || archs.Contains("ARM64"))
                _supportedArchs.Add(Architecture.ARM64);
            if (TFSItem.KBNumber.ToString() == "5018210")
            {
                foreach (Architecture arch in Enum.GetValues(typeof(Architecture)))
                {
                    if (!_supportedArchs.Contains(arch) && arch != Architecture.IA64 && !String.IsNullOrEmpty(TFSItem.GetPatchLocation(arch)))
                    {
                        _supportedArchs.Add(arch);
                    }
                }


            }
            //Sometimes TFS Architecture field may not be correct

            if (_supportedArchs.Count == 0)
            {
                sb.AppendLine("TFS [Architecture] field does not have expected value. Expect: x86 and x64");
            }
            if (TFSItem.KBNumber.ToString() == "5018210")
            {
                foreach (Architecture arch in _supportedArchs)
                {
                    string patchPath = TFSItem.GetPatchFullPath(arch);
                    if (String.IsNullOrEmpty(patchPath))
                        sb.AppendLine(String.Format("TFS doesn't have {0} patch location", arch.ToString()));
                }
            }

            // Try to read expected binaries from Notes field
            ReadBinariesFromTFSNotes();
            if (String.IsNullOrEmpty(StrExpectedBinaries) && !String.IsNullOrEmpty(TFSItem.BaseBuildNumber))
            {
                StaticLogWriter.Instance.logMessage("Failed to find binaries info from TFS Notes field, calling submitpackagerequest.exe to generate it");

                //Call command line to generate binaries to TFS
                string toolPath = ConfigurationManager.AppSettings["SubmitPackageToolPath"];
                string toolArg = ConfigurationManager.AppSettings["SubmitPackageToolArg"];
                Helper.Utility.ExecuteCommandSync(toolPath, String.Format(toolArg, TFSItem.ID), 60 * 60 * 1000);

                // Re-get the TFS WI object
                WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(TFSItem.ID, THTestProcess.TFSServerURI);
                TFSItem = new WorkItemHelper(wi);

                // Read Notes field again
                ReadBinariesFromTFSNotes();
            }

            if (String.IsNullOrEmpty(StrExpectedBinaries))
            {
                sb.AppendLine("Could not find expected binaries list from TFS [Notes] field.");
                sb.AppendLine(StrExpectedBinariesSample);
            }

            if (sb.Length > 0)
                throw new Exception(sb.ToString());
        }

        private void ReadBinariesFromTFSNotes()
        {
            StrExpectedBinaries = String.Empty;
            NotExpectedBinaryList = null;

            if (!String.IsNullOrEmpty(TFSItem.Notes))
            {
                string[] lines = TFSItem.Notes.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string temp = line.Trim();
                    Regex rg = new Regex(@"<.*?>"); //Remove html tags
                    temp = rg.Replace(temp, String.Empty);

                    if (!String.IsNullOrEmpty(temp))
                    {
                        //Current sample string: Binaries Affected:[(system.dll-2.0.50727.8744),(system.dll-4.6.1538.0)]

                        int idx = temp.IndexOf("Binaries Affected:[(");
                        if (idx >= 0)
                        {
                            temp = temp.Substring(idx + 19);
                            idx = temp.IndexOf(']');
                            if (idx > 0)
                            {
                                temp = temp.Substring(0, idx);

                                if (CheckBinariesAffectedFormat(temp))
                                {
                                    StrExpectedBinaries = temp;
                                }
                            }
                        }
                        else if ((idx = temp.IndexOf("Binaries Not Expected:[")) >= 0) // new feature, not support binaries
                        {
                            temp = temp.Substring(idx + 23);
                            idx = temp.IndexOf(']');
                            temp = temp.Substring(0, idx);

                            NotExpectedBinaryList = temp.ToLowerInvariant().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        }
                    }
                }
            }
        }


        private bool CheckBinariesAffectedFormat(string binaries)
        {
            List<string> expectedBinaries = binaries.Split(new char[] { ',', ';' }).ToList();
            for (int i = expectedBinaries.Count - 1; i >= 0; --i)
            {
                if (String.IsNullOrEmpty(expectedBinaries[i].Trim()))
                    expectedBinaries.RemoveAt(i);
            }

            if (expectedBinaries.Count == 0)
                return false;

            foreach (string b in expectedBinaries)
            {
                string[] splitStrings = b.Split('-');
                if (!(splitStrings.Length == 2 || (splitStrings.Length == 3 && splitStrings[0].ToLowerInvariant() == "(presentationframework")))
                    return false;
            }

            return true;
        }


        /// <summary>
        /// Get latest build of package from the package location on TFS
        /// </summary>
        /// <param name="tfsPackagePath">tfs package location</param>
        /// <returns></returns>
        private string GetLatestBuildPath(string tfsPackagePath, ref string version)
        {
            string latestPackagePath = null;
            if (String.IsNullOrEmpty(tfsPackagePath))
                return null;
            if (tfsPackagePath.StartsWith(PackagePathBase))
            {
                latestPackagePath = tfsPackagePath;
                string[] msuFiles = Directory.GetFiles(latestPackagePath, "*.msu");

                if (msuFiles.Length > 0)
                {

                    latestPackagePath = msuFiles.Last();
                }

            }
            else
            {
                latestPackagePath = GetLatestPathWithVsufilePattern(tfsPackagePath, ref version);
            }

            //Try to get from \\winsehotfix
            //string latestPackagePath = GetLatestPathWithWinseHotfixPattern(tfsPackagePath, ref version);

            //Try to get from \\vsufile
            //if (String.IsNullOrEmpty(latestPackagePath))
            //{
            //   latestPackagePath = GetLatestPathWithVsufilePattern(tfsPackagePath, ref version);
            //}
            //if (String.IsNullOrEmpty(latestPackagePath) && Directory.Exists(tfsPackagePath))
            //{
            //    string[] msuFiles = Directory.GetFiles(tfsPackagePath, "*.msu");
            //    latestPackagePath = msuFiles.Length > 0 ? msuFiles.Last() : null;
            //}

            return latestPackagePath;
        }
        //private string GetLatestBuildPath(string tfsPackagePath, ref string version)
        //{
        //    if (String.IsNullOrEmpty(tfsPackagePath))
        //        return null;

        //    //Try to get from \\winsehotfix
        //    string latestPackagePath = GetLatestPathWithWinseHotfixPattern(tfsPackagePath, ref version);

        //    //Try to get from \\vsufile
        //    if (String.IsNullOrEmpty(latestPackagePath))
        //    {
        //        latestPackagePath = GetLatestPathWithVsufilePattern(tfsPackagePath, ref version);
        //    }

        //    //if (String.IsNullOrEmpty(latestPackagePath) && Directory.Exists(tfsPackagePath))
        //    //{
        //    //    string[] msuFiles = Directory.GetFiles(tfsPackagePath, "*.msu");
        //    //    latestPackagePath = msuFiles.Length > 0 ? msuFiles.Last() : null;
        //    //}

        //    return latestPackagePath;
        //}
        public void BuildLocalWinCloudPathDrop()
        {
            if (TFSItem.WindowsPackagingId == 0)
            {
                throw new Exception($"No DropName exists in work item, and the Windows Packaging ID value in work item {TFSItem.ID} is invalid.  Can't get packages.");
            }
            localWinCloudPackages = DownloadPackagesForJob(TFSItem.WindowsPackagingId, TFSItem.KBNumber);
        }

        /// <summary>
        /// Get Latest package path from location like: \\winsehotfix\hotfixes\Windows10\RS1\RTM\KB3206632\V1.008\free\NEU\x86\
        /// </summary>
        private string GetLatestPathWithWinseHotfixPattern(string tfsPackagePath, ref string version)
        {
            Regex rx = new Regex(@"\\V\d\.\d\d\d\\");
            Match match = rx.Match(tfsPackagePath);

            if (!match.Success)
                return null;

            string parentFolder = tfsPackagePath.Substring(0, match.Index);
            string subPath = tfsPackagePath.Substring(match.Index + match.Length);

            if (!Directory.Exists(parentFolder))
                return null;

            string[] subFolders = Directory.GetDirectories(parentFolder, "V?.???", SearchOption.TopDirectoryOnly);

            if (subFolders.Length == 0)
                return null;

            version = Path.GetFileName(subFolders.Last());

            string latestPackagePath = Path.Combine(subFolders.Last(), subPath);
            if (!Directory.Exists(latestPackagePath))
                return null;

            string[] msuFiles = Directory.GetFiles(latestPackagePath, "*.msu");

            return msuFiles.Length > 0 ? msuFiles.Last() : null;
        }

        /// <summary>
        /// Get Latest package path from location like: \\vsufile\patches\sign\KB3211677\01636.08\x86\ENU
        /// </summary>
        private string GetLatestPathWithVsufilePattern(string tfsPackagePath, ref string version)
        {
            Regex rx = new Regex(@"\\\d\d\d\d\d\.\d\d\\");
            Match match = rx.Match(tfsPackagePath);

            if (!match.Success)
                return null;

            string parentFolder = tfsPackagePath.Substring(0, match.Index);
            string subPath = tfsPackagePath.Substring(match.Index + match.Length);

            if (!Directory.Exists(parentFolder))
                return null;

            string[] subFolders = Directory.GetDirectories(parentFolder, "?????.??", SearchOption.TopDirectoryOnly);
            if (subFolders.Length == 0)
                return null;

            // for VSUFILE like path, get package from the latest created folder
            string newestPath = GetLatestCreatedPath(subFolders);
            version = Path.GetFileName(newestPath);

            string latestPackagePath = Path.Combine(newestPath, subPath);
            if (!Directory.Exists(latestPackagePath))
                return null;

            string[] msuFiles = Directory.GetFiles(latestPackagePath, "*.msu");

            return msuFiles.Length > 0 ? msuFiles.Last() : null;
        }

        private string GetLatestCreatedPath(string[] paths)
        {
            if (paths.Length == 1)
                return paths[0];

            DateTime dt = new DateTime();
            string newestPath = null;

            foreach (var path in paths)
            {
                DirectoryInfo di = new DirectoryInfo(path);

                if (newestPath == null || DateTime.Compare(di.CreationTime, dt) > 0)
                {
                    newestPath = path;
                    dt = di.CreationTime;
                }
            }

            return newestPath;
        }

        private void GetExpectedBinariesAndVersions()
        {
            ExpectedBinariesVersions = new Dictionary<string, Dictionary<string, string>>();

            string[] binariesVersions = StrExpectedBinaries.Split(new char[] { ',', ';' });

            foreach (string item in binariesVersions)
            {
                if (String.IsNullOrEmpty(item.Trim()))
                    continue;

                string fileAndVersion = item.Trim().Trim(new char[] { '(', ')' });
                int splitPos = fileAndVersion.LastIndexOf('-');
                string fileName = fileAndVersion.Substring(0, splitPos);
                string fileVersion = fileAndVersion.Substring(splitPos + 1);

                // 'none' means no expected binaries to be verified
                // there will be not expected binaries for verification
                if (fileName == "none" && fileVersion == "none")
                    return;

                string sku = HelperMethods.GetBinarySKUFromFileVersion(this, fileVersion);
                if (String.IsNullOrEmpty(sku))
                {
                    sku = TFSItem.SKU;
                }

                if (!ExpectedBinariesVersions.ContainsKey(sku))
                {
                    Dictionary<string, string> dictBinaries = new Dictionary<string, string>();
                    ExpectedBinariesVersions.Add(sku, dictBinaries);
                }

                if (!ExpectedBinariesVersions[sku].ContainsKey(fileName.ToLowerInvariant()))
                {
                    ExpectedBinariesVersions[sku].Add(fileName.ToLowerInvariant(), fileVersion);
                }
            }

            //Expand the file list to include all files in same patch family
            ExpandExpectedFilesWithPF();
        }

      
        private void ExpandExpectedFilesWithPF()
        {
            Dictionary<string, Dictionary<string, string>> dictExpandedFiles = new Dictionary<string, Dictionary<string, string>>();

            foreach (KeyValuePair<string, Dictionary<string, string>> kvSku in ExpectedBinariesVersions)
            {
                foreach (KeyValuePair<string, string> kvFiles in kvSku.Value)
                {
                    if (!dictExpandedFiles.ContainsKey(kvSku.Key))
                        dictExpandedFiles.Add(kvSku.Key, new Dictionary<string, string>());

                    // Comment out this code as this logic may miss files
                    //if (dictExpandedFiles[kvSku.Key].ContainsKey(kvFiles.Key))
                    //    continue;

                    List<string> allFilesInSamePf = PatchFamily.GetAllFilesInSamePF(kvFiles.Key, kvSku.Key, TFSItem.GetPatchName(Architecture.AMD64));
                    if (kvFiles.Key == "vbc.exe" && allFilesInSamePf.Contains("cvtresui.dll"))
                    {

                        allFilesInSamePf.Remove("cvtresui.dll");
                    }
                    if (allFilesInSamePf != null && allFilesInSamePf.Count > 0)
                    {
                        foreach (string f in allFilesInSamePf)
                        {
                            //if sku version is 4.7+, remove wpffontcache_v0400.exe and wpftxt_v0400.dll
                            if (String.Compare("4.7", kvSku.Key) < 0 && (f == "wpffontcache_v0400.exe" || f == "wpftxt_v0400.dll"))
                                continue;
                            //if sku version is 2.0 3.5, remove cvtresui.dll
                            //if ((kvSku.Key.Equals("2.0") || kvSku.Key.Equals("3.5")) && (f == "cvtresui.dll"))
                            //    continue;

                            // This file is already added
                            if (dictExpandedFiles[kvSku.Key].ContainsKey(f))
                                continue;

                            // Specify a version
                            string version = String.Empty;
                            if (kvSku.Value.ContainsKey(f))
                            {
                                version = kvSku.Value[f];
                            }
                            else if (f.EndsWith(".exe") || f.EndsWith(".dll") || f.EndsWith(".tlb") || f.EndsWith(".mui"))
                            {
                                version = kvFiles.Value;
                            }

                            dictExpandedFiles[kvSku.Key].Add(f, version);
                        }
                    }

                    //If search PF failed, then add only current file
                    if (allFilesInSamePf == null || !allFilesInSamePf.Contains(kvFiles.Key))
                    {
                        dictExpandedFiles[kvSku.Key].Add(kvFiles.Key, kvFiles.Value);
                    }
                }
            }

            ExpectedBinariesVersions = dictExpandedFiles;
        }

        private void GetActualBinaries(PatchInformation patchInfo)
        {
            if (String.IsNullOrEmpty(patchInfo.PatchLocation))
                return;
            //Copy remote package to local and extract it
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {

                if (!String.IsNullOrEmpty(Item["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location X64"].ToString()) ||
                    !String.IsNullOrEmpty(Item["Drop Name"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location"].ToString()))
                {
                    patchInfo.ExtractLocation = TFSItem.GetExpandPackage(patchInfo.Arch);
                }
                else
                {
                    patchInfo.ExtractLocation = Extraction.ExtractPatchToPath(patchInfo.PatchLocation);
                }

            }

            //Collect patch payload info
            patchInfo.ActualBinaries = CBSPayloadAnalyzer.GetPatchDotNetBinaries(this, patchInfo.ExtractLocation, patchInfo.Arch);
        }
        private void GetActualBinariesForUpgradePackage(PatchInformation patchInfo, WorkItem workItem, Dictionary<string, Dictionary<string, string>> ExpectedBinaries)
        {
            if (String.IsNullOrEmpty(patchInfo.PatchLocation))
                return;
            //Copy remote package to local and extract it
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {

                if (!String.IsNullOrEmpty(workItem["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(workItem["Drop Patch Location X64"].ToString()) ||
                    !String.IsNullOrEmpty(workItem["Drop Name"].ToString()) && !String.IsNullOrEmpty(workItem["Drop Patch Location"].ToString()))
                {
                    patchInfo.ExtractLocation = TFSItem.GetExpandPackageForUpgradePackage(patchInfo.Arch, workItem);
                }
                else
                {
                    patchInfo.ExtractLocation = Extraction.ExtractPatchToPath(patchInfo.PatchLocation);
                }

            }
            
            //Collect patch payload info
            patchInfo.ActualBinaries = CBSPayloadAnalyzer.GetPatchDotNetBinariesForUpgradePackage(this,workItem["SKU"].ToString(), patchInfo.ExtractLocation, patchInfo.Arch, ExpectedBinaries, workItem);
        }

        private bool IsThisBuildTested(string x64PatchLocation)
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var results = from r in dataContext.TTHTestRecords
                              where r.Active &&
                              r.TFSID == TFSItem.ID &&
                              String.Compare(r.X64PatchLocation, x64PatchLocation, StringComparison.InvariantCultureIgnoreCase) == 0
                              select r;
                return results.Count() > 0 ? true : false;
            }
        }

        private void AddDBRecords(string flagLocation)
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                //Add a new record to TTHTestRecord
                TTHTestRecord record = new TTHTestRecord();
                record.TFSID = TFSItem.ID;
                record.X64PatchLocation = flagLocation;
                record.TestStartDate = DateTime.Now;
                record.RuntimeStatus = (int)Helper.RunStatus.NotStarted;
                record.LastModifiedDate = record.TestStartDate;
                record.Active = true;
                record.TFSTitle = TFSItem.Title;

                dataContext.TTHTestRecords.InsertOnSubmit(record);

                //Update TTHTestExceptions, mark exception records as inactive
                var records = from r in dataContext.TTHTestExceptions
                              where r.TFSID == TFSItem.ID && r.Active
                              select r;

                foreach (var r in records)
                {
                    r.Active = false;
                }

                dataContext.SubmitChanges();

                _thTestID = record.ID;
            }
        }

        /// <summary>
        /// Update TFS patch locations 
        /// </summary>
        private void UpdateTFSWithLatestPatchLocation()
        {
            try
            {
                int temp = 0;
                foreach (var arch in _supportedArchs)
                {
                    if (!Patches[arch].PatchLocation.Equals(TFSItem.GetPatchFullPath(arch)))
                    {
                        TFSItem.SetPatchName(arch, Path.GetFileName(Patches[arch].PatchLocation));
                        TFSItem.SetPatchLocation(arch, Path.GetDirectoryName(Patches[arch].PatchLocation));
                        temp++;
                    }
                }

                if (temp > 0)
                    TFSItem.SaveWorkItemHelper(THTestProcess.TFSServerURI);
            }
            catch (Exception ex)
            {
                StaticLogWriter.Instance.logError("Exception caught when updating patch location to TFS: " + ex.Message);
            }

        }

        private void SendExceptionMail(Exception ex)
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var records = from r in dataContext.TTHTestExceptions
                              where r.TFSID == TFSItem.ID &&
                              r.Active &&
                              String.Compare(ex.Message, r.ExceptionMsg, StringComparison.InvariantCultureIgnoreCase) == 0
                              select r;

                if (records.Count() > 0)
                    return;

                //If there is no same exception recorded, send an exception mail
                MailHelper helper = new MailHelper();
                helper.SendExceptionMail(TFSItem, ex);

                TTHTestException exceptionRec = new TTHTestException();
                exceptionRec.TFSID = TFSItem.ID;
                exceptionRec.ExceptionMsg = ex.Message;
                exceptionRec.Active = true;

                dataContext.TTHTestExceptions.InsertOnSubmit(exceptionRec);
                dataContext.SubmitChanges();
            }
        }

        private void DeleteExtractPath()
        {
            if (Patches != null && Patches.Count > 0)
            {
                foreach (var pt in Patches)
                {
                    if (!String.IsNullOrEmpty(pt.Value.ExtractLocation) &&
                        Directory.Exists(pt.Value.ExtractLocation))
                        Extraction.DeleteExtractLocation(pt.Value.ExtractLocation);
                }
            }
        }

        private void GetLCUPatchPath()
        {
            // Product Refresh has different logic to find LCU patch
            if (TFSItem.Custom01 == "Product_Refresh")
            {
                IsProductRefresh = true;
                SimplePatch = false;
                GetLCUPathForProductRefresh();

                return;
            }

            SimplePatch = true;

            string lcuKB = TFSItem.LCUKBArticle.Trim();
            if (String.IsNullOrEmpty(lcuKB) || lcuKB == "TBD")
                return;

            // KB article should be a number
            int nKBNum;
            if (!Int32.TryParse(lcuKB, out nKBNum))
                return;

            // If KB number is too small, it must be illegal
            if (nKBNum / 1000000 == 0)
                return;
            WorkItemCollection wc = Connect2TFS.Connect2TFS.QueryWorkItemByKBNumber(THTestProcess.TFSServerURI, lcuKB);
            // If LCU KB doesn't have TFS WI (usually seen in WSD LCU)
            if (wc.Count == 0)
            {
                //StaticLogWriter.Instance.logMessage($"No LCU work item exists for KB number {lcuKB}, trying lcu job id {TFSItem.LCUWindowsPackagingId}");
                //TryGetLCUPathForPatchWithoutTFS(lcuKB);
                TryGetLCUPathForPatchWithoutTFS();
                return;
            }

            // search from last item
            for (int i = wc.Count - 1; i >= 0; --i)
            {
                WorkItemHelper wiHelper = new WorkItemHelper(wc[i]);

                //if (wiHelper.Title.Contains("2023.06 OOB") || wiHelper.WindowsPackagingId == 0 || string.IsNullOrEmpty(wiHelper.GetPatchName(Architecture.AMD64))){
                //    continue;
                //}
                if (string.IsNullOrEmpty(wiHelper.GetPatchName(Architecture.AMD64)))
                {
                    continue;
                }

                string patchPath = Patches[_supportedArchs[0]].PatchLocation;
                using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                {
                    if (!String.IsNullOrEmpty(Item["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location X64"].ToString()) ||
                        !String.IsNullOrEmpty(Item["Drop Name"].ToString()) && !String.IsNullOrEmpty(Item["Drop Patch Location"].ToString()))
                    {
                        DownloadPackages(wc[i], true);
                    }
                    else
                    {
                        List<string> lcupackages = DownloadPackagesForJob(wiHelper.WindowsPackagingId, wiHelper.KBNumber);
                    }

                }

                String[] multiOSes = { "Windows 10 20H1", "Windows 10 20H2", "Windows 10 21H1", "Windows 10 21H2", "Windows 10 22H2" };
                // LCU KB should have same OS and patch technology
                //if (!multiOSes.Contains(wiHelper.OSInstalled + " " + wiHelper.OSSPLevel) && Utility.CompareOS(wiHelper.OSInstalled, wiHelper.OSSPLevel, TFSItem.OSInstalled, TFSItem.OSSPLevel) != 0 ||
                //    TFSItem.PatchTechnology != wiHelper.PatchTechnology)
                //    continue;

                bool allArchAvailable = true;
                foreach (Architecture arch in _supportedArchs)
                {
                    //Modify by jc
                    if (!File.Exists(wiHelper.GetPatchDownloadLocation(arch)))
                    //if (String.IsNullOrEmpty(wiHelper.GetPatchFullPath(arch)))
                    {
                        allArchAvailable = false;
                        break;
                    }
                }
                if (allArchAvailable)
                {
                    foreach (Architecture arch in _supportedArchs)
                    {
                        //Patches[arch].LCUPatchPath = wiHelper.GetPatchFullPath(arch);
                        //Modify by jc
                        Patches[arch].LCUPatchPath = wiHelper.GetPatchDownloadLocation(arch);
                    }

                    SimplePatch = false;

                    break;
                }
            }
        }




        private void GetLCUExtractLocation()
        {
            if (IsTraditionalWin10LCU)
            {
                foreach (Architecture arch in _supportedArchs)
                {
                    if (!String.IsNullOrEmpty(Patches[arch].LCUPatchPath) && Patches[arch].LCUPatchPath.Contains("Packages"))
                    {
                        Patches[arch].LCUExtractLocation = Extraction.ExtractPatchToPath(Patches[arch].LCUPatchPath);
                    }
                }
            }
        }
        private void GetLCUPathForProductRefresh()
        {
            // path is like \\vsufile\patches\sign\KB4486153\04311.09\x86\ENU
            string patchLocation;
            string lcuLoc = TFSItem.GetPatchDownloadLocation(Architecture.X86);
            //if (TFSItem.KBNumber == "5018210")
            //{
            //    patchLocation = TFSItem.GetPatchLocation(Architecture.X86);        
            //}
            //else
            //{
            //    patchLocation = TFSItem.GetPatchLocation(Architecture.AMD64);
            //}
            DownloadPackages(Item, false);
            patchLocation = lcuLoc.Substring(0, lcuLoc.LastIndexOf("\\"));
            string root = Path.GetDirectoryName(patchLocation);
            root = Path.GetDirectoryName(root);

            string version = Path.GetFileName(root);
            string versionPrefix = version.Substring(0, version.IndexOf('.'));

            var folders = Directory.GetDirectories(Path.GetDirectoryName(root));
            string previousVersion = String.Empty;
            for (int i = folders.Length - 1; i >= 0; --i)
            {
                // skip builds that have DoNotUse.txt
                if (File.Exists(Path.Combine(folders[i], "DoNotUse.txt")))
                    continue;

                previousVersion = Path.GetFileName(folders[i]);
                if (previousVersion[0] == '0' && !previousVersion.StartsWith(versionPrefix))
                {
                    break; //find
                }
            }

            foreach (Architecture arch in _supportedArchs)
            {
                Patches[arch].LCUPatchPath = TFSItem.GetPatchDownloadLocation(arch).Replace(version, previousVersion);
            }
        }

        private void TryGetLCUPathForPatchWithoutTFS(string lcuKB)
        {
            // path is like '\\winsehotfix\hotfixes\Windows10\TH1\RTM\KB5008230\V1.004\free\NEU\X64\Windows10.0-KB5008230-x64.msu'
            string patchPath = Patches[_supportedArchs[0]].PatchLocation;
            if (!patchPath.ToLower().StartsWith(@"\\winsehotfix"))
                return;

            patchPath = Path.GetDirectoryName(patchPath);
            patchPath = Path.GetDirectoryName(patchPath);

            Regex rx = new Regex(@"\\V\d\.\d\d\d\\");
            Match match = rx.Match(patchPath);
            if (!match.Success)
                return;

            string parentFolder = patchPath.Substring(0, match.Index);
            string subPath = patchPath.Substring(match.Index + match.Length);

            parentFolder = Path.GetDirectoryName(parentFolder);
            parentFolder = Path.Combine(parentFolder, "KB" + lcuKB);

            if (!Directory.Exists(parentFolder))
                return;

            string[] subFolders = Directory.GetDirectories(parentFolder, "V?.???", SearchOption.TopDirectoryOnly);
            if (subFolders.Length == 0)
                return;

            string latestPackagePath = Path.Combine(subFolders.Last(), subPath);
            if (!Directory.Exists(latestPackagePath))
                return;

            foreach (var arch in _supportedArchs)
            {
                patchPath = String.Empty;
                if (arch == Architecture.AMD64)
                    patchPath = Path.Combine(latestPackagePath, "X64");
                else
                    patchPath = Path.Combine(latestPackagePath, arch.ToString());

                string[] msuFiles = Directory.GetFiles(patchPath, "*.msu");
                if (msuFiles.Length > 0)
                {
                    Patches[arch].LCUPatchPath = msuFiles.Last();
                    SimplePatch = false;
                }
            }
        }
        private void TryGetLCUPathForPatchWithoutTFS()
        {
            //Use the lcu job id to download the packages

            string patchPath = Patches[_supportedArchs[0]].PatchLocation;
            if (patchPath.ToLower().StartsWith(@"\\vsufile"))
                return;

            if (TFSItem.LCUWindowsPackagingId == 0)
            {
                throw new Exception($"LCU Windows Packaging ID for work item {TFSItem.ID} is invalid or empty.  Cannot get LCU packages.");
            }

            List<string> lcupackages = DownloadPackagesForJob(TFSItem.LCUWindowsPackagingId,TFSItem.KBNumber);

            foreach (var arch in _supportedArchs)
            {
                string patharch = arch.ToString();
                if (patharch == "AMD64")
                {
                    patharch = "X64";
                }
                Patches[arch].LCUPatchPath = lcupackages.Where(p => Regex.Match(p, patharch, RegexOptions.IgnoreCase).Success).FirstOrDefault();
                SimplePatch = false;
            }
        }
        private void TryGetLCUPathForPatchWithoutTFS1()
        {
            //Use the lcu job id to download the packages

            string patchPath = Patches[_supportedArchs[0]].PatchLocation;
            if (patchPath.ToLower().StartsWith(@"\\vsufile"))
                return;

            if (TFSItem.LCUWindowsPackagingId == 0)
            {
                throw new Exception($"LCU Windows Packaging ID for work item {TFSItem.ID} is invalid or empty.  Cannot get LCU packages.");
            }

            List<string> lcupackages = DownloadPackagesForJob(TFSItem.LCUWindowsPackagingId,TFSItem.KBNumber);

            foreach (var arch in _supportedArchs)
            {
                string patharch = arch.ToString();
                if (patharch == "AMD64")
                {
                    patharch = "X64";
                }
                Patches[arch].LCUPatchPath = lcupackages.Where(p => Regex.Match(p, patharch, RegexOptions.IgnoreCase).Success).FirstOrDefault();
                SimplePatch = false;
            }
        }
        private void SetEnvironmentVariable()
        {

            string SetEnvironmentVariableCommandPath = ConfigurationManager.AppSettings["SetEnvironmentVariable"];

        }
        public List<string> DownloadPackagesForJob(int jobid,string kbNumber)
        {
            List<string> downloadedpackages = new List<string>();
            if (winCloudPackage == null)
            {
                winCloudPackage = new WinCloudPackage();
            }
            //if (string.IsNullOrEmpty(localWinCloudPackagePath))
            //{
            //    //localWinCloudPackagePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            //    localWinCloudPackagePath = Path.Combine(ConfigurationManager.AppSettings["PackagePath"], jobid.ToString());


            //}
            //Modify by JC
            //localWinCloudPackagePath = Path.Combine(ConfigurationManager.AppSettings["PackagePath"], jobid.ToString());
            localWinCloudPackagePath = ConfigurationManager.AppSettings["DownloadPath"];
            NetFxServicing.SASPackageManagerLib.PackageType packagetype = NetFxServicing.SASPackageManagerLib.PackageType.CBS;
            if (TFSItem.PatchTechnology == "MSI")
            {
                packagetype = NetFxServicing.SASPackageManagerLib.PackageType.MSI;
            }
            string targetarch = string.Empty;
            //string kbnumber = TFSItem.KBNumber.ToString();
            string kbnumber = kbNumber;
            string lcukbnumber = TFSItem.LCUKBArticle.ToString();
            downloadedpackages = winCloudPackage.DownloadWindowsPackages(jobid, packagetype, localWinCloudPackagePath);
            //winCloudPackage.DownloadJobArtifact(jobid, "packagemetadata", kbnumber, localWinCloudPackagePath, targetarch);
            //if (!string.IsNullOrWhiteSpace(lcukbnumber))
            //{
            //    winCloudPackage.DownloadJobArtifact(jobid, "packagemetadata", lcukbnumber, localWinCloudPackagePath, targetarch);
            //}
            //CopypackagemetadataFileToPackagePath(downloadedpackages, localWinCloudPackagePath);
            StaticLogWriter.Instance.logMessage($"Downloaded the following packages to the local drop {localWinCloudPackagePath} for windows packaging id {jobid}");
            foreach (string package in downloadedpackages)
            {

                StaticLogWriter.Instance.logMessage(package);
                if (!File.Exists(package)) { throw new Exception($"{package} does not exist.  It did not download properly."); }
                if (new FileInfo(package).Length == 0) { throw new Exception($"Package file {package} is incomplete."); }
            }

            archs = TFSItem.OSArchitecture.ToLowerInvariant().Split(new char[] { ';', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

            //Ensure all expected packages downloaded
            foreach (string expectedarch in archs)
            {
                if (expectedarch == " ")
                    continue;
                string archpackage = downloadedpackages.Where(p => Regex.Match(p, expectedarch, RegexOptions.IgnoreCase).Success).FirstOrDefault();
                if (String.IsNullOrEmpty(archpackage)) { throw new Exception($"Expect package for arch {expectedarch} to be downloaded, but its not in download location {localWinCloudPackagePath}"); }

            }

            return downloadedpackages;
        }
        private void CopypackagemetadataFileToPackagePath(List<string> downloadedpackages, string localWinCloudPackagePath)
        {
            foreach (string package in downloadedpackages)
            {
                string ArchPath = Path.GetDirectoryName(package);
                string arch = Path.GetFileName(ArchPath);
                string PackagePath = Path.GetDirectoryName(ArchPath);
                int index = PackagePath.IndexOf("kb");
                string kbNumber = PackagePath.Substring(index + 2);

                string packagemetadataPath = Path.Combine(localWinCloudPackagePath, kbNumber, arch);
                string[] files = Directory.GetFiles(packagemetadataPath);
                if (files.Count() > 0)
                {
                    foreach (var filename in files)
                    {
                        string file = Path.GetFileName(filename);
                        File.Copy(filename, Path.Combine(ArchPath, file), true);
                    }
                }

            }


        }
        private void CheckLCUPatchValid()
        {
            if (SimplePatch && !String.IsNullOrEmpty(TFSItem.LCUKBArticle.Trim()) && TFSItem.LCUKBArticle.Trim() != "TBD")
            {
                throw new Exception(String.Format("[LCU KB Article] is set to {0} but failed to find out suitable LCU patch", TFSItem.LCUKBArticle));
            }

            if (!SimplePatch)
            {
                foreach (Architecture arch in _supportedArchs)
                {
                    if (String.IsNullOrEmpty(Patches[arch].LCUPatchPath))
                    {
                        throw new Exception(String.Format("Failed to find out patch location for LCU KB{0} {1}", TFSItem.LCUKBArticle, arch));
                    }
                }
            }
        }

        private DataTable GenerateResultSummaryTable()
        {
            DataTable dt = HelperMethods.CreateDataTable("Overall Test Summary",
                new string[] { "NO.", "Case Description", "Result" },
                new string[] { "style=width:15%;text-align:center", "width=65%", "style=width:20%;text-align:center#ResultCol=1" });

            for (int i = 0; i < TestResults.ResultDetails.Count; ++i)
            {
                string name = TestResults.ResultDetails[i].TableName;
                bool warning = false;

                if (name.StartsWith("WARNING: "))
                {
                    name = name.Substring(9);
                    warning = true;
                    TestResults.HasWarning = true;
                }

                if (name.StartsWith("Test Passed: "))
                    name = name.Substring(13);

                if (name.StartsWith("Test Failed: "))
                    name = name.Substring(13);

                DataRow r = dt.NewRow();

                r["NO."] = (i + 1).ToString();
                r["Case Description"] = name;
                r["Result"] = warning ? "Warning" : TestResults.ResultDetailSummaries[i] ? "Pass" : "Fail";

                dt.Rows.Add(r);
            }

            return dt;
        }

        #endregion

        #region Private Methods for runtime test

        private bool KickoffRuntimeTest()
        {
            //Try to inherit runtime test results of previous test
            if (InheritPreviousRuntimeTest())
                return true;

            string matrixName = GetRuntimeMatrixName();
            if (String.IsNullOrEmpty(matrixName))
                return false;

            //Copy packages to shared location
            string sharedPath = CopyPackages();
            LatestPackage(sharedPath);

            //string sharedPath = ConfigurationManager.AppSettings["SharedPath"];
            //Create patch files
            string localPatch = String.Empty;

            foreach (var sa in _supportedArchs)
            {
                GeneratePatchFile(sharedPath, sa, out localPatch);
                CreateVersionVerififcationFile(sharedPath, sa);
                CreateLCUInstallScript(sharedPath, sa);
                InputData InstallSequence = GetRuntimeInstallSequence(sharedPath, sa, localPatch);
                KickOffRuns(sharedPath, sa, matrixName, InstallSequence);
            }

            //Update TH test record
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                TTHTestRecord record = dataContext.TTHTestRecords.Where(r => r.ID == _thTestID).Single();
                record.RuntimeStatus = 1;
                record.LastModifiedDate = DateTime.Now;
                dataContext.SubmitChanges();
            }

            return true;
        }

        private void LatestPackage(string packagePath)
        {
            if(Directory.Exists(packagePath))
            {
                foreach(var arch in _supportedArchs)
                {
                    string latestPath = Path.Combine(packagePath, arch.ToString(), "LastestPackage");
                    Directory.CreateDirectory(latestPath);
                }
                
            }
            
        }

        private string CopyPackages()
        {
            string sharedPath = ConfigurationManager.AppSettings["SharedPath"];
            sharedPath = Path.Combine(sharedPath, TFSItem.KBNumber);
            if (Directory.Exists(sharedPath))
            {
                string[] subFolders = Directory.GetDirectories(sharedPath);
                sharedPath = Path.Combine(sharedPath, subFolders.Length.ToString());
            }
            else
            {
                sharedPath = Path.Combine(sharedPath, "0");
            }

            Directory.CreateDirectory(sharedPath);
            //copy package
            foreach (var sa in _supportedArchs)
            {
                Directory.CreateDirectory(Path.Combine(sharedPath, sa.ToString()));
            }

            return sharedPath;
        }
        private int SubfolderCount()
        {
            string sharedPath = ConfigurationManager.AppSettings["SharedPath"];
            sharedPath = Path.Combine(sharedPath, TFSItem.KBNumber);
            string[] subFolders = Directory.GetDirectories(sharedPath);
            return subFolders.Length;

        }
        //private void GeneratePatchFile(string sharedPath, Architecture arch, out string localCopyCmd)
        //{
        //    localCopyCmd = String.Empty;
        //    sharedPath = Path.Combine(sharedPath, arch.ToString());
        //    //string packageLocation = String.Empty;
        //    //if (arch == Architecture.X86)
        //    //{
        //    //    packageLocation = Path.Combine(sharedPath, Path.GetFileName(TestResults.x86PackageLocation));
        //    //}
        //    //else
        //    //{
        //    //    packageLocation = Path.Combine(sharedPath, Path.GetFileName(TestResults.x64PackageLocation));
        //    //}
        //    string packageLocation = Patches[arch].PatchLocation;
        //    if (packageLocation.StartsWith("F:\\Packages"))
        //    {
        //        packageLocation = packageLocation.Replace("F:\\Packages", @"\\SETUPPATCHTEST\Packages");
        //    }
        //    long size = new FileInfo(packageLocation).Length;

        //    //if patch size is too large, then install/uninstall from local machine to avoid possible network issue
        //    if (size > 314572800)
        //    {
        //        string localPatch = "%systemdrive%\\school\\" + Path.GetFileName(packageLocation);

        //        localCopyCmd = Path.Combine(sharedPath, "CopyPackage.bat");
        //        using (System.IO.TextWriter textWriter = new System.IO.StreamWriter(localCopyCmd, false))
        //        {
        //            textWriter.Write("copy /y {0} {1}", packageLocation, localPatch);
        //            textWriter.Close();
        //        }

        //        packageLocation = localPatch;
        //    }

        //    string patchFilePath = Path.Combine(sharedPath, TFSItem.KBNumber + ".xml");
        //    ProductSkuPackage package = new ProductSkuPackage
        //    {
        //        Name = "CBS",
        //        Path = packageLocation
        //    };
        //    ProductSku sku = new ProductSku
        //    {
        //        Package = new ProductSkuPackage[] { package },
        //        Name = "Patch"
        //    };

        //    Product product = new Product
        //    {
        //        Name = "Patch",
        //        Schema = "PatchSchema.xml",
        //        Common = new ProductCommon(),
        //        Sku = new ProductSku[] { sku }
        //    };
        //    XMLHelper.XmlSerializeToFile(product, patchFilePath, typeof(Product));
        //}
        private void GeneratePatchFile(string sharedPath, Architecture arch, out string localCopyCmd)
        {
            localCopyCmd = String.Empty;
            sharedPath = Path.Combine(sharedPath, arch.ToString());
            //string packageLocation = String.Empty;
            //if (arch == Architecture.X86)
            //{
            //    packageLocation = Path.Combine(sharedPath, Path.GetFileName(TestResults.x86PackageLocation));
            //}
            //else
            //{
            //    packageLocation = Path.Combine(sharedPath, Path.GetFileName(TestResults.x64PackageLocation));
            //}
            string packageLocation = Patches[arch].PatchLocation;
            if (packageLocation.StartsWith("F:\\Packages"))
            {
                packageLocation = packageLocation.Replace("F:\\Packages", @"\\DOTNETPATCHTEST\Packages");
            }
            if (packageLocation.StartsWith("D:\\Packages"))
            {
                packageLocation = packageLocation.Replace("D:\\Packages", @"\\DOTNETPATCHTEST\Packages");
            }
            long size = new FileInfo(packageLocation).Length;

            //if patch size is too large, then install/uninstall from local machine to avoid possible network issue
            if (size > 314572800)
            {
                string localPatch = "%systemdrive%\\school\\" + Path.GetFileName(packageLocation);

                localCopyCmd = Path.Combine(sharedPath, "CopyPackage.bat");
                using (System.IO.TextWriter textWriter = new System.IO.StreamWriter(localCopyCmd, false))
                {
                    textWriter.Write("copy /y {0} {1}", packageLocation, localPatch);
                    textWriter.Close();
                }
                AddAttachmentToWI(TFSItem.ID, localCopyCmd);
                packageLocation = localPatch;
            }
            int count = SubfolderCount();
            string patchFilePath = Path.Combine(sharedPath, TFSItem.KBNumber + ".xml");
            ProductSkuPackage package = new ProductSkuPackage
            {
                Name = "CBS",
                Path = packageLocation
            };
            ProductSku sku = new ProductSku
            {
                Package = new ProductSkuPackage[] { package },
                Name = "Patch"
            };

            Product product = new Product
            {
                Name = "Patch",
                Schema = "PatchSchema.xml",
                Common = new ProductCommon(),
                Sku = new ProductSku[] { sku }
            };
            XMLHelper.XmlSerializeToFile(product, patchFilePath, typeof(Product));
            AddAttachmentToWI(TFSItem.ID, patchFilePath);
            //UploadFileToWI(TFSItem.ID, patchFilePath);
        }
        public void AddAttachmentToWI(int id, string filePath)
        {
            GoFxService.GoFxService client = new GoFxService.GoFxService();
            client.UseDefaultCredentials = true;

            client.AddAttachmentToWI(id, TFSProject.DevDivServicing, filePath, null);

        }
        //private void CreateVersionVerififcationFile(string sharedPath, Architecture arch)
        //{
        //    sharedPath = Path.Combine(sharedPath, arch.ToString());

        //    StringBuilder sb = new StringBuilder();
        //    DataTable binaryTable = Patches[arch].ActualBinaries;

        //    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
        //    {
        //        foreach (string sku in ExpectedBinariesVersions.Keys)
        //        {
        //            Dictionary<string, string> addedFiles = new Dictionary<string, string>();
        //            bool bHighLevel = sku[0] > '3';

        //            //foreach (KeyValuePair<string, string> fileAndVersion in ExpectedBinariesVersions[sku])
        //            foreach (DataRow row in binaryTable.Rows)
        //            {
        //                if (bHighLevel && row["SKU"].ToString()[0] < '4') // exclude 2.0/3.0/3.5 files when testing 4.x payload
        //                    continue;
        //                else if (!bHighLevel && row["SKU"].ToString()[0] > '3') //exclude 4.x files when testing 2.0/3.0/3.5 payload
        //                    continue;

        //                // The file is already added
        //                string fileName = row["DestinationName"].ToString();
        //                if (addedFiles.ContainsKey(fileName))
        //                    continue;

        //                addedFiles.Add(fileName, fileName);

        //                //string version = GetActualVersion(fileAndVersion.Key, sku, arch);
        //                string version = row["Version"].ToString();
        //                if (String.IsNullOrEmpty(version))
        //                {
        //                    //This means the expected file actually does not exist in patch, so just skip it
        //                    continue;
        //                }

        //                // Query DB for actual file locations
        //                var sanFilesData = from r in dataContext.SANFileLocations
        //                                   where String.Compare(r.FileName, fileName, StringComparison.InvariantCultureIgnoreCase) == 0 &&
        //                                   r.ProductID == Utility.GetDBProductIDFromSKU(sku) &&
        //                                   r.ProductSPLevel == Utility.GetDBProductSPLevel(sku) &&
        //                                   r.CPUID == (int)arch
        //                                   select r;
        //                if (sanFilesData.Count() == 0)
        //                    continue;

        //                //Special processing on the specified KB file
        //                if (TFSItem.KBNumber == "5011048")
        //                {
        //                    Dictionary<string, string> fileCollect = new Dictionary<string, string>();
        //                    string exclusionFilePath = ConfigurationManager.AppSettings["ExclusionFilePath"];
        //                    StreamReader sr = new StreamReader(exclusionFilePath, Encoding.Default);
        //                    string line;
        //                    while ((line = sr.ReadLine()) != null)
        //                    {
        //                        int index = line.LastIndexOf('\\');

        //                        fileCollect.Add(line.Substring(index + 1, line.Length - index - 1), line.Substring(0, index + 1));
        //                    }
        //                    var temp = sanFilesData.ToList();

        //                    foreach (var file in fileCollect)
        //                    {
        //                        var results = temp.Where(p => p.FileName.Equals(file.Key) && p.FileLocation.Equals(file.Value)).ToList();

        //                        if (results.Count <= 1)
        //                        {
        //                            temp.Remove(results.FirstOrDefault());
        //                        }
        //                        else
        //                            for (int i = results.Count() - 1; i > -1; i--)
        //                            {
        //                                temp.Remove(results[i]);
        //                            }
        //                    }
        //                    sanFilesData = temp.AsQueryable();
        //                }

        //                foreach (SANFileLocation f in sanFilesData)
        //                {
        //                    sb.AppendLine(String.Format("{0}#{1}", Path.Combine(f.FileLocation, f.FileName), version));
        //                }
        //            }
        //        }
        //    }

        //    string vvFileName = String.Format("FileVersion_NDP{0}_{1}.txt", TFSItem.SKU.Replace(".", String.Empty), arch.ToString());

        //    using (FileStream fs = new FileStream(Path.Combine(sharedPath, vvFileName), FileMode.Create, FileAccess.Write))
        //    {
        //        using (StreamWriter sw = new StreamWriter(fs))
        //        {
        //            sw.Write(sb.ToString());
        //        }
        //    }
        //}
        private void CreateVersionVerififcationFile(string sharedPath, Architecture arch)
        {
            sharedPath = Path.Combine(sharedPath, arch.ToString());
            string filePath;

            StringBuilder sb = new StringBuilder();
            DataTable binaryTable = Patches[arch].ActualBinaries;

            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                foreach (string sku in ExpectedBinariesVersions.Keys)
                {
                    Dictionary<string, string> addedFiles = new Dictionary<string, string>();
                    bool bHighLevel = sku[0] > '3';

                    //foreach (KeyValuePair<string, string> fileAndVersion in ExpectedBinariesVersions[sku])
                    foreach (DataRow row in binaryTable.Rows)
                    {
                        if (bHighLevel && row["SKU"].ToString()[0] < '4') // exclude 2.0/3.0/3.5 files when testing 4.x payload
                            continue;
                        else if (!bHighLevel && row["SKU"].ToString()[0] > '3') //exclude 4.x files when testing 2.0/3.0/3.5 payload
                            continue;

                        // The file is already added
                        string fileName = row["DestinationName"].ToString();
                        if (addedFiles.ContainsKey(fileName))
                            continue;

                        addedFiles.Add(fileName, fileName);

                        //string version = GetActualVersion(fileAndVersion.Key, sku, arch);
                        string version = row["Version"].ToString();
                        if (String.IsNullOrEmpty(version))
                        {
                            //This means the expected file actually does not exist in patch, so just skip it
                            continue;
                        }

                        // Query DB for actual file locations
                        var sanFilesData = from r in dataContext.SANFileLocations
                                           where String.Compare(r.FileName, fileName, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                                           r.ProductID == Utility.GetDBProductIDFromSKU(sku) &&
                                           r.ProductSPLevel == Utility.GetDBProductSPLevel(sku) &&
                                           r.CPUID == (int)arch
                                           select r;
                        if (sanFilesData.Count() == 0)
                            continue;

                        //Special processing on the specified KB file
                        if (TFSItem.KBNumber == "5011048")
                        {
                            Dictionary<string, string> fileCollect = new Dictionary<string, string>();
                            string exclusionFilePath = ConfigurationManager.AppSettings["ExclusionFilePath"];
                            StreamReader sr = new StreamReader(exclusionFilePath, Encoding.Default);
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                int index = line.LastIndexOf('\\');

                                fileCollect.Add(line.Substring(index + 1, line.Length - index - 1), line.Substring(0, index + 1));
                            }
                            var temp = sanFilesData.ToList();

                            foreach (var file in fileCollect)
                            {
                                var results = temp.Where(p => p.FileName.Equals(file.Key) && p.FileLocation.Equals(file.Value)).ToList();

                                if (results.Count <= 1)
                                {
                                    temp.Remove(results.FirstOrDefault());
                                }
                                else
                                    for (int i = results.Count() - 1; i > -1; i--)
                                    {
                                        temp.Remove(results[i]);
                                    }
                            }
                            sanFilesData = temp.AsQueryable();
                        }

                        foreach (SANFileLocation f in sanFilesData)
                        {
                            sb.AppendLine(String.Format("{0}#{1}", Path.Combine(f.FileLocation, f.FileName), version));
                        }
                    }
                }
            }
            //int count = SubfolderCount();
            string vvFileName = String.Format("FileVersion_NDP{0}_{1}.txt", TFSItem.SKU.Replace(".", String.Empty), arch.ToString());

            using (FileStream fs = new FileStream(Path.Combine(sharedPath, vvFileName), FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(sb.ToString());
                }
            }
            filePath = Path.Combine(sharedPath, vvFileName);
            AddAttachmentToWI(TFSItem.ID, filePath);

        }

        //Test cases may need this batch to install LCU
        private void CreateLCUInstallScript(string sharedPath, Architecture arch)
        {
            //string filePath = Path.Combine(sharedPath, arch.ToString() + "InstallLCU.bat");
            int count = SubfolderCount();
            string filePath = Path.Combine(sharedPath, arch.ToString(), string.Format("InstallLCU.bat"));
            string lcuPath = Patches[arch].LCUPatchPath;
            if (lcuPath != null)
            {
                if (lcuPath.StartsWith("F:\\Packages"))
                {
                    lcuPath = lcuPath.Replace("F:\\Packages", @"\\DOTNETPATCHTEST\Packages");
                }
            }
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (String.IsNullOrEmpty(lcuPath))
                    {
                        sw.Write("exit /B 0");
                    }
                    else
                    {
                        sw.WriteLine(String.Format("call {0} /quiet /norestart", lcuPath));
                        sw.WriteLine("set ret=%ERRORLEVEL%");
                        sw.WriteLine("if \"%ret%\"==\"-2145124329\" set ret=0");
                        sw.WriteLine("if \"%ret%\"==\"2359302\" set ret=0");
                        sw.WriteLine("exit /B %ret%");
                    }
                }
            }
            AddAttachmentToWI(TFSItem.ID, filePath);
        }

        private string GetActualVersion(string fileName, string sku, Architecture arch)
        {
            DataTable dt = Patches[arch].ActualBinaries;

            var rows = from r in dt.AsEnumerable()
                       where String.Compare(r["FileName"].ToString(), fileName, true) == 0 &&
                       r["SKU"].ToString() == sku
                       select r;
            if (rows.Count() == 0)
                return null;

            return (rows.First())["Version"].ToString();
        }

        private string GetRuntimeMatrixName()
        {
            string osName = TFSItem.OSInstalled;
            string thSPLevel = TFSItem.OSSPLevel;

            string skuFilter = TFSItem.SKU + ";";
            string sku2Filter = String.Empty;

            if (this.IsProductRefresh)
                skuFilter += "Product_Refresh";

            // For 2.0/3.0/3.5, need to know if patch is from 4.7.2 or 4.8 patch
            // this can be found out in related link of TFS WI
            if (TFSItem.SKU[0] < '4')
            {
                if (TFSItem.Title.Contains("NDP 4.8.1 "))
                {
                    sku2Filter = "4.8.1;";
                }
                else if (TFSItem.Title.Contains("NDP 4.8 "))
                {
                    sku2Filter = "4.8;";
                }
            }

            //query DB for suitable matrix
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var thTestMatrix = dataContext.TTHTestMatrixes.Where(m => m.OSName == osName && m.SPLevel == thSPLevel && m.Active && m.TargetProduct.Contains(skuFilter));
                if (thTestMatrix.Count() > 0)
                {
                    if (String.IsNullOrEmpty(sku2Filter))
                    {
                        return thTestMatrix.First().MatrixName;
                    }
                    else
                    {
                        var subMatrix = thTestMatrix.Where(m => m.TargetProduct.Contains(sku2Filter)).FirstOrDefault();
                        if (subMatrix != null)
                            return subMatrix.MatrixName;
                    }
                }

                return null;
            }
        }
        public IQueryable<TSmokeRuntimeUpgrade> checkIfNeedUpgrade(PatchTestDataClassDataContext dataContext)
        {
            string TFSID = TFSItem.ID.ToString();
            WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(int.Parse(TFSID), THTestProcess.TFSServerURI);

            string SKU = wi["SKU"].ToString();
            string OSInstalled = wi["Environment"].ToString();
            string OSSPInstalled = wi["Target Architecture"].ToString();
            var result = dataContext.TSmokeRuntimeUpgrades.Where(r => r.OS_Installed == OSInstalled && r.OS_SP_Installed == OSSPInstalled && r.SKU == SKU);
            return result;
        }
        public bool GetParameterList(string SharedPath,out string UpgradeKBNumber, out string UpgradePatch, out string ProductNDP, out string versionFilePathForUpgradePackage, Architecture arch, out Dictionary<Architecture, PatchInformation> Patches)
        {
            UpgradeKBNumber = string.Empty; // 设置默认值为空字符串
            versionFilePathForUpgradePackage = string.Empty; // 设置默认值为空字符串
            ProductNDP = string.Empty;
            Patches = new Dictionary<Architecture, PatchInformation>(); // 初始化Patches
            UpgradePatch = string.Empty;

            using (var dataContext = new PatchTestDataClassDataContext())
            {
                var upgradesNeeded = checkIfNeedUpgrade(dataContext);
                if (upgradesNeeded.Count() == 0 || !upgradesNeeded.Any())
                {
                    return false;
                }
                List<string> relatedWorkItemIds = GetAllRelatedWIByWI();
                if (relatedWorkItemIds == null || !relatedWorkItemIds.Any())
                {
                    return false; 
                }
                relatedWorkItemIds = relatedWorkItemIds.Distinct().ToList();


                bool found = false; 
                foreach (var upgrade in upgradesNeeded)
                {
                    foreach (string relatedWorkItemId in relatedWorkItemIds)
                    {
                        WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(int.Parse(relatedWorkItemId), THTestProcess.TFSServerURI);
                        if (wi != null && wi["SKU"].ToString() == upgrade.Lastest_SKU)
                        {
                            ProductNDP = dataContext.TWUProductInstallMappings.Where(p => p.ProductName.Contains(wi["SKU"].ToString())).FirstOrDefault().BatchPath;
                            UpgradeKBNumber = wi["KB Article"].ToString();
                            //string sharedPathForUpgradePackage = CopyPackagesForLatestPackage(UpgradeKBNumber);
                            Patches = GetPatchInformation(relatedWorkItemId); 
                            if (Patches == null)
                            {
                                return false; 
                            }
                            
                            UpgradePatch= GeneratePatchFileForUpgradePackage(SharedPath, UpgradeKBNumber,arch, Patches,out UpgradePatch);
                            string filePath = CreateVersionVerififcationFilForLatestPackage(SharedPath, arch, Patches, wi);
                            versionFilePathForUpgradePackage = filePath;
                            found = true; 
                            break; 
                        }
                    }
                    if (found) 
                    {
                        break;
                    }
                }
            }
            return true;
        }
        private string GeneratePatchFileForUpgradePackage(string sharedPath,string UpgradeKBNumber,Architecture arch, Dictionary<Architecture, PatchInformation> UpgradePatches,out string localCopyCmd)
        {
            localCopyCmd = String.Empty;
            sharedPath = Path.Combine(sharedPath, arch.ToString());
            //string packageLocation = String.Empty;
            //if (arch == Architecture.X86)
            //{
            //    packageLocation = Path.Combine(sharedPath, Path.GetFileName(TestResults.x86PackageLocation));
            //}
            //else
            //{
            //    packageLocation = Path.Combine(sharedPath, Path.GetFileName(TestResults.x64PackageLocation));
            //}
            string packageLocation = UpgradePatches[arch].PatchLocation;
            if (packageLocation.StartsWith("F:\\Packages"))
            {
                packageLocation = packageLocation.Replace("F:\\Packages", @"\\DOTNETPATCHTEST\Packages");
            }
            if (packageLocation.StartsWith("D:\\Packages"))
            {
                packageLocation = packageLocation.Replace("D:\\Packages", @"\\DOTNETPATCHTEST\Packages");
            }
            long size = new FileInfo(packageLocation).Length;

            //if patch size is too large, then install/uninstall from local machine to avoid possible network issue
            if (size > 314572800)
            {
                string localPatch = "%systemdrive%\\school\\" + Path.GetFileName(packageLocation);

                localCopyCmd = Path.Combine(sharedPath, "CopyPackage.bat");
                using (System.IO.TextWriter textWriter = new System.IO.StreamWriter(localCopyCmd, false))
                {
                    textWriter.Write("copy /y {0} {1}", packageLocation, localPatch);
                    textWriter.Close();
                }
                packageLocation = localPatch;
            }
            int count = SubfolderCount();
            string patchFilePath = Path.Combine(sharedPath,"LastestPackage", UpgradeKBNumber + ".xml");
            ProductSkuPackage package = new ProductSkuPackage
            {
                Name = "CBS",
                Path = packageLocation
            };
            ProductSku sku = new ProductSku
            {
                Package = new ProductSkuPackage[] { package },
                Name = "Patch"
            };

            Product product = new Product
            {
                Name = "Patch",
                Schema = "PatchSchema.xml",
                Common = new ProductCommon(),
                Sku = new ProductSku[] { sku }
            };
            XMLHelper.XmlSerializeToFile(product, patchFilePath, typeof(Product));
            return patchFilePath;
        }
        private string CopyPackagesForLatestPackage(string KBNumber)
        {
            string sharedPath = ConfigurationManager.AppSettings["SharedPath"];
            sharedPath = Path.Combine(sharedPath, KBNumber, "LastestPackage");
            if (Directory.Exists(sharedPath))
            {
                string[] subFolders = Directory.GetDirectories(sharedPath);
                sharedPath = Path.Combine(sharedPath, (subFolders.Length-1).ToString());
            }
            else
            {
                sharedPath = Path.Combine(sharedPath, "0");
            }

            Directory.CreateDirectory(sharedPath);
            //copy package
            foreach (var sa in _supportedArchs)
            {
                Directory.CreateDirectory(Path.Combine(sharedPath, sa.ToString()));
            }

            return sharedPath;
        }
        private string CreateVersionVerififcationFilForLatestPackage(string sharedPath, Architecture arch, Dictionary<Architecture, PatchInformation> UpgradePatches, WorkItem wi)
        {
            sharedPath = Path.Combine(sharedPath, arch.ToString(),"LastestPackage");
            string filePath;
            Dictionary<string, Dictionary<string, string>> ExpectedBinariesVersionsForUpgradePackage = new Dictionary<string, Dictionary<string, string>>();
            ExpectedBinariesVersionsForUpgradePackage = GetExpectedBinariesVersionsForUpgradePackage(ExpectedBinariesVersionsForUpgradePackage, wi);
            GetActualBinariesForUpgradePackage(UpgradePatches[arch],wi, ExpectedBinariesVersionsForUpgradePackage);
            StringBuilder sb = new StringBuilder();
            DataTable binaryTable = UpgradePatches[arch].ActualBinaries;
            

            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                foreach (string sku in ExpectedBinariesVersionsForUpgradePackage.Keys)
                {
                    Dictionary<string, string> addedFiles = new Dictionary<string, string>();
                    bool bHighLevel = sku[0] > '3';

                    //foreach (KeyValuePair<string, string> fileAndVersion in ExpectedBinariesVersions[sku])
                    foreach (DataRow row in binaryTable.Rows)
                    {
                        if (bHighLevel && row["SKU"].ToString()[0] < '4') // exclude 2.0/3.0/3.5 files when testing 4.x payload
                            continue;
                        else if (!bHighLevel && row["SKU"].ToString()[0] > '3') //exclude 4.x files when testing 2.0/3.0/3.5 payload
                            continue;

                        // The file is already added
                        string fileName = row["DestinationName"].ToString();
                        if (addedFiles.ContainsKey(fileName))
                            continue;

                        addedFiles.Add(fileName, fileName);

                        //string version = GetActualVersion(fileAndVersion.Key, sku, arch);
                        string version = row["Version"].ToString();
                        if (String.IsNullOrEmpty(version))
                        {
                            //This means the expected file actually does not exist in patch, so just skip it
                            continue;
                        }

                        // Query DB for actual file locations
                        var sanFilesData = from r in dataContext.SANFileLocations
                                           where String.Compare(r.FileName, fileName, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                                           r.ProductID == Utility.GetDBProductIDFromSKU(sku) &&
                                           r.ProductSPLevel == Utility.GetDBProductSPLevel(sku) &&
                                           r.CPUID == (int)arch
                                           select r;
                        if (sanFilesData.Count() == 0)
                            continue;


                        foreach (SANFileLocation f in sanFilesData)
                        {
                            sb.AppendLine(String.Format("{0}#{1}", Path.Combine(f.FileLocation, f.FileName), version));
                        }
                    }
                }
            }
            //int count = SubfolderCount();
            string vvFileName = String.Format("FileVersion_NDP{0}_{1}.txt", wi["SKU"].ToString().Replace(".", String.Empty), arch.ToString());

            using (FileStream fs = new FileStream(Path.Combine(sharedPath, vvFileName), FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(sb.ToString());
                }
            }
            return Path.Combine(sharedPath, vvFileName);
        }

        public Dictionary<string, Dictionary<string, string>> GetExpectedBinariesVersionsForUpgradePackage(Dictionary<string, Dictionary<string, string>> ExpectedBinariesVersionsForUpgradePackage, WorkItem wi)
        {
            ExpectedBinariesVersionsForUpgradePackage = new Dictionary<string, Dictionary<string, string>>();
            StrExpectedBinaries1=ReadBinariesFromTFSNotesForUpgradePackage(StrExpectedBinaries1, new WorkItemHelper(wi));
            if (String.IsNullOrEmpty(StrExpectedBinaries1) && !String.IsNullOrEmpty(wi["Base Build Number"].ToString()))
            {
                StaticLogWriter.Instance.logMessage("Failed to find binaries info from TFS Notes field, calling submitpackagerequest.exe to generate it");

                //Call command line to generate binaries to TFS
                string toolPath = ConfigurationManager.AppSettings["SubmitPackageToolPath"];
                string toolArg = ConfigurationManager.AppSettings["SubmitPackageToolArg"];
                Helper.Utility.ExecuteCommandSync(toolPath, String.Format(toolArg, TFSItem.ID), 60 * 60 * 1000);

                // Re-get the TFS WI object
                TFSItem = new WorkItemHelper(wi);

                // Read Notes field again
                StrExpectedBinaries1 = ReadBinariesFromTFSNotesForUpgradePackage(StrExpectedBinaries1, TFSItem);
            }
            string[] binariesVersions = StrExpectedBinaries1.Split(new char[] { ',', ';' });

            foreach (string item in binariesVersions)
            {
                if (String.IsNullOrEmpty(item.Trim()))
                    continue;

                string fileAndVersion = item.Trim().Trim(new char[] { '(', ')' });
                int splitPos = fileAndVersion.LastIndexOf('-');
                string fileName = fileAndVersion.Substring(0, splitPos);
                string fileVersion = fileAndVersion.Substring(splitPos + 1);


                string sku = wi["SKU"].ToString();

                if (!ExpectedBinariesVersionsForUpgradePackage.ContainsKey(sku))
                {
                    Dictionary<string, string> dictBinaries = new Dictionary<string, string>();
                    ExpectedBinariesVersionsForUpgradePackage.Add(sku, dictBinaries);
                }

                if (!ExpectedBinariesVersionsForUpgradePackage[sku].ContainsKey(fileName.ToLowerInvariant()))
                {
                    ExpectedBinariesVersionsForUpgradePackage[sku].Add(fileName.ToLowerInvariant(), fileVersion);
                }
            }

            //Expand the file list to include all files in same patch family
            ExpectedBinariesVersionsForUpgradePackage= ExpandExpectedFilesWithPFForUpgradePackage(ExpectedBinariesVersionsForUpgradePackage);
            return ExpectedBinariesVersionsForUpgradePackage;
        }
        private Dictionary<string, Dictionary<string, string>> ExpandExpectedFilesWithPFForUpgradePackage(Dictionary<string, Dictionary<string, string>> ExpectedBinariesVersionsForUpgradePackage)
        {
            Dictionary<string, Dictionary<string, string>> dictExpandedFiles = new Dictionary<string, Dictionary<string, string>>();

            foreach (KeyValuePair<string, Dictionary<string, string>> kvSku in ExpectedBinariesVersionsForUpgradePackage)
            {
                foreach (KeyValuePair<string, string> kvFiles in kvSku.Value)
                {
                    if (!dictExpandedFiles.ContainsKey(kvSku.Key))
                        dictExpandedFiles.Add(kvSku.Key, new Dictionary<string, string>());

                    // Comment out this code as this logic may miss files
                    //if (dictExpandedFiles[kvSku.Key].ContainsKey(kvFiles.Key))
                    //    continue;

                    List<string> allFilesInSamePf = PatchFamily.GetAllFilesInSamePF(kvFiles.Key, kvSku.Key, TFSItem.GetPatchName(Architecture.AMD64));
                    if (kvFiles.Key == "vbc.exe" && allFilesInSamePf.Contains("cvtresui.dll"))
                    {

                        allFilesInSamePf.Remove("cvtresui.dll");
                    }
                    if (allFilesInSamePf != null && allFilesInSamePf.Count > 0)
                    {
                        foreach (string f in allFilesInSamePf)
                        {
                            //if sku version is 4.7+, remove wpffontcache_v0400.exe and wpftxt_v0400.dll
                            if (String.Compare("4.7", kvSku.Key) < 0 && (f == "wpffontcache_v0400.exe" || f == "wpftxt_v0400.dll"))
                                continue;
                            //if sku version is 2.0 3.5, remove cvtresui.dll
                            //if ((kvSku.Key.Equals("2.0") || kvSku.Key.Equals("3.5")) && (f == "cvtresui.dll"))
                            //    continue;

                            // This file is already added
                            if (dictExpandedFiles[kvSku.Key].ContainsKey(f))
                                continue;

                            // Specify a version
                            string version = String.Empty;
                            if (kvSku.Value.ContainsKey(f))
                            {
                                version = kvSku.Value[f];
                            }
                            else if (f.EndsWith(".exe") || f.EndsWith(".dll") || f.EndsWith(".tlb") || f.EndsWith(".mui"))
                            {
                                version = kvFiles.Value;
                            }

                            dictExpandedFiles[kvSku.Key].Add(f, version);
                        }
                    }

                    //If search PF failed, then add only current file
                    if (allFilesInSamePf == null || !allFilesInSamePf.Contains(kvFiles.Key))
                    {
                        dictExpandedFiles[kvSku.Key].Add(kvFiles.Key, kvFiles.Value);
                    }
                }
            }

            return dictExpandedFiles;
        }
        private string ReadBinariesFromTFSNotesForUpgradePackage(string StrExpectedBinaries1, WorkItemHelper wi)
        {
            if (!String.IsNullOrEmpty(wi.Notes))
            {
                string[] lines = wi.Notes.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string temp = line.Trim();
                    Regex rg = new Regex(@"<.*?>"); //Remove html tags
                    temp = rg.Replace(temp, String.Empty);

                    if (!String.IsNullOrEmpty(temp))
                    {
                        //Current sample string: Binaries Affected:[(system.dll-2.0.50727.8744),(system.dll-4.6.1538.0)]

                        int idx = temp.IndexOf("Binaries Affected:[(");
                        if (idx >= 0)
                        {
                            temp = temp.Substring(idx + 19);
                            idx = temp.IndexOf(']');
                            if (idx > 0)
                            {
                                temp = temp.Substring(0, idx);

                                if (CheckBinariesAffectedFormat(temp))
                                {
                                    StrExpectedBinaries1 = temp;
                                }
                            }
                        }
                    }
                }
            }
            return StrExpectedBinaries1;
        }
        public Dictionary<Architecture, PatchInformation> GetPatchInformation(string WI)
        {

            WorkItem workitem = Connect2TFS.Connect2TFS.GetWorkItem(int.Parse(WI), THTestProcess.TFSServerURI);
            List<string> patchLocations;
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                if (!String.IsNullOrEmpty(workitem["Drop Name X64"].ToString()) && !String.IsNullOrEmpty(workitem["Drop Patch Location X64"].ToString()) ||
                    !String.IsNullOrEmpty(workitem["Drop Name"].ToString()) && !String.IsNullOrEmpty(workitem["Drop Patch Location"].ToString()))
                {
                    patchLocations = DownloadPackages(workitem, true);
                }
                else
                {

                    if (workitem["Windows Packaging ID"] == null)
                    {
                        throw new Exception($"No DropName exists in work item, and the Windows Packaging ID value in work item {WI} is invalid.  Can't get packages.");
                    }
                    localWinCloudPackages = DownloadPackagesForJob(int.Parse(workitem["Windows Packaging ID"].ToString()), workitem["KB Article"].ToString());
                }
            }
            Dictionary<Architecture, PatchInformation> patchs = new Dictionary<Architecture, PatchInformation>();
            foreach (Architecture arch in SupportedArchs)
            {
                string patchVersion = String.Empty;
                string patchLocation = String.Empty;
                patchLocation = TFSItem.GetPatchDownloadLocationForUpgradePackage(arch, workitem);
                if (String.IsNullOrEmpty(patchLocation))
                {
                    StaticLogWriter.Instance.logError(String.Format("Test blocked: {0} patch is expected but actually not available", arch.ToString()));
                }
                else
                {
                    StaticLogWriter.Instance.logMessage(String.Format("{0} patch location: {1}", arch.ToString(), patchLocation));
                    StaticLogWriter.Instance.logMessage(string.Format("{0} patch version: {1} ", arch.ToString(), patchVersion));
                    PatchInformation patchInfo = new PatchInformation();
                    patchInfo.Arch = arch;
                    patchInfo.PatchLocation = patchLocation;
                    patchs.Add(arch, patchInfo);
                }

            }

            return patchs;
        }
        private InputData GetRuntimeInstallSequence(string sharedPath, Architecture arch, string localCopyBatch)
        {
            string UpgradeKBNumber = null;
            string UpgradePatch = null;
            string ProductNDP = null;
            string versionFilePathForUpgradePackage = null;
            Dictionary<Architecture, PatchInformation> Patches = null;
            bool upgradeNeeded=GetParameterList(sharedPath,out UpgradeKBNumber, out UpgradePatch,out ProductNDP,out versionFilePathForUpgradePackage,arch, out Patches);
            
            //sharedPath = Path.Combine(sharedPath, arch.ToString());
            InputData inputData = new InputData() { Data = new List<InputDataItem>() };
            string SKU = string.Empty;
            string OSInstalled = string.Empty;
            string OSSPInstalled = string.Empty;
            using (var dataContext = new PatchTestDataClassDataContext())
            {

                string TFSID = TFSItem.ID.ToString();
                WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(int.Parse(TFSID), THTestProcess.TFSServerURI);
                SKU = wi["SKU"].ToString();
                OSInstalled = wi["Environment"].ToString();
                OSSPInstalled = wi["Target Architecture"].ToString();

            }
            //v-zhehu: Meet with some problems on x86 machines, so comment this out for now
            //inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["AutoLogonCorbvt"], FieldType = "Command" });
            //inputData.Data.Add(new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
            //Environment Variables
            //int count = SubfolderCount();
            inputData.Data.Add(new InputDataItem { FieldName = "KBNumberFile", FieldValue = Path.Combine(sharedPath, arch.ToString(), TFSItem.KBNumber + ".xml"), FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "KBNumber", FieldValue = TFSItem.KBNumber, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "RunID", FieldValue = "[RunID]", FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "Arch", FieldValue = arch.ToString(), FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "TFSID", FieldValue = TFSItem.ID.ToString(), FieldType = "EnvironmentVariable" });
            string vvFileName = String.Format("FileVersion_NDP{0}_{1}.txt", TFSItem.SKU.Replace(".", String.Empty), arch.ToString());
            inputData.Data.Add(new InputDataItem { FieldName = "VersionFilePath", FieldValue = Path.Combine(sharedPath, arch.ToString(), vvFileName), FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "PayloadType", FieldValue = "GDR", FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "LCUScriptPath", FieldValue = Path.Combine(sharedPath, arch.ToString(), string.Format("InstallLCU.bat")), FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "OSInstalled", FieldValue = OSInstalled, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "OSSPInstalled", FieldValue = OSSPInstalled, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "SKU", FieldValue = SKU, FieldType = "EnvironmentVariable" });
            if (upgradeNeeded == true) {

                inputData.Data.Add(new InputDataItem { FieldName = "LastestNDP", FieldValue = ProductNDP, FieldType = "EnvironmentVariable" });
                inputData.Data.Add(new InputDataItem { FieldName = "UpgradeKBNumber", FieldValue = UpgradeKBNumber, FieldType = "EnvironmentVariable" });
                inputData.Data.Add(new InputDataItem { FieldName = "UpgradeKBFilePath", FieldValue = UpgradePatch, FieldType = "EnvironmentVariable" });
                inputData.Data.Add(new InputDataItem { FieldName = "UpgradePatchVersionFilePath", FieldValue = versionFilePathForUpgradePackage, FieldType = "EnvironmentVariable" });
            }

            //Commands
            //inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = string.Format("reg.exe Import {0}", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString()), FieldType = "Command" });

            if (ExpectedBinariesVersions.ContainsKey("2.0") ||
                ExpectedBinariesVersions.ContainsKey("3.0") ||
                ExpectedBinariesVersions.ContainsKey("3.5"))
            {
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["EnableNetFx3Win8"].ToString(), FieldType = "Command" });
            }

            // Copy package to local
            if (!String.IsNullOrEmpty(localCopyBatch))
            {
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = localCopyBatch, FieldType = "Command" });
            }

            return inputData;
        }

        private void KickOffRuns(string sharedPath, Architecture arch, string matrixName, InputData installSequece)
        {
            if (arch == Architecture.IA64 || arch == Architecture.ARM)
                return;
            string patchLocation = Patches[arch].PatchLocation;
            int parameterID = 0;
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                TTHTestRuntimeParameter paraInfo = new TTHTestRuntimeParameter();
                paraInfo.THTestID = _thTestID;
                paraInfo.ParameterPath = Path.Combine(sharedPath, arch.ToString());

                dataContext.TTHTestRuntimeParameters.InsertOnSubmit(paraInfo);
                dataContext.SubmitChanges();

                parameterID = paraInfo.ID;
            }
            string rele = string.IsNullOrEmpty(TFSItem.Custom02) ? TFSItem.Title.Substring(0, 9): TFSItem.Custom02;

            string runTitlePrefix = string.Format("{0}-{1}-{2}-[#SKU] ", rele, TFSItem.ID.ToString(), Path.GetFileNameWithoutExtension(patchLocation));
            PatchIntegration integration = new PatchIntegration((int)arch, matrixName, "CBS");

            integration.OperatePatchRuns(runTitlePrefix, installSequece, _thTestID, parameterID, String.Format("{0} {1}", TFSItem.SKU, TFSItem.ProductSPLevel));
        }

        private bool InheritPreviousRuntimeTest()
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                TTHTestRecord thisRecord = dataContext.TTHTestRecords.Where(p => p.ID == _thTestID).SingleOrDefault();

                var inheritInfos = dataContext.TTHTestRuntimeInherits.Where(p => p.TFSID == thisRecord.TFSID && !p.Processed).ToList();
                TTHTestRuntimeInherit inheritInfo = inheritInfos.Count > 0 ? inheritInfos.Last() : null;
                bool processed = false;

                if (inheritInfo != null)
                {
                    try
                    {
                        TTHTestRecord previousRecord = dataContext.TTHTestRecords.Where(p => p.ID == inheritInfo.PreviousTestID).SingleOrDefault();

                        if (!previousRecord.Active && thisRecord.X64PatchLocation.Equals(previousRecord.X64PatchLocation, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // change run info
                            foreach (var runinfo in previousRecord.TTHTestRunInfos)
                            {
                                runinfo.THTestID = _thTestID;
                            }

                            // change run parameter info
                            foreach (var parainfo in previousRecord.TTHTestRuntimeParameters)
                            {
                                parainfo.THTestID = _thTestID;
                            }

                            // change runtime result
                            thisRecord.RuntimeTestLog = previousRecord.RuntimeTestLog;
                            thisRecord.RuntimeStatus = previousRecord.RuntimeStatus;

                            processed = true;
                        }

                        foreach (var info in inheritInfos)
                        {
                            info.Processed = true;
                        }

                        dataContext.SubmitChanges();
                    }
                    catch
                    {
                        processed = false;
                    }
                }

                return processed;
            }
        }

        #endregion //Private Methods for runtime test end

        #endregion  //Private Methods end
    }
}
