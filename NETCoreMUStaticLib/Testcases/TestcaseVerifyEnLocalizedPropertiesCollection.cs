using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyEnLocalizedPropertiesCollection : TestcaseBase
    {
        public TestcaseVerifyEnLocalizedPropertiesCollection(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Enu LocalizedPropertiesCollection Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying Title...");
            if (!_actualUpdate.Title.Equals(_expectedUpdate.Title))
            {
                _result.Result = false;
                _result.LogError("Title verification failed");

                _result.AddFailure("Title", new TestFailure() { ExpectResult = _expectedUpdate.Title, ActualResult = _actualUpdate.Title });
            }

            _result.LogMessage("Verifying Description...");
            if (!_actualUpdate.Description.Equals(_expectedUpdate.Description))
            {
                _result.Result = false;
                _result.LogError("Description verification failed");

                _result.AddFailure("Description", new TestFailure() { ExpectResult = _expectedUpdate.Description, ActualResult = _actualUpdate.Description });
            }

            _result.LogMessage("Verifying UninstallNotes...");
            if (!_actualUpdate.UninstallNotes.Equals(_expectedUpdate.UninstallNotes))
            {
                _result.Result = false;
                _result.LogError("UninstallNotes verification failed");

                _result.AddFailure("UninstallNotes", new TestFailure() { ExpectResult = _expectedUpdate.UninstallNotes, ActualResult = _actualUpdate.UninstallNotes });
            }

            _result.LogMessage("Verifying SupportUrl...");
            if (!_actualUpdate.SupportUrl.Replace("&amp;", "&").Equals(_expectedUpdate.SupportUrl))
            {
                _result.Result = false;
                _result.LogError("SupportUrl verification failed");

                _result.AddFailure("SupportUrl", new TestFailure() { ExpectResult = _expectedUpdate.SupportUrl, ActualResult = _actualUpdate.SupportUrl });
            }

            _result.LogMessage("Verifying MoreInfoUrl...");
            if (!_actualUpdate.MoreInfoUrl.Replace("&amp;", "&").Equals(_expectedUpdate.MoreInfoUrl))
            {
                _result.Result = false;
                _result.LogError("MoreInfoUrl verification failed");

                _result.AddFailure("MoreInfoUrl", new TestFailure() { ExpectResult = _expectedUpdate.MoreInfoUrl, ActualResult = _actualUpdate.MoreInfoUrl });
            }

            _result.LogMessage("Verifying spaces in title...");
            if (CommonHelper.TooManyAdditionalSpaces(_actualUpdate.Title))
            {
                GenerateFailResult("Title spaces verification failed", "Spaces in title", "No unexpected spaces", _actualUpdate.Title);
            }
        }
    }
}
