using System;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using Microsoft.Test.DevDiv.SAFX.CommonLibraries.Utilities;

namespace Helper
{
    public class Extraction
    {
        /// <summary>
        /// Extracts the patch (CBS, MSI, OCM) to local destinatination
        /// </summary>
        /// <param name="exeFilePath">Path to the patch</param>
        /// <param name="patchKBName">The name of the patch will be used as extraction folder under extractPath</param>
        /// <param name="technology">Technology which patch uses, such as CBS, MSI, OCM</param>
        /// <returns>The path to the extracted folder on local machine</returns>
        public static string ExtractPatchToLocalPath(string extractPath, string exeFilePath, string patchKBName, PatchTechnology technology)
        {
            //Create LocalPath            
            string localFolderPath = Path.Combine(extractPath, patchKBName);
            localFolderPath = Path.Combine(localFolderPath, DateTime.Now.Ticks.ToString());
            string localFilePath = Path.Combine(localFolderPath, Path.GetFileName(exeFilePath));

            //Detect if we've already extracted this package (presumably in a previous test).  We do not need to extract again.            
            if (!Directory.Exists(localFolderPath))
            {
                Directory.CreateDirectory(localFolderPath);
                File.Copy(exeFilePath, Path.Combine(localFolderPath, Path.GetFileName(exeFilePath)));

                string processParameters = string.Empty;

                if (technology == PatchTechnology.CBS)
                    processParameters = string.Format(@" /quiet /extract:{0} ", localFolderPath);
                else if (technology == PatchTechnology.OCM)
                {
                    processParameters = string.Format(@" /quiet /x:{0} ", localFolderPath);
                }
                else
                {
                    //use 7zip to extract
                    processParameters = string.Format(@" x {0} -aoa -o{1}", localFilePath, localFolderPath);
                    localFilePath = ConfigurationManager.AppSettings["7ZipLocation"];
                }

                Process process = new Process();
                ProcessStartInfo processInfo = new ProcessStartInfo(localFilePath);
                processInfo.Arguments = processParameters;
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo = processInfo;
                process.Start();
                process.WaitForExit();

                //if it is a CBS patch, continue to extract CAB file
                if (technology == PatchTechnology.CBS)
                {
                    string patchNameWithoutExtension = Path.GetFileNameWithoutExtension(exeFilePath);
                    string cabFilePath = Path.Combine(localFolderPath,
                        string.Format("{0}.cab", patchNameWithoutExtension));

                    ExpandCab(cabFilePath, Path.Combine(localFolderPath, patchNameWithoutExtension));
                }
            }

            return localFolderPath;
        }

        /// <summary>
        /// Extracts a CAB file to a specified location by using system expand tool
        /// </summary>
        /// <param name="cabFilePath">path to the cab file to be extracted</param>
        /// <param name="extractDir">path to the extracted files</param>
        public static void ExpandCab(string cabFilePath, string extractDir)
        {
            if (!Directory.Exists(extractDir))
                Directory.CreateDirectory(extractDir);

            Process process = new Process();
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe");
            processInfo.Arguments = string.Format(@"/c expand -F:* {0} {1}", cabFilePath, extractDir);
            process.StartInfo = processInfo;
            process.Start();
            process.WaitForExit();
        }
    }
}
