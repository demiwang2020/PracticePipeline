using Connect2TFS;
using Helper;
using HotFixLibrary;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using RMIntegration;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace THTestLib.Testcases
{
    class TestCaseVerifyCumulativePayloads : TestCaseBase
    {
        public TestCaseVerifyCumulativePayloads(THTestObject testobj)
            : base(testobj)
        {

        }

        public override bool RunTestCase()
        {
            // if LCU KB is not set, do not run this case
            // Only print warning message
            if (TestObject.SimplePatch)
            {
                DataTable warningFakeTable = new DataTable("WARNING: LCU KB is not set on TFS, please double check if this is expected");

                TestObject.TestResults.ResultDetails.Add(warningFakeTable);
                TestObject.TestResults.ResultDetailSummaries.Add(false);

                return true;
            }
            else if (!TestObject.SimplePatch && TestObject.TFSItem.LCUKBArticle == "TBD") {

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
            resultTable = HelperMethods.CreateDataTable("Verify cumulative .NET payloads in " + arch + " package (Compared to LCU " + lcuPath + ")",
                                            new string[] { "Type", "Assembly Name", "File Name", "ComponentVersion", "File Version", "LCU ComponentVersion", "LCU File Version", "ProcessorArchitecture", "ImportPath" },
                                            new string[] { "style=width:11.11%;text-align:center#ResultCol=1", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center" });

            //Get file names from target patch
            testPath = Path.Combine(Path.GetDirectoryName(testPath), fileName);

            if (!File.Exists(testPath))
            {
                resultTable.TableName = "Cannot find " + fileName + ", skip case VerifyCumulativePayload for " + arch;
                return true;
            }

            List<MetadataFileNode> testFiles = PackageMetadataReader.ReadPackageMetadata(testPath, TestObject.TFSItem.SKU, TestObject.Patches[arch].ExtractLocation);

            //Get file names from LCU KB
            string baseline = Path.Combine(Path.Combine(lcuPath, fileName));
            List<MetadataFileNode> baselineFiles = PackageMetadataReader.ReadPackageMetadata(baseline, TestObject.TFSItem.SKU, TestObject.Patches[arch].LCUExtractLocation);

            //Compare each file with LCU
            foreach (MetadataFileNode t in testFiles)
            {
                // Skip checking new files that expect to have new component version
                if (IsInExpectedFileList(t.FileName.ToLowerInvariant()))
                    continue;

                var searchResult = baselineFiles.Where(p => p.AssemblyName == t.AssemblyName &&
                    p.ProcessorArchitecture == t.ProcessorArchitecture &&
                    p.FileName == t.FileName &&
                    p.ImportPath == t.ImportPath);

                if (searchResult.Count() == 0)
                {
                    //The file is not in baseline
                    result = false;

                    DataRow row = resultTable.NewRow();
                    row["Type"] = "New File";
                    row["Assembly Name"] = t.AssemblyName;
                    row["ComponentVersion"] = t.ComponentVersion;
                    row["File Name"] = t.FileName;
                    row["ProcessorArchitecture"] = t.ProcessorArchitecture;
                    row["ImportPath"] = t.ImportPath;
                    row["File Version"] = t.FileVersion;

                    resultTable.Rows.Add(row);
                }
                else // file is in baseline, compare component version
                {
                    bool bFileVersionDifferent = false;
                    bool bComponentVersionDifferent = false;

                    MetadataFileNode baseFn = searchResult.First();
                    if (String.Compare(baseFn.ComponentVersion, t.ComponentVersion) != 0 &&
                        !HelperMethods.LCUTestFailureIgnorable(baseFn, t))
                    {
                        bComponentVersionDifferent = true;
                    }
                    if (String.Compare(baseFn.FileVersion, t.FileVersion) != 0 &&
                        !String.IsNullOrEmpty(t.FileVersion) &&
                        !String.IsNullOrEmpty(baseFn.FileVersion)) //file version is different
                    {
                        bFileVersionDifferent = true;
                    }
                    if (bFileVersionDifferent || bComponentVersionDifferent)
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

                        if (bFileVersionDifferent && bComponentVersionDifferent)
                        {
                            row["Type"] = "Different Component and File Version";
                        }
                        else if (bFileVersionDifferent)
                        {
                            row["Type"] = "Different File Version";
                        }
                        else if (bComponentVersionDifferent)
                        {
                            row["Type"] = "Different Component Version";
                        }

                        resultTable.Rows.Add(row);
                    }
                }
            }

            //Compare each file in LCU with current package to find out possible missing files
            bool missingFile = false;
            foreach (MetadataFileNode b in baselineFiles)
            {
                var searchResult = testFiles.Where(p => p.AssemblyName == b.AssemblyName &&
                    p.ProcessorArchitecture == b.ProcessorArchitecture &&
                    p.FileName == b.FileName &&
                    p.ImportPath == b.ImportPath);

                if (searchResult.Count() == 0)
                {
                    result = false;
                    missingFile = true;

                    DataRow row = resultTable.NewRow();
                    row["Type"] = "Missing File";
                    row["Assembly Name"] = b.AssemblyName;
                    row["ComponentVersion"] = String.Empty;
                    row["LCU ComponentVersion"] = b.ComponentVersion;
                    row["File Name"] = b.FileName;
                    row["ProcessorArchitecture"] = b.ProcessorArchitecture;
                    row["ImportPath"] = b.ImportPath;
                    row["LCU File Version"] = b.FileVersion;

                    resultTable.Rows.Add(row);
                }
            }

            if (result)
            {
                resultTable.TableName = "Test Passed: " + resultTable.TableName;
            }
            else if(!missingFile) // check if result can be switch from fail to warning (if missing file, then fail test directly)
            {
                result = HelperMethods.IsVersionResultWarning(resultTable, "File Version", TestObject.TFSItem.SKU);
                if (result)
                {
                    resultTable.TableName = "WARNING: " + resultTable.TableName;
                }
            }

            return result;
        }


        private List<MetadataFileNode> CompareFileNodes(List<MetadataFileNode> baselineFiles, List<MetadataFileNode> testFiles)
        {
            List<MetadataFileNode> result = new List<MetadataFileNode>();

            foreach (MetadataFileNode n in testFiles)
            {
                if (!baselineFiles.Contains(n))
                    result.Add(n);
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
            int len = lcuPath.LastIndexOf("\\")+1;
            string patchName = lcuPath.Substring(len , lcuPath.Length - 4 - len);
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
                extractLocation = Path.Combine(patchLoc, "EXPANDED_PACKAGE",patchName);
            }

            lcuPayload = CBSPayloadAnalyzer.GetPatchDotNetBinaries(TestObject, extractLocation, arch);
            if (lcuPayload == null || lcuPayload.Rows.Count == 0)
            {
                throw new Exception("Bad LCU patch, no payload found after extraction");
            }

            resultTable = HelperMethods.CreateDataTable("Verify cumulative .NET payloads in " + arch + " package (Compared to LCU " + lcuPath + ")",
                                            new string[] { "Type", "File Name", "ComponentVersion", "File Version", "LCU ComponentVersion", "LCU File Version", "ProcessorArchitecture" },
                                            new string[] { "style=width:14.28%;text-align:center#ResultCol=1", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center" });
            bool result = true;
            string compVerPrefix = TestObject.TFSItem.SKU[0] < '4' ? "10.0." : "4.0.";
            if (compVerPrefix == "10.0." && !TestObject.IsWindows10Patch)
            {
                compVerPrefix = "6.";
            }

            //2. Verify all old files in new patch has same file version and component version as LCU
            foreach (DataRow f in newPatchPayload.Rows)
            {
                var SkuInPatch = f["SKU"].ToString();
                var SkuInTFS = TestObject.TFSItem.SKU.ToString();

                if (f["InExpectFileList"].ToString() == "0" && f["ComponentVersion"].ToString().StartsWith(compVerPrefix) && SkuInPatch == SkuInTFS)
                {
                    DataRow[] rows = lcuPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and ComponentVersion LIKE '{3}*' ", 
                                                        f["FileName"], f["SKU"], f["ProcessorArchitecture"], compVerPrefix));
                    if (rows != null && rows.Length > 0)
                    {
                        foreach (DataRow r in rows)
                        {
                            bool bFileVersionDifferent = !r["Version"].Equals(f["Version"]);
                            bool bComponentVersionDifferent = !r["ComponentVersion"].Equals(f["ComponentVersion"]);

                            if (bFileVersionDifferent || bComponentVersionDifferent)
                            {
                                DataRow newRow = resultTable.NewRow();
                                //newRow["Type"] = "Different Version";
                                newRow["ComponentVersion"] = f["ComponentVersion"];
                                newRow["File Name"] = f["FileName"];
                                newRow["ProcessorArchitecture"] = f["ProcessorArchitecture"];
                                newRow["File Version"] = f["Version"];
                                newRow["LCU ComponentVersion"] = r["ComponentVersion"];
                                newRow["LCU File Version"] = r["Version"];

                                if (bFileVersionDifferent && bComponentVersionDifferent)
                                {
                                    newRow["Type"] = "Different Component and File Version";
                                }
                                else if (bFileVersionDifferent)
                                {
                                    newRow["Type"] = "Different File Version";
                                }
                                else if (bComponentVersionDifferent)
                                {
                                    newRow["Type"] = "Different Component Version";
                                }

                                resultTable.Rows.Add(newRow);

                                result = false;
                            }
                        }
                    }
                    else
                    {
                        DataRow newRow = resultTable.NewRow();
                        newRow["Type"] = "New File";
                        newRow["ComponentVersion"] = f["ComponentVersion"];
                        newRow["File Name"] = f["FileName"];
                        newRow["ProcessorArchitecture"] = f["ProcessorArchitecture"];
                        newRow["File Version"] = f["Version"];

                        resultTable.Rows.Add(newRow);

                        result = false;
                    }
                }
            }

            //3. Verify all files in LCU are all in new patch
            bool missingFile = false;
            foreach (DataRow r in lcuPayload.Rows)
            {
                string SkuInPatch1 = r["SKU"].ToString();
                string SkuInTFS1 = TestObject.TFSItem.SKU.ToString();
                //string SkuInTFS1 = TestObject.TFSItem.SKU[0].ToString() + ".0";

                if (!r["ComponentVersion"].ToString().StartsWith(compVerPrefix) ||
                    r["InExpectFileList"].ToString() != "0" || SkuInPatch1 != SkuInTFS1)
                    continue;

                DataRow[] rows = newPatchPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and ComponentVersion LIKE '{3}*' and Size = '{4}'", 
                                                        r["FileName"], r["SKU"], r["ProcessorArchitecture"], compVerPrefix, r["Size"]));

                if (rows == null || rows.Length == 0)
                {
                   
                    
                    DataRow newRow = resultTable.NewRow();
                    
                    newRow["File Name"] = r["FileName"];
                    newRow["ProcessorArchitecture"] = r["ProcessorArchitecture"];
                    newRow["LCU ComponentVersion"] = r["ComponentVersion"];
                    newRow["LCU File Version"] = r["Version"];
                    if (!(FindRowsDetail("Sku", r, newPatchPayload, compVerPrefix) == null || FindRowsDetail("Sku", r, newPatchPayload, compVerPrefix).Length == 0))
                        newRow["Type"] = "Different Sku";
                    else if (!(FindRowsDetail("Arch", r, newPatchPayload, compVerPrefix) == null || FindRowsDetail("Arch", r, newPatchPayload, compVerPrefix).Length == 0))
                        newRow["Type"] = "Different Arch";
                    else if (!(FindRowsDetail("Comver", r, newPatchPayload, compVerPrefix) == null || FindRowsDetail("Comver", r, newPatchPayload, compVerPrefix).Length == 0))
                        newRow["Type"] = "Different ComVerPreFix";
                    else if (!(FindRowsDetail("Size", r, newPatchPayload, compVerPrefix) == null || FindRowsDetail("Size", r, newPatchPayload, compVerPrefix).Length == 0))
                        newRow["Type"] = "Different size";
                    else
                        newRow["Type"] = "Missing file";


                        resultTable.Rows.Add(newRow);

                    result = false;
                    missingFile = true;
                }
            }

            if (result)
            {
                resultTable.TableName = "Test Passed: " + resultTable.TableName;
            }
            else if(!missingFile) // check if result can be switch from fail to warning. If missing file caught, fail test directly
            {
                result = HelperMethods.IsVersionResultWarning(resultTable, "File Version", TestObject.TFSItem.SKU);
                if (result)
                {
                    resultTable.TableName = "WARNING: " + resultTable.TableName;
                }
            }

            return result;
        }

        private DataRow[] FindRowsDetail(string detail, DataRow r, DataTable patchPayload,string  comVer)
        {

            if (detail == "Arch")
                return patchPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ComponentVersion LIKE '{2}*' and Size = '{3}'",
                                            r["FileName"], r["SKU"], comVer, r["Size"]));
            else if (detail == "Sku")
                return patchPayload.Select(String.Format("FileName = '{0}' and ProcessorArchitecture = '{1}' and ComponentVersion LIKE '{2}*' and Size = '{3}'",
                                            r["FileName"], r["ProcessorArchitecture"], comVer, r["Size"]));
            else if (detail == "Comver")
                return patchPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and Size = '{3}'",
                                            r["FileName"], r["SKU"], r["ProcessorArchitecture"], r["Size"]));
            else if (detail == "Size")
                return patchPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and ComponentVersion LIKE '{3}*'",
                                            r["FileName"], r["SKU"], r["ProcessorArchitecture"], comVer));
            else
                return null;

        }

        #endregion
    }
}
