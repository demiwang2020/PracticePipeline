using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using MSUAnalyzer;

namespace DataAggregator
{
    public static class Extraction
    {

        private static readonly string ExtractDir;

        /// <summary>
        /// Static constructor to populate the default Extraction Directory
        /// </summary>
        static Extraction()
        {
            ExtractDir = Path.Combine(Environment.GetEnvironmentVariable("temp"), "SAFXExtracted");

        }

        public static string ExtractionDir
        {
            get
            {
                return ExtractDir;
            }
        }

        /// <summary>
        /// Extracts a CAB file to a specified location
        /// </summary>
        /// <param name="cabFilePath">path to the cab file to be extracted</param>
        /// <param name="extractDir">path to the extracted files</param>
        public static void ExtractCabs(string cabFilePath, string extractDir)
        {

            //TODO: Need to point this to the correct external folder on the \\vsufile share
            string toolPath = Path.Combine(Environment.GetEnvironmentVariable("LocalCopy"), @"External\ExtractFiles\Extract.exe");

            try
            {
                Process process = new Process();
                ProcessStartInfo processInfo = new ProcessStartInfo(toolPath);
                processInfo.Arguments = string.Format(@"{0} *.* /L ""{1}"" /Y ", cabFilePath, extractDir);
                process.StartInfo = processInfo;
                if (!process.Start()) Console.WriteLine("Extraction Failed");
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                //TODO: log this exception; figure out if you need to rethrow
                //Assert.Fail("Extraction Failed with the following Exception " + ex.StackTrace);
            }

        }

        /// <summary>
        /// Extracts the SFX exe to local destinatination
        /// </summary>
        /// <param name="exeFilePath">Path to the patch</param>
        /// <param name="patchKBName">The name of the patch will be used as extraction folder under %temp%</param>
        /// <param name="patchTechnology">defines what type of patch this is</param>
        /// <returns>The path to the extracted folder on local machine</returns>
        public static string ExtractPatchToLocalPath(string exeFilePath, string patchKBName, string patchTechnology = "MSI")
        {
            string uniqueSKUId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);

            //Create LocalPath            
            string localFolderPath = Path.Combine(ExtractionDir, patchKBName);
            localFolderPath = Path.Combine(localFolderPath, uniqueSKUId);
            string localFilePath = Path.Combine(localFolderPath, Path.GetFileName(exeFilePath));
            //Detect if we've already extracted this package (presumably in a previous test).  We do not need to extract again.            
            if (!Directory.Exists(localFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(localFolderPath);
                    File.Copy(exeFilePath, Path.Combine(localFolderPath, Path.GetFileName(exeFilePath)));
                }
                catch (Exception ex)
                {
                    throw new Exception("File copy failed for " + exeFilePath + " with the following Exception" + ex.StackTrace);
                }
                try
                {
                    //Extract the pacakge from the local path to the local machine only if the package is an exe
                    if (localFilePath.ToLower().EndsWith(".exe"))
                    {
                        Process process = new Process();
                        ProcessStartInfo processInfo = new ProcessStartInfo(ConfigurationManager.AppSettings["7zaPath"].ToString());
                        // if exe name contains ndp1.1 or ndp1.0
                        string args = string.Format(@" x {0} -aoa -o{1}", localFilePath,localFolderPath);

                        processInfo.Arguments = args;
                        processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        processInfo.UseShellExecute = false;
                        process.StartInfo = processInfo;
                        process.Start();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception("Patch extraction failed. Process Exit Code: " + process.ExitCode.ToString());
                        }

                    }
                    else if (localFilePath.ToLower().EndsWith(".msu"))
                    {
                        // first copy branch force scripts to local folder
                        const string strScriptsPath = @"\\spsrv\gdr\VistaBranchForce"; //TODO: Add this to config file
                        if (Directory.Exists(strScriptsPath))
                        {
                            foreach (string filePath in Directory.GetFiles(strScriptsPath))
                            {
                                if (filePath != null)
                                    System.IO.File.Copy(filePath, Path.Combine(localFolderPath, Path.GetFileName(filePath)));
                            }
                        }
                        else
                            throw new DirectoryNotFoundException(String.Format("BranchForce Scripts directory:{0} does not exist.", strScriptsPath));

                        // now call extract MSU function
                        MSUManipulator.ExtractMSU(localFilePath, localFolderPath + "\\");
                    }
                }
                catch (Exception ex)
                {

                    if (Directory.Exists(localFolderPath)) { Directory.Delete(localFolderPath, true); }

                    throw new Exception("Extraction failed for " + localFilePath + " with the following Exception" + ex.StackTrace);
                }

            }
            return localFolderPath;

        }

    }
}
