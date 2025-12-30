using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportEngine
{
    enum TestResult
    {
        Success,
        Fail,
        InProgress
    }

    enum RunIDArch
    {
        X86,
        AMD64,
        ARM
    }

    enum RunIDType
    {
        SIMULINSTALL,
        REALINSTALL
    }

    struct TestCaseIDDataFields
    {
        public int ID;                        // test case ID
        public string Name;                   // test case name
        public bool Pass;                  // test case result
        public string Log;                 // log path for a test case

        public TestCaseIDDataFields(int myID, string myName, bool myPass, string myLog)
        {
            ID = myID; Name = myName; Pass = myPass; Log = myLog;
        }
    }

    struct RunIDDataFields
    {
        public int RunID;
        public string Arch;
        public string PatchFile;
        public List<TestCaseIDDataFields> TestCaseList;                // A list of test cases bind to a runID. 1:1 for Runtime. N:1 for Safax
        public string MachineNameAndID;                               // machine name and IP address of machine that the run is scheduled on
        public string MaddogGoToURL;                                  // URL that launches maddgo go to for a run
        public string ReproConfigDetails;                             // Machine information in order to reproduce a similar run

        public RunIDDataFields(int myRunID, string myArch, string myPatchFile, List<TestCaseIDDataFields> myTestCaseList,
            string myMachineNameAndID, string myMaddogGoToURL, string myReproConfigDetails)
        {
            RunID = myRunID;
            Arch = myArch;
            PatchFile = myPatchFile;
            TestCaseList = myTestCaseList;
            MachineNameAndID = myMachineNameAndID;
            MaddogGoToURL = myMaddogGoToURL;
            ReproConfigDetails = myReproConfigDetails;
        }
    }

    interface IReportDataJobID
    {
        long JobID
        {
            get;
        }

        // One job ID is mapped to 1 KBNumber  in general
        string GetKBNumber();

        // One job ID includes multiple Run IDs
        List<uint> RunIDList
        {
            get;
        }

        string GetTFSWorkItem();
        string GetCreator();
        DateTime GetCreationTime();
    }

    interface IReportDataRunID
    {
        uint RunID
        {
            get;
            set;
        }

        RunIDDataFields GetDataFields
        {
            get;
        }

        TestResult Result
        {
            get;
        }

        List<uint> TestCasePassedList
        {
            get;
        }

        List<uint> TestCaseFailedList
        {
            get;
        }

        string Title
        {
            get;
        }

        DateTime StartDate
        {
            get;
        }
        DateTime EndDate
        {
            get;
        }
    }

    interface IReportDataTestCase
    {
        uint TestCaseID
        {
            get;
            set;
        }
        string Name
        {
            get;
        }
        TestResult Result
        {
            get;
        }
        string LogDetails
        {
            get;
        }
    }
}
