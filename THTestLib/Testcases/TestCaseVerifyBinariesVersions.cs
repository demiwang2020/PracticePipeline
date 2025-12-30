using ScorpionDAL;
using System;
using Helper;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace THTestLib.Testcases
{
    class TestCaseVerifyBinariesVersions : TestCaseBase
    {
        Dictionary<string, string> _dictPackageDropPath;

        public TestCaseVerifyBinariesVersions(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            // When there is no expected binaries set for testing, just skip this case
            if (TestObject.ExpectedBinariesVersions.Count == 0)
                return true;

            bool overallResult = true;

            DataTable resultTable;
            bool result = false;

            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    if (TestObject.TFSItem.OSInstalled == "Windows 8.1" && patch.Key.ToString() == "ARM")
                        continue;
                    result = ExecuteVerifyBinaryVersions(patch.Value.ActualBinaries, patch.Key, out resultTable);
                    resultTable.TableName = string.Format("Version Verification for {0} package", patch.Key.ToString());
                    TestObject.TestResults.ResultDetails.Add(resultTable);
                    TestObject.TestResults.ResultDetailSummaries.Add(result);
                    overallResult &= result;
                }
            }

            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }

        private bool ExecuteVerifyBinaryVersions(DataTable fileTable, Architecture arch, out DataTable result)
        {
            bool ret = true;

            result = HelperMethods.CreateDataTable(String.Empty,
                new string[] { "FileName", "ProcessorArchitecture", "Expected Version", "Actual Version", "Expected ComponentVersion", "Actual ComponentVersion", "PE Compare", "Result" });

            bool bPECompareExecuted = false;

            foreach (KeyValuePair<string, Dictionary<string, string>> sku_dict in TestObject.ExpectedBinariesVersions)
            {
                foreach (KeyValuePair<string, string> kv in sku_dict.Value)
                {
                    if (kv.Key == "compatjit.dll" && TestObject.TFSItem.SKU.ToString() == "4.8"
    && TestObject.TFSItem.OSInstalled.ToString() == "Windows 10" && (TestObject.TFSItem.OSSPLevel.ToString() == "21H2" || TestObject.TFSItem.OSSPLevel.ToString() == "20H2"))
                        continue;
                    DataRow[] rows = fileTable.Select(String.Format("DestinationName = '{0}' and SKU = '{1}'", kv.Key, sku_dict.Key));

                    //Expected file is not in patch
                    if (rows.Length == 0)
                    {
                        DataRow row = result.NewRow();
                        row["FileName"] = kv.Key;
                        row["Expected Version"] = kv.Value;
                        row["Expected ComponentVersion"] = TestObject.TFSItem.ComponentVersion;

                        if (!IsFileInArch(kv.Key, arch))
                        {
                            row["Actual Version"] = string.Format("There is no {0} file", arch.ToString());
                            row["Result"] = "Pass";
                        }
                        else
                        {
                            row["Actual Version"] = "File NOT Found";
                            row["Result"] = "Fail";
                            ret = false;
                        }
                        result.Rows.Add(row);
                    }
                    else
                    {
                        foreach (DataRow r in rows)
                        {
                            if ((sku_dict.Key == "3.0"|| sku_dict.Key == "2.0" || sku_dict.Key == "3.5") && r["ComponentVersion"].ToString().StartsWith("4."))
                                continue;
                            DataRow row = result.NewRow();
                            row["FileName"] = r["FileName"];
                            row["Expected Version"] = kv.Value.Replace("\\d+", "*"); //Using '*' for readability, it is easier to understand '3.0.*.8826' than '3.0.\d+.8826'
                            row["Actual Version"] = r["Version"];
                            row["ProcessorArchitecture"] = r["ProcessorArchitecture"];
                            row["Expected ComponentVersion"] = TestObject.TFSItem.ComponentVersion;
                            row["Actual ComponentVersion"] = r["ComponentVersion"];

                            //Using RegEx to cover 3.0 different file versions
                            System.Text.RegularExpressions.Regex fvRegex = new System.Text.RegularExpressions.Regex(kv.Value);
                            bool b = fvRegex.IsMatch(r["Version"].ToString());

                            // File version mismatch found
                            if (!b) //Only version mismatch should call PE compare
                            {
                                if (FailureIgnorable(arch, kv.Key, r["Version"].ToString(), r["ComponentVersion"].ToString()))
                                {
                                    b = true;
                                }
                                else
                                {
                                    row["PE Compare"] = PECompare(r);
                                    bPECompareExecuted = true;
                                }
                            }

                            //Check component version here only for NDP4.X
                            //if (sku_dict.Key[0] >= '4' && sku_dict.Key[2] > '5' || //4.6 +
                            //    !TestObject.IsTraditionalWin10LCU && TestObject.IsWindows10Patch //RS5, RS6...
                            //    )
                            //    b &= TestObject.TFSItem.ComponentVersion == r["ComponentVersion"].ToString();
                            if (sku_dict.Key[0] >= '4' && sku_dict.Key[2] > '5' || //4.6 +
                                TestObject.IsWindows10Patch && sku_dict.Key[0] < '4'//3.5 on Win10
                                )
                                b &= TestObject.TFSItem.ComponentVersion == r["ComponentVersion"].ToString();

                            row["Result"] = b ? "Pass" : "Fail";

                            result.Rows.Add(row);

                            ret &= b;
                        }
                    }
                }
            }

            // Remove column of PE compare if it is not executed
            if (!bPECompareExecuted)
            {
                result.Columns.Remove("PE Compare");
            }

            //adjust styles
            if (result.Columns.Count == 7)
            {
                HelperMethods.SetTableColExtendedProperties(result, new string[] { "style=width:16%;text-align:center", "style=width:10%;text-align:center", "style=width:16%;text-align:center", "style=width:16%;text-align:center", "style=width:16%;text-align:center", "style=width:16%;text-align:center", "style=width:10%;text-align:center#ResultCol=1" });
            }
            else if (result.Columns.Count == 8)
            {
                HelperMethods.SetTableColExtendedProperties(result, new string[] { "style=width:14%;text-align:center", "style=width:8%;text-align:center", "style=width:14%;text-align:center", "style=width:14%;text-align:center", "style=width:14%;text-align:center", "style=width:14%;text-align:center", "width=14%", "style=width:8%;text-align:center#ResultCol=1" });
            }

            return ret;
        }

        private bool IsFileInArch(string fileName, Architecture arch)
        {
            // hardcode a known scenario of compatjit on ARM64
            // [gsoria] 20H1 and below ARM64 packages do not have 64bit binaries, 21H2 and newer ARM64 packages have 64bit binaries
            if (arch == Architecture.ARM64 &&
                fileName == "compatjit.dll" &&
                TestObject.TFSItem.OSInstalled == "Windows 10" &&
                TestObject.TFSItem.OSSPLevel.CompareTo("21H2") < 0)
                return false;

            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var searchResult = dataContext.SANFileLocations.Where(p => String.Compare(p.FileName, fileName, true) == 0 && p.CPUID == (int)arch);

                return searchResult.Count() > 0;
            }
        }

        /// <summary>
        /// Hardcode some expect failures
        /// </summary>
        private bool FailureIgnorable(Architecture arch, string fileName, string fileVersion, string componentVersion)
        {
            //1. 8.1 + 4.5.2 + ARM, CRT file version = 12.0.52242.36242
            if (TestObject.TFSItem.OSInstalled == "Windows 8.1" &&
                arch == Architecture.ARM &&
                (fileName.Equals("msvcp120_clr0400.dll", StringComparison.InvariantCultureIgnoreCase) || fileName.Equals("msvcr120_clr0400.dll", StringComparison.InvariantCultureIgnoreCase)) &&
                fileVersion == "12.0.52242.36242")
                return true;

            return false;
        }

        #region Code to call PECompare
        private string PECompare(DataRow row)
        {
            string sku = row["SKU"] as string;
            string configedDropPath = GetDropPath(sku);

            // multiple drop paths is supported, separated with ','
            string[] dropPaths = configedDropPath.Split(new char[] { ',' });
            string destFolder = String.Empty;

            //search for the drop path that contains the build
            foreach (string p in dropPaths)
            {
                string dropPath = p.Trim();
                if (String.IsNullOrEmpty(dropPath))
                    continue;

                string arch = row["ProcessorArchitecture"] as string;
                if (arch == "msil")
                    arch = "x86";
                dropPath = p.Replace("[Arch]", arch);
                if (Directory.Exists(dropPath))
                {
                    destFolder = dropPath;
                    break;
                }
            }

            if (String.IsNullOrEmpty(destFolder))
            {
                return "Failed to find drop path from " + configedDropPath;
            }
            else
            {
                string fileName = row["FileName"] as string;
                string filePath = row["ExtractPath"] as string;

                string fileInDropPath = SearchFileInDropPath(fileName, destFolder);
                if (String.IsNullOrEmpty(fileInDropPath))
                    return String.Format("File not found in {0}", destFolder);

                int result = RunPECompCommand(filePath, fileInDropPath);
                return result == 0 ? "Equivalent" : String.Format("NOT Equivalent, PEComp result is {0}, compared with {1}", result, fileInDropPath);
            }
        }

        private string GetDropPath(string sku)
        {
            if (_dictPackageDropPath == null)
                _dictPackageDropPath = new Dictionary<string, string>();

            if (_dictPackageDropPath.ContainsKey(sku))
                return _dictPackageDropPath[sku];

            string dropPath = String.Empty;

            //down level sku
            if (Utility.IsDownlevelSKU(sku))
            {
                switch (sku)
                {
                    case "2.0":
                        dropPath = System.Configuration.ConfigurationManager.AppSettings["DropPath_20"];
                        break;

                    case "3.0":
                        dropPath = System.Configuration.ConfigurationManager.AppSettings["DropPath_30"];
                        break;

                    case "3.5":
                        dropPath = System.Configuration.ConfigurationManager.AppSettings["DropPath_35"];
                        break;
                }
            } //high level SKU
            else
            {
                using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                {
                    string tfsSKU = TestObject.TFSItem.SKU.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    var testConfig = dataContext.TTHTestConfigs.Where(p => p.SKUName == tfsSKU).FirstOrDefault();
                    if (testConfig != null)
                    {
                        dropPath = testConfig.PECompareSource;
                    }
                }
            }

            if (!String.IsNullOrEmpty(dropPath))
            {
                dropPath = dropPath.Replace("[BuildNumber]", TestObject.TFSItem.BaseBuildNumber);
                _dictPackageDropPath.Add(sku, dropPath);
            }

            return dropPath;
        }

        private string SearchFileInDropPath(string fileName, string dropPath)
        {
            string filePath = Path.Combine(dropPath, fileName);
            if (File.Exists(filePath))
                return filePath;

            filePath = Path.Combine(dropPath, "WPF", fileName);
            if (File.Exists(filePath))
                return filePath;

            //search nativeimages folder, which only exists in binaries.x86ret folder
            if (dropPath.Contains("amd64"))
            {
                filePath = Path.Combine(dropPath.Replace("amd64", "x86"), "NativeImages", "amd64", fileName);
            }
            else
            {
                filePath = Path.Combine(dropPath, "NativeImages", "x86", fileName);
            }
            if (File.Exists(filePath))
                return filePath;

            return null;
        }

        private int RunPECompCommand(string filePath, string dropFilePath)
        {
            string toolPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), @"External\pecomp.exe");
            return Helper.Utility.ExecuteCommandSync(toolPath, String.Format("{0} {1}", filePath, dropFilePath), 3600000);
        }
        #endregion
    }
}
