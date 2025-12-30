using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Data;
using Helper;

namespace THTestLib.Testcases
{
    class TestCaseVerifyAssemblyIdentity : TestCaseBase
    {
        private string _expect_assemblyIdentity;

        public TestCaseVerifyAssemblyIdentity(THTestObject testobj)
            : base(testobj)
        {
            if (testobj.SimplePatch || TestObject.IsProductRefresh)
                _expect_assemblyIdentity = "Package_for_KB" + testobj.TFSItem.KBNumber;
            else if (testobj.TFSItem.SKU == "4.8.1")
                _expect_assemblyIdentity = "Package_for_DotNetRollup_481";
            else
                _expect_assemblyIdentity = "Package_for_DotNetRollup";
        }

        public override bool RunTestCase()
        {
            if (TestObject.SimplePatch)
            {
                DataTable warningFakeTable = new DataTable("WARNING: LCU KB is not set on TFS, please double check if this is expected");

                TestObject.TestResults.ResultDetails.Add(warningFakeTable);
                TestObject.TestResults.ResultDetailSummaries.Add(false);

                return true;
            }
            else if (!TestObject.SimplePatch && TestObject.TFSItem.LCUKBArticle == "TBD")
            {

                DataTable warningFakeTable = new DataTable("WARNING: LCU KB is set to TBD on TFS, please double check if this is expected");

                TestObject.TestResults.ResultDetails.Add(warningFakeTable);
                TestObject.TestResults.ResultDetailSummaries.Add(false);

                return true;
            }
            if (CaseNeeded())
            {
                bool result = true;
                DataTable resultTable = HelperMethods.CreateDataTable("Verify assemblyIdentity in update.mum",
                    new string[] { "Patch Arch", "Expect assemblyIdentity", "Actual assemblyIdentity", "Version", "LCU Version", "Result" },
                    new string[] { "style=width:10%;text-align:center", "style=width:25%;text-align:center", "style=width:25%;text-align:center", "style=width:15%;text-align:center", "style=width:15%;text-align:center", "style=width:10%;text-align:center#ResultCol=1" });

                foreach (Architecture arch in TestObject.SupportedArchs)
                {
                    result &= RunTest(arch, TestObject.Patches[arch].ExtractLocation, TestObject.Patches[arch].LCUPatchPath, resultTable);
                }

                TestObject.TestResults.ResultDetails.Add(resultTable);
                TestObject.TestResults.ResultDetailSummaries.Add(result);
                TestObject.TestResults.Result &= result;

                return result;
            }
            else
            {
                return true;
            }
        }

        private bool CaseNeeded()
        {

            if (TestObject.IsWindows10Patch)
            {
                // All Win10 4.8.X patches
                if (TestObject.TFSItem.SKU.StartsWith("4.8"))
                    return true;

                // RS5 4.7.2 patches
                if (TestObject.TFSItem.OSSPLevel == "1809" && TestObject.TFSItem.SKU.Contains("4.7.2"))
                    return true;
            }
            else if(TestObject.IsProductRefresh)
                return true;
            

            return false;
        }

        private string ParseAssemblyIdentity(string pathOfUpdateMum, out string version)
        {
            version = String.Empty;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathOfUpdateMum);

            XmlNode assemblyNode = null;

            foreach (XmlNode r in xmlDoc.ChildNodes)
            {
                if (r.Name == "assembly")
                {
                    assemblyNode = r;
                    break;
                }
            }

            if (assemblyNode != null)
            {
                foreach (XmlNode r in assemblyNode.ChildNodes)
                {
                    if (r.Name == "assemblyIdentity")
                    {
                        version = r.Attributes["version"].Value;

                        return r.Attributes["name"].Value;
                    }
                }
            }

            return String.Empty;
        }

        private bool RunTest(Architecture arch, string extractLocation, string lcuPath, DataTable resultTable)
        {
            bool result = true;
            string version = String.Empty;
            string lcuVersion = String.Empty;
            string actualValue = ParseAssemblyIdentity(Path.Combine(extractLocation, "update.mum"), out version);
            int len = lcuPath.LastIndexOf("\\") + 1;
            string patchName = lcuPath.Substring(len, lcuPath.Length - 4 - len);
            string patchLoc = lcuPath.Substring(0, lcuPath.LastIndexOf("\\"));
            string patchExpandLocation = TestObject.Patches[arch].ExtractLocation;
            string lcuExtractLocation = string.Empty;
            if (!String.IsNullOrEmpty(lcuPath))
            {               
                if (!patchExpandLocation.Contains("EXPANDED_PACKAGE"))
                {
                    lcuExtractLocation = Extraction.ExtractPatchToPath(lcuPath);
                }
                else
                {
                    lcuExtractLocation = Path.Combine(patchLoc, "EXPANDED_PACKAGE", patchName);
                }
                ParseAssemblyIdentity(Path.Combine(lcuExtractLocation, "update.mum"), out lcuVersion);
            }

            DataRow r = resultTable.NewRow();
            r["Patch Arch"] = arch.ToString();
            r["Expect assemblyIdentity"] = _expect_assemblyIdentity;
            r["Actual assemblyIdentity"] = actualValue;
            r["Version"] = version;
            r["LCU Version"] = String.IsNullOrEmpty(lcuVersion) ? "No LCU" : lcuVersion;


            result &= _expect_assemblyIdentity.Equals(actualValue);
            if (!String.IsNullOrEmpty(lcuVersion))
            {
                result &= String.Compare(version, lcuVersion) > 0;
            }

            r["Result"] = result ? "Pass" : "Fail";

            resultTable.Rows.Add(r);

            return result;
        }
    }
}
