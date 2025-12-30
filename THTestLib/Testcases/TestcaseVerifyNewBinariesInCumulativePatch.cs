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
    class TestcaseVerifyNewBinariesInCumulativePatch : TestCaseBase
    {
        public TestcaseVerifyNewBinariesInCumulativePatch(THTestObject testobj)
           : base(testobj)
        {
        }

        // This case verifies file version and component version of new binaries in cumulative patch
        // are greater than its LCU
        public override bool RunTestCase()
        {
            // not a cumulative patch, skip
            if (TestObject.SimplePatch)
                return true;
            if (!TestObject.SimplePatch && TestObject.TFSItem.LCUKBArticle == "TBD")
            {

                DataTable warningFakeTable = new DataTable("WARNING: LCU KB is set to TBD on TFS, please double check if this is expected");

                TestObject.TestResults.ResultDetails.Add(warningFakeTable);
                TestObject.TestResults.ResultDetailSummaries.Add(false);

                return true;

            }

            bool overallResult = true;
            bool testResult = true;

            if (!TestObject.IsTraditionalWin10LCU) // for RS5
            {
                foreach (var patch in TestObject.Patches)
                {
                    if (patch.Value.ActualBinaries != null && !String.IsNullOrEmpty(patch.Value.LCUPatchPath))
                    {
                        if (TestObject.TFSItem.OSInstalled == "Windows 8.1" && patch.Key.ToString() == "ARM")
                            continue;
                        DataTable Results;
                        testResult = RunTestForRS5Above(patch.Key, out Results);

                        TestObject.TestResults.ResultDetails.Add(Results);
                        TestObject.TestResults.ResultDetailSummaries.Add(testResult);
                        overallResult &= testResult;
                    }
                }
            }
            else
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
                    }
                }
            }

            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }

        #region Traditional Win10 patch - Compare PackageMetadata.xml
        private bool RunTest(Architecture arch, out DataTable resultTable)
        {
            bool result = true;
            resultTable = null;

            string archString = arch == Architecture.AMD64 ? "x64" : arch.ToString();
            string lcuPath = Path.GetDirectoryName(TestObject.Patches[arch].LCUPatchPath);
            string testPath = TestObject.Patches[arch].PatchLocation;

            string fileName = String.Format("PackageMetadata-{0}.xml", archString);
            resultTable = HelperMethods.CreateDataTable("Verify NEW .NET payload has higher component version and file version in " + arch + " package (Compared to LCU " + lcuPath + ")",
                                            new string[] { "Type", "Assembly Name", "File Name", "ComponentVersion", "File Version", "LCU ComponentVersion", "LCU File Version", "ProcessorArchitecture", "ImportPath" },
                                            new string[] { "style=width:11.11%;text-align:center#ResultCol=1", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center" });

            //Get file names from target patch
            testPath = Path.Combine(Path.GetDirectoryName(testPath), fileName);
            if (!File.Exists(testPath))
            {
                resultTable.TableName = "Cannot find " + fileName + ", skip case VerifyNewBinariesInCumulativePatch for " + arch;
                return true;
            }

            List<MetadataFileNode> testFiles = PackageMetadataReader.ReadPackageMetadata(testPath, TestObject.TFSItem.SKU, TestObject.Patches[arch].ExtractLocation);

            //Get file names from LCU KB
            string baseline = Path.Combine(Path.Combine(lcuPath, fileName));
            List<MetadataFileNode> baselineFiles = PackageMetadataReader.ReadPackageMetadata(baseline, TestObject.TFSItem.SKU, TestObject.Patches[arch].LCUExtractLocation);

            //Compare each file with LCU
            foreach (MetadataFileNode t in testFiles)
            {
                if (!IsInExpectedFileList(t.FileName.ToLowerInvariant()))
                    continue;

                var searchResult = baselineFiles.Where(p => p.AssemblyName == t.AssemblyName &&
                    p.ProcessorArchitecture == t.ProcessorArchitecture &&
                    p.FileName == t.FileName &&
                    p.ImportPath == t.ImportPath);

                if (searchResult.Count() > 0) // file is in baseline, compare component version
                {
                    MetadataFileNode baseFn = searchResult.First();

                    int fileVerCmpResult = String.IsNullOrEmpty(t.FileVersion) ? 1 : HelperMethods.VersionCompare(t.FileVersion, baseFn.FileVersion);
                    int componentVerCmpResult = HelperMethods.VersionCompare(t.ComponentVersion, baseFn.ComponentVersion);

                    if ((fileVerCmpResult <= 0 || componentVerCmpResult <= 0) &&
                        !FailureExpected(fileVerCmpResult, componentVerCmpResult, t))
                    {
                        result = false;

                        DataRow row = resultTable.NewRow();
                        row["Assembly Name"] = t.AssemblyName;
                        row["ComponentVersion"] = t.ComponentVersion;
                        row["LCU ComponentVersion"] = baseFn.ComponentVersion;
                        row["File Name"] = t.FileName;
                        row["ProcessorArchitecture"] = t.ProcessorArchitecture;
                        row["ImportPath"] = t.ImportPath;
                        row["File Version"] = t.FileVersion;
                        row["LCU File Version"] = baseFn.FileVersion;

                        if (fileVerCmpResult <= 0 && componentVerCmpResult <= 0)
                        {
                            row["Type"] = "Component and File Version are not higher than LCU";
                        }
                        else if (fileVerCmpResult <= 0)
                        {
                            row["Type"] = "File Version is not higher than LCU";
                        }
                        else if (componentVerCmpResult <= 0)
                        {
                            row["Type"] = "Component Version is not higher than LCU";
                        }

                        resultTable.Rows.Add(row);
                    }
                }
            }

            return result;
        }

        private bool IsInExpectedFileList(string fileName)
        {
            foreach (string sku in TestObject.ExpectedBinariesVersions.Keys)
            {
                if (TestObject.ExpectedBinariesVersions[sku].ContainsKey(fileName))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region New type of Win10 patch - Compare actual payload

        private bool RunTestForRS5Above(Architecture arch, out DataTable resultTable)
        {
            string lcuPath = TestObject.Patches[arch].LCUPatchPath;
            int len = lcuPath.LastIndexOf("\\") + 1;
            string patchName = lcuPath.Substring(len, lcuPath.Length - 4 - len);
            string patchLoc = lcuPath.Substring(0, lcuPath.LastIndexOf("\\"));
            DataTable newPatchPayload = TestObject.Patches[arch].ActualBinaries, lcuPayload = null;
            string patchExpandLocation = TestObject.Patches[arch].ExtractLocation;
            string extractLocation = string.Empty;
            //1. Extract LCU
            //Modify by JC
            if (!patchExpandLocation.Contains("EXPANDED_PACKAGE"))
            {
                extractLocation = Extraction.ExtractPatchToPath(lcuPath);
            }
            else
            {
                extractLocation = Path.Combine(patchLoc, "EXPANDED_PACKAGE", patchName);
            }
            //string extractLocation = Extraction.ExtractPatchToPath(lcuPath);
            lcuPayload = CBSPayloadAnalyzer.GetPatchDotNetBinaries(TestObject, extractLocation, arch);
            if (lcuPayload == null || lcuPayload.Rows.Count == 0)
            {
                throw new Exception("Bad LCU patch, no payload found after extraction");
            }

            resultTable = HelperMethods.CreateDataTable("Verify NEW .NET payload has higher component version and file version in " + arch + " package (Compared to LCU " + lcuPath + ")",
                                            new string[] { "Type", "File Name", "ComponentVersion", "File Version", "LCU ComponentVersion", "LCU File Version", "ProcessorArchitecture" },
                                            new string[] { "style=width:14.28%;text-align:center#ResultCol=1", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center" });
            bool result = true;
            string compVerPrefix = TestObject.TFSItem.SKU[0] < '4' ? "10.0." : "4.0.";
            if (compVerPrefix == "10.0." && !TestObject.IsWindows10Patch)
            {
                compVerPrefix = "6.";
            }

            //2. Verify all new files in new patch has higher file version and component version than LCU
            foreach (DataRow f in newPatchPayload.Rows)
            {
                if (f["InExpectFileList"].ToString() == "1" && f["ComponentVersion"].ToString().StartsWith(compVerPrefix))
                {
                    DataRow[] rows = lcuPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and ComponentVersion LIKE '{3}*'", f["FileName"], f["SKU"], f["ProcessorArchitecture"], compVerPrefix));

                    if (rows != null && rows.Length > 0)
                    {
                        foreach (DataRow r in rows)
                        {
                            int fileVerCmpResult = String.IsNullOrEmpty(f["Version"].ToString()) ? 1 : HelperMethods.VersionCompare(f["Version"].ToString(), r["Version"].ToString());
                            int componentVerCmpResult = HelperMethods.VersionCompare(f["ComponentVersion"].ToString(), r["ComponentVersion"].ToString());

                            if ((fileVerCmpResult <= 0 || componentVerCmpResult <= 0) &&
                                !FailureExpected(fileVerCmpResult, componentVerCmpResult, f["FileName"].ToString()))
                            {
                                DataRow newRow = resultTable.NewRow();
                                //newRow["Type"] = "Different Version";
                                newRow["ComponentVersion"] = f["ComponentVersion"];
                                newRow["File Name"] = f["FileName"];
                                newRow["ProcessorArchitecture"] = f["ProcessorArchitecture"];
                                newRow["File Version"] = f["Version"];
                                newRow["LCU ComponentVersion"] = r["ComponentVersion"];
                                newRow["LCU File Version"] = r["Version"];

                                if (fileVerCmpResult <= 0 && componentVerCmpResult <= 0)
                                {
                                    newRow["Type"] = "Component and File Version are not higher than LCU";
                                }
                                else if (fileVerCmpResult <= 0)
                                {
                                    newRow["Type"] = "File version is not higher than LCU";
                                }
                                else if (componentVerCmpResult <= 0)
                                {
                                    newRow["Type"] = "Component version is not higher than LCU";
                                }

                                resultTable.Rows.Add(newRow);

                                result = false;
                            }
                        }
                    }

                    // else means LCU does not have the same new payload
                }
            }

            return result;
        }

        private bool FailureExpected(int fileVersionResult, int comVersionResult, MetadataFileNode fileInfo)
        {
            // 1. CRT file version usually does not change
            if (fileVersionResult == 0 && comVersionResult > 0 &&
                (TestObject.TFSItem.SKU.StartsWith("4.6") || TestObject.TFSItem.SKU.StartsWith("4.7")) &&
                (fileInfo.FileName.Equals("msvcr120_clr0400.dll") || fileInfo.FileName.Equals("msvcp120_clr0400.dll")))
                return true;

            // 2. aspnet_state_perf.h and aspnet_state_perf.ini in Assembly NetFx-AspNet-NonWow64-Shared
            else if (fileVersionResult == 1 && comVersionResult == 0 &&
                fileInfo.ComponentVersion == "4.0.14393.10001" &&
                fileInfo.AssemblyName.ToLower() == "netfx-aspnet-nonwow64-shared" &&
                (fileInfo.FileName.Equals("aspnet_state_perf.h") || fileInfo.FileName.Equals("aspnet_state_perf.ini")))
                return true;

            else
                return false;
        }

        private bool FailureExpected(int fileVersionResult, int comVersionResult, string fileName)
        {
            // 1. CRT file version usually does not change
            if (fileVersionResult == 0 && comVersionResult > 0 &&
                (TestObject.TFSItem.SKU.StartsWith("4.6") || TestObject.TFSItem.SKU.StartsWith("4.7")) &&
                (fileName.Equals("msvcr120_clr0400.dll") || fileName.Equals("msvcp120_clr0400.dll")))
                return true;

            else
                return false;
        }

        #endregion
    }
}
