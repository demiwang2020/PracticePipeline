using Helper;
using Microsoft.TeamFoundation.Framework.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    class TestcaseVerifyLCUPayloadsInOtherSKU : TestCaseBase
    {
        private string _sku;

        public TestcaseVerifyLCUPayloadsInOtherSKU(THTestObject testobj)
            : base(testobj)
        {

            if (TestObject.TFSItem.SKU[0] >= '4')
                _sku = "2.0";
            else
                _sku = "4.7";
        }

        public override bool RunTestCase()
        {
            //No LCU is set, just skip testing
            if (TestObject.SimplePatch)
            {
                return true;
            }
            if (!TestObject.SimplePatch && TestObject.TFSItem.LCUKBArticle == "TBD")
            {

                DataTable warningFakeTable = new DataTable("WARNING: LCU KB is set to TBD on TFS, please double check if this is expected");

                TestObject.TestResults.ResultDetails.Add(warningFakeTable);
                TestObject.TestResults.ResultDetailSummaries.Add(false);

                return true;

            }

            bool testResult = true;
            bool overallResult = true;

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
            bool warning = false;
            resultTable = null;


            string archString = arch == Architecture.AMD64 ? "x64" : arch.ToString();
            string lcuPath = Path.GetDirectoryName(TestObject.Patches[arch].LCUPatchPath);
            string testPath = TestObject.Patches[arch].PatchLocation;
            string fileName = String.Format("PackageMetadata-{0}.xml", archString);
            resultTable = HelperMethods.CreateDataTable(String.Format("Check cumulative .NET payloads for SKU {0} for {1}", _sku, arch),
                                            new string[] { "Type", "Assembly Name", "File Name", "ComponentVersion", "File Version", "LCU ComponentVersion", "LCU File Version", "ProcessorArchitecture", "ImportPath" },
                                            new string[] { "style=width:11.11%;text-align:center#ResultCol=1", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center", "style=width:11.11%;text-align:center" });

            //Get file names from target patch
            testPath = Path.Combine(Path.GetDirectoryName(testPath), fileName);
            if (!File.Exists(testPath))
            {
                resultTable.TableName = "Cannot find " + fileName + ", skip case VerifyLCUPayloadsInOtherSKU for " + arch;
                return true;
            }

            List<MetadataFileNode> testFiles = PackageMetadataReader.ReadPackageMetadata(testPath, _sku, TestObject.Patches[arch].ExtractLocation);

            //Get file names from LCU KB
            string baseline = Path.Combine(Path.Combine(lcuPath, fileName));
            List<MetadataFileNode> baselineFiles = PackageMetadataReader.ReadPackageMetadata(baseline, _sku, TestObject.Patches[arch].LCUExtractLocation);

            //Compare each file with LCU
            foreach (MetadataFileNode t in testFiles)
            {
                var searchResult = baselineFiles.Where(p => p.AssemblyName == t.AssemblyName &&
                    p.ProcessorArchitecture == t.ProcessorArchitecture &&
                    p.FileName == t.FileName &&
                    p.ImportPath == t.ImportPath);

                if (searchResult.Count() == 0) // file is not in baseline (LCU)
                {
                    DataRow row = resultTable.NewRow();
                    row["Type"] = "New File";
                    row["Assembly Name"] = t.AssemblyName;
                    row["ComponentVersion"] = t.ComponentVersion;
                    row["File Name"] = t.FileName;
                    row["ProcessorArchitecture"] = t.ProcessorArchitecture;
                    row["ImportPath"] = t.ImportPath;
                    row["File Version"] = t.FileVersion;

                    resultTable.Rows.Add(row);

                    // adding new payload to cumulative patch is very often seen, so just warning
                    warning = true;
                }
                else // file is in baseline, compare component version
                {
                    MetadataFileNode baseFn = searchResult.First();
                    bool bFileVersionDifferent = String.Compare(baseFn.FileVersion, t.FileVersion) != 0 &&
                        !String.IsNullOrEmpty(t.FileVersion) &&
                        !String.IsNullOrEmpty(baseFn.FileVersion);
                    bool bComponentVersionDifferent = String.Compare(baseFn.ComponentVersion, t.ComponentVersion) != 0 &&
                        !HelperMethods.LCUTestFailureIgnorable(baseFn, t);

                    if (bFileVersionDifferent || bComponentVersionDifferent)
                    {
                        DataRow row = resultTable.NewRow();
                        //row["Type"] = "Different Component Version";
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
                            // Both component version and file version change, this is very often seen. So just show warning message
                            row["Type"] = "Different Component and File Version";
                            warning = true;
                        }
                        else if (bFileVersionDifferent)
                        {
                            // Only file version changes, component version remains. This is weird so fail test
                            row["Type"] = "Different File Version";
                            result = false;
                        }
                        else if (bComponentVersionDifferent)
                        {
                            // Only component version changes. For now just warning
                            row["Type"] = "Different Component Version";
                            warning = true;
                        }

                        resultTable.Rows.Add(row);
                    }
                }
            }

            //Compare each file in LCU with current package to find out possible missing files
            foreach (MetadataFileNode b in baselineFiles)
            {
                var searchResult = testFiles.Where(p => p.AssemblyName == b.AssemblyName &&
                    p.ProcessorArchitecture == b.ProcessorArchitecture &&
                    p.FileName == b.FileName &&
                    p.ImportPath == b.ImportPath);

                if (searchResult.Count() == 0)
                {
                    // missing any files that are in LCU should fail test
                    result = false;

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
                if (warning)
                    resultTable.TableName = "WARNING: Test Failed: " + resultTable.TableName;
                else
                    resultTable.TableName = "Test Passed: " + resultTable.TableName;
            }

            return result;
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

            resultTable = HelperMethods.CreateDataTable(String.Format("Check cumulative .NET payloads for SKU {0} for {1}", _sku, arch),
                                            new string[] { "Type", "File Name", "ComponentVersion", "File Version", "LCU ComponentVersion", "LCU File Version", "ProcessorArchitecture" },
                                            new string[] { "style=width:14.28%;text-align:center#ResultCol=1", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center", "style=width:14.28%;text-align:center" });
            bool result = true;
            bool warning = false;
            string compVerPrefix = _sku[0] < '4' ? "10.0." : "4.0.";

            //2. Verify all old files in new patch has same file version and component version as LCU
            foreach (DataRow f in newPatchPayload.Rows)
            {
                if (f["ComponentVersion"].ToString().StartsWith(compVerPrefix))
                {
                    DataRow[] rows = lcuPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and ComponentVersion LIKE '{3}*'", f["FileName"], f["SKU"], f["ProcessorArchitecture"], compVerPrefix));

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
                                    // Both component version and file version change, this is very often seen. So just show warning message
                                    newRow["Type"] = "Different Component and File Version";
                                    warning = true;
                                }
                                else if (bFileVersionDifferent)
                                {
                                    // Only file version changes, component version remains. This is weird so fail test
                                    newRow["Type"] = "Different File Version";
                                    result = false;
                                }
                                else if (bComponentVersionDifferent)
                                {
                                    // Only component version changes. For now just warning
                                    newRow["Type"] = "Different Component Version";
                                    warning = true;
                                }

                                resultTable.Rows.Add(newRow);
                            }
                            else // file version and component version match with LCU, 
                                // then compare file size and last modified date, they should also be same as LCU
                            {
                                if (f["Size"].ToString() != r["Size"].ToString())
                                {
                                    string folderName1 = Path.GetFileName(Path.GetDirectoryName(f["ExtractPath"].ToString()));
                                    string folderName2 = Path.GetFileName(Path.GetDirectoryName(r["ExtractPath"].ToString()));

                                    if (folderName1 == folderName2)
                                    {
                                        DataRow newRow = resultTable.NewRow();
                                        newRow["ComponentVersion"] = f["ComponentVersion"];
                                        newRow["File Name"] = f["FileName"];
                                        newRow["ProcessorArchitecture"] = f["ProcessorArchitecture"];
                                        newRow["File Version"] = f["Version"];
                                        newRow["LCU ComponentVersion"] = r["ComponentVersion"];
                                        newRow["LCU File Version"] = r["Version"];
                                        newRow["Type"] = "Different file size";
                                        result = false;

                                        resultTable.Rows.Add(newRow);
                                    }
                                }
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

                        // adding new payload to cumulative patch is very often seen, so just warning
                        warning = true;
                    }
                }
            }

            //3. Verify all files in LCU are all in new patch
            foreach (DataRow r in lcuPayload.Rows)
            {
                if (!r["ComponentVersion"].ToString().StartsWith(compVerPrefix))
                    continue;

                DataRow[] rows = newPatchPayload.Select(String.Format("FileName = '{0}' and SKU = '{1}' and ProcessorArchitecture = '{2}' and ComponentVersion LIKE '{3}*'", r["FileName"], r["SKU"], r["ProcessorArchitecture"], compVerPrefix));

                if (rows == null || rows.Length == 0)
                {
                    DataRow newRow = resultTable.NewRow();
                    newRow["Type"] = "Missing file";
                    newRow["File Name"] = r["FileName"];
                    newRow["ProcessorArchitecture"] = r["ProcessorArchitecture"];
                    newRow["LCU ComponentVersion"] = r["ComponentVersion"];
                    newRow["LCU File Version"] = r["Version"];

                    resultTable.Rows.Add(newRow);

                    // missing any files that are in LCU should fail test
                    result = false;
                }
            }

            if (result)
            {
                if (warning)
                    resultTable.TableName = "WARNING: Test Failed: " + resultTable.TableName;
                else
                    resultTable.TableName = "Test Passed: " + resultTable.TableName;
            }

            return result;
        }

        #endregion
    }
}
