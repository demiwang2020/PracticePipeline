using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper;

namespace THTestLib.Testcases
{
    class TestCaseVerifyContentArch : TestCaseBase
    {
        public TestCaseVerifyContentArch(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            bool overallResult = true;

            DataTable resultTable;
            bool result = false;

            //Verify binaries in all packages
            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    result = ExecutePEArchVerification(patch.Value, out resultTable);
                    resultTable.TableName = string.Format("PE file CPU type verification for {0} package", patch.Key.ToString());
                    TestObject.TestResults.ResultDetails.Add(resultTable);
                    TestObject.TestResults.ResultDetailSummaries.Add(result);
                    overallResult &= result;
                }
            }

            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }

        private bool ExecutePEArchVerification(PatchInformation patch, out DataTable result)
        {
            bool ret = true;

            result = HelperMethods.CreateDataTable(String.Empty,
                new string[] { "FileName", "Version", "ProcessorArchitecture", "PE Architecture", "CPU Neutral", "Result" },
                new string[] { "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center", "style=width:16.66%;text-align:center#ResultCol=1" });

            foreach (DataRow row in patch.ActualBinaries.Rows)
            {
                // Only do this for new files
                //if (row["InExpectFileList"].ToString().Equals("0"))
                //    continue;

                //skip non-versioned files
                if (String.IsNullOrEmpty(row["Version"].ToString()))
                    continue;

                DataRow resultRow = result.NewRow();
                resultRow["FileName"] = row["FileName"];
                resultRow["Version"] = row["Version"];
                resultRow["ProcessorArchitecture"] = row["ProcessorArchitecture"];
                result.Rows.Add(resultRow);

                Architecture actualArch = PEArchDetector.GetPEArch(row["ExtractPath"].ToString());
                resultRow["PE Architecture"] = actualArch.ToString().ToLower();

                bool cpuIndependent = PEArchDetector.IsPEIndependentFromArch(row["FileName"].ToString());
                resultRow["CPU Neutral"] = cpuIndependent.ToString();

                bool thisResult = true;

                if (cpuIndependent)
                {
                    thisResult = actualArch == Architecture.X86;
                }
                else
                {
                    string processor = row["ProcessorArchitecture"].ToString();
                    if (processor.Equals("msil", StringComparison.InvariantCultureIgnoreCase) ||
                        processor.Equals("wow64", StringComparison.InvariantCultureIgnoreCase))
                    {
                        processor = "x86";
                    }
                    else if (processor.Equals("arm64", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //2.0、3.0、3.5 files
                        if (row["ComponentVersion"].ToString().StartsWith("10.0."))
                            processor = "amd64";

                        // This means the file is amd64 one
                        if (row["DestPath"].ToString().Contains("Framework64"))
                            processor = "amd64";

                        //msvcp120_clr0400.dll, msvcr100_clr0400.dll and msvcr120_clr0400.dll should have x86 and x64 version in ARM64 package
                        if (row["FileName"].ToString() == "msvcp120_clr0400.dll" ||
                            row["FileName"].ToString() == "msvcr100_clr0400.dll" ||
                            row["FileName"].ToString() == "msvcr120_clr0400.dll")
                            processor = "amd64";
                        //arm64_vcruntime140_1_clr0400.dll have x64 version in ARM64 package
                        if (row["FileName"].ToString() == "arm64_vcruntime140_1_clr0400.dll")
                            processor = "amd64";
                    }

                    thisResult = actualArch.ToString().Equals(processor, StringComparison.InvariantCultureIgnoreCase);
                }

                resultRow["Result"] = thisResult ? "Pass" : "Fail";
                ret &= thisResult;
            }

            return ret;
        }
    }
}
