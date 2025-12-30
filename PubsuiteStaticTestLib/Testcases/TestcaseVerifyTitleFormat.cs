using Helper;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using PubsuiteStaticTestLib.DbClassContext;
using PubsuiteStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Testcases
{
    class TestcaseVerifyTitleFormat : TestcaseBase
    {
        private List<WorkItemHelper> _tfsWorkItems;
        private Dictionary<string, string> _osNameLookupTable;

        private string _actualReleaseMonth;
        private string _actualReleaseType;
        private List<string> _actualSKUs;

        public TestcaseVerifyTitleFormat(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Verify format of update title")
        {
            GetTFSWorkItems();
            BuildOSLookupTable();
            AnalyzeActualTitle();
        }

        protected override void RunTest()
        {
            // Verify strings like '2019-09'
            _result.LogMessage("Verifying string of release month...");
             string expectReleaseMonth = GetExpectReleaseDate();
            if (!_actualReleaseMonth.Equals(expectReleaseMonth)) 
            {
                _result.LogError("Release month verification FAILED");
                _result.AddFailure("Release month", new TestFailure() { ExpectResult = expectReleaseMonth, ActualResult = _actualReleaseMonth });
                _result.Result = false;
            }

            // Verify TTGL uniform with release month
            _result.LogMessage("Verifying release month same as TTGL...");
            DateTime ttgl = CommonHelper.TTGLString2DateTime(_actualUpdate.Properties.TimeToGoLive);
            string monthFromTTGL = String.Format("{0}-{1:00}", ttgl.Year, ttgl.Month);
            if (!_actualReleaseMonth.Equals(monthFromTTGL))
            {
                _result.LogError("Release month verification FAILED");
                _result.AddFailure("Release month (Compare to TTGL)", new TestFailure() { ExpectResult = monthFromTTGL, ActualResult = _actualReleaseMonth });
                _result.Result = false;
            }

            // Verify strings like 'Security only update'
            _result.LogMessage("Verifying release type name...");
            string expectUpdateType = GetExpectUpdateType();
            if (!_actualReleaseType.Equals(expectUpdateType))
            {
                _result.LogError("Release type verification FAILED");
                _result.AddFailure("Release type", new TestFailure() { ExpectResult = expectUpdateType, ActualResult = _actualReleaseType });
                _result.Result = false;
            }

            // Verify os names are unified with TFS
            _result.LogMessage("Verifying OS names...");
            VerifyOSNamesInTitle();

            // Verify strings like '3.5', '4.7.2', '4.8'
            _result.LogMessage("Verifying .NET names...");
            VerifyNETSKUsInTitle();
        }

        private void GetTFSWorkItems()
        {
            if (_inputData.TFSIDs != null && _inputData.TFSIDs.Count > 0)
            {
                _tfsWorkItems = new List<WorkItemHelper>();

                foreach (var id in _inputData.TFSIDs)
                {
                    _tfsWorkItems.Add(new WorkItemHelper(TFSHelper.TFSURI, id));
                }
            }
        }

        private void BuildOSLookupTable()
        {
            // map the os names in update title to target os of patch in tfs
            _osNameLookupTable = new Dictionary<string, string>();
            _osNameLookupTable.Add("Server 2008", "Windows Vista;Windows Server 2008");
            _osNameLookupTable.Add("Windows 7", "Windows 7");
            _osNameLookupTable.Add("Windows Embedded Standard 7", "Windows 7");
            _osNameLookupTable.Add("Server 2008 R2", "Windows 7");
            _osNameLookupTable.Add("Server 2012", "Windows Server 2012;Windows 8");
            _osNameLookupTable.Add("Windows Embedded 8", "Windows Server 2012;Windows 8");
            _osNameLookupTable.Add("Windows 8.1", "Windows 8.1");
            _osNameLookupTable.Add("Server 2012 R2", "Windows 8.1");
            _osNameLookupTable.Add("Windows Embedded 8.1", "Windows 8.1");
            _osNameLookupTable.Add("Windows 8.1 Embedded", "Windows 8.1");
            _osNameLookupTable.Add("Windows 10 Version 1809", "1809");
            _osNameLookupTable.Add("Server 2019", "1809");
            _osNameLookupTable.Add("Server 2016", "1607");
            _osNameLookupTable.Add("Windows 10 Version 1607", "1607");
            _osNameLookupTable.Add("Windows 10 Version 1703", "1703");
            _osNameLookupTable.Add("Windows 10 Version 1709", "1709");
            _osNameLookupTable.Add("Windows 10 Version 1803", "1803");
            _osNameLookupTable.Add("Windows 10 Version 1903", "1903");
            _osNameLookupTable.Add("Windows Server 2016 (1803)", "1803");
            _osNameLookupTable.Add("Windows Server 2019 (1903)", "1903");
            _osNameLookupTable.Add("Windows Server, version 1903", "1903");

            _osNameLookupTable.Add("Windows 10 Version 1909", "1903");
            _osNameLookupTable.Add("Windows Server, version 1909", "1903");

            _osNameLookupTable.Add("Windows 10 Version 2004", "20H1");
            _osNameLookupTable.Add("Windows Server, version 2004", "20H1");
            _osNameLookupTable.Add("Windows 10 Version 20H2", "20H1;20H2;21H1;21H2;22H2");
            _osNameLookupTable.Add("Windows Server, version 20H2", "20H1;20H2");
            _osNameLookupTable.Add("Windows 10 Version 21H1", "20H1;20H2;21H1;21H2;22H2");
            _osNameLookupTable.Add("Windows 10 Version 21H2", "20H1;20H2;21H1;21H2;22H2");
            _osNameLookupTable.Add("Windows 10 Version 22H2", "20H1;20H2;21H1;21H2;22H2");
            _osNameLookupTable.Add("Microsoft server operating system version 21H2", "2022;Windows Server 2022");
            _osNameLookupTable.Add("Microsoft server operating system, version 22H2", "2022;Windows Server 2022");
            _osNameLookupTable.Add("Microsoft server operating system, version 23H2", "Server OS");
            _osNameLookupTable.Add("Microsoft server operating system version 24H2", "24H2");
            _osNameLookupTable.Add("Windows 11", "SV21H2");
            _osNameLookupTable.Add("Windows 11, version 22H2", "22H2");
            _osNameLookupTable.Add("Windows 11, version 23H2", "23H2;22H2");
            _osNameLookupTable.Add("Windows 11, version 24H2", "24H2");

            _osNameLookupTable.Add("Azure Stack HCI, version 20H2", "1809 HCI");

            _osNameLookupTable.Add("Windows Version Next", "Preview");
            _osNameLookupTable.Add("Windows Server Version Next", "2022;Preview");
        }

        private void AnalyzeActualTitle()
        {
            //title sample: 2019-07 Security and Quality Rollup for .NET Framework 3.5, 4.5.2, 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1, 4.7.2, 4.8 on Windows Server 2012 for x64 (KB4507421)
            
            // release month
            var matches = Regex.Matches(_actualUpdate.Title, @"^\d{4}-\d{2}");
            if (matches != null && matches.Count > 0)
                _actualReleaseMonth = matches[0].Value;
            else
                _actualReleaseMonth = String.Empty;

            // release type
            int endIndex = _actualUpdate.Title.IndexOf(" for ");
            int startIndex = String.IsNullOrEmpty(_actualReleaseMonth) ? 0 : _actualReleaseMonth.Length + 1;
            _actualReleaseType = _actualUpdate.Title.Substring(startIndex, endIndex - startIndex);

            // .NET SKU
            startIndex = _actualUpdate.Title.IndexOf(" .NET Framework ");
            endIndex = _actualUpdate.Title.IndexOf(" on ", startIndex);
            if (endIndex < 0)
                endIndex = _actualUpdate.Title.IndexOf(" for ", startIndex);
            string skus = _actualUpdate.Title.Substring(startIndex + 16, endIndex - startIndex - 16);

            _actualSKUs = skus.Split(new string[] { ", ", " and " }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private string GetExpectReleaseDate()
        {
            string custom02 = String.Empty;

            // Also check if all child bundle is in same release here
            foreach (var t in _tfsWorkItems)
            {
                if (String.IsNullOrEmpty(custom02))
                    custom02 = t.Custom02;
                else if (!custom02.Equals(t.Custom02, StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception("Update has child bundles in different release");
            }

            if (!String.IsNullOrEmpty(custom02))
            {
                //sample: 2019.08 C
                return custom02.Substring(0, 7).Replace('.', '-');
            }
            else
            {
                return "Not found";
            }
        }

        private string GetExpectUpdateType()
        {
            string releaseType = String.Empty;

            if (_inputData.OtherProperties != null && _inputData.OtherProperties.ContainsKey("ReleaseType"))
            {
                releaseType = _inputData.OtherProperties["ReleaseType"];
            }
            
            if(String.IsNullOrEmpty(releaseType) || releaseType.Contains("Catalog")) // try to get release type from TFS title
            {
                string title = _tfsWorkItems.Last().Title;

                if (title.Contains("Security and Quality Rollup"))
                    releaseType = "MonthlyRollup";
                else if (title.Contains("Security Only"))
                    releaseType = "SecurityOnly";
                else if (title.Contains("Preview of Quality Rollup"))
                    releaseType = "Preview";
                else // failed to detect from tfs title, try to use Custom02
                {
                    var wi = _tfsWorkItems.Last();
                    if (wi.Custom02.EndsWith("B"))
                    { 
                        if(wi.ReleaseType == "Update")
                            releaseType = "MonthlyRollup";
                        else
                            releaseType = "SecurityOnly";
                    }
                    else
                        releaseType = "Preview";
                }
            }

            if (_tfsWorkItems.Last().OSInstalled == "Windows 10" || _tfsWorkItems.Last().OSInstalled == "Windows 11"|| _tfsWorkItems.Last().OSSPLevel== "23H2")
            {
                if(releaseType == "Preview" || releaseType == "Catalog(Preview)")
                    return "Cumulative Update Preview";
                else
                    return "Cumulative Update";
            }

            using (var db = new WUSAFXDbContext())
            {
                var queryResult = db.TReleaseTypes.Where(p => p.Name.Equals(releaseType)).FirstOrDefault();

                if (queryResult != null)
                    return queryResult.Keyword;
                else
                    return "Unknown";
            }
        }

        private void VerifyOSNamesInTitle()
        {
            string osInUpdateTitle = OSDetector.ParseTargetOSFromUpdateTitle(_actualUpdate.Title);
            string exceptContent = OSDetector.ParseTargetOSFromUpdateTitle(_expectedUpdate.Title);
            if (String.IsNullOrEmpty(osInUpdateTitle))
            {
                _result.LogError("OS cannot be recognized from title");
                _result.AddFailure("OS Name", new TestFailure() { ExpectResult = exceptContent, ActualResult = osInUpdateTitle });
                _result.Result = false;
            }
            else
            {
                if (!_osNameLookupTable.ContainsKey(osInUpdateTitle))
                {
                    _result.LogError(String.Format("Failed find corresponding OS {0} from OS table", osInUpdateTitle));
                    _result.Result = false;
                }
                else
                {
                    string expectOSes = _osNameLookupTable[osInUpdateTitle];

                    foreach (var wi in _tfsWorkItems)
                    {
                        string actualOS = wi.OSInstalled == "Windows 10" || wi.OSInstalled == "Windows 11" ? wi.OSSPLevel : wi.OSInstalled;

                        string expectOSes2 = wi.PatchTechnology == "MSI" ? "Windows Server 2008;Windows 7" : expectOSes;

                        // 4.x redist patch also applicable on 2008
                        if (osInUpdateTitle == "Server 2008" && wi.PatchTechnology == "MSI")
                            actualOS = "Windows Server 2008";

                        if (actualOS == "19H1")
                            actualOS = "1903";
                        else if (actualOS == "19H2")
                            actualOS = "1909";

                        if (!expectOSes2.Contains(actualOS))
                        {
                            _result.Result = false;
                            _result.LogError("TFS OS doesn't match with actual OS for child KB" + wi.KBNumber);
                            _result.AddFailure("KB OS: " + wi.KBNumber, new TestFailure() { ExpectResult = expectOSes2, ActualResult = actualOS });
                        }
                    }
                }
            }
        }

        private List<string> GetExpectNETSKUs()
        {
            List<string> expectSKUs = new List<string>();
            string osInUpdateTitle = OSDetector.ParseTargetOSFromUpdateTitle(_actualUpdate.Title);

            foreach (var wi in _tfsWorkItems)
            {
                if(osInUpdateTitle.Contains("1809") && _actualUpdate.Title.Contains("ARM64") && wi.SKU == "4.8")
                {
                    continue;
                }
                switch (wi.SKU)
                {
                    case "2.0":
                    case "3.0":
                        if (osInUpdateTitle == "Server 2008")
                        {
                            expectSKUs.Add("2.0");
                            
                            if (!_actualUpdate.Title.Contains("Itanium"))
                                expectSKUs.Add("3.0");

                            if (_actualUpdate.Title.Contains("3.5 SP1"))
                                expectSKUs.Add("3.5 SP1");

                        }
                        else if (osInUpdateTitle == "Windows 7" || 
                            osInUpdateTitle == "Windows Embedded Standard 7" ||
                            osInUpdateTitle == "Server 2008 R2")
                            expectSKUs.Add("3.5.1");
                        else
                            expectSKUs.Add("3.5");
                        break;
                    case "4.5.2":
                    case "4.8":
                    case "4.8.1":
                        expectSKUs.Add(wi.SKU);
                        break;

                    case "4.7.2":
                        if (osInUpdateTitle == "Server 2008")
                        {
                            //expectSKUs.Add("4.6");
                            expectSKUs.Add("4.6.2");
                        }
                        else if (osInUpdateTitle == "Windows 7" ||
                                osInUpdateTitle == "Windows Embedded Standard 7" ||
                                osInUpdateTitle == "Server 2008 R2" ||
                                osInUpdateTitle == "Server 2012" ||
                                osInUpdateTitle == "Windows Embedded 8" ||
                                osInUpdateTitle == "Windows 8.1" ||
                                osInUpdateTitle == "Server 2012 R2"||
                                osInUpdateTitle == "Windows Embedded 8.1"||
                                osInUpdateTitle == "Windows 8.1 Embedded")
                        {
                            //expectSKUs.Add("4.6");
                            //expectSKUs.Add("4.6.1");
                            expectSKUs.Add("4.6.2");
                            expectSKUs.Add("4.7");
                            expectSKUs.Add("4.7.1");
                            expectSKUs.Add("4.7.2");
                        }
                        else
                        {
                            expectSKUs.Add("4.7.2");
                        }
                        break;
                }
            }

            return expectSKUs;
        }

        private void VerifyNETSKUsInTitle()
        {
            List<string> lstExpectSKUs = GetExpectNETSKUs();

            //RS5+ LCU carries 3.5 by default
            WorkItemHelper wi = _tfsWorkItems.Last();
            if ((wi.OSInstalled == "Windows 10" && String.Compare(wi.OSSPLevel, "1809") >= 0 ||
                wi.OSInstalled == "Windows 11") &&
                !lstExpectSKUs.Contains("3.5") || wi.OSSPLevel == "23H2")
            {
                lstExpectSKUs.Add("3.5");
            }
            else if (_actualUpdate.Title.Contains("Server 2008 SP2") && wi.SKU == "3.5") {

                lstExpectSKUs.Add("3.5 SP1");
            }

            List<string> missingSKUs = lstExpectSKUs.Except(_actualSKUs).ToList();
            List<string> additionalSKUs = _actualSKUs.Except(lstExpectSKUs).ToList();

            if (missingSKUs.Count > 0)
            {
                _result.LogError("Title misses SKUs: " + String.Join(", ", missingSKUs));
                _result.AddFailure("Net SKU in Title", new TestFailure() { ExpectResult = String.Join(",",lstExpectSKUs), ActualResult = String.Join(",",_actualSKUs) });
                _result.Result = false;
            }

            if (additionalSKUs.Count > 0)
            {
                _result.LogError("Title carries additional SKUs: " + String.Join(", ", additionalSKUs));
                _result.AddFailure("Net SKU in Title", new TestFailure() { ExpectResult = String.Join(",", lstExpectSKUs), ActualResult = String.Join(",", _actualSKUs) });
                _result.Result = false;
            }
        }
    }
}
