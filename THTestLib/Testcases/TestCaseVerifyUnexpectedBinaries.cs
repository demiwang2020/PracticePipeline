using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    class TestCaseVerifyUnexpectedBinaries : TestCaseBase
    {
        public TestCaseVerifyUnexpectedBinaries(THTestObject testobj)
            : base(testobj)
        {
        }
        public override bool RunTestCase()
        {
            // When there is no unexpected binaries set for testing, just skip this case
            if (TestObject.NotExpectedBinaryList == null || TestObject.NotExpectedBinaryList.Count == 0)
                return true;

            bool overallResult = true;

            DataTable resultTable;
            bool result = false;

            // Verify all patches does not have any unexpected binaries
            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    result = ExecuteVerifyUnexpectedBinaries(patch.Value.ActualBinaries, out resultTable);
                    resultTable.TableName = "Verify unexpected binaries do not exist in " + patch.Key.ToString() + " patch";
                    TestObject.TestResults.ResultDetails.Add(resultTable);
                    TestObject.TestResults.ResultDetailSummaries.Add(result);
                    overallResult &= result;
                }
            }

            TestObject.TestResults.Result &= overallResult;
            return overallResult;
        }


        private bool ExecuteVerifyUnexpectedBinaries(DataTable fileTable, out DataTable result)
        {
            bool ret = true;

            result = HelperMethods.CreateDataTable(String.Empty,
                new string[] { "FileName", "Existence", "ProcessorArchitecture", "Version", "ComponentVersion", "Result" },
                new string[] { "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center#ResultCol=1" });

            foreach (string file in TestObject.NotExpectedBinaryList)
            {
                DataRow[] rows = fileTable.Select(String.Format("FileName = '{0}'", file));

                if (rows.Length == 0) // pass
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = file;
                    row["Existence"] = "Not Found";
                    row["Result"] = "Pass";

                    result.Rows.Add(row);
                }
                else // fail
                {
                    ret = false;

                    foreach (DataRow r in rows)
                    {
                        DataRow row = result.NewRow();

                        row["FileName"] = file;
                        row["Existence"] = "Found";
                        row["Result"] = "Fail";

                        row["ProcessorArchitecture"] = r["ProcessorArchitecture"];
                        row["Version"] = r["Version"];
                        row["ComponentVersion"] = r["ComponentVersion"];

                        result.Rows.Add(row);
                    }
                }
            }

            if (ret)
            {
                result.Columns.Remove("ProcessorArchitecture");
                result.Columns.Remove("Version");
                result.Columns.Remove("ComponentVersion");
            }

            return ret;
        }
    }
}
