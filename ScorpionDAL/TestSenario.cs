using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpionDAL
{
    public class TestSenario
    {
        public int TestCaseID { get; set; }
        public string TestCaseCode { get; set; }
        public string TestCaseName { get; set; }
        public string TestCaseDescripton { get; set; }

        public string ContextBlockName { get; set; }
        public string ContextBlock { get; set; }

        public int MDOSCPUID { get; set; }

        public int MDOSID { get; set; }
        public string OSSPLevel { get; set; }
        public string OSImage { get; set; }

        public int MaddogOSImageID { get; set; }
        public string MaddogOSImageName { get; set; }

        public string LabName { get; set; }
        public int MaddogDBID { get; set; }
    }
}
