using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyCveID : TestcaseBase
    {
        public TestcaseVerifyCveID(InnerData inputData, Update expectUpdate, Update actualUpdate)
           : base(inputData, expectUpdate, actualUpdate, "Verify CveID")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying CVE IDs...");

            if (String.CompareOrdinal(_expectedUpdate.Properties.CveIDs, _actualUpdate.Properties.CveIDs) != 0)
            {
                base.GenerateFailResult("CVE ID mismatch", "CVE ID list", 
                                         _expectedUpdate.Properties.CveIDs, 
                                         _actualUpdate.Properties.CveIDs);
            }
        }
    }
}
