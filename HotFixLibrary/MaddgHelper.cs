using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using ScorpionDAL;
using System.Data;
using HotFixLibrary.MDImageCreation;
//For MadDog APIs
// Legacy MadDog APIs (Whidbey and lower)
using MDL = MaddogObjects.Legacy;
// New MadDog APIs (Orcas and above)
using MDO = MadDogObjects;

namespace HotFixLibrary
{
    public static class MaddogHelper
    {

        private static void Connect2Maddog(PatchTestDataClassDataContext db, string strOwner, int intMaddogDBID)
        {
            if (db == null) { db = new PatchTestDataClassDataContext(); }

            short shExecutionSystemID = db.TExecutionSystems.Single(exs => exs.ApplicationType == HotFixUtility.ApplicationType.SetupTest.ToString() && exs.MaddogDBID == intMaddogDBID).ExecutionSystemID;
            HotFixUtility.ConnectToMadDog(HotFixUtility.ApplicationType.SetupTest, strOwner, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).DatabaseName);
        }

        public static string GetRunStatus(int intMaddogRunID, string strOwner, int intMaddogDBID, bool isWorkerProcess = false)
        {
            if (intMaddogRunID == 0)
            {
                return "Unknown";
            }

            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            short shExecutionSystemID = db.TExecutionSystems.Single
                (exs => exs.ApplicationType == HotFixUtility.ApplicationType.SetupTest.ToString() && exs.MaddogDBID == intMaddogDBID).ExecutionSystemID;

            HotFixUtility.ConnectToMadDog(HotFixUtility.ApplicationType.SetupTest, strOwner, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).DatabaseName);
             
            string strStatus = "Please go to Maddog app to check the status";

            if (intMaddogDBID == 1) //Whidbey
            {
                MDL.Run objMDLRun = new MaddogObjects.Legacy.Run(intMaddogRunID);
                if (objMDLRun == null)
                {
                    strStatus = "Deleted";
                }

                try
                {
                    if (objMDLRun.Running)
                    {
                        strStatus = "Running";
                    }
                    else if (objMDLRun.GetResultsSummary().Split(":".ToCharArray())[1].Contains("100%"))
                    {
                        strStatus = "Passed";
                    }
                    else { strStatus = "Failed"; }
                }
                catch (Exception ex)
                {
                    LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetRunStatus method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                }

            }
            else
            {
                MDO.Run objMDORun = new MDO.Run(intMaddogRunID);
                if (objMDORun == null)
                {
                    strStatus = "Deleted";
                }

                try
                {
                    if (objMDORun.Running)
                    {
                        strStatus = "Running";
                    }
                    else if (objMDORun.GetResultsSummary().Split(":".ToCharArray())[1].Contains("100%"))
                    {
                        strStatus = "Passed";
                    }
                    else
                    {
                        strStatus = "Failed";
                        if (isWorkerProcess)
                        {
                            if (objMDORun.GetResultsSummary().Split(":".ToCharArray())[12].Contains("100.0%"))
                            {
                                strStatus = "Analyzed Passed";
                                List<MadDogObjects.Issue> lstIssues = objMDORun.Issues.GetCollection<MadDogObjects.Issue>();
                                foreach (MadDogObjects.Issue issue in lstIssues)
                                {
                                    if (issue.IssueType.Name.ToLower() == "product")
                                    {
                                        if (issue.LinkedBug != null && issue.LinkedBug.ID > 0)
                                        {
                                            strStatus = "Analyzed Failed";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetRunStatus method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                }
            }

            return strStatus;
        }

        public static string GetRunAnalysis(int intMaddogRunID, string strOwner, int intMaddogDBID)
        {
            string analysis = String.Empty;

            if (intMaddogRunID == 0)
            {
                return analysis;
            }

            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            short shExecutionSystemID = db.TExecutionSystems.Single
                (exs => exs.ApplicationType == HotFixUtility.ApplicationType.SetupTest.ToString() && exs.MaddogDBID == intMaddogDBID).ExecutionSystemID;

            HotFixUtility.ConnectToMadDog(HotFixUtility.ApplicationType.SetupTest, strOwner, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).DatabaseName);

            MDO.Run objMDORun = new MDO.Run(intMaddogRunID);

            if (objMDORun != null && objMDORun.GetResultsSummary().Split(":".ToCharArray())[12].Contains("100.0%"))
            {
                List<MDO.Result> results = objMDORun.ResultsQuery.GetCollection<MDO.Result>();
                List<MDO.Issue> lstIssues = objMDORun.Issues.GetCollection<MDO.Issue>();

                if (results != null && results.Count > 0 && lstIssues != null && lstIssues.Count > 0)
                {
                    MDO.Result result = results.First();
                    foreach (var issue in lstIssues)
                    {
                        if (analysis == String.Empty)
                            analysis = issue.Name;
                        else
                            analysis = String.Format("{0}#{1}", analysis, issue.Name);
                    }

                    analysis += String.Format(" (Last analyzed by {0})", result.LastAnalyzedBy);
                }
            }

            return analysis;
        }

        #region Stop Run

        public static int StopSelectedRuns(List<int> lstExecutionSystemRunIDs, string strOwner, int intMaddogDBID)
        {
            Connect2Maddog(null, strOwner, intMaddogDBID);
            int intRunsStopped = 0;
            foreach (int intExecutionSystemRunID in lstExecutionSystemRunIDs)
            {
                intRunsStopped += StopRun(intExecutionSystemRunID);
            }

            return intRunsStopped;
        }

        public static int StopAllRuns(int intPatchID, string strOwner, int intMaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TRun> lstRun = (from run in db.TRuns
                                 join pfile in db.TTestProdAttributes on run.TTestProdAttributesID equals pfile.TestProdAttributesID
                                 join patch in db.TTestProdInfos on pfile.TTestProdInfoID equals patch.TTestProdInfoID
                                 where patch.TTestProdInfoID == intPatchID
                                 select run).ToList<TRun>();

            Connect2Maddog(db, strOwner, intMaddogDBID);

            int intRunsStopped = 0;
            foreach (TRun objRun in lstRun)
            {
                intRunsStopped += StopRun(objRun.MDRunID);
            }

            return intRunsStopped;
        }

        private static int StopRun(int intExecutionSystemRunID)
        {
            if (intExecutionSystemRunID > 0)
            {
                MDL.Run objMDLRun = new MaddogObjects.Legacy.Run(intExecutionSystemRunID);
                try
                {
                    if (objMDLRun.Running)
                    {
                        MDL.Run.RunHelpers.StopRun(objMDLRun);
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    //ToDo: Log Error
                    //LoggerLibrary.Logger
                    return 0;
                }
            }

            return 0;
        }

        #endregion Stop Run

        #region Start Run

        public static int StartSelectedRuns(List<int> lstExecutionSystemRunIDs, string strOwner, int intMaddogDBID)
        {
            Connect2Maddog(null, strOwner, intMaddogDBID);
            int intRunsStarted = 0;
            foreach (int intExecutionSystemRunID in lstExecutionSystemRunIDs)
            {
                intRunsStarted += StartRun(intExecutionSystemRunID);
            }
            return intRunsStarted;
        }

        public static int StartAllRuns(int intPatchID, string strOwner, int intMaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TRun> lstRun = (from run in db.TRuns
                                 join pfile in db.TTestProdAttributes on run.TTestProdAttributesID equals pfile.TestProdAttributesID
                                 join patch in db.TTestProdInfos on pfile.TTestProdInfoID equals patch.TTestProdInfoID
                                 where patch.TTestProdInfoID == intPatchID
                                 select run).ToList<TRun>();

            Connect2Maddog(db, strOwner, intMaddogDBID);

            int intRunsStarted = 0;
            foreach (TRun objRun in lstRun)
            {
                intRunsStarted += StartRun(objRun.MDRunID);
            }

            return intRunsStarted;
        }

        private static int StartRun(int intExecutionSystemRunID)
        {
            if (intExecutionSystemRunID > 0)
            {
                MDL.Run objMDLRun = new MaddogObjects.Legacy.Run(intExecutionSystemRunID);

                try
                {
                    if (!(objMDLRun.Running))
                    {
                        MDL.Run.RunHelpers.StartRun(objMDLRun);
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    //ToDo: Log Error
                    //LoggerLibrary.Logger
                    return 0;
                }
            }
            return 0;
        }

        #endregion Start Run

        #region Delete Run

        public static int DeleteSelectedRuns(List<int> lstExecutionSystemRunIDs, string strOwner, int intMaddogDBID)
        {
            Connect2Maddog(null, strOwner, intMaddogDBID);
            int intRunsDeleted = 0;
            foreach (int intExecutionSystemRunID in lstExecutionSystemRunIDs)
            {
                intRunsDeleted += DeleteSingleRun(intExecutionSystemRunID);
            }
            return intRunsDeleted;
        }

        public static int DeleteAllRuns(int intPatchID, string strOwner, int intMaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TRun> lstRun = (from run in db.TRuns
                                 join pfile in db.TTestProdAttributes on run.TTestProdAttributesID equals pfile.TestProdAttributesID
                                 join patch in db.TTestProdInfos on pfile.TTestProdInfoID equals patch.TTestProdInfoID
                                 where patch.TTestProdInfoID == intPatchID
                                 select run).ToList<TRun>();

            Connect2Maddog(db, strOwner, intMaddogDBID);

            int intRunsDeleted = 0;
            foreach (TRun objRun in lstRun)
            {
                intRunsDeleted += DeleteSingleRun(objRun.MDRunID);
            }

            return intRunsDeleted;
        }

        private static int DeleteSingleRun(int intExecutionSystemRunID)
        {
            if (intExecutionSystemRunID > 0)
            {
                MDL.Run objMDLRun = new MaddogObjects.Legacy.Run(intExecutionSystemRunID);
                try
                {
                    objMDLRun.Delete();
                    return 1;
                }
                catch (Exception ex)
                {
                    //ToDo: Log Error
                    //LoggerLibrary.Logger
                    return 0;
                }
            }
            return 0;
        }

        #endregion Delete Run

        public static Patch GetPatch(int intPatchID, string strOwner)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            //db.l
            DataLoadOptions options = new DataLoadOptions();

            // Tell the options that we plan on using the Orders.
            options.LoadWith<TTestProdInfo>(p => p.TTestProdAttributes);
            options.LoadWith<TTestProdInfo>(p => p.TPatchSKUs);

            // Set the options on the context.
            db.LoadOptions = options;

            // There we go!
            //var orders = context.Customers.First().Orders;

            var varPatch = from p1 in db.TTestProdInfos
                           where p1.TTestProdInfoID == intPatchID
                           select new { p1, p1.TTestProdAttributes, p1.TPatchSKUs };

            TTestProdInfo objPatch = new TTestProdInfo();
            foreach (var obj in varPatch)
            {
                objPatch = obj.p1;
                objPatch.TTestProdAttributes = obj.TTestProdAttributes;
                objPatch.TPatchSKUs = obj.TPatchSKUs;
            }

            return new Patch(objPatch);
            //TTestProdInfo objPatch = 
        }

        public static List<TRun> GetAssociatedRuns(int intPatchID, string strOwner)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TRun> lstRun = (from run in db.TRuns
                                 join patchFile in db.TTestProdAttributes on run.TTestProdAttributesID equals patchFile.TestProdAttributesID
                                 where patchFile.TTestProdInfoID == intPatchID
                                 select run).ToList<TRun>();

            foreach (TRun objRun in lstRun)
            {
                objRun.Status = GetRunStatus(objRun.MDRunID, strOwner, (int)objRun.TRunTemplate.MaddogDBID);
            }

            return lstRun;
        }

        public static string GetRunResultsFolder(int intMaddogRunID, string strOwner, int MaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            short shExecutionSystemID = db.TExecutionSystems.Single
                (exs => exs.ApplicationType == HotFixUtility.ApplicationType.SetupTest.ToString() && exs.MaddogDBID == MaddogDBID).ExecutionSystemID;

            HotFixUtility.ConnectToMadDog(HotFixUtility.ApplicationType.SetupTest, strOwner, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).DatabaseName);

            string strResultFolder = "N/A";

            if (MaddogDBID == 1) //Whidbey
            {
                try
                {
                    strResultFolder = "N/A:Not Support Whidbey";
                }
                catch (Exception ex)
                {
                    LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetResultsFolder method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                }

            }
            else
            {
                try
                {
                    strResultFolder = MDO.Run.GetResultsFolder(intMaddogRunID);
                }
                catch (Exception ex)
                {
                    LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetResultsFolder method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                }
            }

            return strResultFolder;

        }

        public static void SetRunPropertiesbyMaddogRun(int intMaddogRunID, string strOwner, int MaddogDBID, Run run)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            short shExecutionSystemID = db.TExecutionSystems.Single
                (exs => exs.ApplicationType == HotFixUtility.ApplicationType.SetupTest.ToString() && exs.MaddogDBID == MaddogDBID).ExecutionSystemID;

            HotFixUtility.ConnectToMadDog(HotFixUtility.ApplicationType.SetupTest, strOwner, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).DatabaseName);

            if (MaddogDBID == 1) //Whidbey
            {
                MDL.Run objMDLRun = new MDL.Run(intMaddogRunID);

                try
                {
                    run.RunTitle = objMDLRun.Title;
                    if (objMDLRun.Running)
                        run.Status = "Running";
                    else if (objMDLRun.GetResultsSummary().Split(":".ToCharArray())[1].Contains("100%"))
                        run.Status = "Passed";
                    else run.Status = "Failed";
                    run.RunID = string.Format("Run -> ID: {0},Run -> Goto: {1}", run.RunID, objMDLRun.GetMDURL);
                }
                catch (Exception ex)
                {
                    LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetRunStatus method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                    run.RunTitle = "N/A";
                    run.Status = "Deleted";
                    run.RunID = string.Format("Run -> ID: {0},Run -> Goto: {1}", run.RunID, "N/A");
                }
            }
            else
            {
                MDO.Run objMDORun = new MDO.Run(intMaddogRunID);
                try
                {
                    run.RunTitle = objMDORun.Title;
                    if (objMDORun.Running)
                        run.Status = "Running";
                    else if (objMDORun.GetResultsSummary().Split(":".ToCharArray())[1].Contains("100%"))
                        run.Status = "Passed";
                    else run.Status = "Failed";
                    run.RunID = string.Format("Run -> ID: {0},Run -> Goto: {1}", run.RunID, objMDORun.GetMDURL);
                }
                catch (Exception ex)
                {
                    LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetRunStatus method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                    run.RunTitle = "N/A";
                    run.Status = "Deleted";
                    run.RunID = string.Format("Run -> ID: {0},Run -> Goto: {1}", run.RunID, "N/A");
                }

            }

        }

        public static RunIDReportData GetMadDogReportData(int runID)
        {
            // Initialization
            int MaddogDBID = 2;                              // Databse ID
            string user = @"redmond\vsulab";
            string resultSummary = string.Empty;

            MDL.Run objMDLRun = null;                       // Mad dog run object (legacy)
            MDO.Run objMDORun = null;                       // Mad dog run object (new)

            // Now prepare the raw data needed for report engine
            RunIDReportData myRunReport = new RunIDReportData();
            myRunReport.ID = runID;
            myRunReport.TestCaseList = new List<TestCase>();

            myRunReport.MachineNameAndID = string.Empty;
            myRunReport.MaddogGoToURL = string.Empty;
            myRunReport.ReproConfigDetails = string.Empty;


            // Now connect to Maddog DB to retrieve the information for a runID
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            short shExecutionSystemID = db.TExecutionSystems.Single
                (exs => exs.ApplicationType == HotFixUtility.ApplicationType.SetupTest.ToString() && exs.MaddogDBID == MaddogDBID).ExecutionSystemID;
            HotFixUtility.ConnectToMadDog(HotFixUtility.ApplicationType.SetupTest, user, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == shExecutionSystemID).DatabaseName);
            try
            {
                // step 1: read out result summary
                if (MaddogDBID == 1) //Whidbey
                {
                    objMDLRun = new MaddogObjects.Legacy.Run(runID);
                    resultSummary = objMDLRun.GetResultsSummary();
                    myRunReport.StartTime = objMDLRun.StartTime;
                    myRunReport.EndTime = objMDLRun.EndTime;
                }
                else
                {
                    objMDORun = new MDO.Run(runID);
                    resultSummary = objMDORun.GetResultsSummary();
                    myRunReport.StartTime = objMDORun.StartTime;
                    myRunReport.EndTime = objMDORun.EndTime;
                    myRunReport.MaddogGoToURL = objMDORun.GetMDURL;
                }

                // step 2: read out all test cases
                DataSet ds = new DataSet();
                if (MaddogDBID == 1)
                {
                    MDL.QueryObject qo = new MDL.QueryObject(MDL.QueryConstants.BaseObjectTypes.Results);
                    qo.QueryAdd(MDL.Tables.ResultTable.RUNIDFIELD, MDL.QueryConstants.EQUALTO, runID);
                    qo.DisplayFields = new string[] 
                    { 
                        MDL.Tables.ResultTable.TESTCASEIDFIELD,                       // [0]   test case id
                        MDL.Tables.ResultTable.TESTCASENAMEFIELD,                     // [1]   test case name
                        MDL.Tables.ResultTable.RESULTTYPEDESCRIPTIONFIELD,            // [2]   result
                        MDL.Tables.ResultTable.LOGFILECONTENTSFIELD,                  // [3]   log file
                        MDL.Tables.ResultTable.OSNAMEFIELD,                           // [4]   os name
                        MDL.Tables.ResultTable.MACHINEIDFIELD,                        // [5]   machine name
                        MDL.Tables.ResultTable.MACHINENAMEFIELD                       // [6]   machine id
                    };
                    ds = qo.GetDataSet();
                }
                else
                {
                    MDO.QueryObject qo = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.Results);
                    qo.QueryAdd(MDO.Tables.ResultTable.RUNIDFIELD, MDO.QueryConstants.EQUALTO, runID);
                    qo.DisplayFields = new string[] 
                    { 
                        MDO.Tables.ResultTable.TESTCASEIDFIELD,                       // [0]   test case id
                        MDO.Tables.ResultTable.TESTCASENAMEFIELD,                     // [1]   test case name
                        MDO.Tables.ResultTable.RESULTTYPEDESCRIPTIONFIELD,            // [2]   result
                        MDO.Tables.ResultTable.OSNAMEFIELD,                           // [3]   log file  LOGFILECONTENTSFIELD does not work. undo it for now. 
                        MDO.Tables.ResultTable.OSNAMEFIELD,                           // [4]   os name
                        MDO.Tables.ResultTable.MACHINEIDFIELD,                        // [5]   machine name
                        MDO.Tables.ResultTable.MACHINENAMEFIELD                       // [6]   machine id
                    };
                    ds = qo.GetDataSet();
                }

                // Given a DataSet, retrieve a DataTableReader
                // allowing access to all the DataSet's data:
                using (DataTableReader reader = ds.CreateDataReader())
                {
                    do
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                TestCase myTestCase = new TestCase();
                                myTestCase.ID = (int)reader[0];
                                myTestCase.Name = (string)reader[1];
                                string myResult = (string)reader[2];
                                // myTestCase.Pass = (myResult.Split(":".ToCharArray())[1].Contains("100%"));
                                myTestCase.Pass = myResult.Contains("Pass");

                                // myTestCase.Log = (string)reader[3];
                                // file:\\mdfile3\OrcasTS\Files\Core\Results\Run1910396
                                myTestCase.Log = @"file://mdfile3/OrcasTS/Files/Core/Results/Run" + runID;

                                myRunReport.TestCaseList.Add(myTestCase);

                                myRunReport.ReproConfigDetails = string.Format("OS:{0}", (string)reader[4]);
                                myRunReport.MachineNameAndID = string.Format("{0}:{1}", (string)reader[6], reader[5]);
                            }
                        }
                    } while (reader.NextResult());
                }

            }
            catch (Exception e)
            {
                // to do. add some log here
            }

            return myRunReport;
        }

        public static string CreateMaddogImages(string windowsBuildNumber, List<int> osIds, string windowsBranch)
        {
            Service1Client imageServiceClient = new Service1Client();
            string error = string.Empty;
            try
            {
                foreach (int osId in osIds)
                {
                    imageServiceClient.RunCreateImageIDTool(windowsBuildNumber, osId, windowsBranch);
                }
            }
            catch (Exception ex)
            {
                error = "Error: " + ex.Message;
            }
            finally
            {
                imageServiceClient.Close();
            }

            return error;
        }
      
        public class RunIDReportData
        {
            public int ID { get; set; }                                   // run ID. Maps to 1 test case ID
            public List<TestCase> TestCaseList { get; set; }              // A list of test cases bind to a runID. 1:1 for Runtime. N:1 for Safax
            public DateTime StartTime { get; set; }                       // start running time
            public DateTime EndTime { get; set; }                         // end running time
            public string MachineNameAndID { get; set; }                  // machine name and IP address of machine that the run is scheduled on
            public string MaddogGoToURL { get; set; }                     // URL that launches maddgo go to for a run
            public string ReproConfigDetails { get; set; }                // Machine information in order to reproduce a similar run
        }

        public class TestCase
        {
            public int ID { get; set; }                        // test case ID
            public string Name { get; set; }                   // test case name
            public bool Pass { get; set; }                  // test case result
            public string Log { get; set; }                 // log path for a test case
        }       
    }
}
