using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyApplicabilityRules : TestcaseBase
    {
        public TestcaseVerifyApplicabilityRules(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "ApplicabilityRules Verification")
        {
        }

        protected override void RunTest()
        {
            foreach (Update expectChildUpdate in _expectedUpdate.ChildUpdates)
            {
                Update actualChildUpdate = _actualUpdate.GetMatchedChildUpdate(expectChildUpdate);
                if (actualChildUpdate == null)
                {
                    _result.LogError("Failed to find child update " + expectChildUpdate.Title + " in actual update");
                    _result.Result = false;
                }
                else
                {
                    //Verify IsInstallable rules
                    _result.LogMessage("Verifying IsInstallable rules for " + actualChildUpdate.Title + "...");

                    XmlNode actualIsInstallable = actualChildUpdate.IsInstallableRules;
                    XmlNode expectIsInstallable = expectChildUpdate.IsInstallableRules;
                    if (!XmlHelper.CompareNodes(actualIsInstallable, expectIsInstallable))
                    {
                        _result.LogError("IsInstallable rules are different");
                        _result.Result = false;

                        _result.AddFailure("IsInstallable of " + actualChildUpdate.Title, new TestFailure() { ExpectResult = DetectoidTranslator.TranslateDetectoidsInString(expectIsInstallable.OuterXml), ActualResult = DetectoidTranslator.TranslateDetectoidsInString(actualIsInstallable.OuterXml) });
                    }

                    //Verify IsInstalled rules
                    _result.LogMessage("Verifying IsInstalled rules for " + actualChildUpdate.Title + "...");

                    XmlNode actualIsInstalled = actualChildUpdate.IsInstalledRules;
                    XmlNode expectIsInstalled = expectChildUpdate.IsInstalledRules;
                    if (!XmlHelper.CompareNodes(actualIsInstalled, expectIsInstalled, true)) //ignore case for comparing is-installed rules, since file path or name might be different in case
                    {
                        _result.LogError("IsInstalled rules are different");
                        _result.Result = false;

                        _result.AddFailure("IsInstalled of " + actualChildUpdate.Title, new TestFailure() { ExpectResult = DetectoidTranslator.TranslateDetectoidsInString(expectIsInstalled.OuterXml), ActualResult = DetectoidTranslator.TranslateDetectoidsInString(actualIsInstalled.OuterXml) });
                    }
                }
            }
        }
    }
}
