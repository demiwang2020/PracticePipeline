using Helper;
using NETCoreMUStaticLib.DbClassContext;
using NETCoreMUStaticLib.Model;
using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifySupersedence : TestcaseBase
    {
        public TestcaseVerifySupersedence(InnerData inputData, Update expectUpdate, Update actualUpdate)
           : base(inputData, expectUpdate, actualUpdate, "Verify superseded updates")
        {
        }

        protected override void RunTest()
        {
            List<TTestedUpdate> previousBundles = SearchPreviousUpdate(2);
            List<string> actualSSUpdates = _actualUpdate.GetSupersededUpdates();


            _result.LogMessage("Verify superseded updates...");

            if (actualSSUpdates != null)
            {
                bool result = true;
                int index = 1;
                foreach (var b in previousBundles)
                {
                    if (!actualSSUpdates.Contains(b.UpdateID))
                    {
                        base.GenerateFailResult("Expected superseded bundle is not found",
                                            "SS GUID " + index.ToString(),
                                            String.Format("{0} ({1})", b.UpdateID, b.Title),
                                            "Not found in actual bundle");

                        result = false;
                        ++index;
                    }
                }

                foreach (var s in actualSSUpdates)
                {
                    var b = previousBundles.Where(p => p.UpdateID == s).FirstOrDefault();
                    if (b == null)
                    {
                        var bundle = FindUpdateByGUID(s);

                        base.GenerateFailResult("Unexpected superseded bundle is found",
                                               "SS GUID " + index.ToString(),
                                               "Not present in actual bundle",
                                               bundle != null ? String.Format("{0} ({1})", bundle.UpdateID, bundle.Title) : s);

                        result = false;
                        ++index;
                    }
                }

                if (result)
                {
                    _result.LogMessage("Superdence verification passed");
                }
            }
            else
            {
                if (previousBundles.Count == 0)
                {
                    _result.LogMessage("Superdence verification passed, no bundles are superseded");
                }
                else
                {
                    int index = 1;
                    foreach (var b in previousBundles)
                    {
                        base.GenerateFailResult("Expected superseded bundle is not found",
                                            "SS GUID " + index.ToString(),
                                            String.Format("{0} ({1})", b.UpdateID, b.Title),
                                            "Not found in actual bundle");

                        ++index;
                    }
                }
            }
        }

        private List<TTestedUpdate> SearchPreviousUpdate(int count)
        {
            using (var dbContext = new NetCoreWUSAFXDbContext())
            {
                List<TTestedUpdate> searchedBundles = new List<TTestedUpdate>();

                int privateBuildNumber = Convert.ToInt32(_innerData.ReleaseNumber.Split(new char[] { '.' })[2]);

                --privateBuildNumber;
                while (privateBuildNumber > 0)
                {
                    string buildNumber = String.Format("{0}.{1}", _innerData.MajorRelease, privateBuildNumber);

                    var records = dbContext.TTestedUpdates.Where(p => p.ReleaseNumber == buildNumber &&
                                                                 p.Arch == (int)_innerData.Arch &&
                                                                 p.IsServerBundle == _innerData.IsServerBundle &&
                                                                 p.IsAUBundle == _innerData.IsAUBundle);

                    if (records.Count() > 0)
                        searchedBundles.Add(records.First());

                    if (searchedBundles.Count >= count)
                        break;

                    --privateBuildNumber;
                }

                return searchedBundles;
            }
        }

        private TTestedUpdate FindUpdateByGUID(string id)
        {
            using (var dbContext = new NetCoreWUSAFXDbContext())
            {
                return dbContext.TTestedUpdates.Where(p => p.UpdateID == id).FirstOrDefault();
            }
        }
    }
}
