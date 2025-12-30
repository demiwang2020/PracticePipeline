using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace THTestLib
{
    static class Extraction
    {
        public static string ExtractionPath = System.Configuration.ConfigurationManager.AppSettings["ExtractLocation"];

        public static string ExtractPatchToPath(string patchPath)
        {
            string flagFolderName = DateTime.Now.Ticks.ToString();
            FileInfo fileInfo = null;

            try
            {
                fileInfo = new FileInfo(patchPath);
            }
            catch
            {
            }

            if (fileInfo != null)
            {
                string strUseRemoteExtract = System.Configuration.ConfigurationManager.AppSettings["UseRemoteExtractLocation"];
                bool useRemoteExtract = String.IsNullOrEmpty(strUseRemoteExtract) ? false : Convert.ToBoolean(strUseRemoteExtract);

                // for packages that have size larger than 200MB, use remote extract location if possible
                //if (patchPath.StartsWith(@"\\vsufile") && useRemoteExtract && fileInfo.Length > 200 * 1024 * 1024)
                if (patchPath.StartsWith(@"F:\Packages") && useRemoteExtract && fileInfo.Length > 200 * 1024 * 1024)
                {
                    string remoteExtractLocation = Path.Combine(Path.GetDirectoryName(patchPath), "EXPANDED_PACKAGE", Path.GetFileNameWithoutExtension(patchPath));

                    if (Directory.Exists(remoteExtractLocation))
                        return remoteExtractLocation;
                }

                // build flag folder name which is used to speed up extraction
                // format is: package size + package last modify time
                flagFolderName = fileInfo.Length.ToString() + fileInfo.LastWriteTimeUtc.Ticks.ToString();
            }

            return ExtractPatchToPath(patchPath, flagFolderName);
        }

        private static string ExtractPatchToPath(string patchPath, string flagFolderName)
        {
            string pacthName = Path.GetFileNameWithoutExtension(patchPath);
            string extention = Path.GetExtension(patchPath);

            //Create LocalPath
            string localFolderPath = Path.Combine(ExtractionPath, pacthName);
            localFolderPath = Path.Combine(localFolderPath, flagFolderName);
            string localFilePath = Path.Combine(localFolderPath, Path.GetFileName(patchPath));

            //Detect if we've already extracted this package (presumably in a previous test).  We do not need to extract again.            
            if (!Directory.Exists(localFolderPath))
            {
                Directory.CreateDirectory(localFolderPath);
                File.Copy(patchPath, localFilePath);

                //extract MSU
                if (extention.ToLowerInvariant() == ".msu")
                {
                    localFilePath = ExtractMSU(localFilePath);
                }

                localFilePath = ExtractCAB(localFilePath);

                //Extract all sub
                string[] subCabFiles = Directory.GetFiles(localFilePath, "*.cab", SearchOption.TopDirectoryOnly);
                if (subCabFiles.Length > 0)
                {
                    foreach (string cab in subCabFiles)
                    {
                        ExtractCAB(cab, localFilePath);
                    }
                }

                return localFilePath;
            }
            else
            {
                string extractpath = Path.Combine(localFolderPath, pacthName);
                if (Directory.Exists(extractpath))
                    return extractpath;
                else
                    return extractpath + "_PSFX";
            }
        }

        public static void DeleteExtractLocation(string extractPath)
        {
            int idx = extractPath.IndexOf('\\', ExtractionPath.Length + 1);
            if (idx > 0)
            {
                string folderPath = extractPath.Substring(0, idx);
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch
                { 
                }
            }
        }

        private static string ExtractMSU(string msuPath)
        {
            string dirPath = Path.GetDirectoryName(msuPath);
            string msuName = Path.GetFileNameWithoutExtension(msuPath);

            Process process = new Process();
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe");
            processInfo.Arguments = string.Format(@"/c {0} -F:* {1} {2}", GetExpandToolPath(), msuPath, dirPath);
            process.StartInfo = processInfo;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.Start();
            process.WaitForExit();

            //Return CAB path
            string cabFilePath = Path.Combine(dirPath, String.Format("{0}.cab", msuName));

            if (!File.Exists(cabFilePath))
            {
                cabFilePath = Path.Combine(dirPath, String.Format("{0}_PSFX.cab", msuName));
                if (!File.Exists(cabFilePath))
                {
                    throw new Exception(String.Format("Failed to find {0}.cab or {0}_PSFX.cab after extracting {1}.msu", msuName, msuName));
                }
            }

            return cabFilePath;
        }

        private static string ExtractCAB(string cabPath, string destPath = null)
        {
            string extractDir = destPath;
            if (String.IsNullOrEmpty(extractDir))
            {
                string dirPath = Path.GetDirectoryName(cabPath);
                string cabName = Path.GetFileNameWithoutExtension(cabPath);
                extractDir = Path.Combine(dirPath, cabName);
            }

            if (!Directory.Exists(extractDir))
                Directory.CreateDirectory(extractDir);

            Process process = new Process();
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe");
            processInfo.Arguments = string.Format(@"/c {0} -F:* {1} {2}", GetExpandToolPath(), cabPath, extractDir);
            process.StartInfo = processInfo;
            if (!process.Start())
            {
                throw new Exception(String.Format("Failed to start extraction process to extract CAB {0}", cabPath));
            }

            process.WaitForExit();

            return extractDir;
        }

        private static string GetExpandToolPath()
        {
            string toolPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), @"External\expand.exe");
            if (File.Exists(toolPath))
                return toolPath;

            return "expand";
        }
    }
}
