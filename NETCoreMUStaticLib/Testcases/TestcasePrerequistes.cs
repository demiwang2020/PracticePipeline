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
    class TestcasePrerequistes : TestcaseBase
    {
        public TestcasePrerequistes(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Prerequisites Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying Prerequisites for parent update...");

            // Verify parent update
            if (!XmlHelper.CompareNodes(_actualUpdate.Prerequisites, _expectedUpdate.Prerequisites, true))
            {
                base.GenerateFailResult("Prerequisites verification for parent update FAILED",
                                        "Parent Prerequisites",
                                        DetectoidTranslator.TranslateDetectoidsInString(_expectedUpdate.Prerequisites.OuterXml),
                                        DetectoidTranslator.TranslateDetectoidsInString(_actualUpdate.Prerequisites.OuterXml));
            }

            // Verify child updates
            foreach (Update expectChildUpdate in _expectedUpdate.ChildUpdates)
            {
                Update actualChildUpdate = _actualUpdate.GetMatchedChildUpdate(expectChildUpdate);

                _result.LogMessage("Verifying Prerequisites for child update " + expectChildUpdate.Title);

                if (actualChildUpdate == null)
                {
                    _result.LogError("Failed to find out actual child update, may need to update case");
                    _result.Result = false;
                }
                else
                {
                    if (!XmlHelper.CompareNodes(actualChildUpdate.Prerequisites, expectChildUpdate.Prerequisites, true))
                    {
                        base.GenerateFailResult("Prerequisites verification FAILED for " + actualChildUpdate.Title,
                                                "Prerequisites of " + actualChildUpdate.Title,
                                                DetectoidTranslator.TranslateDetectoidsInString(expectChildUpdate.Prerequisites.OuterXml),
                                                DetectoidTranslator.TranslateDetectoidsInString(actualChildUpdate.Prerequisites.OuterXml));
                    }
                }
            }
        }
    }
}
