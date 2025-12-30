using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportEngine
{
    class FakeDataSource
    {
        public static Dictionary<long, List<RunIDDataFields>> theFakeDataSource = create();

        private static Dictionary<long,List<RunIDDataFields>> create()
        {
            Dictionary<long, List<RunIDDataFields>> myDict = new Dictionary<long, List<RunIDDataFields>>();

            // Let's create raw data for 4 job IDs. 
            //    1: SAFX Success     
            //    2: SAFX Fail
            //    3. RunTime Success
            //    4. Runtime Fail

            for (long job = 1; job <= 4; job++)
            {
                List<RunIDDataFields> myRunList = new List<RunIDDataFields>();
                if (job == 1)
                {
                    for (int run = 1; run <= 12; run++)
                    {
                        string arch = GetArch(run);
                        List<TestCaseIDDataFields> myTestList = new List<TestCaseIDDataFields>();
                        for (int test=1; test<=5;test++)
                        {
                            bool myPass = true;
                            TestCaseIDDataFields myTest = new TestCaseIDDataFields(test, "test case name decscription " + test, myPass, "test" + test + ".log");
                            myTestList.Add(myTest);
                        }
                        RunIDDataFields myRun = new RunIDDataFields(run, arch, "NDP_KBnnnnnnn_" + arch + ".exe", myTestList, "MyPC1:0001", @"http://www.youtube.com", "Windows 7:" + arch);
                        myRunList.Add(myRun);
                    }
                }
                else if (job == 2)
                {
                    for (int run = 1; run <= 12; run++)
                    {
                        string arch = GetArch(run);
                        List<TestCaseIDDataFields> myTestList = new List<TestCaseIDDataFields>();
                        for (int test = 1; test <= 5; test++)
                        {
                            bool myPass = ((run %2 == 1) || (test % 2 == 1));
                            TestCaseIDDataFields myTest = new TestCaseIDDataFields(test, "test case name decscription " + test, myPass, "test" + test + ".log");
                            myTestList.Add(myTest);
                        }
                        RunIDDataFields myRun = new RunIDDataFields(run, arch, "NDP_KBnnnnnnn_" + arch + ".exe", myTestList, "MyPC2:0002", @"http://goto.maddog", "Windows 7:" + arch);
                        myRunList.Add(myRun);
                    }
                }
                else if (job == 3)
                {
                    for (int run = 1; run <= 12; run++)
                    {
                        string arch = GetArch(run);
                        List<TestCaseIDDataFields> myTestList = new List<TestCaseIDDataFields>();
                        for (int test = 1; test <= 1; test++)
                        {
                            bool myPass = true;
                            TestCaseIDDataFields myTest = new TestCaseIDDataFields(test, "test case name decscription " + test, myPass, "test" + test + ".log");
                            myTestList.Add(myTest);
                        }
                        RunIDDataFields myRun = new RunIDDataFields(run, arch, "NDP_KBnnnnnnn_" + arch + ".exe", myTestList, "MyPC3:0003", @"http://goto.maddog", "Windows 7:" + arch);
                        myRunList.Add(myRun);
                    }
                }
                else
                {
                    for (int run = 1; run <= 12; run++)
                    {
                        string arch = GetArch(run);
                        List<TestCaseIDDataFields> myTestList = new List<TestCaseIDDataFields>();
                        for (int test = 1; test <= 1; test++)
                        {
                            bool myPass = (run % 2 == 0);
                            TestCaseIDDataFields myTest = new TestCaseIDDataFields(test, "test case name decscription " + test, myPass, "test" + test + ".log");
                            myTestList.Add(myTest);
                        }
                        RunIDDataFields myRun = new RunIDDataFields(run, arch, "NDP_KBnnnnnnn_" + arch + ".exe", myTestList, "MyPC4:0004", @"http://goto.maddog", "Windows 7:" + arch);
                        myRunList.Add(myRun);
                    }
                }
                myDict.Add(job, myRunList);
            }
            return myDict;
        }

        private static string GetArch(int run)
        {
            if (run % 3 == 1)
            {
                return "X86";
            }
            else if (run % 3 == 2)
            {
                return "AMD64";
            }
            else
            {
                return "ARM";
            }
        }
    }

    class FakeDataJobID : IReportDataJobID
    {
        long jobid;

        public FakeDataJobID(long jobid)
        {
            this.jobid = jobid;
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
            return (9999900 + (int)jobid).ToString();
        }

        public string GetTFSWorkItem()
        {
            return "invalidID";
        }

        public string GetCreator()
        {
            return @"universe\god";
        }

        public DateTime GetCreationTime()
        {
            return DateTime.Now;
        }

        public List<uint> RunIDList
        {
            get
            {
                List<uint> aList = new List<uint>();
                foreach (long key in FakeDataSource.theFakeDataSource.Keys)
                {
                    if (key == jobid)
                    {
                        List<RunIDDataFields> myRunList = FakeDataSource.theFakeDataSource[key];
                        foreach (RunIDDataFields run in myRunList)
                        {
                            aList.Add((uint)run.RunID);
                        }
                    }
                }
                return aList;
            }
        }
    }

    class FakeDataRunID: IReportDataRunID
    {
        private uint runID;
        private long jobID;
        private RunIDDataFields myRunIDDataFields;

        public FakeDataRunID(uint runIDx, long job)
        {
            this.runID = runIDx;
            this.jobID = job;
            myRunIDDataFields = new RunIDDataFields();
            foreach (long key in FakeDataSource.theFakeDataSource.Keys)
            {
                if (key == jobID)
                {
                    List<RunIDDataFields> myRunList = FakeDataSource.theFakeDataSource[key];
                    foreach (RunIDDataFields run in myRunList)
                    {
                        if ((uint)run.RunID == runID)
                        {
                            myRunIDDataFields = run;
                        }
                    }
                }
            }
        }

        public uint RunID
        {
            get
            {
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
                TestResult result = TestResult.Success;
                List<TestCaseIDDataFields> myTestCaseList = myRunIDDataFields.TestCaseList;
                foreach (TestCaseIDDataFields test in myTestCaseList)
                {
                    if (!test.Pass)
                    {
                        result = TestResult.Fail;
                        break;
                    }
                }
                return result;
            }
        }

        public List<uint> TestCasePassedList
        {
            get
            {
                List<uint> myList = new List<uint>();
                List<TestCaseIDDataFields> myTestCaseList = myRunIDDataFields.TestCaseList;
                foreach (TestCaseIDDataFields test in myTestCaseList)
                {
                    if (test.Pass)
                    {
                        myList.Add((uint)test.ID);
                    }
                }
                return myList;
            }
        }

        public List<uint> TestCaseFailedList
        {
            get
            {
                List<uint> myList = new List<uint>();
                List<TestCaseIDDataFields> myTestCaseList = myRunIDDataFields.TestCaseList;
                foreach (TestCaseIDDataFields test in myTestCaseList)
                {
                    if (!test.Pass)
                    {
                        myList.Add((uint)test.ID);
                    }
                }
                return myList;
            }
        }

        public string Title
        {
            get
            {
                return "Faked Title";
            }
        }

        public DateTime StartDate
        {
            get
            {
                return new DateTime(2011, 05, 12, 13, 15, 00);
            }
        }

        public DateTime EndDate
        {
            get
            {
                return new DateTime(2011, 05, 12, 13, 45, 00);
            }
        }

        public RunIDDataFields GetDataFields
        {
            get
            {
                return myRunIDDataFields;
            }
        }
    }

    class FakeDataTestCase : IReportDataTestCase
    {
        private uint id;
        private uint runid;
        private long jobid;
        TestCaseIDDataFields myTest;

        public FakeDataTestCase(uint test, uint run, long job)
        {
            this.id = test;
            this.runid = run;
            this.jobid = job;
            myTest = new TestCaseIDDataFields();

            RunIDDataFields myRunIDDataFields = new RunIDDataFields();
            foreach (long key in FakeDataSource.theFakeDataSource.Keys)
            {
                if (key == jobid)
                {
                    List<RunIDDataFields> myRunList = FakeDataSource.theFakeDataSource[key];
                    foreach (RunIDDataFields myRun in myRunList)
                    {
                        if ((uint)myRun.RunID == runid)
                        {
                            myRunIDDataFields = myRun;
                            List<TestCaseIDDataFields> myTestCaseList = myRunIDDataFields.TestCaseList;
                            foreach (TestCaseIDDataFields aTest in myTestCaseList)
                            {
                                if ((uint)aTest.ID == id)
                                {
                                    myTest = aTest;
                                }
                            }
                        }
                    }
                }
            }
        }
        public uint TestCaseID
        {
            get
            {
                return id;
            }
            set
            {
                this.id = value;
            }
        }

        public string Name
        {
            get
            {
                return myTest.Name;
            }
        }

        public TestResult Result
        {
            get
            {
                return myTest.Pass ? TestResult.Success : TestResult.Fail;
            }
        }

        public string LogDetails
        {
            get
            {
                return myTest.Log;
            }
        }
    }
}
