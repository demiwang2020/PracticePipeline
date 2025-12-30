using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcaseCategories : TestcaseBase
    {
        public TestcaseCategories(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Categories Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying Categories...");

            if (String.Compare(_expectedUpdate.Categories, _actualUpdate.Categories, true) != 0)
            {
                _result.LogError("Categories verification FAILED");
                _result.AddFailure("Categories", new TestFailure() { ExpectResult = DetectoidTranslator.TranslateDetectoidsInString(_expectedUpdate.Categories), ActualResult = DetectoidTranslator.TranslateDetectoidsInString(_actualUpdate.Categories) });

                _result.Result = false;
            }
            else
            {
                _result.LogMessage("Categories verification PASSED");
            }
        }
    }
}
