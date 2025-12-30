using System;
using System.Collections.Generic;
using System.Linq;
using ScorpionDAL;

namespace Helper
{
    public class JobHelper
    {
        public static List<TJob> GetJobByTFSIDAfterSpecifiedTime(int id, DateTime specifiedTime)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                return dataContext.TJobs.Where(p => p.PID == id && p.CreatedDate > specifiedTime && p.StatusID!=4 ).OrderByDescending(p => p.JobID).ToList();
            }
        }

        public static void InsertJob(int tfsID,string title,string creator)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                dataContext.TJobs.InsertOnSubmit(
                    new TJob() {
                        Active=true,
                        PID = tfsID,
                        JobDescription=title,
                        RequestID=new Random().Next(0,5000),
                        StatusID=3,
                        ResultID=1,
                        PercentCompleted=0,
                        CreatedBy=creator,
                        CreatedDate=DateTime.Now
                    });
                dataContext.SubmitChanges();
            }
        }
    }
}
