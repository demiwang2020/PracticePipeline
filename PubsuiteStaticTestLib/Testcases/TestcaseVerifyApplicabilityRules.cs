using PubsuiteStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PubsuiteStaticTestLib.Testcases
{
    class TestcaseVerifyApplicabilityRules : TestcaseBase
    {
        public TestcaseVerifyApplicabilityRules(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "ApplicabilityRules Verification")
        {
        }

        protected override void RunTest()
        {
            foreach (Update actualChildUpdate in _actualUpdate.ChildUpdates)
            {
                // Skip some child KB that do not need to be tested
                if (actualChildUpdate.Title.ToLower().StartsWith("windows6.1-kb4019990") ||
                    actualChildUpdate.Title.ToLower().StartsWith("windows6.0-kb3078601") ||
                    actualChildUpdate.Title.ToLower().StartsWith("windows6.0-kb4019478") ||
                    actualChildUpdate.Title.ToLower().StartsWith("windows8-rt-kb4019990") ||
                    actualChildUpdate.Title.ToLower().StartsWith("windows8-rt-kb5044009") ||
                    actualChildUpdate.Title.ToLower().StartsWith("windows6.1-kb5044011") ||
                    actualChildUpdate.Title.ToLower().StartsWith("windows8.1-kb5044012") )
                    continue;

                Update expectChildUpdate = _expectedUpdate.GetMatchedChildUpdate(actualChildUpdate);
                if (expectChildUpdate == null)
                {
                    _result.LogError("Failed to find child update " + actualChildUpdate.Title + " in expected update");
                    _result.Result = false;
                    _result.AddFailure("Find child update " + actualChildUpdate.Title, new TestFailure() { ExpectResult = "Except child update is null" , ActualResult = actualChildUpdate.Title });

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
