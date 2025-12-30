using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HotFixLibrary;
using ScorpionDAL;
using System.Configuration;

namespace ReportEngine
{
    struct PatchTestCase
    {
        public string patch;        // patch file name: kb-9999901-x86.exe
        public uint runID;          // run ID
        public int testID;          // a test case
        public string Name;         // a test case name
        public bool pass;           // success or fail 
    }

    // runID&testID combined is a unique identifier
    public struct FailTestCase
    {
        public uint runID;
        public int testID;
        public string Name;
        public string MaddogURL;
        public string Log;
        public string MachineNameAndID;
        public DateTime startTime;
        public DateTime finishTime;
        public string reproConfig;
    }

    public class Report
    {
        int Mode;     // 0: Fake Data   1: Production Data
        long JobID;
        string kbnumber;
        List<PatchTestCase> thePatchTestCaseSummaryList;
        List<FailTestCase> theFailTestCaseList;
        private string workItemID;

        // The following data will be passed to MailContent for further process
        Dictionary<string, string> parameters;
        MailContentData myMailContentData;

        public LinqHelper.ReportType TheReportType { get; set; }

        public Report(long jobid, LinqHelper.ReportType type)
            : this(jobid, type, 1)
        {
        }

        public Report(long jobid, LinqHelper.ReportType type, int mode)
        {
            this.Mode = mode;
            this.JobID = jobid;
            this.TheReportType = type;
            this.kbnumber = string.Empty;
            thePatchTestCaseSummaryList = new List<PatchTestCase>();
            theFailTestCaseList = new List<FailTestCase>();
            parameters = new Dictionary<string, string>();
        }

        public void RunReport()
        {
            Console.WriteLine("Hello Report");
            IReportDataJobID reportJobData = null;
            DateTime earliestStart = DateTime.MaxValue;
            DateTime latestEnd = DateTime.MinValue;

            if (this.Mode == 1)
            {
                reportJobData = new ProdDataJobID(JobID, this.TheReportType);
            }
            else
            {
                reportJobData = new FakeDataJobID(JobID);
            }

            kbnumber = reportJobData.GetKBNumber();

            List<uint> runIDs = reportJobData.RunIDList;

            // loop through each runs and collect all run status
            foreach (uint runID in runIDs)
            {
                IReportDataRunID runObj = null;
                if (this.Mode == 1)
                {
                    runObj = new ProdDataRunID(runID, (ProdDataJobID)reportJobData);
                }
                else
                {
                    runObj = new FakeDataRunID(runID,JobID);
                }

                if (runObj.StartDate < earliestStart)
                {
                    earliestStart = runObj.StartDate;
                }
                if (runObj.EndDate > latestEnd)
                {
                    latestEnd = runObj.EndDate;
                }

                RunIDDataFields myRunData = runObj.GetDataFields;

                // loop through all test cases for a particular run
                foreach (TestCaseIDDataFields test in myRunData.TestCaseList)
                {
                    // Add all test cases into the big table for summary breakdown
                    PatchTestCase myTest = new PatchTestCase();
                    myTest.patch = myRunData.PatchFile;
                    myTest.runID = runID;
                    myTest.testID = test.ID;
                    myTest.Name = test.Name;
                    myTest.pass = test.Pass;
                    thePatchTestCaseSummaryList.Add(myTest);

                    // Add all failed test cases into the failed report details
                    if (!test.Pass)
                    {
                        FailTestCase myFailedTest = new FailTestCase();
                        myFailedTest.runID = runID;
                        myFailedTest.testID = test.ID;
                        myFailedTest.Name = test.Name;

                        // to do. Need to get the actual time from server
                        // For now, I will use each Run's start and end time given there is no good way
                        // to get the timestamp for each test case yet
                        myFailedTest.startTime = runObj.StartDate;
                        myFailedTest.finishTime = runObj.EndDate;

                        myFailedTest.Log = test.Log;
                        myFailedTest.MachineNameAndID = myRunData.MachineNameAndID;
                        myFailedTest.MaddogURL = myRunData.MaddogGoToURL;
                        myFailedTest.reproConfig = myRunData.ReproConfigDetails;
                        theFailTestCaseList.Add(myFailedTest);
                    }
                }
            }

            // Now let's figure out the patch test summary data fields for the final report
            ProcessTestCaseBreakDown();

            myMailContentData.FailTestCaseList = theFailTestCaseList;

            // Prepare all required data and fill them into parameters
            // to do here
            parameters["JOBID"] = JobID.ToString();
            parameters["KBNUMBER"] = kbnumber;
            // parameters["TOTALTESTCASES"] = thePatchTestCaseSummaryList.Count.ToString();

            //   https://vstfdevdiv.corp.microsoft.com/web/wi.aspx?pcguid=420dbd19-8e06-413c-b33c-9dc64cd44d32&id=970241
            //    <a href="https://vstfdevdiv.corp.microsoft.com/web/wi.aspx?pcguid=420dbd19-8e06-413c-b33c-9dc64cd44d32&id=976807">976807</a>
            this.workItemID = reportJobData.GetTFSWorkItem();
            if (String.IsNullOrEmpty(workItemID))
            {
                // TFS ID may not exist
                parameters["TFSWORKITEM"] = "None";
            }
            else
            {
                parameters["TFSWORKITEM"] = string.Format("{0}{1}{2}{3}", @"<a href=""https://vstfdevdiv.corp.microsoft.com/web/wi.aspx?pcguid=420dbd19-8e06-413c-b33c-9dc64cd44d32&id=",
                workItemID, @""">", workItemID, @"</a>");
            }
            
            parameters["CREATOR"] = reportJobData.GetCreator();
            parameters["CREATIONTIME"] = reportJobData.GetCreationTime().ToString();
            parameters["STARTTIME"] = earliestStart.ToString();
            parameters["ENDTIME"] = latestEnd.ToString();
            TimeSpan diff = latestEnd - earliestStart;
            string duration = string.Empty;
            if (diff.Days == 0)
            {
                duration = string.Format("{0}:{1}:{2}", diff.Hours, diff.Minutes, diff.Seconds);
            }
            else
            {
                duration = string.Format("{0}.{1}:{2}:{3}", diff.Days, diff.Hours, diff.Minutes, diff.Seconds);
            }
            parameters["DURATION"] = duration;

            // For example, Clicking the hyperlinked patch file name in the table will open a Maddog result query of all runs executed for that patch.
            parameters["PATCHHYPERLINKDESC"] = string.Empty;

            if (theFailTestCaseList.Count == 0)
            {
                ProcessSuccessReport();
            }
            else
            {
                ProcessFailReport();
            }
        }

        private void ProcessTestCaseBreakDown()
        {
            // step 1 figure out all patches and test cases
            List<string> patchNameList = new List<string>();
            List<string> testCaseNameList = new List<string>();

            foreach (PatchTestCase test in thePatchTestCaseSummaryList)
            {
                if (!patchNameList.Contains(test.patch))
                {
                    patchNameList.Add(test.patch);
                }
                if (!testCaseNameList.Contains(test.Name))
                {
                    testCaseNameList.Add(test.Name);
                }
            }

            myMailContentData.PatchList = patchNameList;
            myMailContentData.TestList = testCaseNameList;

            // step 2. Figure out pass and fail count per patch per test case
            int TOTALROW = 0;
            int TOTALCOL = 0;
            foreach (string s in patchNameList) TOTALCOL++;
            foreach (string s in testCaseNameList) TOTALROW++;
            TOTALCOL = TOTALCOL * 2;

            int[,] data = new int[TOTALROW, TOTALCOL];
            for (int row=0; row< TOTALROW; row++)
                for (int col = 0; col<TOTALCOL; col++)
                {
                    data[row,col] = 0;
                }
            uint Col = 0;
            uint Row = 0;
            foreach (string patch in patchNameList)
            {
                Row = 0;
                foreach (string name in testCaseNameList)
                {
                    foreach (PatchTestCase myPatchTestCase in thePatchTestCaseSummaryList)
                    {
                        if (myPatchTestCase.patch.Equals(patch) && myPatchTestCase.Name == name)
                        {
                            if (myPatchTestCase.pass)
                            {
                                data[Row, Col]++;            // fill in data in the pass column
                            }
                            else
                            {
                                data[Row, Col + 1]++;        // fill in data in the fail column
                            }
                        }
                    }
                    Row++;
                }
                Col += 2;
            }

            myMailContentData.Breakdowndata = data;
        }

        private void ProcessSuccessReport()
        {
            MailContent mc = new MailContent(MailType.HTML, parameters, TestResult.Success, TheReportType, myMailContentData);
            MailHelper mh = new MailHelper(Environment.UserName + "@microsoft.com", "password", "smtphost.redmond.corp.microsoft.com");

            // Now we are good to send out email
            // to do. 
            ProcessEmail(mh,mc,parameters);
        }

        private void ProcessFailReport()
        {
            MailContent mc = new MailContent(MailType.HTML, parameters, TestResult.Fail, TheReportType, myMailContentData);
            MailHelper mh = new MailHelper(Environment.UserName + "@microsoft.com", "password", "smtphost.redmond.corp.microsoft.com");

            // Now we are good to send out email
            ProcessEmail(mh, mc, parameters);
        }

        private void ProcessInProgressReport()
        {
        }

        private void ProcessEmail(MailHelper myMailHelper, MailContent myMailContent, Dictionary<string, string> parameters)
        {
            MailHelper mh = myMailHelper;
            MailContent mc = myMailContent;
            mh.MailSubject = "success";
            mh.MailBody = mc.Generate();
            // mh.MailTo = "simwu@microsoft.com;ashk@microsoft.com;sophyw@microsoft.com";

            string mailTo = ConfigurationManager.AppSettings["MailTo"];
            string mailCC = ConfigurationManager.AppSettings["MailCC"];
            if (String.IsNullOrEmpty(mailTo))
            {
                mh.MailTo = "fxsetupauto@microsoft.com";
                mh.MailCC = "charitys@microsoft.com";
            }
            else
            {
                mh.MailTo = ProcessMailAddresses(mailTo);
                mh.MailCC = ProcessMailAddresses(mailCC);
            }

            string title = "title";
            if (mc.result == TestResult.Success)
            {
                if (mc.reportType == LinqHelper.ReportType.SAFX)
                {
                    title = string.Format(@"PASS: [Scorpion] SAFX Test Results for TFS # {0}", this.workItemID);
                }
                else if (mc.reportType == LinqHelper.ReportType.RunTime)
                {
                    title = string.Format(@"PASS: [Scorpion] Runtime Test Results for TFS # {0}", this.workItemID);
                }
                else if (mc.reportType == LinqHelper.ReportType.WUAutomation)
                {
                    title = string.Format(@"PASS: [Scorpion] WU Automation Test Result for WU Job # {0}", this.JobID);
                }
            }
            else if (mc.result == TestResult.Fail)
            {
                if (mc.reportType == LinqHelper.ReportType.SAFX)
                {
                    title = string.Format(@"FAIL: [Scorpion] SAFX Test Results for TFS # {0}", this.workItemID);
                }
                else if (mc.reportType == LinqHelper.ReportType.RunTime)
                {
                    title = string.Format(@"FAIL: [Scorpion] Runtime Test Results for TFS # {0}", this.workItemID);
                }
                else if (mc.reportType == LinqHelper.ReportType.WUAutomation)
                {
                    title = string.Format(@"FAIL: [Scorpion] WU Automation Test Results for WU Job # {0}", this.JobID);
                }
            }

            if (this.Mode == 1)
            {
                mc.Save();
                mh.SendMail(title, mc.GetContent());
            }
            else
            {
                mc.Save();
            }
        }

        private string ProcessMailAddresses(string mailAddresses)
        {
            string[] addresses = mailAddresses.Split(new char[] { ';' });
            StringBuilder sb = new StringBuilder();
            foreach(string s in addresses)
            {
                sb.Append(s);
                
                if(!s.Contains('@'))
                {
                    sb.Append("@microsoft.com");
                }

                sb.Append(";");
            }

            sb.Remove(sb.Length-1, 1);
            return sb.ToString();
        }
    }
}
