using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using System.Data.Linq;

using ScorpionDAL;
using HotFixLibrary;

namespace ReportEngine
{
    class ProdDataJobID : IReportDataJobID
    {
        long jobid;
        LinqHelper.JobIDReportData myJobIDReportData;

        public ProdDataJobID(long jobid, LinqHelper.ReportType type)
        {
            this.jobid = jobid;

            if (type != LinqHelper.ReportType.WUAutomation) //setup run reports
                myJobIDReportData = LinqHelper.GetPTATReportData(this.JobID, type);
            else
                myJobIDReportData = LinqHelper.GetWUAutomationReportData(this.JobID);
        }

        public long JobID
        {
            get
            {
                return jobid;
            }
        }

        public string GetKBNumber()
        {
            List<LinqHelper.RunIDData> RunIDList = myJobIDReportData.RunIDList;
            if (RunIDList.Count > 0)
            {
                LinqHelper.RunIDData myRun = RunIDList.First();
                return myRun.KBNumber;
            }
            return string.Empty;
        }

        public string GetTFSWorkItem()
        {
            return myJobIDReportData.TFSWorkItem;
        }

        public string GetCreator()
        {
            return myJobIDReportData.Creator;
        }

        public DateTime GetCreationTime()
        {
            return myJobIDReportData.CreationTime;
        }

        public LinqHelper.RunIDData GetRunIDDataObject(int runID)
        {
            foreach (LinqHelper.RunIDData runObj in myJobIDReportData.RunIDList)
            {
                if (runObj.RunID == runID)
                {
                    return runObj;
                }
            }
            return null;
        }

        public List<uint> RunIDList
        {
            get
            {
                List<uint> theList = new List<uint>();
                foreach (LinqHelper.RunIDData run in myJobIDReportData.RunIDList)
                {
                    theList.Add((uint)run.RunID);
                }

                // to do: uncomment this out. For prod testing purpose. hardcode it since we do not have a good 
                // database for testing prior to Jun 8th,2012
                // theList = new List<uint> { 1123571, 1764763 };

                return theList;
            }
        }
    }

    class ProdDataRunID : IReportDataRunID
    {
        private uint runID;
        private MaddogHelper.RunIDReportData myRunIDReportData;
        private RunIDDataFields myRunIDDataFields;
        ProdDataJobID jobObj;

        public List<TestCaseIDDataFields> testCaseTable;

        public ProdDataRunID(uint myRunID, ProdDataJobID myJobObj)
        {
            this.runID = myRunID;
            this.testCaseTable = new List<TestCaseIDDataFields>();
            this.myRunIDReportData = MaddogHelper.GetMadDogReportData((int)this.runID);
            this.jobObj = myJobObj;

            // Get the test case table
            foreach (var testcase in this.myRunIDReportData.TestCaseList)
            {
                TestCaseIDDataFields myTest = new TestCaseIDDataFields();
                myTest.ID = testcase.ID;
                myTest.Name = testcase.Name;
                myTest.Pass = testcase.Pass;
                myTest.Log = testcase.Log;
                testCaseTable.Add(myTest);
            }
        }

        public uint RunID
        {
            get
            {
                // For example, MDSQL3\OrcasTS   intMaddogRunID = 1764763;
                return runID;
            }
            set
            {
                this.runID = value;
            }
        }

        public TestResult Result
        {
            get
            {
                TestResult myResult = TestResult.Success;
                foreach (TestCaseIDDataFields test in this.testCaseTable)
                {
                    if (!test.Pass)
                    {
                        myResult = TestResult.Fail;
                    }
                }
                return myResult;
            }
        }
        public List<uint> TestCasePassedList
        {
            get
            {
                List<uint> theList = new List<uint>();
                foreach (TestCaseIDDataFields test in this.testCaseTable)
                {
                    if (test.Pass)
                    {
                        theList.Add((uint)test.ID);
                    }
                }
                return theList;
            }
        }
        public List<uint> TestCaseFailedList
        {
            get
            {
                List<uint> theList = new List<uint>();
                foreach (TestCaseIDDataFields test in this.testCaseTable)
                {
                    if (!test.Pass)
                    {
                        theList.Add((uint)test.ID);
                    }
                }
                return theList;
            }
        }

        public string Title
        {
            get
            {
                return string.Empty;
            }
        }

        public DateTime StartDate
        {
            get
            {
                return myRunIDReportData.StartTime;
            }
        }

        public DateTime EndDate
        {
            get
            {
                return myRunIDReportData.EndTime;
            }
        }

        public RunIDDataFields GetDataFields
        {
            get
            {
                myRunIDDataFields = new RunIDDataFields();
                myRunIDDataFields.RunID = myRunIDReportData.ID;
                myRunIDDataFields.Arch = string.Empty;

                myRunIDDataFields.PatchFile = jobObj.GetRunIDDataObject((int)runID).PatchFile;

                myRunIDDataFields.TestCaseList = testCaseTable;
                myRunIDDataFields.MachineNameAndID = myRunIDReportData.MachineNameAndID;
                myRunIDDataFields.MaddogGoToURL = myRunIDReportData.MaddogGoToURL;
                myRunIDDataFields.ReproConfigDetails = myRunIDReportData.ReproConfigDetails;

                return myRunIDDataFields;
            }
        }
    }

    class ProdDataTestCase : IReportDataTestCase
    {
        private TestCaseIDDataFields myTestCase;
        private uint id;

        public ProdDataTestCase(uint testid, List<TestCaseIDDataFields> myTable)
        {
            myTestCase = new TestCaseIDDataFields();
            this.id = testid;

            foreach (var test in myTable)
            {
                if (test.ID == testid)
                {
                    myTestCase = test;
                    return;
                }
            }
            // to do. it should not come this far. If yes, log some error here
            this.id = 0;

        }
        public uint TestCaseID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }
        public TestResult Result
        {
            get
            {
                return myTestCase.Pass ? TestResult.Success : TestResult.Fail;
            }
        }

        public string Name
        {
            get
            {
                return myTestCase.Name;
            }
        }

        public string LogDetails
        {
            get
            {
                return myTestCase.Log;
            }
        }
    }
}
