using Connect2TFS;
using HotFixLibrary;
using LogAnalyzer;
using LoggerLibrary;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace THTestLib
{
    class RuntimeStatusRefresher
    {
        public static void UpdateRuntimeStatus()
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                //Check if runtime is running
                var testRecords = dataContext.TTHTestRecords.Where(r => r.RuntimeStatus == 1 && r.Active);
                if (testRecords.Count() == 0)
                {
                    StaticLogWriter.Instance.logMessage("No runtime runs to be refreshed");
                    return;
                }

                foreach (TTHTestRecord record in testRecords)
                {
                    UpdateRuntimeStatus(record.ID);
                }
            }
        }


        private static void UpdateRuntimeStatus(int testID)
        {
            try
            {
                using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                {
                    var testRecords = dataContext.Connection.ConnectionString.ToString();
                    TTHTestRecord record = dataContext.TTHTestRecords.Where(p => p.ID == testID).SingleOrDefault();

                    StaticLogWriter.Instance.logMessage(String.Format("Refreshing run status for ID-{0} TFSID-{1}", record.ID, record.TFSID));
                    //Helper.THTestLibHelper.RefreshTHTestRunInfo(testID);
                    if(!RunRefreshApi(testID))
                        StaticLogWriter.Instance.logMessage("Refresh run fail");
                    var runs = dataContext.TTHTestRunInfos.Where(r => r.THTestID == record.ID);
                    int completedCount = runs.Where(r => r.RunStatusID != (int)Helper.RunStatus.Running).Count();
                    int maxElapsedTime = Convert.ToInt32(ConfigurationManager.AppSettings["RuntimeMaxElapsedTime"]);
                    bool bCompleted = false;
                    bool bForceCompleted = false;
                    //All runs have completed
                    if (completedCount == runs.Count())
                    {
                        StaticLogWriter.Instance.logMessage("All runs completed");
                        record.RuntimeStatus = (int)Helper.RunStatus.Completed;
                        record.LastModifiedDate = DateTime.Now;

                        bCompleted = true;
                    }
                    else  // if 24 hours have elapsed since runs created
                    {
                        StaticLogWriter.Instance.logMessage("Some runs still running");
                        if ((DateTime.Now - record.TestStartDate).TotalHours >= maxElapsedTime)
                        {
                            StaticLogWriter.Instance.logMessage(String.Format("Stop monitoring runs since runs have been running more than {0} hours", maxElapsedTime));

                            record.RuntimeStatus = (int)Helper.RunStatus.Completed;
                            record.LastModifiedDate = DateTime.Now;

                            bCompleted = true;
                            bForceCompleted = true;
                        }
                    }

                    // Run auto analysis for runs that haven't been analyzed
                    if (bCompleted)
                    {
                        var unanalyzedRuns = runs.Where(r => r.RunResultID == (int)Helper.RunResult.Failed && r.AutoAnalysis == null && r.ManualAnalysis == null);
                        foreach (var run in unanalyzedRuns)
                        {
                            Result analyzedResult = RunAutoAnalysis(run.MDRunID);
                            if (analyzedResult.OverallResult) //Pass result
                            {
                                run.RunResultID = (int)Helper.RunResult.Passed;
                            }

                            if (!String.IsNullOrEmpty(analyzedResult.FailReason))
                            {
                                run.AutoAnalysis = analyzedResult.FailReason;
                            }
                        }
                    }

                    dataContext.SubmitChanges();

                    if (bCompleted)
                    {
                        WorkItemBO tfsItem = Connect2TFS.Connect2TFS.GetWorkItemByID(record.TFSID, THTestProcess.TFSServerURI);

                        bool runtimeResult = (!bForceCompleted) && (runs.Where(r => r.RunStatusID == (int)Helper.RunStatus.Completed && r.RunResultID != (int)Helper.RunResult.Passed).Count()) == 0;

                        MailHelper mailHelper = new MailHelper();
                        mailHelper.SendRuntimeResultsMail(tfsItem, record, runs.ToList(), runtimeResult, bForceCompleted, maxElapsedTime);

                        //Store test results to DB
                        TTHTestLog runtimeLog = new TTHTestLog();
                        runtimeLog.IsStaticTest = false;
                        runtimeLog.LogResult = runtimeResult;
                        runtimeLog.LogFilePath = HelperMethods.SaveTestLog(mailHelper.MailContent, record.TFSID.ToString() + "_Runtime");
                        runtimeLog.TimeStamp = DateTime.Now;
                        dataContext.TTHTestLogs.InsertOnSubmit(runtimeLog);
                        dataContext.SubmitChanges();

                        record.RuntimeTestLog = runtimeLog.ID;
                        record.LastModifiedDate = runtimeLog.TimeStamp;

                        dataContext.SubmitChanges();
                    }
                }
            }

            catch (Exception e)
            {
                StaticLogWriter.Instance.logMessage(e.StackTrace);

            }
        }

        private static bool RunRefreshApi(int id)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            string url = ConfigurationManager.AppSettings["ConnectToMadUrl"] + $"api/RefreshRun?testID={id}";
            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

            }
            catch (Exception e)
            {
                StaticLogWriter.Instance.logMessage(e.Message);
                StaticLogWriter.Instance.logMessage(e.ToString());
                return false;
            }
            return true;
        }

        private static Result RunAutoAnalysis(int runID)
        {
            TestLogAnalyzer logAnalyzer = TestLogAnalyzer.CreateLogAnalyzer(LogType.NetfxSetup);
            return logAnalyzer.Analyze(runID);
        }
    }
}
