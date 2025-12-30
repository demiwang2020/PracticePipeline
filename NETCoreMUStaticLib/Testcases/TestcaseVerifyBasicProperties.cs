using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyBasicProperties : TestcaseBase
    {
        public TestcaseVerifyBasicProperties(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Basic Properties Verification")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying SuggestedRecipient...");

            bool result = true;
            if (_expectedUpdate.Properties.Site != _actualUpdate.Properties.Site)
            {
                result = false;
                _result.AddFailure("Site", 
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.Site.ToString(), ActualResult = _actualUpdate.Properties.Site.ToString() });
            }

            if (_expectedUpdate.Properties.AU != _actualUpdate.Properties.AU)
            {
                result = false;
                _result.AddFailure("AU",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.AU.ToString(), ActualResult = _actualUpdate.Properties.AU.ToString() });
            }

            if (_expectedUpdate.Properties.SUS != _actualUpdate.Properties.SUS)
            {
                result = false;
                _result.AddFailure("SUS",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.SUS.ToString(), ActualResult = _actualUpdate.Properties.SUS.ToString() });
            }

            if (_expectedUpdate.Properties.Catalog != _actualUpdate.Properties.Catalog)
            {
                result = false;
                _result.AddFailure("Catalog",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.Catalog.ToString(), ActualResult = _actualUpdate.Properties.Catalog.ToString() });
            }

            if (_expectedUpdate.Properties.Csa != _actualUpdate.Properties.Csa)
            {
                result = false;
                _result.AddFailure("Csa",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.Csa.ToString(), ActualResult = _actualUpdate.Properties.Csa.ToString() });
            }

            if (!result)
            {
                _result.Result &= result;
                _result.LogError("SuggestedRecipient verification failed");
            }


            _result.LogMessage("Verifying update importance...");
            result = true;
            if (_expectedUpdate.Properties.AutoSelectOnWebSites != _actualUpdate.Properties.AutoSelectOnWebSites)
            {
                result = false;
                _result.AddFailure("AutoSelectOnWebSites",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.AutoSelectOnWebSites.ToString(), ActualResult = _actualUpdate.Properties.AutoSelectOnWebSites.ToString() });
            }
            if (_expectedUpdate.Properties.BrowseOnly != _actualUpdate.Properties.BrowseOnly)
            {
                result = false;
                _result.AddFailure("BrowseOnly",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.BrowseOnly.ToString(), ActualResult = _actualUpdate.Properties.BrowseOnly.ToString() });
            }
            if(!_expectedUpdate.Properties.MsrcSeverity.Equals(_actualUpdate.Properties.MsrcSeverity))
            {
                result = false;
                _result.AddFailure("MsrcSeverity",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.MsrcSeverity, ActualResult = _actualUpdate.Properties.MsrcSeverity.ToString() });
            }

            if (!result)
            {
                _result.Result &= result;
                _result.LogError("Importance verification failed");
            }

            _result.LogMessage("Verifying update classification...");
            result = true;
            if (!_expectedUpdate.Properties.UpdateClassification.Equals(_actualUpdate.Properties.UpdateClassification, StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                _result.AddFailure("Classification",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.UpdateClassification, ActualResult = _actualUpdate.Properties.UpdateClassification.ToString() });
            }

            if (!result)
            {
                _result.Result &= result;
                _result.LogError("Classification verification failed");
            }

            _result.LogMessage("Verifying update SecurityBulletinID...");
            result = true;
            if (!_expectedUpdate.Properties.SecurityBulletinID.Equals(_actualUpdate.Properties.SecurityBulletinID))
            {
                result = false;
                _result.AddFailure("SecurityBulletinID",
                    new TestFailure() { ExpectResult = _expectedUpdate.Properties.SecurityBulletinID, ActualResult = _actualUpdate.Properties.SecurityBulletinID });
            }

            if (!result)
            {
                _result.Result &= result;
                _result.LogError("SecurityBulletinID verification failed");
            }

            _result.LogMessage("Verifying EulaID is not set...");
            result = true;
            if (!String.IsNullOrEmpty(_actualUpdate.Properties.EulaID))
            {
                result = false;
                _result.AddFailure("EulaID",
                    new TestFailure() { ExpectResult = String.Empty, ActualResult = _actualUpdate.Properties.EulaID });
            }

            if (!result)
            {
                _result.Result &= result;
                _result.LogError("EulaID verification failed");
            }

            _result.LogMessage("Verifying TTGL...");
            bool timezoneSet = false;
            DateTime dtTTGL = CommonHelper.TTGLString2DateTime(_actualUpdate.Properties.TimeToGoLive, out timezoneSet);
            string releaseMonth = String.Format("{0}-{1:00}", dtTTGL.Year, dtTTGL.Month);
            if (releaseMonth != _innerData.ReleaseDate)
            {
                base.GenerateFailResult("Release Month in title doesn't match with TTGL",
                                        "Release Month",
                                        _innerData.ReleaseDate,
                                        releaseMonth);
            }
            if (dtTTGL.Hour != (timezoneSet ? 17: 10) || dtTTGL.Minute != 0 || dtTTGL.Second != 0)
            {
                base.GenerateFailResult("TTGL is not 10 AM",
                                        "Release Time",
                                        "10:00:00",
                                        String.Format("{0:00}:{1:00}:{2:00}", dtTTGL.Hour, dtTTGL.Minute, dtTTGL.Second));
            }

            if (_innerData.Parameters.ContainsKey("ReleaseDate"))
            {
                if (!_actualUpdate.Properties.TimeToGoLive.StartsWith(_innerData.Parameters["ReleaseDate"]))
                {
                    base.GenerateFailResult("Release date is not correct",
                                        "Release Date",
                                        _innerData.Parameters["ReleaseDate"],
                                        _actualUpdate.Properties.TimeToGoLive);
                }
            }

            _result.LogMessage("Verifying Readiness Date Exist ...");
            if (!_actualUpdate.Properties.isReadiness) {
                _result.Result &= false ;
                _result.LogError("Readiness Date Does Not Exist");
            }
            //if (!_innerData.IsSecurityRelease && _actualUpdate.Properties.BusinessDate != null && _actualUpdate.Properties.RTWDate != null)
            if ( _actualUpdate.Properties.BusinessDate != null && _actualUpdate.Properties.RTWDate != null)
            {
                _result.LogMessage("Verifying Readiness Date...");
                result = true;
                if (!_actualUpdate.Properties.BusinessDate.Equals(_actualUpdate.Properties.TimeToGoLive))
                {
                    result = false;
                    _result.AddFailure("Readiness Date(Business)",
                        new TestFailure() { ExpectResult = _actualUpdate.Properties.TimeToGoLive, ActualResult = _actualUpdate.Properties.BusinessDate });
                }
                if (!_actualUpdate.Properties.RTWDate.Equals(_actualUpdate.Properties.TimeToGoLive))
                {
                    result = false;
                    _result.AddFailure("Readiness Date(RTW)",
                        new TestFailure() { ExpectResult = _actualUpdate.Properties.TimeToGoLive, ActualResult = _actualUpdate.Properties.RTWDate });
                }
                if (!result)
                {
                    _result.Result &= result;
                    _result.LogError("Readiness Date verification failed");
                }
            }

            _result.LogMessage("Verify RebootBehavior of Install for each child update...");
            result = true;
            foreach (var child in _actualUpdate.ChildUpdates)
            {
                if (!child.InstallRebootBehavior.Equals("CanRequestReboot"))
                {
                    result = false;
                    _result.AddFailure("RebootBehavior of " + child.Title,
                        new TestFailure() { ExpectResult = "CanRequestReboot", ActualResult = child.InstallRebootBehavior });
                }
            }
            if (!result)
            {
                _result.Result &= result;
                _result.LogError("RebootBehavior verification failed");
            }
        }
    }
}
