using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotFixLibrary;
using DataAggregator;
using ScorpionDAL;
using System.Configuration;
using Helper;
using System.IO;

namespace WorkerProcess
{
    public class Job
    {
        #region Data Member

        TJob objTJob;
        public long JobID { get; private set; }
        private string CurrentUser { get; set; }

        private string FileListData { get; set; }

        #region Status Name Maddog Helper Provided

        private string RunStatusPassed = "Passed";
        private string RunStatusFailed = "Failed";
        private string RunStatusRunning = "Running";
        private string RunStatusAnalyzedPassed = "Analyzed Passed";
        private string RunStatusAnalyzedFailed = "Analyzed Failed";
        //string RunStatusDeleted = "Deleted"; 
        #endregion Status Name Maddog Helper Provided

        #endregion Data Member

        #region Constructor

        public Job(long lgJobID)
        {
            JobID = lgJobID;
            if (JobID > 0)
                GetJobDetails();

            this.CurrentUser = "redmond\\vsulab";
        }

        public Job(long lgJobID, string strCurrentUser)
        {
            JobID = lgJobID;
            if (JobID > 0)
                GetJobDetails();

            this.CurrentUser = strCurrentUser;
        }

        #endregion Constructor

        #region Member Function

        private void GetJobDetails()
        {
            //ToDo: Get the Job object from TJob table and populate objTJob 
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                objTJob = dataContext.TJobs.SingleOrDefault(c => c.JobID == JobID && c.Active == true);
            }
        }

        /// <summary>
        /// 1. Go to Job Tables and look for the tasks whose state is "NotStarted"
        /// 2. Create a Worker Process object, 
        /// 3. Call LoadData to pull the data from Data Aggregator
        /// 4. Call KickOffRuns method to kick off Runs
        /// 5. Update the status of Job Tables appropriately
        /// </summary>
        /// <returns></returns>
        public bool StartJob()
        {
            var kbNumber = string.Empty;
            List<string> ExceptionSku = new List<string>() { "NDP462", "NDP47", "NDP471" };
            try
            {
                DataBuilder objDataBuilder = new DataBuilder(Convert.ToInt32(objTJob.PID));
                PatchTargetInfo info = null;
                List<TProduct> multiTargetProductList = null;
                List<TProduct> newTargetProductList = new List<TProduct>();

                if (objDataBuilder.AvailableArchitectures.Count == 0)
                {
                    using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
                    {
                        var tJob = dataContext.TJobs.SingleOrDefault(c => c.JobID == JobID);
                        tJob.StatusID = (int)Helper.RunStatus.NotStarted;
                        tJob.ResultID = (int)Helper.RunResult.Unknown;

                        dataContext.SubmitChanges();
                    }

                    return true;
                }


                #region Usual Patch Smoke test
                if (!objDataBuilder.IsRefreshRedistHFR || objDataBuilder.IsCBSRefreshRedistHFR)
                {
                    foreach (Architecture arch in objDataBuilder.AvailableArchitectures)
                    {
                        info = objDataBuilder.GetPatchTargetInfo(arch);

                        if (string.IsNullOrEmpty(kbNumber))
                        {
                            kbNumber = info.KbNumber;
                        }

                        foreach (var item in info.TargetOperatingSystems)
                        {
                            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
                            {
                                multiTargetProductList = (from multiTargetProduct in dataContext.TMultiTargetProducts
                                                          join multiTargetProductMapping in dataContext.TMultiTargetProductMappings on multiTargetProduct.ID equals multiTargetProductMapping.MultiTargetProductID
                                                          join product in dataContext.TProducts on multiTargetProductMapping.ProductID equals product.ProductID
                                                          where multiTargetProduct.ActiveForSmokeTest == true
                                                               && (multiTargetProduct.OS.Equals(item.OSName) || multiTargetProduct.OS == null)
                                                               && multiTargetProduct.TargetProduct.Equals(info.TargetProduct)
                                                               && multiTargetProduct.CPUID == (int)arch
                                                               && info.PatchTechnology.Equals(multiTargetProduct.PatchTechnology)
                                                          select product).ToList();

                                multiTargetProductList.ForEach(p =>
                                {
                                    if (!ExceptionSku.Any(y => p.DecaturProduct==y))
                                        newTargetProductList.Add(p);
                                });

                                if (newTargetProductList.Count > 0)
                                    break;
                            }
                        }

                        if (newTargetProductList.Count == 0)
                        {
                            #region Original Kickoff
                            //For SAFX Test
                            KickoffSAFXRuns(objDataBuilder, arch);

                            //For Setup Test
                            WorkerProcess objWorkerProcessSetup = new WorkerProcess(HotFixUtility.ApplicationType.SetupTest, objTJob.PID, arch.ToString());
                            objWorkerProcessSetup.objDataBuilder = objDataBuilder;
                            objWorkerProcessSetup.JobID = objTJob.JobID;
                            objWorkerProcessSetup.LoadData();
                            objWorkerProcessSetup.KickOffRuns();

                            FileListData = objWorkerProcessSetup.ListofFilesSetup;
                            #endregion
                        }
                        else
                        {
                            //target muit products
                            //SAFX has been updated to test multiple products
                            KickoffSAFXRuns(objDataBuilder, arch);

                            //kickoff runtime run
                            foreach (var item in newTargetProductList)
                            {
                                //For Setup Test
                                WorkerProcess objWorkerProcessSetup = new WorkerProcess(HotFixUtility.ApplicationType.SetupTest, objTJob.PID, arch.ToString());
                                objWorkerProcessSetup.objDataBuilder = objDataBuilder;
                                objWorkerProcessSetup.JobID = objTJob.JobID;

                                objWorkerProcessSetup.CurrentTargetProductName = item.ProductFriendlyName;
                                objWorkerProcessSetup.CurrentTargetProductID = item.ProductID;

                                objWorkerProcessSetup.LoadData();
                                objWorkerProcessSetup.KickOffRuns();

                                FileListData = objWorkerProcessSetup.ListofFilesSetup;
                            }

                        }
                    }//end foreach
                }
                #endregion

                #region RefreshRedist Hotfix Rollup smoke test

                if (objDataBuilder.IsRefreshRedistHFR)
                {
                    //Kick off runtime run
                    WorkerProcess objWorkerProcessSetup = new WorkerProcess(HotFixUtility.ApplicationType.ProductSetupTest, objTJob.PID, Architecture.X86.ToString());
                    objWorkerProcessSetup.objDataBuilder = objDataBuilder;
                    objWorkerProcessSetup.JobID = objTJob.JobID;

                    objWorkerProcessSetup.LoadData();
                    objWorkerProcessSetup.KickOffRuns();

                    //Kick off SAFX run for FullRedist and ISV HFR
                    if (objWorkerProcessSetup.PackageType == RefreshRedistHFRType.FullRedist.ToString() || objWorkerProcessSetup.PackageType == RefreshRedistHFRType.FullRedistISV.ToString())
                    {
                        WorkerProcess objWorkerProcessSAFX = new WorkerProcess(HotFixUtility.ApplicationType.SAFX, objWorkerProcessSetup.InputFile);
                        objWorkerProcessSAFX.objDataBuilder = objDataBuilder;
                        objWorkerProcessSAFX.JobID = objTJob.JobID;

                        objWorkerProcessSAFX.LoadData();
                        objWorkerProcessSAFX.KickOffRuns();
                    }
                }

                #endregion

                using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
                {
                    var tJob = dataContext.TJobs.SingleOrDefault(c => c.JobID == JobID);

                    tJob.StatusID = (int)Helper.RunStatus.Running;
                    tJob.ResultID = (int)Helper.RunResult.Unknown;
                    tJob.PercentCompleted = 0;

                    dataContext.SubmitChanges();
                }

                if (ConfigurationManager.AppSettings["IsSendingMail"] == "TRUE")
                {
                    SendMailAfterKickingOff();
                }

                return true;
            }
            catch (Exception ex)
            {
                if (ConfigurationManager.AppSettings["IsSendingMail"] == "TRUE")
                {
                    MailHelper mailHelper = new MailHelper(Environment.UserName);

                    mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
                    mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

                    mailHelper.SendKickOffExceptionMail(objTJob, ex);
                }

                throw;
            }
            finally
            {
                using (StreamWriter writer = new StreamWriter("StartJobDeleteError.log", true))
                {
                    writer.WriteLine("Init delete info...");

                    //delete extracted files to save drive space
                    var extractPath = Path.Combine(DataAggregator.Extraction.ExtractionDir, kbNumber);
                    writer.WriteLine("Target Directory:" + extractPath);
                    if (Directory.Exists(extractPath))
                    {
                        writer.WriteLine("Directory exists");
                        string deleteCommand = string.Format("rd /s /q {0}", extractPath);
                        writer.WriteLine("Start delete! Command is" + deleteCommand);
                        Utility.ExecuteCommandSync(deleteCommand);
                        writer.WriteLine("Delete success!");
                    }
                }
            }
        }

        private void KickoffSAFXRuns(DataBuilder objDataBuilder, Architecture arch)
        {
            WorkerProcess objWorkerProcess = new WorkerProcess(HotFixUtility.ApplicationType.SAFX, objTJob.PID, arch.ToString());
            objWorkerProcess.objDataBuilder = objDataBuilder;
            objWorkerProcess.JobID = objTJob.JobID;
            objWorkerProcess.LoadData();
            objWorkerProcess.KickOffRuns();
        }

        private void Report(long lgJobID, LinqHelper.ReportType reportType)
        {
            if (UpdateMailSentDate(lgJobID, reportType))
            {
                ReportEngine.Report report = new ReportEngine.Report(lgJobID, reportType, 1);
                report.RunReport();
            }
        }

        private bool UpdateMailSentDate(long lgJobID, LinqHelper.ReportType reportType)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                var job = db.TJobs.SingleOrDefault(c => c.JobID == lgJobID && c.Active == true);
                if (job != null)
                {
                    if (reportType.Equals(LinqHelper.ReportType.RunTime))
                    {
                        if (job.RuntimeMailSentDate == null)
                        {
                            job.RuntimeMailSentDate = DateTime.Now;
                            db.SubmitChanges();
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        if (job.SAFXMailSentDate == null)
                        {
                            job.SAFXMailSentDate = DateTime.Now;
                            db.SubmitChanges();
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
            }

        }

        /// <summary>
        /// Updates Job Status 
        /// </summary>
        /// <returns></returns>
        public bool UpdateJobStatus()
        {
            if (objTJob != null)
            {
                /// Get the Patch ID for JobID and call UpdateStatus(int PatchID)
                try
                {
                    #region For Setup Test
                    UpdateJobRuntimeStatus();
                    #endregion For Setup Test

                    #region For SAFX Test
                    UpdateJobSAFXStatus();
                    #endregion For SAFX Test

                    #region Set TJob's  Status & Result & PercentCompleted
                    //Set TJob's  Status & Result & PercentCompleted
                    //do the Percent Completed based on the Percentcompleted for TPatch and TSAFXProjectSubmittedData
                    UpdateTJobStatus(JobID);
                    #endregion
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Comment out abnormal run notification mails since it is too noisy
                //if (ConfigurationManager.AppSettings["IsSendingMail"] == "TRUE")
                //{
                //    //Sends abnormal run IDs for this job
                //    SendMailForAbnormalRun();
                //}
            }
            return true;
        }

        /// <summary>
        /// Updates Job Runtime test Status 
        /// </summary>
        /// <returns></returns>
        private void UpdateJobRuntimeStatus()
        {
            bool IsAllRunCompleted = true;

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var netfxsetupRuns = from c in dataContext.TNetFxSetupRunStatus
                                     where c.JobID == objTJob.JobID && (c.SubmissionType == "Patch" || c.SubmissionType == "HFR")
                                     select c;

                if (netfxsetupRuns.Count() == 0) //Beacon runtime runs
                {
                    var patches = from r in dataContext.TTestProdInfos
                                  where r.JobID == objTJob.JobID
                                  select r;

                    foreach (var patch in patches)
                    {
                        #region Set TPatch, TPatchFile and TRun's Status & Result & TPatchFile's PercentCompleted
                        IsAllRunCompleted &= UpdateStatus(patch.TTestProdInfoID);
                        #endregion

                    }
                }
                else //for runs NetfxSetup runs 
                {
                    string status = "";
                    Helper.RunStatus runStatus;
                    Helper.RunResult runResult;
                    List<TNetFxSetupRunStatus> runs = netfxsetupRuns.ToList<TNetFxSetupRunStatus>();

                    //Read run status and result, and update them to TNetFxSetupRunStatus
                    foreach (TNetFxSetupRunStatus run in runs)
                    {
                        status = MaddogHelper.GetRunStatus((int)run.MDRunID, CurrentUser, 2, true);
                        TranslateRunStatus(status, out runStatus, out runResult);
                        IsAllRunCompleted &= IsRunCompleted(runStatus);

                        run.RunResultID = (short?)runResult;
                        run.RunStatusID = (short?)runStatus;

                        run.LastModifiedBy = CurrentUser;
                        run.LastModifiedDate = DateTime.Now;
                    }

                    dataContext.SubmitChanges();
                }

                if (IsAllRunCompleted)
                    Report(this.JobID, LinqHelper.ReportType.RunTime);
            }
        }

        /// <summary>
        /// Updates Job SAFX test Status 
        /// </summary>
        /// <returns></returns>
        private void UpdateJobSAFXStatus()
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                bool IsAllRunCompleted = true;
                //Gets Run IDs
                var SAFXRunIDs = (from r in dataContext.TSAFXProjectSubmittedDatas
                                  where r.JobID == JobID
                                  select new { r.RunID }).Distinct();
                if (SAFXRunIDs.Count() == 0)
                    return;

                //Since SAFX runs details doesn't be stored in DB just only run ID
                string status = "";
                string owner = "redmond\\vsulab";
                Helper.RunStatus runStatus;
                Helper.RunResult runResult;
                foreach (var runID in SAFXRunIDs)
                {
                    //runID.RunID
                    status = MaddogHelper.GetRunStatus((int)runID.RunID, owner, 2, true);

                    TranslateRunStatus(status, out runStatus, out runResult);
                    IsAllRunCompleted &= IsRunCompleted(runStatus);
                    int percentCompleted = runStatus == Helper.RunStatus.Completed ? 100 : 0;

                    UpdateTSAFXProjectSubmittedDataStatus(runID.RunID, (int)runStatus, (int)runResult, percentCompleted);
                }

                if (IsAllRunCompleted)
                    Report(this.JobID, LinqHelper.ReportType.SAFX);
            }
        }

        private void TranslateRunStatus(string status, out Helper.RunStatus runStatus, out Helper.RunResult runResult)
        {
            if (status == RunStatusPassed || status == RunStatusAnalyzedPassed)
            {
                runStatus = Helper.RunStatus.Completed;
                runResult = Helper.RunResult.Passed;
            }
            else if (status == RunStatusFailed)
            {
                runStatus = Helper.RunStatus.Analyzing;//Analyzing
                runResult = Helper.RunResult.Unknown;//Unknown
            }
            else if (status == RunStatusRunning)
            {
                runStatus = Helper.RunStatus.Running;//Running
                runResult = Helper.RunResult.Unknown;//Unknown
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

        private bool IsRunCompleted(Helper.RunStatus runStatus)
        {
            return runStatus != Helper.RunStatus.Running;
        }

        /// <summary>
        /// Send mail about abnormal runs
        /// </summary>
        /// <param name="blnIsAsync"></param>
        public void SendMailForAbnormalRun(bool blnIsAsync = true)
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

            List<TRun> lstAbnormalIntervalRuns = new List<TRun>();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                #region Test if netfxsetup runs
                var netfxsetupRuns = from c in dataContext.TNetFxSetupRunStatus
                                     where c.JobID == objTJob.JobID && (c.SubmissionType == "Patch" || c.SubmissionType == "HFR")
                                     select c;
                if (netfxsetupRuns.Count() > 0)
                {
                    //select abnormal runs
                    var runs = from r in netfxsetupRuns
                               where r.RunStatusID != (int)Helper.RunStatus.Completed
                               && (r.LastModifiedDate != null && abnormaInterval < (r.LastModifiedDate - r.CreatedDate).TotalHours && (r.LastModifiedDate - r.CreatedDate).TotalHours < deadlineHours)
                               && ((r.LastNotificationMailSent == null) || (r.LastNotificationMailSent != null && (DateTime.Now - (DateTime)r.LastNotificationMailSent).TotalHours > lastMailsentInterval))
                               select r;

                    if (runs.Count() > 0)
                    {
                        //send mail
                        SendMailForAbnormalNetFxSetupRuns(blnIsAsync, runs.ToList(), abnormaInterval);

                        //update last mail sent
                        DateTime dtMailSent = DateTime.Now;
                        foreach (var run in runs)
                        {
                            run.LastNotificationMailSent = dtMailSent;
                        }

                        dataContext.SubmitChanges();
                    }

                    return;
                }

                #endregion

                #region Gets Abnormal runs
                var patches = from r in dataContext.TTestProdInfos
                              where r.JobID == objTJob.JobID
                              select r;
                foreach (var patch in patches)
                {
                    var patchFiles = from c in dataContext.TTestProdAttributes where c.TTestProdInfoID == patch.TTestProdInfoID select c;

                    foreach (var patchFile in patchFiles)
                    {
                        var runs = from r in dataContext.TRuns
                                   where r.TTestProdAttributesID == patchFile.TestProdAttributesID
                                   && r.RunStatusID != (int)Helper.RunStatus.Completed
                                   && (r.LastModifiedDate != null && (r.LastModifiedDate - r.CreatedDate).TotalHours > abnormaInterval && (r.LastModifiedDate - r.CreatedDate).TotalHours < deadlineHours)
                                   && ((r.LastNotificationMailSent == null) ||
                                   (r.LastNotificationMailSent != null && (DateTime.Now - (DateTime)r.LastNotificationMailSent).TotalHours > lastMailsentInterval))
                                   select r;
                        lstAbnormalIntervalRuns.AddRange(runs.ToList());
                    }
                }
                #endregion

                #region Sends mail
                //don't send mail if there is no abnormal runs
                if (lstAbnormalIntervalRuns.Count > 0)
                {

                    MailHelper mailHelper = new MailHelper(Environment.UserName);

                    mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
                    mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

                    if (blnIsAsync)
                    {
                        Action action = new Action(delegate()
                        {
                            mailHelper.SendMailAboutAbnormalRun(objTJob, lstAbnormalIntervalRuns, abnormaInterval);
                        });

                        action.BeginInvoke(null, null);
                    }
                    else
                    {
                        mailHelper.SendMailAboutAbnormalRun(objTJob, lstAbnormalIntervalRuns, abnormaInterval);
                    }
                }
                #endregion

                #region Updates LastNotificationMailSent in TRun
                //gets send mail datetime
                DateTime dtLastNotificationMailSent = DateTime.Now;
                var runIDs = from r in lstAbnormalIntervalRuns select r.RowID;
                var runsforUpdates = from run in dataContext.TRuns where runIDs.Contains(run.RowID) select run;
                foreach (var run in runsforUpdates)
                {
                    run.LastNotificationMailSent = dtLastNotificationMailSent;
                }

                dataContext.SubmitChanges();
                #endregion
            }

        }

        /// <summary>
        /// Send mail about abnormal runs for NetFXSetup runs
        /// </summary>
        /// <param name="blnIsAsync"></param>
        private void SendMailForAbnormalNetFxSetupRuns(bool blnIsAsync, List<TNetFxSetupRunStatus> abnormalRuns, int abnormaInterval)
        {

            MailHelper mailHelper = new MailHelper(Environment.UserName);

            mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
            mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

            if (blnIsAsync)
            {
                Action action = new Action(delegate()
                {
                    mailHelper.SendMailAboutAbnormalRun(objTJob, abnormalRuns, abnormaInterval);
                });

                action.BeginInvoke(null, null);
            }
            else
            {
                mailHelper.SendMailAboutAbnormalRun(objTJob, abnormalRuns, abnormaInterval);
            }
        }

        /// <summary>
        /// Updates Status of TPatch, TPatchFile, TRun
        /// </summary>
        /// <param name="PatchID"></param>
        /// <returns></returns>
        private bool UpdateStatus(int PatchID)
        {
            #region Local variables
            bool IsAllRunCompleted = true;
            bool IsAllRunsPassed = true;

            #endregion

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                #region Pull PatchFiles for a PatchID
                //int abnormaInterval = 0;
                //if (int.TryParse(ConfigurationManager.AppSettings["AbNormalInterval"], out abnormaInterval))
                //{ }
                //List<int> lstAbnormalIntervalRunIDs = new List<int>();
                var patchFiles = from c in dataContext.TTestProdAttributes where c.TTestProdInfoID == PatchID select c;

                #endregion  Pulling PatchFiles bying PatchID

                foreach (var patchFile in patchFiles)
                {
                    #region Pull Runs for each PatchFileID

                    var runs = from r in dataContext.TRuns where r.TTestProdAttributesID == patchFile.TestProdAttributesID select r;

                    #endregion Pulling Runs bying PatchFileID

                    #region Update TRun's Status & Result
                    //int TotalRunsCount = runs.Count();
                    //int PassedRunsCount = 0;
                    //int FailedRunsCount = 0;
                    //int RunningRunsCount = 0;
                    foreach (var run in runs)
                    {
                        if (run.MDRunID > 0)
                        {
                            run.Status = MaddogHelper.GetRunStatus(run.MDRunID, run.CreatedBy, (int)run.TRunTemplate.MaddogDBID, true);

                            #region Set TRun's Value for RunStatusID & RunResultID

                            if (run.Status == RunStatusPassed || run.Status == RunStatusAnalyzedPassed)
                            {
                                //accumulate passed runs count
                                //PassedRunsCount++;
                                run.RunStatusID = (int)Helper.RunStatus.Completed;//Completed
                                run.RunResultID = (int)Helper.RunResult.Passed;//Passed
                            }
                            else if (run.Status == RunStatusFailed)
                            {
                                //accumulate failed runs count
                                //FailedRunsCount++;
                                run.RunStatusID = (int)Helper.RunStatus.Analyzing;//Analyzing
                                run.RunResultID = (int)Helper.RunResult.Unknown;//Unknown
                            }
                            else if (run.Status == RunStatusRunning)
                            {
                                IsAllRunCompleted = false;
                                //accumulate running runs count
                                //RunningRunsCount++;
                                run.RunStatusID = (int)Helper.RunStatus.Running;//Running
                                run.RunResultID = (int)Helper.RunResult.Unknown;//Unknown
                            }
                            else if (run.Status == RunStatusAnalyzedFailed)
                            {
                                run.RunStatusID = (int)Helper.RunStatus.Completed;//Completed
                                run.RunResultID = (int)Helper.RunResult.Failed;//Failed
                            }
                            else
                            {
                                run.RunStatusID = (int)Helper.RunStatus.Error;//Error
                                run.RunResultID = (int)Helper.RunResult.Error;//Error
                            }

                            #endregion Set TRun's Value for RunStatusID & RunResultID
                            if (run.Status != RunStatusPassed)
                            {
                                IsAllRunsPassed = false;
                            }

                            //Update LastModifiedBy & LastModifiedDateTime
                            run.LastModifiedBy = CurrentUser;
                            run.LastModifiedDate = DateTime.Now;

                            //TimeSpan ts = run.LastModifiedDate - run.CreatedDate;
                            //if (ts.TotalHours > abnormaInterval)
                            //    lstAbnormalIntervalRunIDs.Add(run.ExecutionSystemRunID);
                        }
                    }

                    //Update RunStatusID & RunResultID
                    dataContext.SubmitChanges();
                    #endregion Update Status & Result

                    #region UPdate TPatchFile Status & Result & PercentCompleted

                    UpdateTPatchFileStatus(patchFile.TestProdAttributesID);

                    //if (ConfigurationManager.AppSettings["IsSendingMail"] == "TRUE")
                    //{
                    //    SendMailForAbnormalRun(lstAbnormalIntervalRunIDs, abnormaInterval);
                    //}
                    #endregion UPdate TPatchFile Status & Result & PercentCompleted

                }

                #region Set Patch's Status & Result & PercentCompleted

                UpdateTPatchStatus(PatchID);

                #endregion
            }


            ///ToDo: Get all the patch Files for this Patch ID and then pull all the RunIDs
            ///Call Maddog Helper class to pull the status from Maddog 
            ///Update TRun table with the status and result info
            ///
            ///If all Runs are either Passed or Failed, return True or else False
            ///Update TPatchFile record with the Status and Result info
            ///Also calculate the PercentCompleted
            ///Also update the TPatch and TJob table with the status and Result and PercentCompleted

            return IsAllRunCompleted;
        }

        /// <summary>
        /// Updates TJob's Status, Result, PercentCompleted
        /// </summary>
        /// <param name="jobID"></param>
        private void UpdateTJobStatus(long jobID)
        {
            //Calc base on TPatch & TSAFXProjectSubmittedData
            //int TotalCout=patches.Count<TPatch>()+TSAFXProjectSubmittedData.Count
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                int TotalCout = 0;
                int CompletedCount = 0;
                int SAFXCount = 0;
                int SetupCount = 0;

                //Set TJob's  Status & Result & PercentCompleted
                //do the Percent Completed based on the Percentcompleted for TPatch and TSAFXProjectSubmittedData
                var tJob = dataContext.TJobs.SingleOrDefault(c => c.JobID == objTJob.JobID);

                #region Calc SAFX PercentCompleteds
                var SAFXRunIDs = from r in dataContext.TSAFXProjectSubmittedDatas
                                 where r.JobID == JobID && r.SAFXProjectInputDataID == 5
                                 select r;

                SAFXCount = SAFXRunIDs.Count();

                int SAFXCompleted = 0;
                int SAFXPassedCount = 0;
                int SAFXAnalysisFailedCount = 0;
                int SAFXRunningCount = 0;
                int SAFXFailedCount = 0;
                int SAFXErrorCount = 0;
                foreach (var run in SAFXRunIDs)
                {
                    //Since there is one for each platform in SAFX
                    //For each run of SAFX, just only 2 for percentcompleted, one is 100, the other is zero
                    SAFXCompleted += (run.PercentCompleted == null ? 0 : (int)run.PercentCompleted);
                    if (run.StatusID == (int)Helper.RunStatus.Completed)
                    {
                        if (run.ResultID == (int)Helper.RunResult.Passed)
                            SAFXPassedCount++;
                        else
                            SAFXAnalysisFailedCount++;
                    }
                    else if (run.StatusID == (int)Helper.RunStatus.Running)
                    {
                        SAFXRunningCount++;
                    }
                    else if (run.StatusID == (int)Helper.RunStatus.Analyzing)
                    {
                        SAFXFailedCount++;
                    }
                    else
                    {
                        SAFXErrorCount++;
                    }
                }

                #endregion

                #region Calc Setup PerconetCompleteds

                int SetupPassedCount = 0;
                int SetupAnalysisFailedCount = 0;
                int SetupRunningCount = 0;
                int SetupFailedCount = 0;
                int SetupErrorCount = 0;
                int SetupCompleted = 0;

                var netfxsetupRuns = from c in dataContext.TNetFxSetupRunStatus
                                     where c.JobID == objTJob.JobID && (c.SubmissionType == "Patch" || c.SubmissionType == "HFR")
                                     select c;
                if (netfxsetupRuns.Count() == 0) //Beacon runtime runs
                {
                    var setupPatches = from r in dataContext.TTestProdInfos
                                       where r.JobID == objTJob.JobID
                                       select r;
                    SetupCount = setupPatches.Count();

                    foreach (var patch in setupPatches)
                    {
                        SetupCompleted += (patch.PercentCompleted == null ? 0 : (int)patch.PercentCompleted);
                        if (patch.StatusID == (int)Helper.RunStatus.Completed)
                        {
                            if (patch.ResultID == (int)Helper.RunResult.Passed)
                                SetupPassedCount++;
                            else if (patch.ResultID == (int)Helper.RunResult.Failed)
                                SetupAnalysisFailedCount++;
                        }
                        else if (patch.StatusID == (int)Helper.RunStatus.Running)
                        {
                            SetupRunningCount++;
                        }
                        else if (patch.StatusID == (int)Helper.RunStatus.Analyzing)
                        {
                            SetupFailedCount++;
                        }
                        else
                        {
                            SetupErrorCount++;
                        }
                    }
                }
                else //NetFxSetup Runtime runs
                {
                    SetupCount = netfxsetupRuns.Count();

                    foreach (var run in netfxsetupRuns)
                    {
                        if (run.RunStatusID == (int)Helper.RunStatus.Completed)
                        {
                            SetupCompleted += 100;
                            if (run.RunResultID == (int)Helper.RunResult.Passed)
                                SetupPassedCount++;
                            else if (run.RunResultID == (int)Helper.RunResult.Failed)
                                SetupAnalysisFailedCount++;
                        }
                        else if (run.RunStatusID == (int)Helper.RunStatus.Running)
                        {
                            SetupRunningCount++;
                        }
                        else if (run.RunStatusID == (int)Helper.RunStatus.Analyzing)
                        {
                            SetupFailedCount++;
                        }
                        else
                        {
                            SetupErrorCount++;
                        }
                    }
                }

                #endregion

                CompletedCount = SetupCompleted + SAFXCompleted;
                TotalCout = SAFXCount + SetupCount;

                if (TotalCout > 0)
                {
                    tJob.PercentCompleted = CompletedCount / TotalCout;

                    if (SAFXPassedCount + SetupPassedCount == TotalCout)
                    {
                        tJob.StatusID = (int)Helper.RunStatus.Completed;
                        tJob.ResultID = (int)Helper.RunResult.Passed;
                    }
                    else if (SAFXAnalysisFailedCount + SetupAnalysisFailedCount > 0 && tJob.PercentCompleted == 100)
                    {
                        tJob.StatusID = (int)Helper.RunStatus.Completed;
                        tJob.ResultID = (int)Helper.RunResult.Failed;
                    }
                    else if (SAFXRunningCount + SetupRunningCount > 0)
                    {
                        tJob.StatusID = (int)Helper.RunStatus.Running;
                        tJob.ResultID = (int)Helper.RunResult.Unknown;
                    }
                    else if (SAFXFailedCount + SetupFailedCount > 0)
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
                    tJob.LastModifiedBy = CurrentUser;
                    tJob.LastModifiedDate = DateTime.Now;

                    dataContext.SubmitChanges();
                }
            }
        }

        /// <summary>
        /// Updates TPatch's Status, Result, PercentCompleted
        /// </summary>
        /// <param name="patchID"></param>
        private void UpdateTPatchStatus(int patchID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var patch = dataContext.TTestProdInfos.SingleOrDefault(c => c.TTestProdInfoID == patchID);

                #region Set Patch's Status & Result & PercentCompleted
                //update TPatch's Status & Result & PercentCompleted
                var patchFiles = from r in dataContext.TTestProdAttributes
                                 where r.TTestProdInfoID == patchID
                                 select r;

                int PercentCompleted = 0;
                int PatchFilesCount = patchFiles.Count<TTestProdAttribute>();

                int PassedCount = 0;
                int AnalysisFailedCount = 0;
                int FailedCount = 0;
                int RunningCount = 0;
                int ErrorCount = 0;

                if (PatchFilesCount > 0)
                {
                    foreach (var patchFile in patchFiles)
                    {
                        PercentCompleted += patchFile.PercentCompleted == null ? 0 : (int)patchFile.PercentCompleted;

                        if (patchFile.StatusID == (int)Helper.RunStatus.Completed)
                        {
                            if (patchFile.ResultID == (int)Helper.RunResult.Passed)
                                PassedCount++;
                            else if (patchFile.ResultID == (int)Helper.RunResult.Failed)
                                AnalysisFailedCount++;
                        }
                        else if (patchFile.StatusID == (int)Helper.RunStatus.Analyzing)
                        {
                            FailedCount++;
                        }
                        else if (patchFile.StatusID == (int)Helper.RunStatus.Running)
                        {
                            RunningCount++;
                        }
                        else
                        {
                            ErrorCount++;
                        }
                    }

                    patch.PercentCompleted = PercentCompleted / PatchFilesCount;

                    if (PassedCount == PatchFilesCount)
                    {
                        patch.StatusID = (int)Helper.RunStatus.Completed;//Completed
                        patch.ResultID = (int)Helper.RunResult.Passed;//Passed
                    }
                    else if (AnalysisFailedCount > 0 && (AnalysisFailedCount + PassedCount == PatchFilesCount))
                    {
                        patch.StatusID = (int)Helper.RunStatus.Completed;//Completed
                        patch.ResultID = (int)Helper.RunResult.Failed;//Passed
                    }
                    else if (RunningCount > 0)
                    {
                        patch.StatusID = (int)Helper.RunStatus.Running;//Running
                        patch.ResultID = (int)Helper.RunResult.Unknown;//Unknown
                    }
                    else if (FailedCount > 0)
                    {
                        patch.StatusID = (int)Helper.RunStatus.Analyzing;//Analyzing
                        patch.ResultID = (int)Helper.RunResult.Unknown;//Unknown
                    }
                    else if (ErrorCount > 0)
                    {
                        patch.StatusID = (int)Helper.RunStatus.Error;//Error
                        patch.ResultID = (int)Helper.RunResult.Unknown;//Unknown
                    }
                }

                //Update TPatch's the LastModifiedBy and LastModifiedDateTime 
                patch.LastModifiedBy = CurrentUser;
                patch.LastModifiedDate = DateTime.Now;

                dataContext.SubmitChanges();
                #endregion Set Patch's Status & Result & PercentCompleted
            }
        }

        /// <summary>
        /// Updates TpatchFile's Status , Result, PercentCompleted
        /// </summary>
        /// <param name="patchFileID"></param>
        private void UpdateTPatchFileStatus(int patchFileID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var patchFile = dataContext.TTestProdAttributes.SingleOrDefault(c => c.TestProdAttributesID == patchFileID);

                var runs = from r in dataContext.TRuns where r.TTestProdAttributesID == patchFile.TestProdAttributesID select r;
                int TotalRunsCount = runs.Count<TRun>();
                int PassedCount = 0;
                int FailedCount = 0;
                int AnalysisFailedCount = 0;
                int RunningCount = 0;

                foreach (var run in runs)
                {
                    if (run.RunStatusID == (int)Helper.RunStatus.Analyzing)
                    {
                        FailedCount++;
                    }
                    else if (run.RunStatusID == (int)Helper.RunStatus.Completed)
                    {
                        if (run.RunResultID == (int)Helper.RunResult.Passed)
                            PassedCount++;
                        else if (run.RunResultID == (int)Helper.RunResult.Failed)
                            AnalysisFailedCount++;
                    }
                    else if (run.RunStatusID == (int)Helper.RunStatus.Running)
                    {
                        RunningCount++;
                    }
                }

                if (TotalRunsCount > 0)
                {
                    patchFile.PercentCompleted = ((PassedCount + AnalysisFailedCount) * 100) / TotalRunsCount;

                    if (PassedCount == TotalRunsCount)
                    {
                        patchFile.StatusID = (int)Helper.RunStatus.Completed;
                        patchFile.ResultID = (int)Helper.RunResult.Passed;
                    }
                    else if (AnalysisFailedCount > 0 && RunningCount == 0)
                    {
                        patchFile.StatusID = (int)Helper.RunStatus.Completed;
                        patchFile.ResultID = (int)Helper.RunResult.Failed;
                    }
                    else if (RunningCount > 0)
                    {
                        patchFile.StatusID = (int)Helper.RunStatus.Running;
                        patchFile.ResultID = (int)Helper.RunResult.Unknown;
                    }
                    else if (FailedCount > 0)
                    {
                        patchFile.StatusID = (int)Helper.RunStatus.Analyzing;
                        patchFile.ResultID = (int)Helper.RunResult.Unknown;
                    }
                    else
                    {
                        patchFile.StatusID = (int)Helper.RunStatus.Error;
                        patchFile.ResultID = (int)Helper.RunResult.Unknown;
                    }

                    //Update PatchFile's LastModifiedBy and LastModifiedDateTime 
                    patchFile.LastModifiedBy = CurrentUser;
                    patchFile.LastModifiedDate = DateTime.Now;
                }

                dataContext.SubmitChanges();
            }

            return;
        }

        /// <summary>
        /// Updates set of SAFX submited input items' Status, Result, PercentCompleted
        /// </summary>
        /// <param name="runID">Run ID</param>
        /// <param name="statusID">Status ID</param>
        /// <param name="resultID">Result ID</param>
        /// <param name="percentCompleted">Percent completed</param>
        private void UpdateTSAFXProjectSubmittedDataStatus(long? runID, int statusID, int resultID, int percentCompleted)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var datas = from r in dataContext.TSAFXProjectSubmittedDatas
                            where r.JobID == JobID
                            && r.RunID == runID
                            select r;
                foreach (var data in datas)
                {
                    data.StatusID = (short?)statusID;
                    data.ResultID = (short?)resultID;
                    data.PercentCompleted = percentCompleted;
                    //Update TSAFXProjectSubmittedDatas' LastModifiedBy and LastModifiedDateTime
                    data.LastModifiedBy = CurrentUser;
                    data.LastModifiedDate = DateTime.Now;
                }

                dataContext.SubmitChanges();
            }
        }

        /// <summary>
        /// send an email when the Kicking off Runs are done by calling objJob.StartJob
        /// </summary>
        public void SendMailAfterKickingOff(bool blnIsAsync = true)
        {
            //send an email when the Kicking off Runs are done 
            List<RunReport> lstRunReport = this.GetRunReportData();
            if (lstRunReport.Count > 0)
            {
                MailHelper mailHelper = new MailHelper(Environment.UserName);

                mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
                mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

                if (blnIsAsync)
                {
                    Action action = new Action(delegate()
                    {
                        mailHelper.SendMailAfterKickingOff(objTJob, lstRunReport, FileListData);
                    });

                    action.BeginInvoke(null, null);
                }
                else
                {
                    mailHelper.SendMailAfterKickingOff(objTJob, lstRunReport, FileListData);
                }
            }
        }

        /// <summary>
        /// Gets runs for sending mail after Kicking off
        /// </summary>
        /// <returns></returns>
        private List<RunReport> GetRunReportData()
        {
            List<RunReport> lstRunReport = new List<RunReport>();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                #region Gets Setup Automated Runs
                // check if there are any runs with expected job id in TNetFxSetupRunStatus
                var netfxsetupRuns = from c in dataContext.TNetFxSetupRunStatus
                                     where c.JobID == objTJob.JobID && (c.SubmissionType == "Patch" || c.SubmissionType == "HFR")
                                     select c;
                if (netfxsetupRuns.Count() > 0) //This job kicked off NetfxSetup runtime runs
                {
                    List<TNetFxSetupRunStatus> runs = netfxsetupRuns.ToList<TNetFxSetupRunStatus>();
                    foreach (TNetFxSetupRunStatus run in runs)
                    {
                        lstRunReport.Add(new RunReport()
                        {
                            IsSAFXRun = false,
                            RunTitle = run.RunTitle,
                            RunID = run.MDRunID.ToString(),
                            Status = HotFixUtility.GetStatusName((int?)run.RunStatusID)
                        });
                    }
                }
                else //This job kicked off Beacon runtime runs
                {
                    var patchInfo = from c in dataContext.TTestProdAttributes
                                    join p in dataContext.TTestProdInfos on c.TTestProdInfoID equals p.TTestProdInfoID
                                    where p.JobID == objTJob.JobID
                                    select new { c.TestProdAttributesID, p.WorkItem, p.TTestProdInfoID };

                    var setupRuns = from c in dataContext.TRuns
                                    join p in patchInfo on c.TTestProdAttributesID equals p.TestProdAttributesID
                                    where c.JobID == objTJob.JobID
                                    select new { RunID = c.MDRunID, c.RunStatusID, RunTitle = p.WorkItem, IsSAFXRun = false };

                    foreach (var run in setupRuns)
                    {
                        lstRunReport.Add(new RunReport()
                        {
                            IsSAFXRun = run.IsSAFXRun,
                            RunTitle = run.RunTitle,
                            RunID = run.RunID.ToString(),
                            Status = HotFixUtility.GetStatusName((int?)run.RunStatusID)
                        });
                    }
                }
                #endregion

                #region Gets SAFX Automated Runs
                var SAFXRuns = from c in dataContext.TSAFXProjectSubmittedDatas
                               where c.JobID == objTJob.JobID
                               && c.SAFXProjectInputDataID == 5 //this InputData is KBNumber
                               select new { RunID = c.RunID == null ? 0 : (long)c.RunID, RunTitle = "SAFX Run for " + c.FieldValue, IsSAFXRun = true, c.StatusID };
                foreach (var run in SAFXRuns)
                {
                    lstRunReport.Add(new RunReport()
                    {
                        IsSAFXRun = run.IsSAFXRun,
                        RunTitle = run.RunTitle,
                        RunID = run.RunID.ToString(),
                        Status = HotFixUtility.GetStatusName((int?)run.StatusID)
                    });
                }
                #endregion
            }

            return lstRunReport;
        }

        /// <summary>
        /// for each Run if the diff between Created Date and Last Modified Date is more than abNormalInterval hours (keep it configurable), 
        /// send an email with Run ID
        /// </summary>
        /// <param name="abnormalIntervalRunIDs"></param>
        /// <param name="abNormalInterval"></param>
        public void SendMailForAbnormalRun(int abnormalIntervalRunIDs, int abNormalInterval, bool blnIsAsync = true)
        {
            MailHelper mailHelper = new MailHelper(Environment.UserName);

            mailHelper.MailTo = ConfigurationManager.AppSettings["MailTo"];
            mailHelper.MailCC = ConfigurationManager.AppSettings["MailCC"];

            if (blnIsAsync)
            {
                Action action = new Action(delegate()
                {
                    mailHelper.SendMailAboutAbnormalRun(abnormalIntervalRunIDs, abNormalInterval);
                });

                action.BeginInvoke(null, null);
            }
            else
            {
                mailHelper.SendMailAboutAbnormalRun(abnormalIntervalRunIDs, abNormalInterval);
            }
        }

        /// <summary>
        /// for each Run if the diff between Created Date and Last Modified Date is more than abNormalInterval hours (keep it configurable), 
        /// send an email with Run ID
        /// </summary>
        /// <param name="lstAbnormalIntervalRunIDs"></param>
        /// <param name="abNormalInterval"></param>
        private void SendMailForAbnormalRun(List<int> lstAbnormalIntervalRunIDs, int abNormalInterval)
        {
            foreach (int runID in lstAbnormalIntervalRunIDs)
            {
                SendMailForAbnormalRun(runID, abNormalInterval);
            }
        }

        #endregion Member Function

    }
}