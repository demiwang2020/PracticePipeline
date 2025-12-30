using Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace THTestLib.Testcases
{
    class TestCaseVerifyARP : TestCaseBase
    {
        public TestCaseVerifyARP(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            bool result = true;
            DataTable resultTable = HelperMethods.CreateDataTable("Verify ARP Branding",
                new string[] { "Patch Arch", "Expect KB Article", "Actual KB Article", "Expect Release Type", "Actual Release Type", "Result" },
                new string[] { "style=width:10%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:10%;text-align:center#ResultCol=1" });

            foreach(var patch in TestObject.Patches)
            {
                if(patch.Value.ActualBinaries!= null)
                {
                    result &= RunTest(patch.Key, patch.Value.ExtractLocation, resultTable);
                }
            }

            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(result);
            TestObject.TestResults.Result &= result;

            return result;
        }

        private bool RunTest(Architecture arch, string extractLocation, DataTable resultTable)
        {
            bool result = true;

            DataRow r = resultTable.NewRow();
            r["Patch Arch"] = arch.ToString();
            r["Expect KB Article"] = "KB" + TestObject.TFSItem.KBNumber;
            r["Expect Release Type"] = TestObject.TFSItem.ReleaseType;

            resultTable.Rows.Add(r);

            string kb, releaseType;
            if (!ParsePackageNode(Path.Combine(extractLocation, "update.mum"), out kb, out releaseType))
            {
                r["Actual KB Article"] = "Not found";
                r["Actual Release Type"] = "Not found";
                r["Result"] = "Fail";

                result = false;
            }
            else
            {
                r["Actual KB Article"] = kb;
                r["Actual Release Type"] = releaseType;

                //check if it is .NET Only path
                if (TestObject.TFSItem.Title.Contains(".NET Only"))
                {
                    //if it is .NET Only path, the Expect Relese type and Actual Release type can mismatched
                    result = r["Expect KB Article"].ToString().Equals(kb);
                }
                else
                {
                    //if it is not .NET Only path, the Expect Release type should be the same with Actual Release type
                    result = r["Expect KB Article"].ToString().Equals(kb) && r["Expect Release Type"].ToString().Equals(releaseType);
                }

                r["Result"] = result ? "Pass" : "Fail";
            }

            return result;
        }

        private bool ParsePackageNode(string pathOfUpdateMum, out string kbNumber, out string releaseType)
        {
            kbNumber = releaseType = String.Empty;

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
                    if (r.Name == "package")
                    {
                        kbNumber = r.Attributes["identifier"].Value;
                        releaseType = r.Attributes["releaseType"].Value;

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
