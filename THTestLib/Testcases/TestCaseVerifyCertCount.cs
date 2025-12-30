using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using Helper;
using System.Web;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
namespace THTestLib.Testcases
{
    //This case is to verify cert count for each binary
    class TestCaseVerifyCertCount : TestCaseBase
    {
        private string _signTestPath;
            
        public TestCaseVerifyCertCount(THTestObject testobj)
            : base(testobj)
        {
            _signTestPath = Path.Combine(Extraction.ExtractionPath, "SigningTest");
        }

        public override bool RunTestCase()
        {
            //Delete old binaries
            foreach (Architecture arch in TestObject.SupportedArchs)
            {
                DeleteTestBinaries(arch);
            }
            PrepareForTest();

            //Create result table
            DataTable resultTable = HelperMethods.CreateDataTable("Cert Count Verification",
                new string[] { "Patch", "SKU", "LogPath", "Result" },
                new string[] { "style=width:15%;text-align:center", "style=width:15%;text-align:center", "width=55%", "style=width:15%;text-align:center#ResultCol=1" });

            //Run test
            bool result = true;

            bool isProductRefresh = TestObject.IsProductRefresh;

            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    result &= RunTest(patch.Key, patch.Value.PatchLocation, resultTable, isProductRefresh);
                }
            }

            TestObject.TestResults.ResultDetails.Add(resultTable);
            TestObject.TestResults.Result &= result;
            TestObject.TestResults.ResultDetailSummaries.Add(result);

            //Delete old binaries
            foreach (Architecture arch in TestObject.SupportedArchs)
            {
                DeleteTestBinaries(arch);
            }
            return result;
        }

        private void PrepareForTest()
        {
            //Copy signingverification tool to local path
            CopySVTool();

            //Copy actual binaries to test location
            foreach (var patch in TestObject.Patches)
            {
                if (patch.Value.ActualBinaries != null)
                {
                    CopyTestBinaries(patch.Value.ActualBinaries, patch.Key);
                }
            }
        }

       
        private void CopySVTool()
        {
            if (!Directory.Exists(_signTestPath))
            {
                Directory.CreateDirectory(_signTestPath);
            }

            string signToolPath = System.Configuration.ConfigurationManager.AppSettings["SigningVerificationToolLocation"];

            HelperMethods.RobocopyFolder(signToolPath, _signTestPath);
        }

        //Copy binaries from extracted location to a signtest folder
        private void CopyTestBinaries(DataTable payloadTable, Architecture arch)
        {
            string folderName = arch.ToString();
            string binaryDestPath = Path.Combine(_signTestPath, folderName);

            Directory.CreateDirectory(binaryDestPath);

            int index = 0;
            foreach (DataRow r in payloadTable.Rows)
            {
                // Only do this for new files
                if (!r["SKU"].ToString().Equals(TestObject.TFSItem.SKU.ToString()))
                    continue;

                CopyTestBinary(r["ExtractPath"].ToString(), r["SKU"].ToString(), binaryDestPath, index++);
            }
        }

        private void DeleteTestBinaries(Architecture arch)
        {
            string folderName = arch.ToString();
            string binaryDestPath = Path.Combine(_signTestPath, folderName);

            if (Directory.Exists(binaryDestPath))
            {
                try
                {
                    File.SetAttributes(binaryDestPath, FileAttributes.Normal);
                    Directory.Delete(binaryDestPath, true);
                }
                catch
                { }
            }
        }

        private void CopyTestBinary(string filePath, string sku, string binaryDestPath, int index)
        {
            string destPath = Path.Combine(binaryDestPath, sku, index.ToString());
            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);
            if (filePath.Contains("arm64_netfx4arm64-"))
            {
                File.Copy(filePath, Path.Combine(destPath, "arm64_" + Path.GetFileName(filePath)));
            }
            else
            {
                HelperMethods.RobocopyFile(filePath, destPath);
            }
        }

        private bool RunTest(Architecture arch, string patchLocation,DataTable resultTable,bool isProductRefresh)
        {
          
            string patchFolder = Path.GetDirectoryName(patchLocation);
            string SpecifiedFolder = "SpecifiedFolder";
            string newFolderPath = Path.Combine(patchFolder, SpecifiedFolder);
            if (!Directory.Exists(newFolderPath)) {

                Directory.CreateDirectory(newFolderPath);
                File.Copy(patchLocation, Path.Combine(newFolderPath, Path.GetFileName(patchLocation)));
            }
          
            string toolPath = Path.Combine(_signTestPath, "Tool\\SigningVerification.exe");
            string binaryPath = Path.Combine(_signTestPath, arch.ToString());
            string[] skus = Directory.GetDirectories(binaryPath);
            //bool overallResult = true;
            bool result = true;
            bool packageSignResult;
            foreach (string subFolder in skus)
            {
                string sku = Path.GetFileName(subFolder);
                string configXML = GetConfigXMLPath(sku);
                if(isProductRefresh && sku == "4.8.1" )
                {
                    configXML = configXML.Replace("481CBS", "481Pro");
                }
                if(TestObject.TFSItem.OSSPLevel == "25H2")
                {
                    configXML = configXML.Replace("481CBS", "48125H2");
                }
                result = ExecuteSigningVerificationTool(arch, configXML, toolPath, subFolder);
                packageSignResult = ExecuteSigningVerificationTool1(arch, configXML, toolPath, newFolderPath);

                string logPath = CopyLogToSharedLocation(arch, sku);
                string logPathSec = CopyLogToSharedLocationSec(arch, sku);
                result &= packageSignResult;
                //Store result
                DataRow row = resultTable.NewRow();
                row["Patch"] = arch.ToString();
                row["SKU"] = sku;
                row["LogPath"] =logPath + Environment.NewLine + logPathSec ;
                row["Result"] = result ? "Pass" : "Fail";
                resultTable.Rows.Add(row);

                //result &= packageSignResult;
            }

            return result;
        }



        private string GetConfigXMLPath(string sku)
        {
            switch (sku)
            {
                case "2.0":
                    return Path.Combine(_signTestPath, "ConfigFiles\\2.0\\Configuration.xml");

                case "3.0":
                    if (TestObject.IsWindows10Patch &&
                        TestObject.TFSItem.OSSPLevel.CompareTo("1903") >= 0 &&
                        !TestObject.TFSItem.OSSPLevel.Equals("RTM"))
                    {
                        return Path.Combine(_signTestPath, "ConfigFiles\\3.0\\Configuration_RS6.xml");
                    }
                    else
                    {
                        return Path.Combine(_signTestPath, "ConfigFiles\\3.0\\Configuration.xml");
                    }

                case "3.5":
                    return Path.Combine(_signTestPath, "ConfigFiles\\3.5\\Configuration.xml");

                case "4.5":
                case "4.5.1":
                case "4.5.2":
                    return Path.Combine(_signTestPath, "ConfigFiles\\45XCBS\\Configuration.xml");
                case "4.6.2":
                    return Path.Combine(_signTestPath, "ConfigFiles\\46XCBS\\Configuration_TH1.xml");
                case "4.8":
                    return Path.Combine(_signTestPath, "ConfigFiles\\46XCBS\\Configuration_48.xml");
                case "4.8.1":
                    return Path.Combine(_signTestPath, "ConfigFiles\\481CBS\\Configuration.xml");

                default: //special config for TH1 because mscorlib.ni.dll has 2 certs
                    return Path.Combine(_signTestPath, "ConfigFiles\\46XCBS\\Configuration.xml");
            }
        }

        // Call signing verification
        private bool ExecuteSigningVerificationTool(Architecture arch, string config, string toolPath, string testPath)
        {
            string args = string.Format(" /Path:\"{0}\" /Arch:{1} /ResultPath:\"{2}\" /Config:\"{3}\" /noExtract /CheckThumbprints:True", testPath, arch.ToString(), _signTestPath, config);
            return Helper.Utility.ExecuteCommandSync(toolPath, args, -1) == 0;
        }
        private bool ExecuteSigningVerificationTool1(Architecture arch, string config, string toolPath, string testPath)
        {
            string args = string.Format(" /Path:\"{0}\" /Arch:{1} /ResultPath:\"{2}\" /ResultName:\"{3}\" /CheckThumbprints:True", testPath, arch.ToString(), _signTestPath, "PackageSigningVerificationResult");
            return Helper.Utility.ExecuteCommandSync(toolPath, args, -1) == 0;
        }

        private string CopyLogToSharedLocation(Architecture arch, string sku)
        {
            string logPath = Path.Combine(_signTestPath, "SigningVerificationResult.html");
            if (File.Exists(logPath))
            {
                string logBase = System.Configuration.ConfigurationManager.AppSettings["LogStorePath"];
                string newLogName = String.Format("{0}_{1}_{2}_SigningVerification_{3}.html", TestObject.TFSItem.ID, arch.ToString(), sku.Replace(".", String.Empty), DateTime.Now.Ticks.ToString());
                string uploadedPath = Path.Combine(logBase, newLogName);

                try
                {
                    File.Copy(logPath, uploadedPath, true);
                }
                catch
                {
                    uploadedPath = "Uploading log failed";
                }
                TestObject.AddAttachmentToWI(TestObject.TFSItem.ID, uploadedPath);
                return uploadedPath;
                
            }
            else
            {
                return "Log not found";
            }
        }

        private string CopyLogToSharedLocationSec(Architecture arch, string sku)
        {
            string packLogPath = Path.Combine(_signTestPath, "PackageSigningVerificationResult.html");
            if (File.Exists(packLogPath))
            {
                string logBase = System.Configuration.ConfigurationManager.AppSettings["LogStorePath"];
                string newPacLogName = String.Format("{0}_{1}_{2}_PackageSigningVerification_{3}.html", TestObject.TFSItem.ID, arch.ToString(), sku.Replace(".", String.Empty), DateTime.Now.Ticks.ToString());
                string uploadedPath = Path.Combine(logBase, newPacLogName);

                try
                {
                    File.Copy(packLogPath, Path.Combine(logBase, newPacLogName), true);
                }
                catch
                {
                    uploadedPath = "Uploading log failed";
                }
                TestObject.AddAttachmentToWI(TestObject.TFSItem.ID, uploadedPath);
                return uploadedPath;

            }
            else
            {
                return "Log not found";
            }
        }
    }
}
