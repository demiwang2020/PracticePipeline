using Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    // This case verifies *netfx4clientcorecomp*.manifest include in RS5 and downlevel 4.8 patch
    // But not include in 19h1+ patch
    class TestCaseVerifySpecificManifests : TestCaseBase
    {
        public TestCaseVerifySpecificManifests(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            List<ManifestInfo> manifestsToVerify = null;

            if (CaseNeeded(out manifestsToVerify))
            {
                bool result = true;

                DataTable resultTable = HelperMethods.CreateDataTable(BuildResultTableName(manifestsToVerify),
                    new string[] { "Patch Arch", "Expected Specific Manifests", "Unexpected Specific Manifests", "Result" },
                    new string[] { "style=width:10%;text-align:center", "style=width:40%;text-align:center", "style=width:40%;text-align:center", "style=width:10%;text-align:center#ResultCol=1" });

                foreach (var patchInfo in TestObject.Patches)
                {
                    DataRow row = resultTable.NewRow();
                    row["Patch Arch"] = patchInfo.Key.ToString();
                    bool stepResult = true;

                    foreach (var manifest in manifestsToVerify)
                    {
                        string colName = manifest.Expect ? "Expected Specific Manifests" : "Unexpected Specific Manifests";

                        string[] files = Directory.GetFiles(patchInfo.Value.ExtractLocation, manifest.Name, SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            var fileNames = files.Select(p => Path.GetFileName(p));

                            if (String.IsNullOrEmpty(row[colName].ToString()))
                            {
                                row[colName] = String.Join("<br/> ", fileNames);
                            }
                            else
                            {
                                row[colName] = row[colName].ToString() + "<br/> "  + String.Join("<br/> ", fileNames);
                            }
                        }

                        stepResult &= !(manifest.Expect ^ (files.Length > 0));
                    }

                    row["Result"] = stepResult ? "Pass" : "Fail";
                    result &= stepResult;

                    if (String.IsNullOrEmpty(row["Expected Specific Manifests"].ToString()))
                        row["Expected Specific Manifests"] = "NA";
                    if (String.IsNullOrEmpty(row["Unexpected Specific Manifests"].ToString()))
                        row["Unexpected Specific Manifests"] = "NA";

                    resultTable.Rows.Add(row);
                }

                TestObject.TestResults.ResultDetails.Add(resultTable);
                TestObject.TestResults.ResultDetailSummaries.Add(result);
                TestObject.TestResults.Result &= result;

                return result;
            }
            else
            {
                return true;
            }
        }

        private bool CaseNeeded(out List<ManifestInfo> manifests)
        {
            manifests = null;

            if (TestObject.TFSItem.SKU.StartsWith("4.8") && 
                !String.IsNullOrEmpty(TestObject.Patches.First().Value.LCUPatchPath))
            {
                if (TestObject.TFSItem.SKU =="4.8.1" && TestObject.IsProductRefresh == false) {

                    return false;
                }
                ManifestInfo manifest = new ManifestInfo()
                {
                    Name = "*netfx4clientcorecomp*.manifest",
                    //Expect = !TestObject.IsWindows10Patch || TestObject.TFSItem.OSSPLevel.CompareTo("19H1") < 0
                    Expect = TestObject.TFSItem.KBNumber.Equals(5018210) || TestObject.IsProductRefresh || !TestObject.IsWindows10Patch || TestObject.TFSItem.OSSPLevel.CompareTo("19H1") < 0
                };

                manifests = new List<ManifestInfo>() { manifest };

                return true;
            }
            else if (TestObject.TFSItem.OSInstalled == "Windows 7" &&
                    !String.IsNullOrEmpty(TestObject.Patches.First().Value.LCUPatchPath))
            {
                ManifestInfo manifest = new ManifestInfo()
                {
                    Name = "*microsoft-windows-wcfcorecomp*.manifest",
                    Expect = true
                };

                manifests = new List<ManifestInfo>() { manifest };

                return true;
            }
            else
                return false;
        }

        private string BuildResultTableName(List<ManifestInfo> manifests)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Verify ");
            foreach (var info in manifests)
            {
                if (info.Expect)
                    sb.AppendFormat("{0} exist, ", info.Name);
                else
                    sb.AppendFormat("{0} NOT exist, ", info.Name);
            }

            sb.Remove(sb.Length - 2, 2);

            return sb.ToString();
        }

        struct ManifestInfo
        {
            public string Name;
            public bool Expect;
        }
    }
}
