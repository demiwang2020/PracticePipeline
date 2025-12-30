using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcaseVerifyRoqData : TestcaseBase
    {
        public TestcaseVerifyRoqData(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Roq Data Verification")
        {
        }

        protected override void RunTest()
        {
            bool isCriticalUpdate = _expectedUpdate.Properties.MsrcSeverity.Equals("Critical") || 
                                    _expectedUpdate.Properties.MsrcSeverity.Equals("Important") ||
                                    _expectedUpdate.Properties.MsrcSeverity.Equals("Moderate");

            // Verify parent update
            _result.LogMessage("Verifying Roq Data of parent update...");
            VerifyRoqData(_actualUpdate, isCriticalUpdate);

            //Verify each child update
            foreach (var child in _actualUpdate.ChildUpdates)
            {
                _result.LogMessage("Verifying Roq Data of child update " + child.Title);
                VerifyRoqData(child, isCriticalUpdate);
            }
        }

        private void VerifyRoqData(Update update, bool isCritical)
        {
            if (update.RoqData == null)
            {
                _result.LogError("RoqData is not set");
                _result.Result = false;
            }
            else
            {
                // Verify UserID
                if (String.IsNullOrEmpty(update.RoqData.Owner))
                {
                    _result.LogError("Owner UserID is not set");
                    _result.Result = false;
                }

                // Verify IsTestOnly
                if (update.RoqData.IsTestOnly)
                {
                    _result.LogError("IsTestOnly is set to True");
                    _result.Result = false;
                }

                // Verify IsCritical
                if (update.RoqData.IsCritical != isCritical)
                {
                    _result.LogError("IsCritical is wrong");
                    _result.Result = false;

                    string updateName = update.IsParentUpdate ? "parent update" : update.Title;
                    _result.AddFailure("IsCritical of " + updateName, new TestFailure() { ExpectResult = isCritical.ToString(), ActualResult = update.RoqData.IsCritical.ToString() });
                }
            }
        }
    }
}
