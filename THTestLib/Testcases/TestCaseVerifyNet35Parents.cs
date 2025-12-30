using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace THTestLib.Testcases
{
    class TestCaseVerifyNet35Parents : TestCaseBase
    {
        private List<string> _expectVersions = new List<string>() {
            "10.0.10240.16384", //TH1
            "10.0.10586.0", //TH2
            "10.0.14393.0", //RS1
            "10.0.15063.0", //RS2
            "10.0.16299.15", //RS3
            "10.0.17134.1"}; //RS4
        
        public TestCaseVerifyNet35Parents(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            // Test if patch is TH1~RS4 WSD LCU
            if (!TestObject.IsTraditionalWin10LCU)
                return true;

            // Only for 2.0/3.0/3.5
            if (TestObject.TFSItem.SKU[0] < '2' || TestObject.TFSItem.SKU[0] > '3')
                return true;

            DataTable resultTable = CreateResultsTable();
            bool overallResult = true;
            foreach (var patch in TestObject.Patches)
            {
                overallResult &= RunTest(patch.Value, resultTable);
            }

            if (overallResult)
            {
                resultTable.TableName = "Test Passed: " + resultTable.TableName;
            }

            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.ResultDetailSummaries.Add(overallResult);
            TestObject.TestResults.Result &= overallResult;

            return overallResult;
        }

        private DataTable CreateResultsTable()
        {
            DataTable resultTable = HelperMethods.CreateDataTable("Verify multiple parents of NET3.5",
                new string[] { "Patch Arch", "Result", "File Name", "AssemblyIdentify Name", "Version" },
                new string[] { "style=width:15%;text-align:center", "style=width:20%;text-align:center#ResultCol=1", "style=width:25%;text-align:center", "style=width:25%;text-align:center", "style=width:15%;text-align:center" });

            return resultTable;
        }

        private bool RunTest(PatchInformation patch, DataTable resultTable)
        {
            Regex regexOCName = new Regex(@"Microsoft-Windows-NetFx[-|2|3]"); //This regex will exclude NetFx4 OCs
            Regex regexOCWPF = new Regex(@"Microsoft-Windows-Presentation-Package");

            List<Regex> regexNet35Parents = new List<Regex>() { regexOCName, regexOCWPF };
            List<AssemblyIdentity> assemblies = new List<AssemblyIdentity>();

            bool result = true;
            string[] mumFiles = System.IO.Directory.GetFiles(patch.ExtractLocation, "*.mum", System.IO.SearchOption.TopDirectoryOnly);

            foreach (var mumFile in mumFiles)
            {
                assemblies.Clear();

                CollectNet35ParentsFromMumFile(mumFile, regexNet35Parents, assemblies);

                // check if there are missing or additional parents
                if (assemblies.Count > 0)
                {
                    var assemblyNames = assemblies.Select(p => p.Name).Distinct();

                    foreach (var name in assemblyNames)
                    {
                        List<string> assemblyVersions = assemblies.Where(p => p.Name == name).Select(p => p.Version).ToList();

                        var additionalAssembilies = assemblyVersions.Except(_expectVersions);
                        var missingAssembilies = _expectVersions.Except(assemblyVersions);

                        if(additionalAssembilies.Count() > 0)
                        {
                            result = false;

                            foreach (var s in additionalAssembilies)
                            {
                                DataRow r = resultTable.NewRow();
                                r["Patch Arch"] = patch.Arch.ToString();
                                r["Result"] = "Additional Parent";
                                r["File Name"] = Path.GetFileName(mumFile);
                                r["AssemblyIdentify Name"] = name;
                                r["Version"] = s;

                                resultTable.Rows.Add(r);
                            }
                        }
                        if (missingAssembilies.Count() > 0)
                        {
                            result = false;

                            foreach (var s in missingAssembilies)
                            {
                                DataRow r = resultTable.NewRow();
                                r["Patch Arch"] = patch.Arch.ToString();
                                r["Result"] = "Missing Parent";
                                r["File Name"] = Path.GetFileName(mumFile);
                                r["AssemblyIdentify Name"] = name;
                                r["Version"] = s;

                                resultTable.Rows.Add(r);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get NET35 parents from one mum file
        /// </summary>
        private void CollectNet35ParentsFromMumFile(string mumFilePath, List<Regex> regexNet35Parents, List<AssemblyIdentity> assemblies)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(mumFilePath);

            XmlNamespaceManager xmlNS = new XmlNamespaceManager(xmlDoc.NameTable);
            xmlNS.AddNamespace("def", "urn:schemas-microsoft-com:asm.v3");

            // Test if the mum file is for .NET 4.X
            // If the mum file is for .NET 4.X, skip following detection
            XmlNode installNode = xmlDoc.SelectSingleNode("def:assembly/def:package/def:update/def:applicable/def:updateComponent/def:assemblyIdentity", xmlNS);
            if (installNode != null)
            {
                string version = installNode.Attributes["version"].Value;
                if (!String.IsNullOrEmpty(version) && version.StartsWith("4.0."))
                {
                    return;
                }
            }

            XmlNodeList nodes = xmlDoc.SelectNodes("def:assembly/def:package/def:parent/def:assemblyIdentity", xmlNS);
            foreach (XmlNode node in nodes)
            {
                string name = node.Attributes["name"].Value;
                string language = node.Attributes["language"].Value;

                // skip parents that are not neutral and en-us
                if (!String.IsNullOrEmpty(language) && (language.Equals("neutral") || language.Equals("en-us")) && //language
                    !name.Equals("Microsoft-Windows-NetFx-Shared-WPF-Package", StringComparison.InvariantCultureIgnoreCase)) //hard code an exception
                {
                    foreach (Regex reg in regexNet35Parents)
                    {
                        if (reg.IsMatch(name))
                        {
                            assemblies.Add(new AssemblyIdentity() { Name = name, Version = node.Attributes["version"].Value });
                            break;
                        }
                    }
                }
            }
        }
    }

    class AssemblyIdentity
    {
        public string Name;
        public string Version;
    }
}
