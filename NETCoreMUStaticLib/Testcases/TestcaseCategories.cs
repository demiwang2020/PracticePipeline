using NETCoreMUStaticLib;
using NETCoreMUStaticLib.Testcases;
using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseCategories : TestcaseBase
    {
        public TestcaseCategories(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Categories Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying Categories...");

            if (String.Compare(_expectedUpdate.Categories, _actualUpdate.Categories, true) != 0)
            {
                base.GenerateFailResult("Categories verification FAILED",
                                        "Categories",
                                        DetectoidTranslator.TranslateDetectoidsInString(_expectedUpdate.Categories),
                                        DetectoidTranslator.TranslateDetectoidsInString(_actualUpdate.Categories));
            }
            else
            {
                _result.LogMessage("Categories verification PASSED");
            }
        }
    }
}
