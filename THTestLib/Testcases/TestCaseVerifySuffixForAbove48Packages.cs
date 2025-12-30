using Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    class TestCaseVerifySuffixForAbove48Packages : TestCaseBase { 
    
        public TestCaseVerifySuffixForAbove48Packages(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            if (TestObject.TFSItem.Custom01.Equals("Product_Refresh") || !TestObject.TFSItem.SKU.StartsWith("4.8")) {
                //string PatchName = TestObject.TFSItem.GetPatchName(Helper.Architecture.AMD64).ToString();
                
                //if ((TestObject.TFSItem.SKU[0] < '4' && PatchName.StartsWith("Windows10"))||
                //   (TestObject.TFSItem.SKU[0] < '4' && PatchName.StartsWith("Windows11")))
                //{
                //    //DataTable warningFakeTable = new DataTable("WARNING: This Package have been tested in 4.x PTGs");

                //    //TestObject.TestResults.ResultDetails.Add(warningFakeTable);
                //    //TestObject.TestResults.ResultDetailSummaries.Add(false);
                //    return true;
                //}

                return true;
            }
                

            //Create result table
            DataTable resultTable = HelperMethods.CreateDataTable("Verify 4.8+ packages have suffix",
                new string[] { "Architecture", "FileName", "Result" },
                new string[] { "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center#ResultCol=1" });

            //Run test
            bool overallResult = true;

            bool result = false;

            // Verify the specified mainifest file exists
            foreach (var patch in TestObject.Patches)
            {
                result = ExecuteVerifySuffixForPackage(patch.Value.PatchLocation, patch.Key, resultTable);
                resultTable.TableName = "Verify 4.8+ packages have suffix";
                overallResult &= result;
            }
            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(result);
            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }
        private bool ExecuteVerifySuffixForPackage(string PathLocation,Helper.Architecture arch, DataTable resultTable) {
            
            string PatchName = Path.GetFileNameWithoutExtension(PathLocation);
            
            bool ret = false;
            
            DataRow row = resultTable.NewRow();
            row["Architecture"] = arch.ToString();
            row["FileName"] = PatchName;
            row["Result"] = "Fail";
       
            switch (TestObject.TFSItem.SKU.ToString()) 
            {
                case "4.8":
                    if (PatchName.EndsWith("-NDP48")) {
                        
                        row["Result"] = "Pass";
                        ret = true;
                    }
                    break;
                case "4.8.1":
                    if (PatchName.EndsWith("-NDP481"))
                    {
                        row["Result"] = "Pass";
                        ret = true;
                    }
                    break;

            }
            resultTable.Rows.Add(row);
            return ret;

        }
    }
}
