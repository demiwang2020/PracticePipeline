using PubsuiteStaticTestLib.UpdateHelper;
using RMDataAccess.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Testcases
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

            if(_actualUpdate.ChildUpdates.Count != 1 && _inputData.Title.Contains("Windows 10") && 
                _inputData.Title.Contains("Cumulative Update Preview")&&
                (_inputData.Title.Contains("22H2") || _inputData.Title.Contains("21H2")))
            {
                _expectedUpdate.Prerequisites.InnerXml = _expectedUpdate.Prerequisites.InnerXml.Replace("<pub:UpdateIdentity UpdateID=\"5671b1d0-eb3f-4259-b777-ae7aa53b51aa\" xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\" />", "");

            }

            if(_inputData.Title.Contains("Microsoft server operating system") && 
                (_inputData.ShipChannels.Equals("Site") || _inputData.ShipChannels.Equals("SUSCatalog")))
            {
                _expectedUpdate.Prerequisites.InnerXml = _expectedUpdate.Prerequisites.InnerXml + "<pub:UpdateIdentity UpdateID=\"04259680-2dca-4c9d-b2e0-5d3e2d37e7c5\" xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\" />";
            }


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
                //if (actualChildUpdate.Title.ToLower().StartsWith("windows6.1-kb5044011") ||
                //    actualChildUpdate.Title.ToLower().StartsWith("windows8-rt-kb5044009") ||
                //    actualChildUpdate.Title.ToLower().StartsWith("windows8.1-kb5044012"))
                //    continue;
                Update expectChildUpdate = _expectedUpdate.GetMatchedChildUpdate(actualChildUpdate);

                _result.LogMessage("Verifying Prerequisites for child update " + actualChildUpdate.Title);

                if (expectChildUpdate == null)
                {
                    _result.LogError("Failed to find out expect child update, may need to update case");
                    _result.Result = false;
                    //_result.AddFailure("Prerequisites of " + actualChildUpdate.Title, new TestFailure() { ExpectResult = "Exceptchild is null", ActualResult = DetectoidTranslator.TranslateDetectoidsInString(actualChildUpdate.Prerequisites.OuterXml) });

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
