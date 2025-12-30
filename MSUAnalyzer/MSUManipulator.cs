using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MSUAnalyzer
{
    public class MSUManipulator
    {
        /// <summary>
        /// Extract MSU file
        /// </summary>
        /// <param name="strFullPatchFilePath"></param>
        /// <param name="strPatchCopyLocation"></param>
        public static void ExtractMSU(string strFullPatchFilePath, string strPatchCopyLocation)
        {
            string msuName = Path.GetFileNameWithoutExtension(strFullPatchFilePath);
            RunCmd(@"md " + strPatchCopyLocation + "Expanded_" + msuName);
            RunCmd(GetExpandToolPath() + " -F:* \"" + strFullPatchFilePath + "\" \"" + strPatchCopyLocation + "Expanded_" + msuName + "\"");
            RunCmd(@"md " + strPatchCopyLocation + "Expanded_" + msuName + "\\" + msuName);
            RunCmd(GetExpandToolPath() + " -F:* \"" + strPatchCopyLocation + "Expanded_" + msuName + "\\" + msuName + ".cab" + "\" \"" + strPatchCopyLocation + "Expanded_" + msuName + "\\" + msuName);
            if (File.Exists(strPatchCopyLocation + "Expanded_" + msuName + "\\" + "WSUSSCAN.cab"))
            {
                RunCmd(@"md " + strPatchCopyLocation + "Expanded_" + msuName + "\\" + "WSUSSCAN");
                RunCmd(GetExpandToolPath() + " -F:* \"" + strPatchCopyLocation + "Expanded_" + msuName + "\\" + "WSUSSCAN.cab" + "\" \"" + strPatchCopyLocation + "Expanded_" + msuName + "\\WSUSSCAN");
            }
        }
        private static string GetExpandToolPath()
        {
            string toolPath = System.Configuration.ConfigurationManager.AppSettings["ExpandToolPath"];
            if (!String.IsNullOrEmpty(toolPath) && File.Exists(toolPath))
                return toolPath;
            return "expand";
        }
        /// <summary>
        /// Extract CAB file
        /// </summary>
        /// <param name="strCABFullPath"></param>
        /// <param name="strDestinationPath"></param>
        public static void ExtractCAB(string strCABFullPath, string strDestinationPath)
        {
            if (!Directory.Exists(strDestinationPath))
            {
                Directory.CreateDirectory(strDestinationPath);
            }
            if (strDestinationPath.Substring(strDestinationPath.Length - 1, 1).Equals("\\"))
            {
                string strDestinationPath1 = strDestinationPath.Substring(0, strDestinationPath.Length - 1);
                RunCmd(GetExpandToolPath() + " -F:* \"" + strCABFullPath + "\" Destination \"" + strDestinationPath1 + "\"");
            }
            else
            {
                RunCmd(GetExpandToolPath() + " -F:* \"" + strCABFullPath + "\" Destination \"" + strDestinationPath + "\"");
            }
        }
        /// <summary>
        /// excute cmd
        /// </summary>
        /// <param name="strCommand"></param>
        private static void RunCmd(string strCommand)
        {
            System.Diagnostics.Process procExtraction = new System.Diagnostics.Process();
            procExtraction.StartInfo.CreateNoWindow = true;
            procExtraction.StartInfo.FileName = "cmd.exe";
            procExtraction.StartInfo.Arguments = @"/c " + strCommand;
            procExtraction.StartInfo.UseShellExecute = false;
            procExtraction.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            procExtraction.Start();
            procExtraction.WaitForExit();

            if (procExtraction.ExitCode != 0 && !strCommand.Contains("Windows8.1"))
            {
                throw new Exception("Patch extraction failed. Process Exit Code: " + procExtraction.ExitCode.ToString());
            }
        }
    }
}
