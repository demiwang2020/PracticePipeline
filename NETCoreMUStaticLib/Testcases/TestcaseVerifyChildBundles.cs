using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyChildBundles : TestcaseBase
    {
        public TestcaseVerifyChildBundles(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Verify Child Bundles")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying actual update carries all expected child bundles...");
            foreach (var expUpdate in _expectedUpdate.ChildUpdates)
            {
                if (_actualUpdate.GetMatchedChildUpdate(expUpdate) == null)
                {
                    _result.Result = false;
                    _result.LogError("Missing child bundle: " + expUpdate.Title);
                }
            }

            _result.LogMessage("Verifying actual update does not carry additional child bundles...");
            foreach (var actUpdate in _actualUpdate.ChildUpdates)
            {
                if (_expectedUpdate.GetMatchedChildUpdate(actUpdate) == null)
                {
                    _result.Result = false;
                    _result.LogError("Additional child bundle: " + actUpdate.Title);
                }
            }
        }
    }
}
