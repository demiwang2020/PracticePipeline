using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper;

namespace THTestLib.Testcases
{
    class TestCaseVerifyConflict4XFilesNotInclude : TestCaseBase
    {
        public TestCaseVerifyConflict4XFilesNotInclude(THTestObject testobj)
           : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            List<string> unexpectedVersionPrefix = new List<string>();
            if (TestObject.TFSItem.SKU.StartsWith("4.8"))
            {
                unexpectedVersionPrefix.Add("4.7");
                unexpectedVersionPrefix.Add("4.6");
                unexpectedVersionPrefix.Add("4.0");
            }
            else if (TestObject.TFSItem.SKU.StartsWith("4.6") || TestObject.TFSItem.SKU.StartsWith("4.7"))
            {
                unexpectedVersionPrefix.Add("4.8");
                unexpectedVersionPrefix.Add("4.0");
            }
            else if (TestObject.TFSItem.SKU.StartsWith("4.5")) // for the purpose of extension of testing 4.5.2
            {
                unexpectedVersionPrefix.Add("4.7");
                unexpectedVersionPrefix.Add("4.6");
                unexpectedVersionPrefix.Add("4.8");
            }
            else // for 2.0/3.0/3.5, skip this case
            {
                return true;
            }

            Dictionary<Architecture, List<DataRow>> issuedRows = new Dictionary<Architecture, List<DataRow>>();

            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    var rows = RunTest(patch.Value.ActualBinaries, unexpectedVersionPrefix);
                    if(rows.Count > 0)
                    {
                        issuedRows.Add(patch.Key, rows);
                    }
                }
            }

            DataTable resultTable = null;
            bool result = true;
            if (issuedRows.Count > 0) // found unexpected version
            {
                result = false;

                resultTable = HelperMethods.CreateDataTable("Verify Conflicting 4.X binaries are not included",
                    new string[] { "Patch Arch", "FileName", "ProcessorArchitecture", "Version", "Result" },
                    new string[] { "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center#ResultCol=1" });

                foreach(var kv in issuedRows)
                {
                    foreach(DataRow r in kv.Value)
                    {
                        AddResult(resultTable, kv.Key, r);
                    }
                }
            }
            else // nothing unexpected found
            {
                resultTable = HelperMethods.CreateDataTable("Verify Conflicting 4.X binaries are not included",
                    new string[] { "Patch Arch", "Test", "Result" },
                    new string[] { "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center#ResultCol=1" });

                string strUnexpectedVersionPrefix = String.Join(" or ", unexpectedVersionPrefix);

                foreach (var patch in TestObject.Patches)
                {
                    if (patch.Value.ActualBinaries != null)
                    {
                        AddResult(resultTable, patch.Key, strUnexpectedVersionPrefix);
                    }
                }

            }

            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(result);

            TestObject.TestResults.Result &= result;

            return result;
        }

        private List<DataRow> RunTest(DataTable patchPayload, List<string> unexpectVersionPrefix)
        {
            List<string> KnownIssueList = new List<string>() {
            "sbsnclperf.dll", "mscorlib.tlb"
            };
            List<DataRow> rows = new List<DataRow>();

            foreach (DataRow r in patchPayload.Rows)
            {
                
                bool found = false;
                foreach (string prefix in unexpectVersionPrefix)
                {
                    if (KnownIssueList.Any(p => p.Equals(r["FileName"].ToString())) &&
                        r["Version"].ToString().StartsWith(prefix) &&
                        TestObject.TFSItem.OSSPLevel == "25H2")
                    {
                        continue;
                    }
                    if (r["Version"].ToString().StartsWith(prefix) && 
                        !FailureIgnorable(r["FileName"].ToString(), r["Version"].ToString()))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    rows.Add(r);
            }

            return rows;
        }

        private void AddResult(DataTable resultTable, Architecture patchArch, DataRow issueRow)
        {
            DataRow r = resultTable.NewRow();

            r["Patch Arch"] = patchArch.ToString();
            r["FileName"] = issueRow["FileName"];
            r["ProcessorArchitecture"] = issueRow["ProcessorArchitecture"];
            r["Version"] = issueRow["Version"];
            r["Result"] = "Fail";

            resultTable.Rows.Add(r);
        }

        private void AddResult(DataTable resultTable, Architecture patchArch, string unexpectedVersionPrefix)
        {
            DataRow r = resultTable.NewRow();

            r["Patch Arch"] = patchArch.ToString();
            r["Test"] = "None of 4.x binaries have version starts with " + unexpectedVersionPrefix;
            r["Result"] = "Pass";

            resultTable.Rows.Add(r);
        }

        private bool FailureIgnorable(string fileName, string fileVersion)
        {
            // hardcode some known exceptions in 4.8 product refresh
            if (TestObject.IsProductRefresh)
            {
                if (fileName == "sbsnclperf.dll" && fileVersion == "4.0.41209.0" ||
                   fileName == "dfshim.dll.mui" && fileVersion == "4.0.41209.0" ||
                   fileName == "mscorlib.tlb" && fileVersion == "4.0.30319.17554")
                    return true;
            }

            return false;
        }
    }
}