using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HotFixLibrary;
using ScorpionDAL;

namespace Helper
{
    public class THTestLibHelper
    {
        private static readonly string RUN_OWNER = "vsulab";
        private static readonly string RunStatusPassed = "Passed";
        private static readonly string RunStatusFailed = "Failed";
        private static readonly string RunStatusRunning = "Running";
        private static readonly string RunStatusAnalyzedPassed = "Analyzed Passed";
        private static readonly string RunStatusAnalyzedFailed = "Analyzed Failed";

        public static List<TTHTestRecord> GetTHTestRecordsByTFSID(int tfsID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                return dataContext.TTHTestRecords.Where(p => p.Active && p.TFSID == tfsID).ToList();
            }
        }

        public static TTHTestRecord GetLatestTHTestRecordByTFSID(int tfsID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                return dataContext.TTHTestRecords.Where(p => p.TFSID == tfsID).OrderByDescending(r => r.ID).FirstOrDefault();
            }
        }

        public static TTHTestLog GetTHTestLog(int logid)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                return dataContext.TTHTestLogs.Where(p => p.ID == logid).FirstOrDefault();
            }
        }

        public static List<TTHTestRunInfo> GetTHTestRunsByTestID(int testID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                return dataContext.TTHTestRunInfos.Where(r => r.THTestID == testID).ToList();
            }
        }

        /// <summary>
        /// Get MSRC group from a TFS title
        /// </summary>
        public static string GetMsrcGroup(string title)
        {
            int index = title.IndexOf("NDP");
            if (index < 0)
                index = title.IndexOf("NPD");
            if (index < 0)
                return title;

            return title.Substring(0, index).TrimEnd(new char[] { ' ', '-' });
        }

        /// <summary>
        /// Get VX.XXXX from patch location
        /// </summary>
        public static string GetPackageVersion(string x64PatchLocation)
        {
            Regex rx = new Regex(@"\\V\d\.\d\d\d\\");
            Match match = rx.Match(x64PatchLocation);

            if (!match.Success)
            {
                rx = new Regex(@"\\\d\d\d\d\d\.\d\d\\");
                match = rx.Match(x64PatchLocation);
            }

            if (match.Success)
            {
                return match.Value.Trim(new char[] { '\\' });
            }
            else
                return "null";
        }

        public static void RefreshTHTestRunInfo(int testID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var runs = dataContext.TTHTestRunInfos.Where(p => p.THTestID == testID);
                if (runs.Count() > 0)
                {
                    foreach (var run in runs)
                    {
                        RefreshRunInfo(run);

                        dataContext.SubmitChanges();
                    }
                }
            }
        }

        private static void RefreshRunInfo(TTHTestRunInfo run)
        {
            // if a run is still running or has failed result, refresh info from actual Maddog run
            if (run.RunStatusID != (int)RunStatus.Completed || run.RunResultID != (int)RunResult.Passed)
            {
                string status = MaddogHelper.GetRunStatus(run.MDRunID, RUN_OWNER, 2, true);

                RunStatus runStatus;
                RunResult runResult;

                TranslateRunStatus(status, out runStatus, out runResult);

                run.RunStatusID = (int)runStatus;

                // Refresh run status when run is not running
                if (runStatus == RunStatus.Completed)
                {
                    run.RunResultID = (int)runResult;

                    if (status == RunStatusAnalyzedPassed || status == RunStatusAnalyzedFailed && String.IsNullOrEmpty(run.ManualAnalysis))
                    {
                        run.ManualAnalysis = MaddogHelper.GetRunAnalysis(run.MDRunID, RUN_OWNER, 2);
                    }
                }
            }
        }

        private static void TranslateRunStatus(string status, out Helper.RunStatus runStatus, out Helper.RunResult runResult)
        {
            if (status == RunStatusPassed || status == RunStatusAnalyzedPassed)
            {
                runStatus = Helper.RunStatus.Completed;
                runResult = Helper.RunResult.Passed;
            }
            else if (status == RunStatusFailed)
            {
                runStatus = Helper.RunStatus.Completed;
                runResult = Helper.RunResult.Failed;
            }
            else if (status == RunStatusRunning)
            {
                runStatus = Helper.RunStatus.Running;
                runResult = Helper.RunResult.Unknown;
            }
            else if (status == RunStatusAnalyzedFailed)
            {
                runStatus = Helper.RunStatus.Completed;
                runResult = Helper.RunResult.Failed;
            }
            else
            {
                runStatus = Helper.RunStatus.Error;//Error
                runResult = Helper.RunResult.Error;//Error
            }
        }
    }
}
