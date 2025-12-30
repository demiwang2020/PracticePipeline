using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotFixLibrary;
using DataAggregator;
using ScorpionDAL;

namespace WorkerProcess
{
    public static class JobHandler
    {
        #region Interface to Work Queue Monitor

        public delegate bool AsyncDelegate();

        public static bool StartScheduledJobs(bool blnIsAsync = true)
        {
            /// 1. Go to Job Tables and look for the tasks whose state is "NotStarted" and Active
            /// For each such jobs, create a Job object and ASYNC call StartJob function 
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var jobs = from r in dataContext.TJobs
                           where r.StatusID == (int)Helper.RunStatus.NotStarted
                           && r.Active == true
                           select r;
                List<TJob> lstJobs = jobs.ToList<TJob>();

                // set all jobs pending to avoid other threads to handle them
                foreach (TJob job in lstJobs)
                {
                    SetTJobPending(job.JobID);
                }

                foreach (TJob job in lstJobs)
                {
                    //create a Job object
                    Job objJob = new Job(job.JobID);

                    if (blnIsAsync)
                    {
                        AsyncDelegate asyncDelegate = new AsyncDelegate(objJob.StartJob);
                        asyncDelegate.BeginInvoke(null, null);
                    }
                    else
                    {
                        try
                        {
                            objJob.StartJob();
                        }
                        catch (Exception ex)
                        {
                            int index = lstJobs.IndexOf(job);
                            for (int i = index + 1; i < lstJobs.Count; ++i)
                            {
                                lstJobs[i].StatusID = (int)Helper.RunStatus.NotStarted;
                            }
                            if (index + 1 < lstJobs.Count)
                                dataContext.SubmitChanges();

                            throw;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// picked the job and update status & result to pending befor start it
        /// </summary>
        /// <param name="jobID"></param>
        private static void SetTJobPending(long jobID)
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var job = dataContext.TJobs.SingleOrDefault(c => c.JobID == jobID);
                job.StatusID = (int)Helper.RunStatus.Pending;
                job.ResultID = (int)Helper.RunResult.Pending;

                dataContext.SubmitChanges();
            }
        }

        public static bool UpdateWorkStatus(bool blnIsAsync = true)
        {
            /// 1. Go to Job Tables and look for the tasks whose state is not "Completed" and Active
            /// For each such jobs, create a Job object and ASYNC call UpdateJobStatus function 
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                //1. Update smoke test jobs
                var jobs = from r in dataContext.TJobs
                           where (
                                      r.StatusID == (int)Helper.RunStatus.Running
                                   || r.StatusID == (int)Helper.RunStatus.Analyzing
                                 )
                            && r.Active == true
                           select r;

                foreach (var job in jobs)
                {
                    Job objJob = new Job(job.JobID);
                    if (blnIsAsync)
                    {
                        AsyncDelegate asyncDelegate = new AsyncDelegate(objJob.UpdateJobStatus);
                        asyncDelegate.BeginInvoke(null, null);
                    }
                    else
                    {
                        objJob.UpdateJobStatus();
                    }
                }

                //2. Update WU test jobs
                var wujobs = from r in dataContext.TWUJobs
                             where (
                                       r.StatusID == (int)Helper.RunStatus.Running
                                    || r.StatusID == (int)Helper.RunStatus.Analyzing
                                   )
                             && r.Active == true
                             select r;
                foreach (var wujob in wujobs)
                {
                    WUJob objJob = new WUJob(wujob.ID);
                    if (blnIsAsync)
                    {
                        AsyncDelegate asyncDelegate = new AsyncDelegate(objJob.UpdateJobStatus);
                        asyncDelegate.BeginInvoke(null, null);
                    }
                    else
                    {
                        objJob.UpdateJobStatus();
                    }
                }
            }
            return true;
        }

        #endregion Interface Work Queue Monitor
    }
}
