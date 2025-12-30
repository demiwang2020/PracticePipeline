using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcasePrerequistes : TestcaseBase
    {
        public TestcasePrerequistes(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Prerequisites Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying Prerequisites for parent update...");

            // Verify parent update
            if (!XmlHelper.CompareNodes(_actualUpdate.Prerequisites, _expectedUpdate.Prerequisites, true))
            {
                _result.LogError("Prerequisites verification for parent update FAILED");
                _result.AddFailure("Parent Prerequisites", new TestFailure() { ExpectResult = DetectoidTranslator.TranslateDetectoidsInString(_expectedUpdate.Prerequisites.OuterXml), ActualResult = DetectoidTranslator.TranslateDetectoidsInString(_actualUpdate.Prerequisites.OuterXml) });

                _result.Result = false;
            }

            // Verify child updates
            foreach (Update actualChildUpdate in _actualUpdate.ChildUpdates)
            {
                Update expectChildUpdate = _expectedUpdate.GetMatchedChildUpdate(actualChildUpdate);

                _result.LogMessage("Verifying Prerequisites for child update " + actualChildUpdate.Title);

                if (expectChildUpdate == null)
                {
                    _result.LogError("Failed to find out expect child update, may need to update case");
                    _result.Result = false;
                }
                else
                {
                    if (!XmlHelper.CompareNodes(actualChildUpdate.Prerequisites, expectChildUpdate.Prerequisites, true))
                    {
                        _result.LogError("Prerequisites verification FAILED for " + actualChildUpdate.Title);
                        _result.AddFailure("Prerequisites of " + actualChildUpdate.Title, new TestFailure() { ExpectResult = DetectoidTranslator.TranslateDetectoidsInString(expectChildUpdate.Prerequisites.OuterXml), ActualResult = DetectoidTranslator.TranslateDetectoidsInString(actualChildUpdate.Prerequisites.OuterXml) });

                        _result.Result = false;
                    }
                }
            }
        }
    }
}
