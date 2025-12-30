using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.UpdateHelper
{
    class MUACWorker
    {
        private static string pubsuitetool = System.Configuration.ConfigurationManager.AppSettings["MUACToolPath"];
        private static string pubsuiteOutputPath = System.Configuration.ConfigurationManager.AppSettings["MUACToolOutputPath"];

        public static string GetPubSuiteXMLFile(string guid)
        {
            string xmlfile = string.Empty;
            string args = String.Format("-getp {0}", guid);
            string output = ExecuteTool(pubsuitetool, args);
            if (output.Contains("Publishing XML does not exist."))
                throw new Exception(String.Format("Could not get an XML file from MUAC for GUID '{0}'. Check that this GUID exists in MUAC or on the pubsuite explorer. {1}", guid, output));
            Regex filenameregex = new Regex(String.Format(@"{0}\.out\.xml", guid), RegexOptions.Multiline);
            Match filenamematch = filenameregex.Match(output);
            if (filenamematch.Success)
            {
                xmlfile = Path.Combine(pubsuiteOutputPath, filenamematch.Value);
                if (!File.Exists(xmlfile))
                {
                    throw new Exception(String.Format("Found file name {0} in PubUtil.exe output, but couldn't find file {1}", filenamematch.Value, xmlfile));
                }
            }
            else
            {
                throw new Exception(String.Format("Could not parse out file name from PubUtil.exe output {0}", output));
            }
            return xmlfile;
        }

        public static string QueryUpdates(string queryXmlPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(queryXmlPath) + ".out.xml";
            string xmlfile = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            string args = String.Format("-q {0}", queryXmlPath);
            string output = ExecuteTool(pubsuitetool, args);

            if (File.Exists(xmlfile))
                return xmlfile;
            else
                return null;
        }

        private static string ExecuteTool(string tool, string arguments)
        {
            // specify log output location (this is useful on website environment)
            string curDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(pubsuiteOutputPath);

            string result = string.Empty;
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo(tool, arguments);

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
                result = proc.StandardOutput.ReadToEnd();
            }
            catch (Exception objException)
            {
                throw new Exception("error occur in processing of excute command" + objException.ToString());
            }
            finally
            {
                Directory.SetCurrentDirectory(curDirectory);
            }

            return result;
        }
    }
}
