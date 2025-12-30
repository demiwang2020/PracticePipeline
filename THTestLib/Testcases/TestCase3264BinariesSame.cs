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
    class TestCase3264BinariesSame : TestCaseBase
    {
        public TestCase3264BinariesSame(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            DataTable resultTable;
            bool result;
            if (TestObject.TFSItem.KBNumber == "5018210")
            {
                //If x86 only, no need to test this case, kb is 5018210
                if (TestObject.Patches[Architecture.ARM64].ActualBinaries == null)
                    return true;
                result = ExecuteVerifyX86FilesSameAsARM64(out resultTable);
            }
            else
            {
                //If x86 only, no need to test this case
                if (TestObject.Patches[Architecture.AMD64].ActualBinaries == null)
                    return true;
                result = ExecuteVerifyX86FilesSameAsX64(out resultTable);
            }
            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(result);
            TestObject.TestResults.Result &= result;
            return result;
        }

        private bool ExecuteVerifyX86FilesSameAsX64(out DataTable result)
        {
            bool ret = true;

            result = HelperMethods.CreateDataTable("Checking amd64 pacth: x86 binaries are same as x64 binaries",
                new string[] { "FileName", "SKU", "Result" },
                new string[] { "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center#ResultCol=1" });

            foreach (KeyValuePair<string, Dictionary<string, string>> sku_dict in TestObject.ExpectedBinariesVersions)
            {
                DataRow[] x86Rows = TestObject.Patches[Architecture.AMD64].ActualBinaries.Select(String.Format("SKU = '{0}' and InExpectFileList = '1' and (ProcessorArchitecture = 'x86' or ProcessorArchitecture = 'msil' or ProcessorArchitecture = 'wow64')", sku_dict.Key));
                DataRow[] amd64Rows = TestObject.Patches[Architecture.AMD64].ActualBinaries.Select(String.Format("SKU = '{0}' and InExpectFileList = '1' and ProcessorArchitecture = 'amd64'", sku_dict.Key));

                List<string> x86List = (from r in x86Rows
                                            //where sku_dict.Value.ContainsKey(r["FileName"] as string)
                                        select r["FileName"].ToString()).Distinct().ToList();

                List<string> amd64List = (from r in amd64Rows
                                              //where sku_dict.Value.ContainsKey(r["FileName"] as string)
                                          select r["FileName"].ToString()).Distinct().ToList();

                for (int i = x86List.Count - 1; i >= 0; --i)
                {
                    if (Utility.x86OnlyBinaries.Contains(x86List[i]))
                    {
                        x86List.RemoveAt(i);
                    }
                }
                for (int i = amd64List.Count - 1; i >= 0; --i)
                {
                    if (Utility.x64OnlyBinaries.Contains(amd64List[i]))
                    {
                        amd64List.RemoveAt(i);
                    }
                }

                //hard code for some exceptions
                if (sku_dict.Key[0] >= '4') //servicemodel.mof.uninstall, servicemodel.mof is amd64 only for 4.x
                {
                    if (amd64List.Contains("servicemodel.mof"))
                        amd64List.Remove("servicemodel.mof");
                    if (amd64List.Contains("servicemodel.mof.uninstall"))
                        amd64List.Remove("servicemodel.mof.uninstall");
                }

                List<string> missingx64Files = x86List.Except(amd64List).ToList();
                List<string> missingx86Files = amd64List.Except(x86List).ToList();

                // for 20H1 and above OS, files that are not Architecture specific only have amd64 files in package
                // so remove them from amd64 file list before comparing
                if (missingx86Files.Count > 0 &&
                    sku_dict.Key[0] >= '4' &&
                    Utility.CompareOS(TestObject.TFSItem.OSInstalled, TestObject.TFSItem.OSSPLevel, "Windows 10", "20H1") >= 0)
                {
                    for (int i = missingx86Files.Count - 1; i >= 0; --i)
                    {
                        if (PEArchDetector.IsPEIndependentFromArch(missingx86Files[i]))
                        {
                            missingx86Files.RemoveAt(i);
                        }
                    }
                }

                if (missingx64Files.Count > 0)
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = GetStringFromFileList(missingx64Files);
                    row["SKU"] = sku_dict.Key;
                    row["Result"] = "Missing amd64 files";
                    result.Rows.Add(row);

                    ret = false;
                }

                if (missingx86Files.Count > 0)
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = GetStringFromFileList(missingx86Files);
                    row["SKU"] = sku_dict.Key;
                    row["Result"] = "Missing x86 files";
                    result.Rows.Add(row);

                    ret = false;
                }

                if (missingx64Files.Count == 0 && missingx86Files.Count == 0)
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = "All files in expected file list";
                    row["SKU"] = sku_dict.Key;
                    row["Result"] = "Pass";
                    result.Rows.Add(row);
                }
            }

            return ret;
        }


        private bool ExecuteVerifyX86FilesSameAsARM64(out DataTable result)
        {
            bool ret = true;

            result = HelperMethods.CreateDataTable("Checking arm64 pacth: x86 binaries are same as arm64 binaries",
                new string[] { "FileName", "SKU", "Result" },
                new string[] { "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center", "style=width:33.33%;text-align:center#ResultCol=1" });

            foreach (KeyValuePair<string, Dictionary<string, string>> sku_dict in TestObject.ExpectedBinariesVersions)
            {
                DataRow[] x86Rows = TestObject.Patches[Architecture.ARM64].ActualBinaries.Select(String.Format("SKU = '{0}' and InExpectFileList = '1' and (ProcessorArchitecture = 'x86' or ProcessorArchitecture = 'msil' or ProcessorArchitecture = 'wow64')", sku_dict.Key));
                DataRow[] arm64Rows = TestObject.Patches[Architecture.ARM64].ActualBinaries.Select(String.Format("SKU = '{0}' and InExpectFileList = '1' and ProcessorArchitecture = 'arm64'", sku_dict.Key));

                List<string> x86List = (from r in x86Rows
                                            //where sku_dict.Value.ContainsKey(r["FileName"] as string)
                                        select r["FileName"].ToString()).Distinct().ToList();

                List<string> arm64List = (from r in arm64Rows
                                              //where sku_dict.Value.ContainsKey(r["FileName"] as string)
                                          select r["FileName"].ToString()).Distinct().ToList();

                for (int i = x86List.Count - 1; i >= 0; --i)
                {
                    if (Utility.x86OnlyBinaries.Contains(x86List[i]))
                    {
                        x86List.RemoveAt(i);
                    }
                }
                for (int i = arm64List.Count - 1; i >= 0; --i)
                {
                    if (Utility.arm64OnlyBinaries.Contains(arm64List[i]))
                    {
                        arm64List.RemoveAt(i);
                    }
                }

                //hard code for some exceptions
                //if (sku_dict.Key[0] >= '4') //servicemodel.mof.uninstall, servicemodel.mof is amd64 only for 4.x
                //{
                //    if (arm64List.Contains("servicemodel.mof"))
                //        arm64List.Remove("servicemodel.mof");
                //    if (arm64List.Contains("servicemodel.mof.uninstall"))
                //        arm64List.Remove("servicemodel.mof.uninstall");
                //}

                List<string> missingarm64Files = x86List.Except(arm64List).ToList();
                List<string> missingx86Files = arm64List.Except(x86List).ToList();

                // for 20H1 and above OS, files that are not Architecture specific only have amd64 files in package
                // so remove them from amd64 file list before comparing
                if (missingx86Files.Count > 0 &&
                    sku_dict.Key[0] >= '4' &&
                    Utility.CompareOS(TestObject.TFSItem.OSInstalled, TestObject.TFSItem.OSSPLevel, "Windows 10", "20H1") >= 0)
                {
                    for (int i = missingx86Files.Count - 1; i >= 0; --i)
                    {
                        if (PEArchDetector.IsPEIndependentFromArch(missingx86Files[i]))
                        {
                            missingx86Files.RemoveAt(i);
                        }
                    }
                }

                if (missingarm64Files.Count > 0)
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = GetStringFromFileList(missingarm64Files);
                    row["SKU"] = sku_dict.Key;
                    row["Result"] = "Missing arm64 files";
                    result.Rows.Add(row);

                    ret = false;
                }

                if (missingx86Files.Count > 0)
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = GetStringFromFileList(missingx86Files);
                    row["SKU"] = sku_dict.Key;
                    row["Result"] = "Missing x86 files";
                    result.Rows.Add(row);

                    ret = false;
                }

                if (missingarm64Files.Count == 0 && missingx86Files.Count == 0)
                {
                    DataRow row = result.NewRow();
                    row["FileName"] = "All files in expected file list";
                    row["SKU"] = sku_dict.Key;
                    row["Result"] = "Pass";
                    result.Rows.Add(row);
                }
            }

            return ret;
        }



        private string GetStringFromFileList(List<string> fileList)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in fileList)
            {
                sb.Append(s);
                sb.Append(", ");
            }

            return sb.ToString(0, sb.Length - 2);
        }
    }
}
