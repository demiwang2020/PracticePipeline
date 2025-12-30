using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    class TestCaseVerifyNoUnexpectPayloadInSimplePatch : TestCaseBase
    {
        public TestCaseVerifyNoUnexpectPayloadInSimplePatch(THTestObject testobj)
            : base(testobj)
        {
        }


        //This test is to verify SO doesn't carry any other files besides expect files
        public override bool RunTestCase()
        {
            // If a patch is LCU, no need to run this test
            // There are other tests to verify LCU payload
            if (!TestObject.SimplePatch && TestObject.TFSItem.LCUKBArticle!="TBD")
                return true;

            DataTable resultTable = HelperMethods.CreateDataTable("Verify simple patch doesn't have extra files", 
                new string[] { "Parch Arch", "Result", "File Name", "Version", "Component Version", "ProcessorArchitecture" },
                new string[] { "style=width:12%;text-align:center", "style=width:20%;text-align:center#ResultCol=1", "style=width:26%;text-align:center", "style=width:15%;text-align:center", "style=width:15%;text-align:center", "style=width:12%;text-align:center" });

            bool overallResult = true;
            foreach (var patch in TestObject.Patches)
            {
                DataRow[] rows = patch.Value.ActualBinaries.Select("InExpectFileList = '0'");
                if (rows.Length > 0)
                {
                    overallResult = false;

                    foreach (DataRow r in rows)
                    {
                        DataRow row = resultTable.NewRow();

                        row["Parch Arch"] = patch.Key.ToString();
                        row["Result"] = "Additional File";
                        row["File Name"] = r["FileName"];
                        row["Version"] = r["Version"];
                        row["Component Version"] = r["ComponentVersion"];
                        row["ProcessorArchitecture"] = r["ProcessorArchitecture"];

                        resultTable.Rows.Add(row);
                    }
                }
            }

            // If test passed, only print test name
            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(overallResult);

            if (overallResult)
            {
                resultTable.TableName = "Test Passed: " + resultTable.TableName;
            }
            else
            {
                overallResult = HelperMethods.IsVersionResultWarning(resultTable, "Version", TestObject.TFSItem.SKU);
                TestObject.TestResults.Result &= overallResult;

                if (overallResult) //switch to warning result
                {
                    resultTable.TableName = "WARNING: " + resultTable.TableName;
                }
            }

            return overallResult;
        }
    }
}
