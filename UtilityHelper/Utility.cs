using System;
using System.Diagnostics;
using System.Linq;

namespace Helper
{
    public static class Utility
    {
        public static void ExecuteCommandSync(object command)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                Console.WriteLine(result);
            }
            catch (Exception objException)
            {
                throw new Exception("error occur in processing of excute command" + objException.ToString());
            }
        }

        public static int ExecuteCommandSync(string CommandPath, string parameter, int maxWaitTime)
        {
            if (String.IsNullOrEmpty(CommandPath))
            {
                return -1;
            }

            CommandPath = Environment.ExpandEnvironmentVariables(CommandPath.Trim());

            //Check if commandPath has whitespace, if yes, add " at the front and the end.
            if (CommandPath.Contains(" ") && CommandPath[0] != '\"')
            {
                CommandPath = "\"" + CommandPath + "\"";
            }

            Process externalCommandProcess = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CommandPath;
            startInfo.Arguments = parameter;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;

            externalCommandProcess.StartInfo = startInfo;
            externalCommandProcess.Start();
            if (!externalCommandProcess.WaitForExit(maxWaitTime))
            {
                externalCommandProcess.Kill();
                return 999;
            }
            else
            {
                return externalCommandProcess.ExitCode;
            }
        }

        public static int ExecuteCommandSync(string CommandPath, string parameter, int maxWaitTime, out string output)
        {
            output = String.Empty;

            CommandPath = CommandPath.Trim();
            //Check if commandPath has whitespace, if yes, add " at the front and the end.
            if (CommandPath.Contains(' ') && CommandPath[0] != '\"')
            {
                CommandPath = "\"" + CommandPath + "\"";
            }

            Process externalCommandProcess = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CommandPath;
            startInfo.Arguments = parameter;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            externalCommandProcess.StartInfo = startInfo;
            externalCommandProcess.Start();
            if (!externalCommandProcess.WaitForExit(maxWaitTime))
            {
                externalCommandProcess.Kill();
                output = externalCommandProcess.StandardOutput.ReadToEnd();
                return 999;
            }
            else
            {
                output = externalCommandProcess.StandardOutput.ReadToEnd();
                return externalCommandProcess.ExitCode;
            }
        }

        /// <summary>
        /// Check if a given product is 4.5 above product. Product name should be like '.NET Framework 4.5.1 RTM'
        /// </summary>
        public static bool IsNDP45AboveFamework(string productName)
        {
            string[] splitedProduct = productName.Split(new char[] { ' ' });
            if (splitedProduct.Length < 4)
                return false;

            string sku = splitedProduct[2];
            if (sku.TrimEnd('/').Contains("/"))
            {
                sku = sku.Substring(sku.LastIndexOf("/") + 1);
            }

            string[] splitedSKU = sku.Split(new char[] { '.' });

            if (Convert.ToInt16(splitedSKU[0]) > 4)
                return true;

            if (Convert.ToInt16(splitedSKU[0]) < 4)
                return false;

            if (splitedSKU.Length > 1 && Convert.ToInt16(splitedSKU[1]) > 5) //eg. 4.6
                return true;

            if (splitedSKU.Length > 2) //eg, 4.5.2, 4.6.1
                return true;

            return false;
        }
    }
}
