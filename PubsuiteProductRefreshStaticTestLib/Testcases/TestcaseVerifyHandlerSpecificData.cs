using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcaseVerifyHandlerSpecificData : TestcaseBase
    {
        public TestcaseVerifyHandlerSpecificData(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "HandlerSpecificData Verification")
        {
        }

        protected override void RunTest()
        {
            foreach (Update expectChildUpdate in _expectedUpdate.ChildUpdates)
            {
                _result.LogMessage("Verifying HandlerSpecificData for " + expectChildUpdate.Title + "...");

                Update actualChildUpdate = _actualUpdate.GetMatchedChildUpdate(expectChildUpdate);
                if (actualChildUpdate == null)
                {
                    _result.LogError("Failed to find matching child update");
                    _result.Result = false;
                }
                else
                {
                    XmlNode expectHandlerSpecificData = expectChildUpdate.HandlerSpecificData;
                    XmlNode actualHandlerSpecificData = actualChildUpdate.HandlerSpecificData;

                    if (XmlHelper.CompareNodes(actualHandlerSpecificData, expectHandlerSpecificData, true))
                    {
                        _result.LogMessage("HandlerSpecificData Verification PASSED");
                    }
                    else
                    {
                        _result.LogError("HandlerSpecificData Verification Failed");

                        _result.Result = false;
                        _result.AddFailure("HandlerSpecificData of " + expectChildUpdate.Title, new TestFailure { ExpectResult = expectHandlerSpecificData.OuterXml, ActualResult = actualHandlerSpecificData.OuterXml });
                    }
                }
            }
        }
    }
}
