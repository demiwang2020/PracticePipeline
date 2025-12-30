using Helper;
using LogAnalyzer;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    class TestCaseVerifyComponentVer : TestCaseBase
    {
        public TestCaseVerifyComponentVer(THTestObject testobj)
            : base(testobj)
        {

        }

        public override bool RunTestCase()
        {
            bool overallResult = true;
            bool testResult = true;
            if (TestObject.TFSItem.OSSPLevel == "25H2")
            {


                foreach (var patch in TestObject.Patches)
                {
                    if (patch.Value.ActualBinaries != null && !String.IsNullOrEmpty(patch.Value.LCUPatchPath))
                    {
                        DataTable Results;
                        testResult = RunTest(patch.Key, out Results);

                        TestObject.TestResults.ResultDetails.Add(Results);
                        TestObject.TestResults.ResultDetailSummaries.Add(testResult);
                        overallResult &= testResult;
                        if (overallResult)
                            Results.TableName = "Test Passed: Verify ComponentVersion";
                    }
                }
            }
            TestObject.TestResults.Result &= overallResult;
            return overallResult;
        }

        private bool RunTest(Architecture arch, out DataTable resultTable)
        {

            bool result = true;
            resultTable = null;

            DataTable newPatchPayload = TestObject.Patches[arch].ActualBinaries;
            string archString = arch == Architecture.AMD64 ? "x64" : arch.ToString();

            resultTable = HelperMethods.CreateDataTable("Verify ComponentVersion in " + arch + " package ",
                                new string[] {  "File Name", "ProcessorArchitecture", "ComponentVersion",  "Result" },
                                new string[] { "style=width:11.11%;text-align:center#ResultCol=1", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center" });

            foreach(DataRow f in newPatchPayload.Rows)
            {
                if(!f["ComponentVersion"].ToString().StartsWith("4.0.15920") && f["SKU"].ToString().StartsWith("4."))
                {
                    result = false;
                    DataRow row = resultTable.NewRow();
                    row["File Name"] = f["FileName"];
                    row["ProcessorArchitecture"] = f["ProcessorArchitecture"];
                    row["ComponentVersion"] = f["ComponentVersion"];
                    row["Result"] = "Fail";
                    resultTable.Rows.Add(row);
                }
            }

            return result;
        }

    }
}
