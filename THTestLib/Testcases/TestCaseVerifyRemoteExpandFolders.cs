using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Helper;
using System.Net.Http.Headers;
using System.Configuration;

namespace THTestLib.Testcases
{
    class TestCaseVerifyRemoteExpandFolders : TestCaseBase
    {
        public TestCaseVerifyRemoteExpandFolders(THTestObject testobj)
            : base(testobj)
        {
        }

        public override bool RunTestCase()
        {
            if (TestObject.IsTraditionalWin10LCU)
                return true;

            DataTable resultTable = CreateResultsTable();
            bool overallResult = true;

            foreach (var patch in TestObject.Patches)
            {

                if (patch.Value.PatchLocation.StartsWith(ConfigurationManager.AppSettings["PackagePath"]))
                    return true;

                string remoteExpandedPath = Path.Combine(Path.GetDirectoryName(patch.Value.PatchLocation),
                                                           "EXPANDED_PACKAGE",
                                                           Path.GetFileNameWithoutExtension(patch.Value.PatchLocation));
                 if (!Directory.Exists(remoteExpandedPath))
                {
                    remoteExpandedPath += "_PSFX";
                }

                overallResult &= RunTest(patch.Key, patch.Value.ExtractLocation, remoteExpandedPath, resultTable);
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
            DataTable resultTable = HelperMethods.CreateDataTable("Verify patches are correctly extracted to EXPANDED_PACKAGE folder",
                new string[] { "Patch Arch", "Fail Type", "File Name" },
                new string[] { "style=width:10%;text-align:center", "style=width:15%;text-align:center#ResultCol=1", "style=width:75%;text-align:center" });

            return resultTable;
        }

        /// <summary>
        /// Compare each folders/files in 2 paths
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="remotePath"></param>
        /// <param name="resultTable"></param>
        /// <returns>True if 2 paths have exactly same structure</returns>
        private bool RunTest(Architecture arch, string localPath, string remotePath, DataTable resultTable)
        {
            bool result = true;
            List<string> pathList1 = new List<string>();
            List<string> pathList2 = new List<string>();
            CollectPath(pathList1, localPath, localPath);
            CollectPath(pathList2, remotePath, remotePath);

            List<string> diffList1 = pathList1.Except(pathList2).ToList();//get files in localPath but not in remotePath
            List<string> diffList2 = pathList2.Except(pathList1).ToList();//get files in remotePath but not in localPath

            if (diffList1.Count != 0)//show all files in path1 but not in path2
            {
                foreach (string s in diffList1)
                {
                    DataRow dr = resultTable.NewRow();
                    dr["Patch Arch"] = arch.ToString();
                    dr["Fail Type"] = "Missing";
                    dr["File Name"] = s;
                }

                result = false;
            }

            if (diffList2.Count != 0)//show all files in path2 but not in path1
            {
                foreach (string s in diffList2)
                {
                    DataRow dr = resultTable.NewRow();
                    dr["Patch Arch"] = arch.ToString();
                    dr["Fail Type"] = "Additional";
                    dr["File Name"] = s;
                }
                result = false;
            }

            return result;
        }

        private void CollectPath(List<string> pathList, string currentPath, string rootPath)
        {
            DirectoryInfo d = new System.IO.DirectoryInfo(currentPath);
            FileSystemInfo[] f = d.GetFileSystemInfos();//get directories and files in the path
            foreach (FileSystemInfo fs in f)
            {
                if (fs is DirectoryInfo)//is directory
                {
                    string x = fs.FullName.Replace(rootPath, String.Empty); //delete root directory
                    pathList.Add(x); //save paths in the list
                    CollectPath(pathList, fs.FullName, rootPath);
                }
                else//is file
                {
                    pathList.Add(fs.FullName.Replace(rootPath, String.Empty));//save paths in the list
                }
            }
        }
    }
}
