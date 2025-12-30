using Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace THTestLib.Testcases
{
    //This case runs sn.exe to all binaries to check if they are test signed
    class TestCaseRealSigned : TestCaseBase
    {
        public TestCaseRealSigned(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            if (TestObject.Patches.ContainsKey(Architecture.X86) && TestObject.Patches[Architecture.X86].ActualBinaries != null ||
                TestObject.Patches.ContainsKey(Architecture.AMD64) && TestObject.Patches[Architecture.AMD64].ActualBinaries != null)
            {
                bool result = true;
                DataTable resultTable = HelperMethods.CreateDataTable("Verify if files are officially signed",
                    new string[] { "Patch", "FileName", "SKU", "ProcessorArchitecture", "Result" },
                    new string[] { "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center#ResultCol=1" });

                foreach (var patch in TestObject.Patches)
                {
                    if (patch.Value.ActualBinaries != null)
                    {
                        result &= RunTest(resultTable, patch.Value.ActualBinaries, patch.Key);
                    }
                }

                TestObject.TestResults.ResultDetails.Add(resultTable);
                TestObject.TestResults.ResultDetailSummaries.Add(result);
                TestObject.TestResults.Result &= result;

                if (result)
                    resultTable.TableName = "Test Passed: Verify if files are officially signed";

                return result;
            }
            else
            {
                return true;
            }
        }

        private bool RunTest(DataTable resultTable, DataTable payloadTable, Architecture arch)
        {
            bool result = true;

            foreach (DataRow r in payloadTable.Rows)
            {
                // Only do this for new files
                //if (r["InExpectFileList"].ToString().Equals("0"))
                //    continue;

                // skip non-versioned files
                if (String.IsNullOrEmpty(r["Version"].ToString()))
                    continue;

                if (!RunSnCommand(r["ExtractPath"].ToString()))
                {
                    DataRow newRow = resultTable.NewRow();
                    newRow["FileName"] = r["FileName"];
                    newRow["SKU"] = r["SKU"];
                    newRow["ProcessorArchitecture"] = r["ProcessorArchitecture"];
                    newRow["Patch"] = arch.ToString();
                    newRow["Result"] = "Fail";
                    resultTable.Rows.Add(newRow);
                    result = false;
                }
            }

            return result;
        }

        private bool RunSnCommand(string filePath)
        {
            string toolPath = System.Configuration.ConfigurationManager.AppSettings["SN_Tool_Path"];

            string output;
            int ret = Helper.Utility.ExecuteCommandSync(toolPath, String.Format("-vf {0}", filePath), -1, out output);

            if (output.Contains("is a delay-signed or test-signed assembly"))
                return false;

            return true;
        }
    }
}
