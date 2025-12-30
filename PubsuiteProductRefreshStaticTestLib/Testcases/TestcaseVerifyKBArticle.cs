using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcaseVerifyKBArticle : TestcaseBase
    {
        public TestcaseVerifyKBArticle(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "KB Article Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying Property KBArticleID ...");

            if (String.Compare(_expectedUpdate.Properties.KBArticle, _actualUpdate.Properties.KBArticle, true) != 0)
            {
                _result.LogError("KBArticleID verification FAILED");
                _result.AddFailure("KBArticleID", new TestFailure() { ExpectResult = _expectedUpdate.Properties.KBArticle, ActualResult = _actualUpdate.Properties.KBArticle });

                _result.Result = false;
            }

            _result.LogMessage("Verifying KBArticleID same as title ...");
            if (!_actualUpdate.Title.EndsWith(String.Format("(KB{0})", _actualUpdate.Properties.KBArticle)))
            {
                _result.LogError("KBArticleID is different from update title");

                _result.Result = false;
            }

            _result.LogMessage("Verifying KBArticleID same as the only child update...");
            if (_actualUpdate.ChildUpdates != null &&
                _actualUpdate.ChildUpdates.Count == 1 &&
                ArchDetector.ParseArchFromUpdateTitle(_actualUpdate.Title) != 3) //exclude IA64
            {
                Update childUpdate = _actualUpdate.ChildUpdates.First();
                string packageName = childUpdate.PackageName;
                //string name = System.IO.Path.GetFileNameWithoutExtension(packagePath).ToUpperInvariant();
                if (!packageName.Contains("KB" + _actualUpdate.Properties.KBArticle))
                {
                    if (!packageName.Contains("kb" + _actualUpdate.Properties.KBArticle))
                    {
                        _result.LogError("Child KB article is different from parent KB article");
                        _result.Result = false;
                    }
                }
            }
            else
            {
                _result.LogMessage("Update contains 0 or multiple child updates or is IA64, skip verification");
            }
        }

    }
}
