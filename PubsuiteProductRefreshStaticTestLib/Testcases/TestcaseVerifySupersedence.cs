using Helper;
using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcaseVerifySupersedence : TestcaseBase
    {
        public TestcaseVerifySupersedence(InputData inputData, Update expectUpdate, Update actualUpdate)
           : base(inputData, expectUpdate, actualUpdate, "Verify superseded updates")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Getting Superseded updates...");

            List<string> ssGuids = _actualUpdate.GetSupersededUpdates();
            int actualSSCount = 0;
            if (ssGuids != null && ssGuids.Count > 0)
            {
                _result.LogMessage(ssGuids.Count.ToString() + " superseded updates found: ");
                _result.LogMessage(String.Join(", ", ssGuids));
                actualSSCount = ssGuids.Count;
            }
            else
            {
                _result.LogMessage("0 superseded updates found");
            }

            _result.LogMessage("Verifying Superseded updates...");

            List<string> expectedSSKBs = null;
            int expectSSCount = 0;
            if (!String.IsNullOrEmpty(base._inputData.SupersededKB))
            {
                var ssKb = base._inputData.SupersededKB.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                expectedSSKBs = new List<string>();
                foreach(var s in ssKb)
                {
                    expectedSSKBs.Add(s.Trim());
                }

                expectSSCount = expectedSSKBs.Count;
            }

            if (actualSSCount != expectSSCount && expectSSCount > actualSSCount)
            {
                _result.LogError("Expect SS KB count doesn't match with actual");
                _result.AddFailure("SS KB count", new TestFailure() { ExpectResult = expectSSCount.ToString(), ActualResult = actualSSCount.ToString() });

                _result.Result = false;
            }
            else if (actualSSCount > 0)
            {
                List<Update> ssUpdates = new List<Update>();

                foreach (var s in ssGuids)
                {
                    ssUpdates.Add(UpdateBuilder.QueryUpdateFromGUID(s));
                }

                //1. Compare KB number
                _result.LogMessage("Verifying KB numbers Superseded updates...");
                List<string> actualSSKBs = new List<string>();
                foreach (var update in ssUpdates)
                {
                    actualSSKBs.Add(update.Properties.KBArticle);
                }

                var missingSSKBs = expectedSSKBs.Except(actualSSKBs);
                var additionalSSKBs = actualSSKBs.Except(expectedSSKBs);
                if (missingSSKBs.Count() > 0)
                {
                    _result.LogError("Missing SS KB: " + String.Join(", ", missingSSKBs));
                    _result.Result = false;
                }
                if (additionalSSKBs.Count() > 0)
                {
                    _result.LogError("Additional SS KB: " + String.Join(", ", additionalSSKBs));
                    _result.Result = false;
                }

                //2. Verify arch of SS KBs
                _result.LogMessage("Verifying CPU architecture Superseded updates...");
                Architecture expectArch = (Architecture)ArchDetector.ParseArchFromUpdateTitle(_actualUpdate.Title);

                foreach (var update in ssUpdates)
                {
                    Architecture actualArch = (Architecture)ArchDetector.ParseArchFromUpdateTitle(update.Title);
                    if (actualArch != expectArch)
                    {
                        _result.LogError("Found mismatched arch for update " + update.Properties.KBArticle);
                        _result.AddFailure("Arch of " + update.Properties.KBArticle, new TestFailure() { ExpectResult = expectArch.ToString(), ActualResult = actualArch.ToString() });
                        _result.Result = false;
                    }
                }

                //3. Verify OS of SS update
                _result.LogMessage("Verifying OS OF Superseded updates...");
                List<string> expectOSes = OSDetector.ParseAllTargetOSFromUpdateTitle(_actualUpdate.Title);
                foreach (var update in ssUpdates)
                {
                    List<string> actualOSes = OSDetector.ParseAllTargetOSFromUpdateTitle(update.Title);
                    var diff1 = expectOSes.Except(actualOSes);
                    var diff2 = actualOSes.Except(expectOSes);

                    if (diff1.Count() > 0 || diff2.Count() > 0)
                    {
                        _result.LogError("Found mismatched OS for SS update " + update.Properties.KBArticle);
                        _result.AddFailure("OS of " + update.Properties.KBArticle, new TestFailure() { ExpectResult = String.Join(", ", expectOSes), ActualResult = String.Join(", ", actualOSes) });
                        _result.Result = false;
                    }
                }

                //4. Compare destination for B/C release
                if (_inputData.OtherProperties.ContainsKey("Destination"))
                {
                    bool bSUS = _inputData.OtherProperties["Destination"].Contains("SUS");

                    if(bSUS)
                        VerifySSDestination(ssUpdates, "SUS", bSUS);
                    else
                        VerifySSDestination(ssUpdates, "Site", true);
                }
                else
                {
                    _result.LogMessage("No need to run SS destination verification");
                }
            }

            if (_result.Result)
            {
                _result.LogMessage("Supersedence verification PASSED");
            }
            else
            {
                _result.LogMessage("Supersedence verification FAILED");
            }
        }

        private void VerifySSDestination(List<Update> ssUpdates, string dest, bool expect)
        {
            foreach (var update in ssUpdates)
            {
                if ((dest.Contains("AU") || dest.Contains("WU")) && _actualUpdate.Properties.AU != expect)
                {
                    GenerateSSDestinationFailure(update, "AU", expect);
                }

                if (dest.Contains("Catalog") && _actualUpdate.Properties.Catalog != expect)
                {
                    GenerateSSDestinationFailure(update, "Catalog", expect);
                }

                if (dest.Contains("Site") && _actualUpdate.Properties.Site != expect)
                {
                    GenerateSSDestinationFailure(update, "Site", expect);
                }

                if (dest.Contains("SUS") && update.Properties.SUS != expect)
                {
                    GenerateSSDestinationFailure(update, "SUS", expect);
                }

                if (dest.Contains("Csa") && update.Properties.Csa != expect)
                {
                    GenerateSSDestinationFailure(update, "Csa", expect);
                }
            }
        }

        private void GenerateSSDestinationFailure(Update update, string channel, bool expect)
        {
            base.GenerateFailResult(String.Format("{0} of SS update {1} is not expected", channel, update.ID),
                                    String.Format("{0} of {1}", channel, update.ID),
                                    expect.ToString(),
                                    (!expect).ToString());
        }
    }
}
