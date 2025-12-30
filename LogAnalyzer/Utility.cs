using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalyzer
{
    class Utility
    {
        public static string ExtractCab(string cabFilePath, string file2Extract, string destPath)
        {
            string extractDir = destPath;
            if (String.IsNullOrEmpty(extractDir))
            {
                string dirPath = Path.GetDirectoryName(cabFilePath);
                string cabName = Path.GetFileNameWithoutExtension(cabFilePath);
                extractDir = Path.Combine(dirPath, cabName);
            }

            if (!Directory.Exists(extractDir))
                Directory.CreateDirectory(extractDir);

            Process process = new Process();
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe");
            processInfo.Arguments = string.Format(@"/c expand -F:{0} {1} {2}", file2Extract, cabFilePath, extractDir);
            processInfo.CreateNoWindow = true;
            process.StartInfo = processInfo;
            process.Start();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return Path.Combine(extractDir, "temp", file2Extract);
            }
            else
            {
                return null;
            }
        }

        public static string GetMDResultLogPath(int runID)
        {
            return String.Format(@"\\mdfile3\OrcasTS\Files\Core\Results\Run{0}", runID);
        }

        public static string ReadLog(string logPath)
        {
            using (StreamReader sr = new StreamReader(logPath))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
