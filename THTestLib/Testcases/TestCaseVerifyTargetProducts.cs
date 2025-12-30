using Helper;
using ScorpionDAL;
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
    class TestCaseVerifyTargetProducts : TestCaseBase
    {
        public TestCaseVerifyTargetProducts(THTestObject testobj)
           : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            bool result = true;

            DataTable resultTable = HelperMethods.CreateDataTable("Verify Target .NET Products",
                new string[] { "Arch", "AssemblyIdentity Name", "Version", "Language",  "PublicKeyToken", "Result" },
                new string[] { "style=width:5%;text-align:center", "style=width:25%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:20%;text-align:center", "style=width:10%;text-align:center#ResultCol=1" });
            var tfsItem = TestObject.TFSItem;
            if (!CheckCaseNeedToRun(tfsItem.OSInstalled, tfsItem.OSSPLevel, tfsItem.SKU))
                return true;
            foreach (var patch in TestObject.Patches)
            {
                if (patch.Key.Equals(Architecture.ARM))
                    continue;
                //Get actual baselines
                string mumFileName = String.Format("package_for_KB{0}_rtm_gm~", tfsItem.KBNumber);
                if (tfsItem.OSInstalled.Equals("Windows 10"))
                {
                    mumFileName = String.Format("package_for_KB{0}_gm~", tfsItem.KBNumber);
                }
                
                string[] mumFiles = Directory.GetFiles(patch.Value.ExtractLocation, "*.mum", SearchOption.TopDirectoryOnly);
                string mumFilePath = (from r in mumFiles
                                      where Path.GetFileNameWithoutExtension(r).StartsWith(mumFileName, StringComparison.InvariantCultureIgnoreCase)
                                      select r).FirstOrDefault();
                if (String.IsNullOrEmpty(mumFilePath))
                {
                    throw new Exception("Unable to find mum file after extract target package.");
                }

                XmlDocument mumXml = new XmlDocument();
                mumXml.Load(mumFilePath);

                XmlNamespaceManager namespaces = new XmlNamespaceManager(mumXml.NameTable);
                namespaces.AddNamespace("ns", mumXml.DocumentElement.NamespaceURI);
                var xpath = "//ns:assembly/ns:package/ns:parent/ns:assemblyIdentity";
                XmlNodeList actualNodes = mumXml.SelectNodes(xpath, namespaces);

                using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
                {
                    var dtExpect = (from B in dataContext.CBSBaseline
                                    where B.SKU == tfsItem.SKU
                                    && B.OS == tfsItem.OSInstalled+" "+tfsItem.OSSPLevel
                                    select B).ToList();
                    foreach (var de in dtExpect)
                    {
                        bool parentFound = false;
                        foreach (XmlNode r in actualNodes)
                        {
                            if (r.Attributes["name"].Value == de.Name
                                && r.Attributes["version"].Value == de.Version
                                && r.Attributes["language"].Value == de.Language
                                && r.Attributes["publicKeyToken"].Value == de.PublicKeyToken)
                            {
                                parentFound = true;
                            }
                        }
                        if (!parentFound)
                        {
                            result = false;
                            DataRow dr = resultTable.NewRow();
                            dr["Arch"] = patch.Key;
                            dr["AssemblyIdentity Name"] = de.Name;
                            dr["Version"] = de.Version;
                            dr["Language"] = de.Language;
                            dr["PublicKeyToken"] = de.PublicKeyToken;
                            dr["Result"] = "Missing Parent";
                            resultTable.Rows.Add(dr);
                        }
                    }

                    foreach (XmlNode r in actualNodes)
                    {
                        var dtResult = (from B in dtExpect
                                        where 
                                         B.Name == r.Attributes["name"].Value
                                        && B.Version == r.Attributes["version"].Value
                                        && B.Language == r.Attributes["language"].Value
                                        && B.PublicKeyToken == r.Attributes["publicKeyToken"].Value
                                        select B).ToList();
                        if (dtResult.Count == 0)
                        {
                            result = false;
                            DataRow dr = resultTable.NewRow();
                            dr["Arch"] = patch.Key;
                            dr["AssemblyIdentity Name"] = r.Attributes["name"].Value;
                            dr["Version"] = r.Attributes["version"].Value;
                            dr["Language"] = r.Attributes["language"].Value;
                            dr["PublicKeyToken"] = r.Attributes["publicKeyToken"].Value;
                            dr["Result"] = "Additional Parent";
                            resultTable.Rows.Add(dr);
                        }
                    }

                }
            }
            if (resultTable.Rows.Count == 0)
            {
                resultTable.TableName = "Test Passed: Verify Target .NET Products";
            }
            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(result);
            TestObject.TestResults.Result &= result;
            return result;
        }

        private bool CheckCaseNeedToRun(string osInstalled, string osSPInstalled, string SKU)
        {
            if (!TestObject.IsProductRefresh)
            {
                if (osInstalled.StartsWith("Windows 8"))
                {
                    if (SKU.Equals("4.7.2") || SKU.Equals("4.8"))
                    {
                        return true;
                    }
                }
                else if (osInstalled.Equals("Windows 10"))
                {
                    string[] osSPInstalledArray = { "1607", "1703", "1709", "1803", "1809" };
                    if (osSPInstalledArray.Contains(osSPInstalled))
                    {
                        if (SKU.Equals("4.8"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
