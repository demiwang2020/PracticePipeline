using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WUTestManagerLib;
using System.Configuration;
using HotFixLibrary;
using ScorpionDAL;

namespace WorkerProcess
{
    public class WUJob
    {
        #region Data Member

        public long JobID { get; private set; }
        private string currentUser;

        #endregion

        #region Status Name Maddog Helper Provided

        private const string RUNSTATUSPASSED = "Passed";
        private const string RUNSTATUSFAILED = "Failed";
        private const string RUNSTATUSRUNNING = "Running";
        private const string RUNSTATUSANALYZEDPASSED = "Analyzed Passed";
        private const string RUNSTATUSANALYZEDFAILED = "Analyzed Failed";

        #endregion Status Name Maddog Helper Provided

        #region Constructor

        public WUJob(long lgJobID)
        {
            JobID = lgJobID;
            this.currentUser = "redmond\\vsulab";
        }

        public WUJob(long lgJobID, string strCurrentUser)
        {
            JobID = lgJobID;
            this.currentUser = strCurrentUser;
        }

        #endregion

        #region Public Member Function

        /// <summary>
        /// Starts WU Job
        /// (Pending)
        /// </summary>
        /// <returns></returns>
        public bool StartJob()
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var tJob = dataContext.TWUJobs.SingleOrDefault(c => c.ID == JobID && c.Active == true);
                if (tJob != null)
                {
                    WUIntegration workerprocess = new WUIntegration(JobID, currentUser);

                    workerprocess.KickOffRuns();

                    //Update TWUJob table
                    tJob.StatusID = (int)Helper.RunStatus.Running;
                    tJob.ResultID = (int)Helper.RunResult.Unknown;
                    tJob.PercentCompleted = 100;

                    dataContext.SubmitChanges();

                    //sending kicking off mail
                    //if (ConfigurationManager.AppSettings["IsSendingMail"] == "TRUE")
                    //{
                    //    try
                    //    {
                    //        SendKickingOffMail();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        throw ex;
                    //    }
                    //}

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates WU Job Status 
        /// </summary>
        /// <returns></returns>
        public bool UpdateJobStatus()
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var tJob = dataContext.TWUJobs.SingleOrDefault(c => c.ID == JobID && c.Active == true);
                if (tJob != null)
                {
                    #region Update WU Run status

                    var runs = from c in dataContext.TWURuns
                               where c.JobID == JobID
                               select c;

                    string status = "";
                    Helper.RunStatus runStatus;
                    Helper.RunResult runResult;
                    bool IsAllRunCompleted = true;

                    //Read run status and result, and update them to TWURun
                    foreach (TWURun run in runs)
                    {
                        status = MaddogHelper.GetRunStatus(run.MDRunID, currentUser, 2, true);
                        TranslateRunStatus(status, out runStatus, out runResult);
                        IsAllRunCompleted &= runStatus != Helper.RunStatus.Running;

                        run.RunResultID = Convert.ToInt32(runResult);
                        run.RunStatusID = Convert.ToInt32(runStatus);

                        //run.LastModifiedBy = CurrentUser;
                        //run.LastModifiedDate = DateTime.Now;
                    }

                    dataContext.SubmitChanges();     

                    #endregion

                    #region Update TWUJob status

                    int PassedCount = 0;
                    int AnalysisFailedCount = 0;
                    int RunningCount = 0;
                    int FailedCount = 0;
                    int ErrorCount = 0;
                    int Completed = 0;
                    foreach (var run in runs)
                    {
                        if (run.RunStatusID == (int)Helper.RunStatus.Completed)
                        {
                            Completed += 100;
                            if (run.RunResultID == (int)Helper.RunResult.Passed)
                                PassedCount++;
                            else if (run.RunResultID == (int)Helper.RunResult.Failed)
                                AnalysisFailedCount++;
                        }
                        else if (run.RunStatusID == (int)Helper.RunStatus.Running)
                        {
                            RunningCount++;
                        }
                        else if (run.RunStatusID == (int)Helper.RunStatus.Analyzing)
                        {
                            FailedCount++;
                        }
                        else
                        {
                            ErrorCount++;
                        }
                    }

                    if (runs.Count() > 0)
                    {
                        tJob.PercentCompleted = Completed / runs.Count();

                        if (PassedCount == runs.Count())
                        {
                            tJob.StatusID = (int)Helper.RunStatus.Completed;
                            tJob.ResultID = (int)Helper.RunResult.Passed;
                        }
                        else if (FailedCount > 0 && tJob.PercentCompleted == 100)
                        {
                            tJob.StatusID = (int)Helper.RunStatus.Completed;
                            tJob.ResultID = (int)Helper.RunResult.Failed;
                        }
                        else if (RunningCount > 0)
                        {
                            tJob.StatusID = (int)Helper.RunStatus.Running;
                            tJob.ResultID = (int)Helper.RunResult.Unknown;
                        }
                        else if (FailedCount > 0)
                        {
                            tJob.StatusID = (int)Helper.RunStatus.Analyzing;
                            tJob.ResultID = (int)Helper.RunResult.Unknown;
                        }
                        else
                        {
                            tJob.StatusID = (int)Helper.RunStatus.Error;
                            tJob.ResultID = (int)Helper.RunResult.Unknown;
                        }

                        //Update TJob's the LastModifiedBy and LastModifiedDateTime 
                        //tJob.LastModifiedBy = CurrentUser;
                        //tJob.LastModifiedDate = DateTime.Now;

                        dataContext.SubmitChanges();
                    }

                    #endregion

                    #region Send report/abnormal mail

                    if (ConfigurationManager.AppSettings["IsSendingMail"] == "TRUE")
                    {
                        if (IsAllRunCompleted)
                        {
                            //Send a report mail
                            SendReportMail(dataContext, tJob);
                        }
                        //else
                        //{
                        //    //Send an Abnormal mail if necessary
                        //    SendMailForAbnormalRun(dataContext, tJob);
                        //}
                    }

                    #endregion
                }
            }

            return true;
        }

        #endregion


        #region Private Member Function

        /// <summary>
        /// send an email after runs that belong to this job are kicked off
        /// </summary>
        private void SendKickingOffMail(bool blnIsAsync = true)
        {
            List<RunEx> lstReport = GetRunReportData();
            if (lstReport.Count > 0)
            {
                MailHelper mailHelper = new MailHelper(Environment.UserName);

                mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
                mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

                using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
                {
                    TWUJob objTJob = dataContext.TWUJobs.SingleOrDefault(p => p.ID == JobID);
                    if (blnIsAsync)
                    {
                        Action action = new Action(delegate()
                            {
                                mailHelper.SendWUKickingOffMail(objTJob, lstReport);
                            });

                        action.BeginInvoke(null, null);
                    }
                    else
                    {
                        mailHelper.SendWUKickingOffMail(objTJob, lstReport);
                    }
                }
            }
        }

        /// <summary>
        /// Gets necessary run data for sending kicking off mail
        /// </summary>
        /// <returns></returns>
        private List<RunEx> GetRunReportData()
        {
            List<RunEx> lstReport = new List<RunEx>();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var runs = from c in dataContext.TWURuns
                           where c.JobID == JobID
                           select c;

                foreach (TWURun run in runs)
                {
                    lstReport.Add(new RunEx()
                    {
                        RunTitle = run.Title,
                        RunID = run.MDRunID.ToString(),
                        Status = HotFixUtility.GetStatusName((int?)run.RunStatusID)
                    });
                }
            }

            return lstReport;
        }

        private void TranslateRunStatus(string status, out Helper.RunStatus runStatus, out Helper.RunResult runResult)
        {
            switch (status)
            {
                case RUNSTATUSPASSED:
                case RUNSTATUSANALYZEDPASSED:
                    {
                        runStatus = Helper.RunStatus.Completed;
                        runResult = Helper.RunResult.Passed;
                        break;
                    }
                case RUNSTATUSFAILED:
                    {
                        runStatus = Helper.RunStatus.Analyzing;
                        runResult = Helper.RunResult.Unknown;
                        break;
                    }
                case RUNSTATUSRUNNING:
                    {
                        runStatus = Helper.RunStatus.Running;
                        runResult = Helper.RunResult.Unknown;
                        break;
                    }
                case RUNSTATUSANALYZEDFAILED:
                    {
                        runStatus = Helper.RunStatus.Completed;
                        runResult = Helper.RunResult.Failed;
                        break;
                    }
                default:
                    {
                        runStatus = Helper.RunStatus.Error;
                        runResult = Helper.RunResult.Error;
                        break;

                    }
            }
        }

        /// <summary>
        /// Send mail about abnormal runs
        /// </summary>
        /// <param name="blnIsAsync"></param>
        private void SendMailForAbnormalRun(ScorpionDAL.PatchTestDataClassDataContext dataContext, TWUJob jobObj, bool blnIsAsync = true)
        {
            int abnormaInterval = 0;
            int lastMailsentInterval = 0;
            int deadlineHours = 0;

            if (!int.TryParse(ConfigurationManager.AppSettings["AbNormalInterval"], out abnormaInterval))
            {
                throw new Exception("Convert AbNormalInterval Error");
            }

            //the difference between last email sent and now is less than [LastMailSentInterval] hours
            if (!int.TryParse(ConfigurationManager.AppSettings["LastMailSentInterval"], out lastMailsentInterval))
            {
                throw new Exception("Convert LastMailSentInterval Error");
            }

            if (!int.TryParse(ConfigurationManager.AppSettings["DeadlineHours"], out deadlineHours))
            {
                throw new Exception("Convert DeadlineHours Error");
            }

            bool bSendMail = false;
            if ((DateTime.Now - jobObj.CreateDate).TotalHours < deadlineHours)
            {
                if (jobObj.LastNotificationMailSent == null)
                {
                    if ((DateTime.Now - jobObj.CreateDate).TotalHours > abnormaInterval)
                        bSendMail = true;
                }
                else
                {
                    if ((DateTime.Now - (DateTime)jobObj.LastNotificationMailSent).TotalHours > lastMailsentInterval)
                        bSendMail = true;
                }
            }

            if (bSendMail == true)
            {
                jobObj.LastNotificationMailSent = DateTime.Now;
                dataContext.SubmitChanges();

                var runs = from c in dataContext.TWURuns
                           where c.JobID == JobID && c.RunStatusID != (int)Helper.RunStatus.Completed
                           select c;

                int actualTime = Convert.ToInt32((DateTime.Now - jobObj.CreateDate).TotalHours);

                if (runs.Count() > 0)
                {
                    MailHelper mailHelper = new MailHelper(Environment.UserName);

                    mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
                    mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

                    if (blnIsAsync)
                    {
                        Action action = new Action(delegate()
                        {
                            mailHelper.SendMailAboutAbnormalWURun(jobObj, runs.ToList(), actualTime);
                        });

                        action.BeginInvoke(null, null);
                    }
                    else
                    {
                        mailHelper.SendMailAboutAbnormalWURun(jobObj, runs.ToList(), actualTime);
                    }
                }
            }
        }

        private void SendReportMail(ScorpionDAL.PatchTestDataClassDataContext dataContext, TWUJob job)
        {
            if (job != null && job.MailSendDate == null)
            {
                job.MailSendDate = DateTime.Now;
                dataContext.SubmitChanges();

                ReportEngine.Report report = new ReportEngine.Report(JobID, LinqHelper.ReportType.WUAutomation, 1);
                report.RunReport();
            }
        }

        #endregion
    }
}
