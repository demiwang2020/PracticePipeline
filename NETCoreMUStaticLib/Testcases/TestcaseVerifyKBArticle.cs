using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyKBArticle : TestcaseBase
    {
        public TestcaseVerifyKBArticle(InnerData inputData, Update expectUpdate, Update actualUpdate)
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
            var matches = System.Text.RegularExpressions.Regex.Matches(_actualUpdate.Title, @"KB\d{6,}");
            if (matches.Count != 1)
            {
                base.GenerateFailResult("KB Artile is not found from title", "KBArticleID in title", _expectedUpdate.Properties.KBArticle, "Not found");
            }
            else if (matches[0].Value.Substring(2) != _expectedUpdate.Properties.KBArticle)
            {
                base.GenerateFailResult("KB Artile in title does not match with KBArticleID", "KBArticleID in title", _expectedUpdate.Properties.KBArticle, matches[0].Value);
            }
        }
    }
}
