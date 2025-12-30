using System;
using System.Collections.Generic;
using System.Linq;
using MDO = MadDogObjects;

namespace Helper
{
    public class RunHelper
    {
        public static List<SmokeRun> GetRunsByJodID(long jobID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var maddogRunIDs = (from c in dataContext.TRuns where c.JobID == jobID select c.MDRunID)
                            .Union(from e in dataContext.TSAFXProjectSubmittedDatas where e.JobID == jobID select (int)e.RunID)
                            .Union(from f in dataContext.TNetFxSetupRunStatus where f.JobID == jobID && f.SubmissionType == "Patch" select f.MDRunID).ToList();

                if (maddogRunIDs.Count==0)
                {
                    return null;
                }

                List<SmokeRun> maddogSmokeRuns = new List<SmokeRun>();
                MaddogHelperAPI maddogHepler = MaddogHelperAPI.Instance(@"$/Dev10/pu/dtg", "ORCASTS", "MDSQL3.corp.microsoft.com", "vsulab", "SetupTest");

                foreach (var maddogRunID in maddogRunIDs)
                {
                    SmokeRun smokeRun = new SmokeRun();
                    maddogSmokeRuns.Add(smokeRun);

                    smokeRun.RunID = maddogRunID;


                    smokeRun.RunResultFolder = MDO.Run.GetResultsFolder(maddogRunID);

                    MDO.Run objMDORun = new MDO.Run(maddogRunID);
                    if (objMDORun == null)
                    {
                        smokeRun.RunTitle = "Unknown";
                        smokeRun.RunStatus = "Deleted";

                        var runCreateDate = (from c in dataContext.TRuns where c.MDRunID == maddogRunID select c.CreatedDate)
                                .Union(from e in dataContext.TSAFXProjectSubmittedDatas where e.RunID == maddogRunID select e.CreatedDate).First();
                        smokeRun.CreateDate = runCreateDate;
                        break;
                    }
                    try
                    {
                        smokeRun.RunTitle = objMDORun.Title;
                    }
                    catch(MadDogObjects.InvalidIDException)
                    {
                        smokeRun.RunTitle = "Unknown";
                        smokeRun.RunStatus = "Deleted";
                        break;
                    }
                    smokeRun.CreateDate = objMDORun.StartTime;

                    try
                    {
                        if (objMDORun.Running)
                        {
                            smokeRun.RunStatus = "Running";
                        }
                        else if (objMDORun.GetResultsSummary().Split(":".ToCharArray())[1].Contains("100%"))
                        {
                            smokeRun.RunStatus = "Passed";
                        }
                        else
                        {
                            smokeRun.RunStatus = "Failed";
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLibrary.Logger.Instance.AddLogMessage("Error in GetRunStatus method of MaddgHelper. Error: " + ex.Message, LoggerLibrary.LogHelper.LogLevel.ERROR, null);
                    }
                }
                return maddogSmokeRuns;
            }
        }

        public static string GetRunStatusName(string statusID)
        {
            switch (statusID)
            {
                case "1":
                    return "Running";
                case "2":
                    return "Complete";
                case "3":
                    return "NotStarted";
                case "4":
                    return "Pending";
                case "5":
                    return "Analyzing";
                case "6":
                    return "Error";
                default:
                    return "UnKnow";
            }
        }

        public static List<string> SecurityOSList = new List<string> { "TH2", "RS1", "RS2", "RS3", "RS4", "RS5" };
    }

    public class SmokeRun
    {
        public int RunID { get; set; }
        public string RunTitle { get; set; }
        public string RunStatus { get; set; }
        public string RunResultFolder { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
