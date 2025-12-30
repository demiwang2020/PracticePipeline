using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper;
namespace THTestLib.Testcases
{
    class TestCaseVerifyBinariesDestination : TestCaseBase
    {
        //List<Tuple<string, int, string>> _sku2Product;
        Dictionary<string, string> _destManifiest2Actual;
        
        public TestCaseVerifyBinariesDestination(THTestObject testobj)
            : base(testobj)
        {
            InitDestLookupTable();
        }

        public override bool RunTestCase()
        {
            bool overallResult = true;
            
            DataTable resultTable;
            bool result = false;

            foreach(var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    result = VerifyBinariesDestination(patch.Value.ActualBinaries, patch.Key, out resultTable);
                    resultTable.TableName = string.Format("Destination verification result of {0} package",patch.Key.ToString());
                    TestObject.TestResults.ResultDetails.Add(resultTable);
                    TestObject.TestResults.ResultDetailSummaries.Add(result);
                    overallResult &= result;
                }
            }

            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }

        /// <summary>
        /// Create a table to map actual destination path to the dest path in manifest file
        /// </summary>
        private void InitDestLookupTable()
        {
            _destManifiest2Actual = new Dictionary<string, string>();

            //2.0
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v1.0.3705\", @"%windir%\Microsoft.NET\Framework\v1.0.3705\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v2.0.50727\", @"%windir%\Microsoft.NET\Framework\v2.0.50727\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v2.0.50727\", @"%windir%\Microsoft.NET\Framework64\v2.0.50727\");

            //3.0
            _destManifiest2Actual.Add(@"$(runtime.programFiles)\Reference Assemblies\Microsoft\Framework\v3.0\", @"%programfiles%\Reference Assemblies\Microsoft\Framework\v3.0\");
            _destManifiest2Actual.Add(@"Program_Files\Reference_Assemblies\Microsoft\Framework\v3.0\", @"%programfiles%\Reference Assemblies\Microsoft\Framework\v3.0\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v3.0\", @"%windir%\Microsoft.NET\Framework\v3.0\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v3.0\", @"%windir%\Microsoft.NET\Framework64\v3.0\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v3.0\WPF\", @"%windir%\Microsoft.NET\Framework\v3.0\WPF\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v3.0\WPF\", @"%windir%\Microsoft.NET\Framework64\v3.0\WPF\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v3.0\Windows Communication Foundation\", @"%windir%\Microsoft.NET\Framework\v3.0\Windows Communication Foundation\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v3.0\Windows Communication Foundation\", @"%windir%\Microsoft.NET\Framework64\v3.0\Windows Communication Foundation\");

            //3.5
            _destManifiest2Actual.Add(@"$(runtime.programFiles)\Reference Assemblies\Microsoft\Framework\v3.5\", @"%programfiles%\Reference Assemblies\Microsoft\Framework\v3.5\");
            _destManifiest2Actual.Add(@"Program_Files\Reference_Assemblies\Microsoft\Framework\v3.5\", @"%programfiles%\Reference Assemblies\Microsoft\Framework\v3.5\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v3.5\", @"%windir%\Microsoft.NET\Framework\v3.5\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v3.5\", @"%windir%\Microsoft.NET\Framework64\v3.5\");

            //4.6
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v4.0.30319\", @"%windir%\Microsoft.NET\Framework\v4.0.30319\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v4.0.30319\", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\FrameworkArm64\v4.0.30319\", @"%windir%\Microsoft.NET\FrameworkARM64\v4.0.30319\");

            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v4.0.30319\NativeImages\", @"%windir%\Microsoft.NET\Framework\v4.0.30319\NativeImages\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v4.0.30319\NativeImages\", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\NativeImages\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\FrameworkArm64\v4.0.30319\NativeImages\", @"%windir%\Microsoft.NET\FrameworkARM64\v4.0.30319\NativeImages\");

            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v4.0.30319\WPF\", @"%windir%\Microsoft.NET\Framework\v4.0.30319\WPF\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v4.0.30319\WPF\", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\WPF\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\FrameworkArm64\v4.0.30319\WPF\", @"%windir%\Microsoft.NET\FrameworkARM64\v4.0.30319\WPF\");

            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v4.0.30319\WPF\en-US\", @"%windir%\Microsoft.NET\Framework\v4.0.30319\WPF\en-US\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v4.0.30319\WPF\en-US\", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\WPF\en-US\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\FrameworkArm64\v4.0.30319\WPF\en-US\", @"%windir%\Microsoft.NET\FrameworkARM64\v4.0.30319\WPF\en-US\");

            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v4.0.30319\WPF\Fonts\", @"%windir%\Microsoft.NET\Framework\v4.0.30319\WPF\Fonts\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v4.0.30319\WPF\Fonts\", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\WPF\Fonts\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\FrameworkArm64\v4.0.30319\WPF\Fonts\", @"%windir%\Microsoft.NET\FrameworkARM64\v4.0.30319\WPF\Fonts\");

            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework\v4.0.30319\1033\", @"%windir%\Microsoft.NET\Framework\v4.0.30319\1033\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\Framework64\v4.0.30319\1033\", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\1033\");
            _destManifiest2Actual.Add(@"$(runtime.windows)\Microsoft.NET\FrameworkArm64\v4.0.30319\1033\", @"%windir%\Microsoft.NET\FrameworkArm64\v4.0.30319\1033\");

            _destManifiest2Actual.Add(@"$(runtime.system32)\", @"%windir%\system32\");
            _destManifiest2Actual.Add(@"$(runtime.wbem)\", @"%windir%\system32\wbem\");

            _destManifiest2Actual.Add(@"$(runtime.inf)\SMSvcHost 4.0.0.0\", @"%windir%\INF\SMSvcHost 4.0.0.0\");
            _destManifiest2Actual.Add(@"$(runtime.inf)\SMSvcHost 4.0.0.0\0000\", @"%windir%\INF\SMSvcHost 4.0.0.0\0000\");

            //_destManifiest2Actual.Add(@"$(runtime.system32)\XPSViewer\", @"%windir%\system32\XPSViewer\");
            _destManifiest2Actual.Add(@"$(runtime.system32)\XPSViewer\", @"%system%\XPSViewer\");

        }

        private bool VerifyBinariesDestination(DataTable fileTable, Architecture arch, out DataTable resultTable)
        {
            resultTable = HelperMethods.CreateDataTable(String.Empty,
                new string[] { "FileName", "Version", "ProcessorArchitecture", "Destination Path in Manifest", "Destination Path in DB", "Result" },
                new string[] { "style=width:15%;text-align:center", "style=width:10%;text-align:center", "style=width:8%;text-align:center", "width=30%", "width=29%", "style=width:8%;text-align:center#ResultCol=1" });

            bool ret = true;

            foreach (DataRow row in fileTable.Rows)
            {

                // Only do this for new files
                if (row["FileName"].ToString().Contains("vbc.exe"))
                {
                    int i = 0;
                }
                if (row["FileName"].ToString().Contains("vbc.rsp"))
                {
                    int i = 0;
                }
                if (row["InExpectFileList"].ToString().Equals("0"))
                    continue;
                if ((row["SKU"].ToString() == "3.0" || row["SKU"].ToString() == "2.0" || row["SKU"].ToString() == "3.5") && row["ComponentVersion"].ToString().StartsWith("4."))
                    continue;
                DataRow resultRow = resultTable.NewRow();
                
                resultRow["FileName"] = row["FileName"];
                resultRow["Version"] = row["Version"];
                resultRow["ProcessorArchitecture"] = row["ProcessorArchitecture"];
                resultRow[3] = row["DestPath"];
                resultTable.Rows.Add(resultRow);

                if(!_destManifiest2Actual.ContainsKey(row["DestPath"].ToString()))
                {
                    if (String.IsNullOrEmpty(row["DestPath"].ToString()) && row["ProcessorArchitecture"].ToString().ToLowerInvariant() == "msil")
                    {
                        resultRow[4] = String.Empty;
                        resultRow["Result"] = "Pass";
                    }
                    else
                    {
                        resultRow[4] = "Not found";
                        resultRow["Result"] = "Fail";
                        ret = false;
                    }

                    continue;
                }

                List<string> manifestDestPaths = new List<string>();
                manifestDestPaths.Add(_destManifiest2Actual[row["DestPath"].ToString()].ToLower());

                // collect linked destination path too
                List<string> links = row["Links"] as List<string>;
                if (links != null)
                {
                    foreach (var link in links)
                        manifestDestPaths.Add(_destManifiest2Actual[link].ToLower());
                }

                List<string> actualDestPathInDB = QueryActualBinaryPath(row["sku"].ToString(), row["DestinationName"].ToString(), arch);
                if (actualDestPathInDB == null || actualDestPathInDB.Count == 0)
                {
                    resultRow[4] = "Not found";
                    resultRow["Result"] = "Fail";
                    ret = false;
                }
                else //Compare with DB path
                {
                    for (int i = 0; i < actualDestPathInDB.Count; ++i)
                    {
                        if (!actualDestPathInDB[i].EndsWith("\\"))
                        {
                            actualDestPathInDB[i] = actualDestPathInDB[i] + "\\";
                        }
                    }

                    int failCount = 0;
                    foreach (var manifestDestPath in manifestDestPaths)
                    {
                        if (!actualDestPathInDB.Contains(manifestDestPath))
                            ++failCount;
                    }

                    if (failCount > 0)
                    {
                        resultRow[4] = String.Format("{0} paths were not found in DB", failCount);
                        resultRow["Result"] = "Fail";
                        ret = false;
                    }
                    else
                    {
                        resultRow["Result"] = "Pass";
                        resultRow[4] = manifestDestPaths.First();
                    }
                }
            }

            return ret;
        }

        private List<string> QueryActualBinaryPath(string sku, string fileName, Architecture arch)
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                int cpuId = (int)arch;

                var results = (from r in dataContext.SANFileLocations
                              where String.Compare(r.FileName,fileName, StringComparison.InvariantCultureIgnoreCase) == 0 && 
                              r.ProductID == Utility.GetDBProductIDFromSKU(sku) &&
                              r.ProductSPLevel == Utility.GetDBProductSPLevel(sku) &&
                              r.CPUID == cpuId
                              select r.FileLocation.ToLowerInvariant()).ToList();

                return results;
            }
        }
    }
}
