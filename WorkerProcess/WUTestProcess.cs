using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Connect2TFS;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using LoggerLibrary;
using Helper;
using RMIntegration;
using ScorpionDAL;
using WUTestManagerLib;
using PubsuiteStaticTestLib;
using PubsuiteStaticTestLib.Testcases;
using RMDataAccess.Model;
using System.Runtime.Remoting.Contexts;
using PubUtilManager;
using KeyVaultManagementLib;
using System.Text.RegularExpressions;
using System.Net.Http;
using CAIPub;
using CAIPub.dbo;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace WorkerProcess
{
    public class WUTestProcess
    {
        public const string TFSServerURI = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
        private static readonly string parameterFileRootPath = ConfigurationManager.AppSettings["WUParameterFileRootPath"].ToString();
        private static readonly string CASENAME_BASIC_IUR = "WU_BasicIUR";
        private static readonly string CASENAME_LIVE_BASIC_IUR = "WU_LiveBasicIUR";
        private static readonly string CASENAME_SS_VERIFICATION = "WU_SSVerification";
        //private static readonly string CASENAME_INSTALL_ALL = "WU_InstallAll";
        private static readonly string CASENAME_INSTALL_ALL = "WU_InstallLastUpdates";
        private static readonly string CASENAME_NEGATIVE_CHILD = "WU_Negative_MultiSKUUpdateOnSingleSKU";
        private static readonly string CASENAME_BASIC_IUR_CROSS_SKU = "WU_BasicIURCrossSKU";
        private static readonly string CASENAME_BASIC_INSTALL_MR_SO = "WU_BasicInstallMRSO";
        private static readonly List<string> OSExceptionList = new List<string>() { "Microsoft server operating system version 22H2", "Windows Embedded Standard 7",
            "Windows 10 Version 1809", "Windows Server 2008 SP2","Windows 10 Version 21H2 for ARM64","Windows 10 Version 22H2 for ARM64" };
        private static RTConfigDataContext _context = new RTConfigDataContext();
        private ReleaseDataContext _rmContext = new ReleaseDataContext();
        public void StatTest(bool generateCSVFromLocal = true)
        {
            string logPath = System.IO.Path.Combine(@"C:\WUTestMonitorLogs", DateTime.Now.Date.ToString("yyyyMMdd") + "_Verbose.log");
            StaticLogWriter.createInstance(logPath);
            StaticLogWriter.Instance.logMessage(StaticLogWriter.LineSeparator2);



            WorkItemCollection tfsItems = Connect2TFS.Connect2TFS.QueryWorkItemsInWUTest(TFSServerURI);

            //if (tfsItems.Count == 0)
            //{
            //    StaticLogWriter.Instance.logMessage("There is no TFS WI in WUTest");
            //    StaticLogWriter.Instance.close();
            //    return;
            //}

            WorkItem item = Connect2TFS.Connect2TFS.GetWorkItem(1261125, TFSServerURI);//1260876

            //foreach (WorkItem item in tfsItems)
            //{
                bool flag = false;
                bool OnlyNeedStatic = false;
                //if (IsParentTFSWI(item.Id))
                if (item.Title.ToLower().Contains("parent workitem"))
                {
                    if (VerifyOS(item))
                        //continue;
                    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                    {
                        if (dataContext.TWUJobs.Where(x => x.JobDescription == item.Title && x.Active).Any() &&
                            dataContext.TPubsuiteUpdates.Where(x => x.FileName.Contains(item.Title) && x.RunState == 1).Any())
                        {
                            StaticLogWriter.Instance.logMessage(String.Format("WU runs for TFS ID - {0} have already been kicked off.", item.Id));
                            //continue;
                        }

                    }
                    StaticLogWriter.Instance.logMessage(String.Format("TFS ID - {0}", item.Id));
                    StaticLogWriter.Instance.logMessage(item.Id.ToString() + " is parent WI");

                    string csvPath = Path.Combine(Path.GetTempPath(), item.Id + ".csv");
                    if (generateCSVFromLocal)
                    {
                        csvPath = CreatCSV(item.Id);

                        if (csvPath == "false")
                        {
                            StaticLogWriter.Instance.logMessage(item.Id.ToString() + " generate csv fail!");
                           // continue;
                        }

                    }
                    else
                    {
                        var content = item.Attachments.Cast<Attachment>().Where(x => x.Name.ToLowerInvariant().Contains(".csv"));
                        if (content.Count() == 0)
                        {
                            StaticLogWriter.Instance.logMessage("Wu test cvs is not set in WI");
                            //continue;
                        }
                        else
                        {
                            var uri = item.Attachments.Cast<Attachment>().Where(x => x.Name.ToLowerInvariant().Contains(".csv")).Last().Uri;
                            StaticLogWriter.Instance.logMessage("Downloading Wu test cvs");
                            DownloadFileAsync(Convert.ToString(uri), csvPath);
                            StaticLogWriter.Instance.logMessage("Wu test cvs download finish");
                        }
                        //using (var client = new WebClient())
                        //{
                        //    client.UseDefaultCredentials = true;
                        //    client.Credentials = new NetworkCredential("v-jiachengma@microsoft.com", "");
                        //    client.DownloadFile(uri, csvPath);
                        //}

                    }
                    DataTable dt = ProcessDataTable(ConvertCsvToDataTable(csvPath, generateCSVFromLocal), item);
                    List<ExcelData> list = new List<ExcelData>();
                    List<ExcelData> updatedList = new List<ExcelData>();

                    list = (from DataRow row in dt.Rows
                            select new ExcelData
                            {
                                TFSID = row["ID"].ToString(),
                                KB = row["KB"].ToString(),
                                ProductLayer = row["Product Layer"].ToString(),
                                Title = row["Title"].ToString(),
                                GUID = row["GUID"].ToString(),
                                SSKBs = row["SS"].ToString(),
                                IsCatalogOnly = bool.Parse(row["IsCatalogOnly"].ToString()),
                                ShipChannels = row["ShipChannels"].ToString(),
                                OtherProperties = row["OtherProperties"].ToString()
                            }).ToList();
                    list.ForEach(x =>
                    {
                        if (!OSExceptionList.Any(y => x.Title.Contains(y)))
                        {
                            if (!x.IsCatalogOnly)
                                updatedList.Add(x);
                        }

                    });

                    OutPutExcel(dt, item.Title);

                    StaticLogWriter.Instance.logMessage("Remove os which not run runtime test");
                    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                    {
                        if (dataContext.TPubsuiteUpdates.Where(x => x.FileName.Contains(item.Title) && x.RunState == 1).Any())
                        {
                            list.ForEach(x =>
                            {
                                if (OSExceptionList.Any(y => x.Title.Contains(y)))
                                {
                                    StaticLogWriter.Instance.logMessage(String.Format("TFS ID - {0} only need to run static test.", item.Id));
                                    OnlyNeedStatic = true;
                                }
                            });
                        }
                    }
                    if (OnlyNeedStatic)
                        //continue;


                    #region StaticTest
                    StaticLogWriter.Instance.logMessage($"Start static test for WI {item.Id}");
                    bool overallResult = true;
                    try
                    {
                        List<InputData> expectedUpdateInfos = PrepareDataForStaticTests(list);
                        int updateID = SaveUpdateInfoToSelfDB($"{item.Id}-{item.Title}");
                        //if (!IfNeedToRun(AllTFS(expectedUpdateInfos)))
                        //{
                        //    StaticLogWriter.Instance.logMessage($"Fail in version compare for {item.Id}");
                        //    LogForFailGuid(item.Title);
                        //    continue;
                        //}
                        foreach (InputData eu in expectedUpdateInfos)
                        {
                            try
                            {

                                //List<TestResult> testResults = PubsuiteStaticTestLib.PubsuiteStaticTestLib.RunTests(eu, updateID);
                                //overallResult &= LogResults(eu, testResults);

                            }
                            catch (Exception ex)
                            {
                                StaticLogWriter.Instance.logError($"Error Message:{ex.Message} {Environment.NewLine} Stack Trace {ex.StackTrace}");
                                LogForFailGuid(eu.UpdateID);
                            }


                        }
                        using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                        {
                            //if (dataContext.TWUJobs.Where(x => x.JobDescription == item.Title && x.Active).Any())
                            var info = dataContext.TPubsuiteUpdates.Where(x => x.ID == updateID).FirstOrDefault();
                            info.RunState = 1;
                            dataContext.SubmitChanges();
                        }

                    }
                    catch (Exception ex)
                    {
                        overallResult = false;
                        StaticLogWriter.Instance.logError($"Error Message:{ex.Message} {Environment.NewLine} Stack Trace {ex.StackTrace}");

                    }
                    //if (!overallResult)
                    if (!overallResult)
                    {
                        StaticLogWriter.Instance.logError("Static tests failed!");

                    }
                    else
                    {
                        StaticLogWriter.Instance.logMessage("Static tests passed");
                    }

                    #endregion

                    #region RuntimeTest
                    if (!flag)
                    {

                        using (var dbContext = new PatchTestDataClassDataContext())
                        {
                            var per = dbContext.TWUJobs.Where(p => p.JobDescription == item.Title).OrderByDescending(p => p.ID).FirstOrDefault();
                            if (per != null)
                            {
                                if (per.PercentCompleted == 50 || per.PercentCompleted == 0)
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                flag = true;
                            }
                        }
                        if (flag)
                        //if (value >= 2)
                        {
                            StaticLogWriter.Instance.logMessage($"Start runtime test for WI {item.Id}");
                            Dictionary<string, KBGroup> kbgroups = WUTestManagerLib.WUTestManagerLib.DataAggregator(updatedList);

                            //Create an job
                            TWUJob job = new TWUJob() { JobDescription = item.Title.ToString(), CreateDate = DateTime.Now, Active = true, StatusID = 3 };
                            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
                            {
                                db.TWUJobs.InsertOnSubmit(job);
                                db.SubmitChanges();
                            }
                            int jobID = job.ID;
                            //Generate parameter files and init WU Runs into database
                            foreach (KBGroup kbgroupkb in kbgroups.Values)
                            {
                                //Create a unique parameter folder for each time of kicking off
                                string parameterRootForThisKB = GetUniqueParameterFileFolder(kbgroupkb.KB);

                                foreach (KBToTest kbtotest in kbgroupkb.GroupKBs)
                                {
                                    //var tempLayer = SetProductLayer(kbtotest.ProductLayer);
                                    //var targetProduct = tempLayer[0].Trim();
                                    //var patchTechnology = tempLayer[1].Trim();

                                    //if needed, generate SS file
                                    //kbtotest.GetSSInfo();
                                    //var sskbs = string.Empty;
                                    //if (kbtotest.SSUpdates != null && kbtotest.SSUpdates.Count > 0)
                                    //{
                                    //    sskbs = string.Join(";", kbtotest.SSUpdates.Select(p => p.KBNumber).ToArray());
                                    //}

                                    var parameterFileLocation = Path.Combine(parameterRootForThisKB, kbtotest.ARCH.ToString(), kbtotest.GUID);
                                    if (!Directory.Exists(parameterFileLocation))
                                    {
                                        Directory.CreateDirectory(parameterFileLocation);
                                    }

                                    #region init patchDetail and inset into db
                                    TWUPatchDetail patchDetail =
                                                new TWUPatchDetail()
                                                {
                                                    KB = kbgroupkb.KB,
                                                    JobID = job.ID,
                                                    ProductLayer = kbtotest.ProductLayer,
                                                    GUID = kbtotest.GUID,
                                                    //SSKBs = sskbs,
                                                    SSKBs = String.Empty,
                                                    CPUID = Convert.ToInt16(kbtotest.ARCH),
                                                    ParameterFileRootPath = parameterFileLocation,
                                                    Title = kbtotest.Title,
                                                    OtherProperties = FormatOtherPropertiesDict(kbtotest.OtherUpdateProperties),
                                                };

                                    using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
                                    {
                                        db.TWUPatchDetails.InsertOnSubmit(patchDetail);
                                        db.SubmitChanges();
                                    }
                                    #endregion


                                    using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                                    {
                                        #region detect and generate target oses, insert them into TWUPatchTargetOS

                                        foreach (var os in kbtotest.TargetOSes)
                                        {
                                            if (dataContext.TWUPatchTargetOSes.Where(p => p.PatchID == patchDetail.ID && p.OSImageID == os.OSImageID).Count() == 0)
                                            {
                                                TWUPatchTargetOS targetOS = new TWUPatchTargetOS() { OSImageID = os.OSImageID, PatchID = patchDetail.ID };
                                                dataContext.TWUPatchTargetOSes.InsertOnSubmit(targetOS);
                                                dataContext.SubmitChanges();
                                            }
                                        }

                                        #endregion
                                    }

                                    #region Create test cases and their parameter files

                                    GenerateAllCases(patchDetail.ID, kbtotest);

                                    #endregion


                                }
                            }

                            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                            {
                                var currentJob = dataContext.TWUJobs.Single(p => p.ID == jobID);
                                currentJob.StatusID = 4;
                                dataContext.SubmitChanges();

                                //Get all additonal case IDs
                                List<int> addtionalCaseIDs = (from p in dataContext.TWUAdditionalCases
                                                              select p.CaseID).Distinct().ToList();

                                var patches = (from j in dataContext.TWUJobs
                                               join p in dataContext.TWUPatchDetails on j.ID equals p.JobID
                                               where j.ID == jobID
                                               select p).ToList();

                                foreach (var patch in patches)
                                {
                                    var oses = (from tpatch in dataContext.TWUPatchDetails
                                                join targetOS in dataContext.TWUPatchTargetOSes on tpatch.ID equals targetOS.PatchID
                                                join twuos in dataContext.TWUOS on targetOS.OSImageID equals twuos.OSImageID
                                                where tpatch.ID == patch.ID
                                                orderby twuos.OSWeight descending
                                                select twuos).ToList();

                                    var subPatches = (from s in dataContext.TWUSubPatchDetails
                                                      where s.ParentID == patch.ID
                                                      select s).ToList();

                                    var patchArchName = ((Architecture)patch.CPUID).ToString();

                                    foreach (TWUSubPatchDetail subPatch in subPatches)
                                    {
                                        string[] productLayers = GetProductLayers(subPatch.ProductLayer);
                                        var tempLayer = SetProductLayer(productLayers.Last());
                                        var targetProduct = tempLayer[0].Trim();
                                        var patchTechnology = tempLayer[1].Trim();

                                        #region kick off runs with normal cases
                                        foreach (var os in oses)
                                        {
                                            var productIDList = new List<short>();

                                            // For legacy OS, need to expand .NET SKU, for e.g. 4.7.2 = 4.6+4.6.1+4.6.2+4.7+4.7.1+4.7.2
                                            // For Win10 for now, no need to do SKU expansion. Might need to do it in future
                                            if (!(os.OSName.Contains("Windows 10") || os.OSName.Contains("2019")))
                                            {
                                                //if this patch target multi product
                                                //get multi product from TMultiTargetProduct
                                                //else get single product id from TWUproductInstallMapping
                                                //HFR products should be filter out
                                                var query = from multiTargetProduct in dataContext.TMultiTargetProducts
                                                            join multiTargetProductMapping in dataContext.TMultiTargetProductMappings on multiTargetProduct.ID equals multiTargetProductMapping.MultiTargetProductID
                                                            join product in dataContext.TProducts on multiTargetProductMapping.ProductID equals product.ProductID
                                                            where multiTargetProduct.ActiveForWUTest == true
                                                                 && (multiTargetProduct.OSNameForWU.Contains(os.OSName) || multiTargetProduct.OSNameForWU == null)
                                                                 && multiTargetProduct.TargetProduct.Equals(targetProduct)
                                                                 && multiTargetProduct.CPUID == Convert.ToInt16(os.OSCPUID)
                                                                 && multiTargetProduct.PatchTechnology.Equals(patchTechnology)
                                                                 && !product.ProductFriendlyName.ToLower().Contains("hotfix rollup")
                                                            select product.ProductID;

                                                if (query.Count() > 0)
                                                {
                                                    productIDList = query.ToList();
                                                }
                                            }

                                            if (productIDList.Count == 0) //single product here
                                            {
                                                var singleProduct = dataContext.TWUProductInstallMappings.Where(p => p.ProductName.Equals(targetProduct)).FirstOrDefault();
                                                if (singleProduct == null)
                                                {
                                                    throw new Exception("Can not find Product in TWUProductInstallMapping, Product name is " + targetProduct);
                                                    throw new Exception("OS is " + os.OSName + " ImageID is " + os.OSImageID);
                                                }
                                                productIDList.Add(singleProduct.ProductID);
                                            }

                                            //create runs by target products
                                            //if patch target multi prodcuts, each product create corresponding run
                                            //if patch target single product, create one corresponding run
                                            foreach (var productID in productIDList)
                                            {
                                                // skip .NET 4.6.1+ on 2008 SP2
                                                if (os.OSImageID == 916 || os.OSImageID == 1011)
                                                {
                                                    if (productID > 33 || productID == 32)
                                                    {
                                                        continue;
                                                    }
                                                }

                                                var product = dataContext.TWUProductInstallMappings.Single(p => p.ProductID == productID);
                                                if (product == null)
                                                    throw new Exception("Can not find Product in TWUProductInstallMapping, Product id is " + productID);

                                                string titlePrefix = string.Format("WU-Job {0}-Update {1}-Child Update {2}-{3}-{4}-{5}-",
                                                            jobID.ToString(), patch.KB, subPatch.KB, os.OSName + os.OSSPLevel, GenerateProductNamesInRunTitle(subPatch.ProductLayer, product.ProductName), patchArchName);

                                                var targetTestcases = (from a in dataContext.TWUPatchTargetTestcases
                                                                       join b in dataContext.TMDQueryMappings on a.TestCaseID equals b.MatrixTestcaseID
                                                                       where a.PatchID == subPatch.ID && a.IsChecked == true && a.TestCaseID != 2775453
                                                                       select new { TestcaseID = b.MatrixTestcaseID, TestcaseName = b.TestcaseDesc }).ToList();

                                                // for basic IUR case, it covers all applicable OSes and all applicable products
                                                // for other cases, it only runs on the OS which has largest OSWeight, and covers each arch and each applicable products.
                                                foreach (var testcase in targetTestcases)
                                                {
                                                    if (addtionalCaseIDs.Contains(testcase.TestcaseID))
                                                        continue;

                                                    if (IsCaseToRunOnAllOS(testcase.TestcaseName) || os == oses.First())
                                                    {
                                                        dataContext.TWURuns.InsertOnSubmit(new TWURun()
                                                        {
                                                            JobID = jobID,
                                                            PatchDetailID = subPatch.ID,
                                                            Title = titlePrefix + testcase.TestcaseName,
                                                            OSImageID = os.OSImageID,
                                                            ParameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, String.Format("{0}_{1}_{2}.txt", subPatch.KB, patchArchName, testcase.TestcaseName)),
                                                            ActualProductID = productID,
                                                            TestCaseID = testcase.TestcaseID,
                                                            CreateDate = DateTime.Now
                                                        });
                                                    }
                                                }
                                            }

                                            dataContext.SubmitChanges();
                                        }
                                        #endregion
                                    }

                                    #region kick off runs with additional case

                                    foreach (TWUSubPatchDetail subPatch in subPatches)
                                    {
                                        var additionalScenarios = from s in dataContext.TWUAdditonalTestScenarios
                                                                  join m in dataContext.TWUPatchTargetAdditionalScenarios on s.ID equals m.ScenarioID
                                                                  where m.PatchID == subPatch.ID && m.IsChecked == true
                                                                  select s;

                                        foreach (var scenario in additionalScenarios)
                                        {
                                            dataContext.TWURuns.InsertOnSubmit(new TWURun()
                                            {
                                                JobID = jobID,
                                                PatchDetailID = subPatch.ID,
                                                Title = String.Format("WU-Job {0}-Update {1}-Child Update {2}-[{3}]", jobID.ToString(), patch.KB, subPatch.KB, scenario.Comment),
                                                OSImageID = scenario.SpecificOSImageID,
                                                ParameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, String.Format("{0}_{1}_{2}.txt", subPatch.KB, patchArchName, CASENAME_BASIC_IUR)), //Use parameter file of BasicIUR by default
                                                ActualProductID = scenario.SpecificProductID,
                                                TestCaseID = scenario.MDCaseID,
                                                CreateDate = DateTime.Now
                                            });
                                        }
                                    }

                                    dataContext.SubmitChanges();

                                    #endregion
                                }
                            }

                            try
                            {
                                WUJob wuJob = new WUJob(jobID);
                                wuJob.StartJob();
                            }
                            catch
                            {
                                using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                                {
                                    var currentJob = dataContext.TWUJobs.Single(p => p.ID == jobID);
                                    currentJob.Active = false;
                                    dataContext.SubmitChanges();
                                }
                            }


                        }
                        //}



                        #endregion
                    }



                    //else
                    //{
                    //    StaticLogWriter.Instance.logMessage(item.Id.ToString() + " is not parent WI, skipping");
                }
            //}

            StaticLogWriter.Instance.logMessage(StaticLogWriter.LineSeparator2);
            StaticLogWriter.Instance.close();

            //WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(1260138, TFSServerURI);
            //var uri = wi.Attachments.Cast<Attachment>().Where(x => x.Name.ToLowerInvariant().Contains(".csv")).Last().Uri;
            //using(var client = new WebClient())
            //{
            //    client.UseDefaultCredentials = true;
            //    client.DownloadFile(uri, @"D:\temp\aaa.csv");
            //}
            // DataTable dt = ProcessDataTable(ConvertCsvToDataTable(@"D:\temp\aaa.csv"), wi);
        }

        public string[] AllTFS(List<InputData> datas)
        {
            List<int> tfs = new List<int>();
            foreach (var item in datas)
            {
                tfs = tfs.Union(item.TFSIDs).ToList();
            }
            return tfs.ConvertAll(p => p.ToString()).ToArray();
        }
        public void LogForFailGuid(string Guid)
        {
            string path = Path.Combine(ConfigurationManager.AppSettings["OutputXml"], $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt");
            if (File.Exists(path))
            {
                string fileContent = File.ReadAllText(path);
                if (!fileContent.Contains(Guid))
                {
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine($"{Guid} fail to run");
                    }
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine($"{Guid} fail to run");
                }
            }

        }

        //public bool IfNeedToRun(string[] workItemIds)
        //{

        //    LogInfo.CreateInstance("SignoffTool");
        //    List<PatchInfo> patches = _rmContext.PatchInfos.Where(p => workItemIds.Contains(p.WorkItemId.ToString())).ToList();
        //    TestGroupSignoffAudit testGroup = new TestGroupSignoffAudit();
        //    TestSubjectSignOffAudit testSubject = testGroup.TestSignoffAudit(patches, _rmContext, $"WorkItemAudit.{DateTime.Now:yyyyy.mm.dd.hh.ss}");
        //    if (testSubject.PackageVersion != null)
        //    {
        //        StaticLogWriter.Instance.logMessage($"test subject not null");
        //    }
        //    return PubSuitVersionComp(testSubject);

        //}

        //public bool PubSuitVersionComp(TestSubjectSignOffAudit testSubject)
        //{
        //    TestFunction function = new TestFunction();
        //    if (!function.TestSubjectEligableForReleaseTicketTests(testSubject))
        //    {
        //        return true;
        //    }
        //    try
        //    {
        //        if ((bool)testSubject.PatchInfo.IsParentWorkItem)
        //        {
        //            if (testSubject.RTConfig.TargetMediaChannelsOnly || testSubject.RTConfig.DCATOnboarded)
        //            {
        //                return true;
        //            }

        //            return function.RunForParent(testSubject).Contains("Fail") ? false : true;
        //        }
        //        else if (SignoffAudit.PackageBuiltInWindowsLab(testSubject.WorkItem, testSubject.PatchInfo.ReleaseProductTargetGroup.OSId))
        //        {
        //            return true;
        //        }
        //        List<BundleUpdateInfo> bundleUpdates = function.GetBundleInformation(testSubject);
        //        List<string> allBundlesResults = new List<string>();
        //        foreach (BundleUpdateInfo bundleUpdate in bundleUpdates)
        //        {
        //            allBundlesResults.Add(function.Test(bundleUpdate.BundleUpdate.Arch, bundleUpdate.BundleUpdate.GUID, bundleUpdate.BuildVersion, testSubject.PackageVersion, WorkItemExtensions.GetDeliverable(testSubject.WorkItem), bundleUpdate.SSesExpiredBundles).ToString());
        //        }
        //        return allBundlesResults.Contains("FAIL") ? false : true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
        public static string GetManagedId()
        {
            if (Regex.Match(Environment.GetEnvironmentVariable("COMPUTERNAME"), "DotNetPatchTest", RegexOptions.IgnoreCase).Success)
            {
                return ConfigurationManager.AppSettings["gofxservinfra01ManagedId"];
            }
            return string.Empty;
        }
        public bool DownloadFileAsync(string uri, string localFilePath)
        {

            using (HttpClient httpClient = new HttpClient())
            {
                string token = KeyVaultAccess.GetGoFXKVSecret("PATTokenToTFS", GetManagedId());
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(":" + token)));
                var response = httpClient.GetAsync(uri).Result;
                if (response.IsSuccessStatusCode)
                {
                    using (FileStream fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var streamFromService = response.Content.ReadAsStreamAsync().Result;
                        streamFromService.CopyTo(fs);
                    }
                }

            }
            return true;

        }

        public bool VerifyOS(WorkItem item)
        {
            WorkItemHelper itemHelper = new WorkItemHelper(item);
            if (itemHelper.OSSPLevel == "24H2" || (itemHelper.OSSPLevel == "22H2" && itemHelper.OSInstalled == "Windows 11"))
                return true;
            return false;
        }

        public bool IsReleaseTicketNull(WorkItem workItem)
        {
            RTDB rTDB = new RTDB();
            List<ReleaseTicket> releaseTickets = rTDB.GetReleaseTicketsForWorkItem(workItem.Id);
            if (releaseTickets.Count == 0 && workItem.State == "WU Test")
            {
                return false;
            }
            return true;

        }
        /// <summary>
        /// WU Test XML
        /// </summary>
        /// <param name="workItemId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string CreatCSV(int workItemId)
        {
            PubUtilClient _pubUtilClient = new PubUtilClient(ConfigurationManager.AppSettings["ServiceAccountName2"],
                //"6xQ63d5Q3n@Y#pnf9ktf",
                KeyVaultAccess.GetGoFXKVSecret("VsulabServiceAccountPassword", GetManagedId()),
                ConfigurationManager.AppSettings["PubsuiteName"]);
            var connectionstring = "Data Source=ddsedb;Initial Catalog=ReleaseMan;Integrated Security=True";

            using (ReleaseDataContext context = new ReleaseDataContext(connectionstring))
             {
                string _devdivServicingUri = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
                List<string> csvRows = new List<string>();
                WorkItem workItem = Connect2TFS.Connect2TFS.GetWorkItem(workItemId, _devdivServicingUri);
                string csvFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), String.Format("{0}.{1}.{2}.WUTestInfo.csv", workItem["KB Article"].ToString(), workItem.Id, String.Format("{0:MMddHmmss}", DateTime.Now)));

                try
                {

                    string allWis = workItem.Id.ToString();
                    string os = $"{workItem["Environment"]} {workItem["Target Architecture"]}";
                    string osToMatch = string.Empty;


                    PatchInfo patch = context.PatchInfos.Where(p => p.WorkItemId == workItem.Id).OrderBy(p => p.Id).FirstOrDefault();
                    RTConfig config = GetReleaseTicketConfig(os);
                    osToMatch = config.OsToMatch;
                    if (!String.IsNullOrEmpty(config.CatalogOnlyArchs))
                    {
                        //indicates a seperate catalog only release ticket needs to be made
                        //code assumes each linked child is a catalog only candidate
                        foreach (Link link in workItem.Links)
                        {
                            if (link is RelatedLink)
                            {
                                RelatedLink rLink = link as RelatedLink;
                                WorkItem childItem = Connect2TFS.Connect2TFS.GetWorkItem(rLink.RelatedWorkItemId, _devdivServicingUri);
                                //if (!IsReleaseTicketNull(childItem))
                                //{
                                //    continue;
                                //}
                                List<string> cRows = GetCSVRows(childItem, _pubUtilClient, true, context, osToMatch, null, patch, null);
                                if (cRows.Count == 0)
                                {
                                    throw new Exception($"Unable to find published guids for work item {childItem.Id}.");
                                }
                                csvRows.AddRange(cRows);
                                allWis = String.Format("{0}+{1}", allWis, childItem.Id);
                            }
                        }
                    }
                    List<string> rows = GetCSVRows(workItem, _pubUtilClient, false, context, osToMatch, null, patch, allWis);
                    if (rows.Count == 0)
                        return "false";
                    //throw new Exception($"Unable to find published guids for work item {workItemId}.");
                    csvRows.AddRange(rows);
                    using (StreamWriter writer = new StreamWriter(csvFile, true))
                    {
                        writer.WriteLine("ID;KB;Product Layer;Title;GUID;SS;IsCatalogOnly");
                        foreach (string row in csvRows)
                        {
                            writer.WriteLine(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    StaticLogWriter.Instance.logMessage(ex.Message);
                    StaticLogWriter.Instance.logMessage(ex.ToString());
                    return "false";
                }

                return csvFile;
            }
        }

        private RTConfig GetReleaseTicketConfig(string os)
        {
            RTConfig config = _context.RTConfigs.Where(r => r.Os == os).FirstOrDefault();
            if (config == null)
            {
                throw new Exception($"Could not find release ticket config for OS {os}");
            }
            return config;
        }
        private List<string> GetCSVRows(WorkItem workitem, PubUtilClient pubUtilClient, bool iscatalogonly, ReleaseDataContext context, string ostomatch, string expectedshipchannels, PatchInfo patch, string allwis = null)
        {
            DateTime releasedate = patch.Release.ReleaseSchedule.ShipDate;
            List<string> csvrows = new List<string>();
            PatchInfo patchInfo = context.PatchInfos.Where(p => p.WorkItemId == workitem.Id).OrderBy(p => p.Id).FirstOrDefault();
            //if ((bool)patch.IsPromotedPatch && (IsLegacyServicingOS(patch.ReleaseProductTargetGroup.OSId) || patch.Release.Type == 4))
            if ((bool)patchInfo.IsPromotedPatch && (IsLegacyServicingOS(patchInfo.ReleaseProductTargetGroup.OSId) || patchInfo.Release.Type == 4))
            {
                foreach (string promotedtitle in GetTitlesForPromotedPatch(Convert.ToInt32(patchInfo.KBNumber), context, pubUtilClient, ostomatch))
                {
                    string updatedtitle = Regex.Replace(promotedtitle, @"\d{4}-\d{2}", releasedate.ToString("yyyy-MM")).Replace("Preview", "");
                    csvrows.Add($"Promoted Package;{patchInfo.KBNumber};;{updatedtitle};;;True");
                }
            }
            else
            {
                List<string> guidsandtitles = pubUtilClient.GetGuidsAndTitlesByKBNumber(workitem["KB Article"].ToString(), releasedate, ostomatch);
                foreach (string guidandtitle in guidsandtitles)
                {
                    string ss = string.Empty;
                    string guid = guidandtitle.Split(';')[0];
                    string title = guidandtitle.Split(';')[1];
                    string shipchannels = "Catalog";
                    if (!iscatalogonly) //find ss
                    {

                        string xml = pubUtilClient.GetPublishingXML(guid, true);
                        shipchannels = MUACHelper.MUACWorker.GetShipChannels(xml, guid);
                        StaticLogWriter.Instance.logMessage($"Ship channel value '{shipchannels}' found for guid {guid}");
                        if (!String.IsNullOrEmpty(expectedshipchannels))
                        {
                            StaticLogWriter.Instance.logMessage($"Checking ship channels for match to {expectedshipchannels}");
                            if (!shipchannels.Contains(expectedshipchannels))
                            {
                                StaticLogWriter.Instance.logMessage($"XML file for guid {guid} does not contain expected ship channels.");
                                continue;
                            }
                        }
                        List<string> ssguids = MUACHelper.MUACWorker.GetSSKBGUIDs(guid, xml);
                        bool first = true;
                        foreach (string ssguid in ssguids)
                        {
                            string ssxml = pubUtilClient.GetPublishingXML(ssguid, true);
                            if (first)
                            {
                                ss = MUACHelper.MUACWorker.GetTitle(ssxml, ssguid);
                                first = false;
                            }
                            else
                                ss += String.Format(",{0}", MUACHelper.MUACWorker.GetTitle(ssxml, ssguid));
                        }
                    }
                    string workitemid = workitem.Id.ToString();
                    if (allwis != null)
                        workitemid = allwis;
                    csvrows.Add(String.Format("{0};{1};;{2};{3};{4};{5};{6}", workitemid, workitem["KB Article"].ToString(), title, guid, ss, iscatalogonly, shipchannels));
                }
            }
            return csvrows;
        }

        public static bool IsLegacyServicingOS(string osIds)
        {
            List<string> legacyoses = new List<string>(System.Configuration.ConfigurationManager.AppSettings["LegacyServicingOS"].Split(';'));
            List<string> ids = new List<string>(osIds.Split('|'));
            foreach (string id in ids)
            {
                if (legacyoses.Contains(id.Trim())) return true;
            }
            return false;
        }

        private List<string> GetTitlesForPromotedPatch(int kbarticle, ReleaseDataContext context, PubUtilClient pubutilclient, string ostomatch)
        {
            PatchInfo patch = context.PatchInfos.Where(p => p.KBNumber == kbarticle).FirstOrDefault();
            int? originalworkitemid = patch.WorkItemId;
            List<string> promotedtitles = new List<string>();
            if (originalworkitemid != null)
            {
                List<string> guidsandtitles = pubutilclient.GetGuidsAndTitlesByKBNumber(kbarticle.ToString(), DateTime.MinValue, ostomatch);
                foreach (string guidandtitle in guidsandtitles)
                    promotedtitles.Add(guidandtitle.Split(';')[1]);
            }
            return promotedtitles;
        }

        private DataTable ConvertCsvToDataTable(string filePath, bool generateCSVFromLocal)
        {
            DataTable dataTable = new DataTable();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string[] headers = reader.ReadLine().Split(';');
                foreach (string header in headers)
                {
                    dataTable.Columns.Add(header);
                }
                if (generateCSVFromLocal)
                {
                    dataTable.Columns.Add("ShipChannels");
                }
                while (!reader.EndOfStream)
                {
                    string[] rows = reader.ReadLine().Split(';');
                    dataTable.Rows.Add(rows);
                }
            }
            return dataTable;
        }

        private DataTable ProcessDataTable(DataTable dt, WorkItem item)
        {
            string channel = string.Empty;
            dt.Columns.Add("OtherProperties");
            foreach (DataRow row in dt.Rows)
            {

                channel = row["ShipChannels"].ToString().Trim().ToLowerInvariant();
                string title = item.Title.ToLowerInvariant();
                foreach (WorkItemLink link in item.WorkItemLinks)
                {
                    WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(link.TargetId, TFSServerURI);
                    if (row["ID"].ToString() == item.Id.ToString())
                    {
                        row["ID"] = wi.Id;
                        break;
                    }
                }

                row["ID"] = row["ID"].ToString().Replace(item.Id + "+", "");

                if (title.Contains("preview"))
                {
                    if (channel == "site" || channel == "sitecatalog")
                        row["OtherProperties"] = "ReleaseType=Preview";
                    else if (channel == "catalog")
                        row["OtherProperties"] = "ReleaseType=Catalog(Preview)";
                    else if (channel == "siteau")
                        row["OtherProperties"] = "ReleaseType=MonthlyRollup";
                }
                else if (title.Contains("promotion"))
                {
                    if (channel == "suscatalog")
                        row["OtherProperties"] = "ReleaseType=Promotion;Destination=SUSCatalog";
                    else if (channel == "site")
                        row["OtherProperties"] = "ReleaseType=Promotion;Destination=WU";
                    else if (channel == "catalog")
                        row["OtherProperties"] = "ReleaseType=Catalog(Preview)";
                }
                else if (title.Contains("monthly rollup"))
                {
                    if (channel == "siteaususcatalog" || channel == "siteau")
                        row["OtherProperties"] = "ReleaseType=MonthlyRollup";
                    else if (channel == "catalog")
                        row["OtherProperties"] = "ReleaseType=Catalog(Security)";
                    else if (channel == "suscatalog")
                        row["OtherProperties"] = "ReleaseType=SecurityOnly";

                }
                else if (title.Contains("security only"))
                {
                    if (channel == "catalog")
                        row["OtherProperties"] = "ReleaseType=Catalog(Security)";
                    else if (channel == "suscatalog")
                        row["OtherProperties"] = "ReleaseType=SecurityOnly";
                }
            }

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                DataRow row = dt.Rows[i];
                if (row["ID"].ToString().ToLower().Contains("promoted package"))
                {
                    dt.Rows.Remove(row);
                    continue;
                }
            }
            return dt;
        }

        private static List<InputData> PrepareDataForStaticTests(List<ExcelData> lstExcelData)
        {
            List<InputData> expectedUpdateInfos = new List<InputData>();
            int tfsId = 0;
            foreach (WUTestManagerLib.ExcelData data in lstExcelData)
            {
                InputData expData = new InputData();

                expData.KB = data.KB;
                expData.SupersededKB = SplitSS(data.SSKBs);
                expData.TFSIDs = (from s in data.TFSID.Split(new char[] { '+', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                  select Convert.ToInt32(s)).Distinct().ToList();
                expData.Title = data.Title;
                expData.UpdateID = data.GUID;
                expData.ShipChannels = data.ShipChannels;
                expData.IsCatalogOnly = data.IsCatalogOnly;
                if (data.Title.Contains("Windows 10 ") && data.Title.Contains("ARM64") && (data.Title.Contains("21H2") || data.Title.Contains("22H2")))
                {
                    foreach (var id in expData.TFSIDs)
                    {
                        WorkItem item = Connect2TFS.Connect2TFS.GetWorkItem(id, TFSServerURI);
                        if ((item.Title.Contains("21H2") || item.Title.Contains("22H2")) && item.Title.Contains("4.8.1"))
                        {
                            tfsId = id;
                        }
                    }

                    expData.TFSIDs.Remove(tfsId);

                }

                if (data.Title.Contains("1809") && data.Title.Contains("ARM64"))
                {
                    foreach (var id in expData.TFSIDs)
                    {
                        WorkItem item = Connect2TFS.Connect2TFS.GetWorkItem(id, TFSServerURI);
                        if (item.Title.Contains("1809") && item.Title.Contains("4.8"))
                        {
                            tfsId = id;
                        }
                    }

                    expData.TFSIDs.Remove(tfsId);

                }

                if (!String.IsNullOrEmpty(data.OtherProperties))
                {
                    string[] temp = data.OtherProperties.Split(new char[] { ';', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    expData.OtherProperties = new Dictionary<string, string>();
                    try
                    {
                        for (int i = 0; i < temp.Length; i += 2)
                        {
                            expData.OtherProperties[temp[i]] = temp[i + 1];
                        }
                    }
                    catch
                    { }
                }

                expectedUpdateInfos.Add(expData);
            }

            return expectedUpdateInfos;
        }

        private static string SplitSS(string SSKB)
        {
            if (SSKB == "")
            {
                return SSKB;
            }
            else
            {
                string[] ssKBs = SSKB.Split(new char[] { ')' });
                string kb = null;
                for (int i = 0; i < ssKBs.Length - 1; i++)
                {
                    int temp = ssKBs[i].IndexOf("KB") + 2;
                    if (i == ssKBs.Length - 2)
                    {
                        kb += ssKBs[i].Substring(temp, ssKBs[i].Length - temp);
                    }
                    else
                        kb += ssKBs[i].Substring(temp, ssKBs[i].Length - temp) + ", ";
                }
                return kb;
            }
        }

        private void OutPutExcel(DataTable table, string Title)
        {
            string path = ConfigurationManager.AppSettings["OutputXml"] + Title.Substring(0, 9) + ".xlsx";
            IWorkbook workbook = new XSSFWorkbook();
            if (File.Exists(path))
            {
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    workbook = new XSSFWorkbook(file);
                }
                ISheet sheet = workbook.GetSheetAt(0);
                int lastRowNum = sheet.LastRowNum;
                //IRow headRow = sheet.CreateRow(lastRowNum + 1);

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    IRow row = sheet.CreateRow(lastRowNum + i + 1);
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        row.CreateCell(j).SetCellValue(table.Rows[i][j].ToString());
                    }
                }
                using (FileStream steam = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(steam);
                }
            }
            else
            {
                ISheet sheet = workbook.CreateSheet("Sheet1");
                IRow headRow = sheet.CreateRow(0);
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    headRow.CreateCell(i).SetCellValue(table.Columns[i].ColumnName);
                }

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    IRow row = sheet.CreateRow(i + 1);
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        row.CreateCell(j).SetCellValue(table.Rows[i][j].ToString());
                    }
                }
                using (FileStream steam = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                {
                    workbook.Write(steam);
                }
            }

        }

        private bool IsParentTFSWI(int tfsID)
        {
            RMSvcMethods rm = new RMSvcMethods(tfsID);

            rm.Populate();

            return rm.PPatch.MetaData.IsParent != null && (bool)rm.PPatch.MetaData.IsParent;
        }

        private string GetUniqueParameterFileFolder(string kb)
        {
            string parameterFilePath = Path.Combine(parameterFileRootPath, kb);
            int subFolderCount = 0;
            if (Directory.Exists(parameterFilePath))
            {
                string[] folders = Directory.GetDirectories(parameterFilePath);
                subFolderCount = folders.Length;
            }

            return Path.Combine(parameterFilePath, subFolderCount.ToString());
        }

        private string FormatOtherPropertiesDict(Dictionary<string, string> dict)
        {
            if (dict == null || dict.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in dict)
            {
                sb.AppendFormat("{0}={1};", kv.Key, kv.Value);
            }

            return sb.ToString(0, sb.Length - 1);
        }

        private void GenerateAllCases(int patchID, KBToTest kbtotest)
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var patchDetail = dataContext.TWUPatchDetails.Single(p => p.ID == patchID);

                List<TWUSubPatchDetail> singleSKUPatches = new List<TWUSubPatchDetail>();
                List<TWUSubPatchDetail> complexSKUPatches = new List<TWUSubPatchDetail>();

                //Step1. Generate sub patchdetails with single sku
                foreach (SubKBInfo subKb in kbtotest.SubKBs.Values)
                {
                    singleSKUPatches.Add(new TWUSubPatchDetail()
                    {
                        KB = subKb.KB,
                        ParentID = patchID,
                        ProductLayer = subKb.ProductLayer,
                        ParameterFileRootPath = Path.Combine(patchDetail.ParameterFileRootPath, subKb.KB),
                        MultipleSKU = false,
                        TFSID = subKb.TFSID,
                    });
                }

                //Step2. Generate sub patchdetails with multiple sku
                List<string> downlevelSubKB = (from a in kbtotest.SubKBs.Values
                                               where WUTestManagerLib.WUTestManagerLib.IsDownlevelDotNetSKU(a.ProductLayer) == true
                                               select a.KB).ToList();
                List<string> highlevelSubKB = (from a in kbtotest.SubKBs.Keys
                                               where downlevelSubKB.Contains(a) == false
                                               select a).ToList();

                bool hasComplexScenarios = downlevelSubKB.Count > 0 && highlevelSubKB.Count > 0;

                if (hasComplexScenarios)
                {
                    string downlevelProductLayers = String.Empty, downlevelKBNums = String.Empty, combinedTFSID = String.Empty;
                    bool bFirst = true;
                    foreach (string subKb in downlevelSubKB)
                    {
                        if (bFirst)
                        {
                            downlevelProductLayers = kbtotest.SubKBs[subKb].ProductLayer;
                            downlevelKBNums = subKb;
                            combinedTFSID = kbtotest.SubKBs[subKb].TFSID;
                            bFirst = false;
                        }
                        else
                        {
                            downlevelProductLayers += "+" + kbtotest.SubKBs[subKb].ProductLayer;
                            downlevelKBNums += "+" + subKb;
                            combinedTFSID += "+" + kbtotest.SubKBs[subKb].TFSID;
                        }
                    }

                    foreach (string subKb in highlevelSubKB)
                    {
                        complexSKUPatches.Add(new TWUSubPatchDetail()
                        {
                            KB = String.Format("{0}+{1}", downlevelKBNums, subKb),
                            ParentID = patchID,
                            ProductLayer = String.Format("{0}+{1}", downlevelProductLayers, kbtotest.SubKBs[subKb].ProductLayer),
                            ParameterFileRootPath = Path.Combine(patchDetail.ParameterFileRootPath, String.Format("{0}+{1}", downlevelKBNums, subKb)),
                            MultipleSKU = true,
                            TFSID = String.Format("{0}+{1}", combinedTFSID, kbtotest.SubKBs[subKb].TFSID),
                        });
                    }
                }

                //Add sub patch detail to DB
                foreach (TWUSubPatchDetail subPatchDetail in singleSKUPatches)
                {
                    dataContext.TWUSubPatchDetails.InsertOnSubmit(subPatchDetail);
                }
                foreach (TWUSubPatchDetail subPatchDetail in complexSKUPatches)
                {
                    dataContext.TWUSubPatchDetails.InsertOnSubmit(subPatchDetail);
                }
                dataContext.SubmitChanges();

                //Step3. Generate single SKU parameter files
                foreach (TWUSubPatchDetail subPatchDetail in singleSKUPatches)
                {
                    //Basic IUR
                    GenerateBasicIURParameterFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB]);
                    GenerateLiveBasicIURParameterFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB]);
                    GenerateSSParameterFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB]); //ss
                    GenerateBasicInstallMRSOParameterFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB]);

                    if (!hasComplexScenarios) //If no cross SKU scenarios, generate installall parament file
                    {
                        GenerateInstallAllParameterFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB]); //install all
                    }

                    //Negative-Missing target product of child patch (only for legacy OS update, execlude win10 for now)
                    if (!kbtotest.IsWin10Update && kbtotest.SubKBs.Count > 1)
                    {
                        if (WUTestManagerLib.WUTestManagerLib.IsDownlevelDotNetSKU(kbtotest.SubKBs[subPatchDetail.KB].ProductLayer))
                        {
                            //If the KB is downlevel, high level SKU update is not expected when only downlevel sku installed
                            GenerateNegativeChildUpdateFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB], highlevelSubKB);
                        }
                        else
                        {
                            //If the KB is highlevel, only this KB is expected when only high level sku installed
                            List<string> otherKBs = kbtotest.SubKBs.Keys.Where(a => a != subPatchDetail.KB).ToList();
                            GenerateNegativeChildUpdateFile(dataContext, subPatchDetail, kbtotest, kbtotest.SubKBs[subPatchDetail.KB], otherKBs);
                        }
                    }

                    GenerateAdditionalCases(dataContext, patchDetail, subPatchDetail);
                }

                //Step4. Create Multiple SKU cases: All downlevel KB + one high level KB at a time
                if (hasComplexScenarios)
                {
                    List<SubKBInfo> downlevelSubKBInfos = kbtotest.SubKBs.Where(a => downlevelSubKB.Contains(a.Key)).Select(a => a.Value).ToList();

                    foreach (TWUSubPatchDetail subPatchDetail in complexSKUPatches)
                    {
                        string highlevelkb = subPatchDetail.KB.Split(new char[] { '+' }).Last();

                        //1. Basic IUR on top of multiple sku
                        GenerateBasicIURCrossSKUFile(dataContext, subPatchDetail, kbtotest, downlevelSubKBInfos, kbtotest.SubKBs[highlevelkb]);

                        //2. Install all on top of multiple sku
                        GenerateInstallAllParameterFile(dataContext, subPatchDetail, kbtotest, downlevelSubKBInfos, kbtotest.SubKBs[highlevelkb]);

                        //3. SS
                        //GenerateSSParameterFile(dataContext, subPatchDetail, kbtotest);

                        //4. Addtional cases if there are
                        //GenerateAdditionalCases(dataContext, subPatchDetail);
                    }
                }

                dataContext.SubmitChanges();
            }
        }

        #region Parameter file for single SKU scenarios
        private void GenerateBasicIURParameterFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, SubKBInfo subInfo)
        {
            var parameterFileNameForBasicIUR = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_BASIC_IUR);
            var parameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForBasicIUR);
            var result = WUTestManagerLib.WUTestManagerLib.CreateBasicIURFile(kbtotest, subInfo, parameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_BASIC_IUR, true);
            }
        }

        private void GenerateInstallAllParameterFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, SubKBInfo subInfo)
        {
            var parameterFileNameForInstallAll = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_INSTALL_ALL);
            var installAllParameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForInstallAll);
            var result = WUTestManagerLib.WUTestManagerLib.CreateInstallAllFile(kbtotest, subInfo, installAllParameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_INSTALL_ALL, true);
            }
        }

        private void GenerateLiveBasicIURParameterFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, SubKBInfo subInfo)
        {
            var parameterFileNameForBasicIUR = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_LIVE_BASIC_IUR);
            var parameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForBasicIUR);
            var result = WUTestManagerLib.WUTestManagerLib.CreateLiveBasicIURFile(kbtotest, subInfo, parameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_LIVE_BASIC_IUR, false);
            }
        }

        private void GenerateNegativeChildUpdateFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, SubKBInfo subkb, List<string> otherKBs)
        {
            var parameterFileNameForNegative = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_NEGATIVE_CHILD);
            var parameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForNegative);
            var result = WUTestManagerLib.WUTestManagerLib.CreateNegativeMissingChildSKUFile(kbtotest, subkb, otherKBs, parameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_NEGATIVE_CHILD, true);
            }
        }

        private void GenerateBasicInstallMRSOParameterFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, SubKBInfo subInfo)
        {
            var parameterFileNameForBasicIUR = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_BASIC_INSTALL_MR_SO);
            var parameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForBasicIUR);

            var result = WUTestManagerLib.WUTestManagerLib.CreateBasicInstallMRSOFile(kbtotest, subInfo, parameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_BASIC_INSTALL_MR_SO, false);
            }
        }
        #endregion

        #region Paramenter files for multiple (cross) SKUs
        private void GenerateBasicIURCrossSKUFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, List<SubKBInfo> downlevelKBs, SubKBInfo highlevelKB)
        {
            var parameterFileNameForComplex = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_BASIC_IUR_CROSS_SKU);
            var parameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForComplex);
            var result = WUTestManagerLib.WUTestManagerLib.CreateBasicIURCrossSKUFile(kbtotest, downlevelKBs, highlevelKB, parameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_BASIC_IUR_CROSS_SKU, true);
            }
        }

        private void GenerateInstallAllParameterFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, List<SubKBInfo> downlevelKBs, SubKBInfo highlevelKB)
        {
            var parameterFileNameForInstallAll = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_INSTALL_ALL);
            var installAllParameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForInstallAll);
            var result = WUTestManagerLib.WUTestManagerLib.CreateInstallAllFile(kbtotest, downlevelKBs, highlevelKB, installAllParameterFilePath);

            if (!String.IsNullOrEmpty(result))
            {
                SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_INSTALL_ALL, true);
            }
        }
        #endregion

        private void GenerateSSParameterFile(PatchTestDataClassDataContext dataContext, TWUSubPatchDetail subPatch, KBToTest kbtotest, SubKBInfo subInfo)
        {
            //if (kbtotest.SSUpdates != null && kbtotest.SSUpdates.Count > 0)
            //{
            //    var parameterFileNameForSS = String.Format("{0}_{1}_{2}.txt", subPatch.KB, kbtotest.ARCH.ToString(), CASENAME_SS_VERIFICATION);
            //    var parameterFilePath = Path.Combine(subPatch.ParameterFileRootPath, parameterFileNameForSS);
            //    var result = WUTestManagerLib.WUTestManagerLib.CreateSSFile(kbtotest, subInfo, parameterFilePath);

            //    if (!String.IsNullOrEmpty(result))
            //    {
            //        SubmitPatchTargetCase(dataContext, subPatch.ID, CASENAME_SS_VERIFICATION, true);
            //    }
            //}
        }

        private void SubmitPatchTargetCase(PatchTestDataClassDataContext dataContext, int patchDetailID, string caseName, bool bChecked)
        {
            int caseID = dataContext.TMDQueryMappings.Where(p => p.TestcaseDesc.Equals(caseName)).FirstOrDefault().MatrixTestcaseID;
            dataContext.TWUPatchTargetTestcases.InsertOnSubmit(new TWUPatchTargetTestcase() { PatchID = patchDetailID, TestCaseID = caseID, IsChecked = bChecked });
        }

        private void GenerateAdditionalCases(PatchTestDataClassDataContext dataContext, TWUPatchDetail patch, TWUSubPatchDetail subPatch)
        {
            // 1st, get additioanl test filter id
            var imageIDs = from os in dataContext.TWUPatchTargetOSes
                           where os.PatchID == subPatch.ParentID
                           select os.OSImageID;
            string imageName = dataContext.TWUOS.Where(p => p.OSImageID == imageIDs.First()).First().OSName;

            var filterEntries = dataContext.TWUAdditionalTestFilters.Where(p => p.TargetOSes.Contains(imageName) && p.Active);
            TWUAdditionalTestFilter filter = null;
            foreach (var f in filterEntries)
            {
                if (CompareProductLayers(f.ProductLayers, patch.ProductLayer))
                {
                    filter = f;
                    break;
                }
            }
            if (filter == null)
                return;

            // 2nd, query for all additional scenarios
            var scenarios = from s in dataContext.TWUAdditonalTestScenarios
                            join m in dataContext.TWUAdditionalTestMatrixes on s.ID equals m.ScenarioID
                            where m.FilterID == filter.ID && m.Active == true
                            select s;
            if (scenarios.Count() == 0)
                return;

            // 3nd, filter out scenarios by target OS and target product
            string highestProductLayer = GetProductLayers(subPatch.ProductLayer).Last();
            var tempLayer = SetProductLayer(highestProductLayer);
            var targetProduct = tempLayer[0].Trim();
            var patchTechnology = tempLayer[1].Trim();

            var productID = dataContext.TWUProductInstallMappings.Where(p => p.ProductName.Equals(targetProduct)).FirstOrDefault().ProductID;

            var validScenarios = scenarios.Where(p => p.ProductID == productID && imageIDs.Contains(p.OSImageID));

            //4th, add additional case items
            if (validScenarios.Count() > 0)
            {
                foreach (var s in validScenarios)
                {
                    dataContext.TWUPatchTargetAdditionalScenarios.InsertOnSubmit(new TWUPatchTargetAdditionalScenario() { PatchID = subPatch.ID, ScenarioID = s.ID, IsChecked = true });
                }
            }
        }

        /// <summary>
        /// Compare product layers like 4.5.2 RTM MSI + 4.6 RTM MSI, ingore product sequence
        /// </summary>
        /// <returns></returns>
        private bool CompareProductLayers(string productLayers1, string productLayers2)
        {
            List<string> lstProducts1 = GetProductLayers(productLayers1).ToList();
            List<string> lstProducts2 = GetProductLayers(productLayers2).ToList();

            if (lstProducts1.Count != lstProducts2.Count)
                return false;

            for (int i = 0; i < lstProducts1.Count; ++i)
                lstProducts1[i] = FormatDownlevelProductNameForQuerying(lstProducts1[i]);
            for (int i = 0; i < lstProducts1.Count; ++i)
                lstProducts2[i] = FormatDownlevelProductNameForQuerying(lstProducts2[i]);

            var r1 = lstProducts1.Except(lstProducts2);
            var r2 = lstProducts2.Except(lstProducts1);
            if (r1.Count() > 0 || r2.Count() > 0)
                return false;

            return true;
        }

        /// <summary>
        /// analyse target product name & patch technology via product layer
        /// </summary>
        /// <param name="inputLayer">product layer</param>
        /// <returns>array which contains two elements, target product name & patch technology</returns>
        private string[] SetProductLayer(string inputLayer)
        {
            if (string.IsNullOrEmpty(inputLayer))
            {
                throw new Exception("Product layer is empty, Please double check!");
            }
            string[] tempArrayStr = inputLayer.Split(' ');

            if (tempArrayStr.Length < 3)
            {
                throw new Exception("Product layer is incorrect, Please double check!");
            }

            return new string[] { string.Format(".NET Framework {0} {1}", tempArrayStr[0], tempArrayStr[1]), tempArrayStr[2] };
        }

        /// <summary>
        /// Split product layers from a string that separate product with ',' or ';' or '+'
        /// </summary>
        /// <param name="productLayer">string of product layers</param>
        /// <returns>An array with splitted product layers</returns>
        private string[] GetProductLayers(string productLayer)
        {
            return productLayer.Split(new char[] { ',', ';', '+' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string FormatDownlevelProductNameForQuerying(string productLayer)
        {
            if (productLayer.Contains("2.0 SP2") || productLayer.Contains("3.0 SP2") || productLayer.Contains("3.5 SP1"))
            {
                return "Downlevel(3.5)";
            }

            return productLayer;
        }

        private string GenerateProductNamesInRunTitle(string productLayer, string fullProductName)
        {
            string[] productLayers = GetProductLayers(productLayer);
            string[] temp = SetProductLayer(productLayers.Last());

            string[] fullProductSplit = fullProductName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string name = String.Empty;
            try
            {
                productLayers[productLayers.Length - 1] = String.Format("{0} {1} {2}", fullProductSplit[2], fullProductSplit[3], temp[1]);
                name = String.Join("+", productLayers);
            }
            catch
            {
                return productLayer;
            }

            return name;
        }

        private bool IsCaseToRunOnAllOS(string caseName)
        {
            return caseName.Equals("WU_BasicIUR", StringComparison.InvariantCultureIgnoreCase) ||
                caseName.Equals("WU_LiveBasicIUR", StringComparison.InvariantCultureIgnoreCase);
        }

        private static int SaveUpdateInfoToSelfDB(string filename)
        {
            TPubsuiteUpdate pubsuiteUpdate = new TPubsuiteUpdate();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                pubsuiteUpdate.FileName = filename;
                pubsuiteUpdate.Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dbContext.TPubsuiteUpdates.InsertOnSubmit(pubsuiteUpdate);
                dbContext.SubmitChanges();
            }
            return pubsuiteUpdate.ID;
        }

        private static bool LogResults(InputData expectInfo, List<TestResult> results)
        {
            bool result = true;

            StaticLogWriter.Instance.logMessage("*********************************************************");
            StaticLogWriter.Instance.logMessage(String.Format("* {0}", expectInfo.UpdateID));
            StaticLogWriter.Instance.logMessage(String.Format("* {0}", expectInfo.Title));
            StaticLogWriter.Instance.logMessage("*********************************************************");

            StaticLogWriter.Instance.TimestampOff = true;

            foreach (TestResult r in results)
            {
                StaticLogWriter.Instance.logMessage(String.Empty);
                StaticLogWriter.Instance.logScenario(String.Format("Executing case: {0}", r.CaseName));
                result &= r.Result;

                StaticLogWriter.Instance.logMessage(r.Log);

                if (!r.Result && r.Failures != null && r.Failures.Count > 0)
                {
                    StaticLogWriter.Instance.logMessage(String.Empty);

                    foreach (KeyValuePair<string, TestFailure> kv in r.Failures)
                    {
                        StaticLogWriter.Instance.logMessage("Expect " + kv.Key + ":");
                        StaticLogWriter.Instance.logMessage(kv.Value.ExpectResult);

                        StaticLogWriter.Instance.logMessage("Actual " + kv.Key + ":");
                        StaticLogWriter.Instance.logMessage(kv.Value.ActualResult);
                    }
                }

                StaticLogWriter.Instance.logMessage(StaticLogWriter.LineSeparator1);
                StaticLogWriter.Instance.logMessage("Case executing result --> " + (r.Result ? "Pass" : "Fail"));
            }

            StaticLogWriter.Instance.TimestampOff = false;

            StaticLogWriter.Instance.logScenario("Overall result --> " + (result ? "Pass" : "Fail"));

            return result;
        }
    }
}
