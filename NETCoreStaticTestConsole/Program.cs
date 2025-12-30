using LoggerLibrary;
using NETCoreMUStaticLib.Model;
using NETCoreMUStaticLib.Testcases;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreStaticTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //Check arguments
            string xmlPath;
            List<int> casesToExeute;
            string logPath;
            Dictionary<string, string> parameters;
            if (!CheckArgs(args, out xmlPath, out casesToExeute, out logPath, out parameters))
            {
                PrintHelp();
                return;
            }

            //Init logging
            logPath = InitLog(logPath);
            bool overallResult = true;

            try
            {
                //Read excel
                string xmlContent = ReadPublishingXml(xmlPath);
                xmlContent = xmlContent.Replace("<pub:Update xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\">", "<pub:Update>");

                //Run tests for each updates
                List <TestResult> testResults = NETCoreMUStaticLib.NETCoreMUStaticLib.RunTests(xmlContent,xmlPath, casesToExeute, parameters);
                overallResult &= LogResults(xmlPath, testResults);
            }
            catch (Exception ex)
            {
                StaticLogWriter.Instance.logError("Exception caught: " + ex.Message);
                StaticLogWriter.Instance.logMessage(ex.StackTrace);
                overallResult = false;
            }
            finally
            {
                CloseLog(overallResult);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("NETCoreStaticTestConsole.exe <publish xml path> [/testcase:case_id1,case_id2] [/output:logpath] [/eName:value]");
            Console.WriteLine("\tpublish xml path: The path of publishing xml");
            Console.WriteLine("\t/testcase: Specify cases to run, separated with commas. If not specified, all supported cases except localized cases will be executed");
            Console.WriteLine("\t/output: Specify the path & name of output log");
            Console.WriteLine("\t/eName: specify parameters for test");

            List<TTestCaseInfo> testcases = NETCoreMUStaticLib.NETCoreMUStaticLib.QuerySupportedCases();
            Console.WriteLine("For now, {0} cases supported", testcases.Count);
            Console.WriteLine("\tID\tName\tDescription");
            foreach (var s in testcases)
            {
                Console.WriteLine("\t{0}\t{1}\t{2}", s.ID, s.Name, s.Description);
            }
        }

        private static bool CheckArgs(string[] args, out string xmlPath, out List<int> casesToExecute, out string logPath, out Dictionary<string, string> parameters)
        {
            xmlPath = null;
            casesToExecute = null;
            logPath = null;
            parameters = new Dictionary<string, string>();

            foreach (string s in args)
            {
                if (s.StartsWith("/"))
                {
                    string[] temp = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    //if (temp.Length >= 2)
                    {
                        switch (temp[0].ToLowerInvariant())
                        {
                            case "/testcase":
                                casesToExecute = temp[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => Convert.ToInt32(p)).ToList();
                                break;

                            case "/output":
                                logPath = s.Substring(s.IndexOf(':') + 1);
                                break;

                            default:
                                if (temp.Length == 2 && temp[0].StartsWith("/e"))
                                {
                                    parameters.Add(temp[0].Substring(2), temp[1]);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    if (File.Exists(s))
                    {
                        xmlPath = s;
                    }
                    else
                        return false;
                }
            }

            return !String.IsNullOrEmpty(xmlPath);
        }

        private static string ReadPublishingXml(string xmlPath)
        {
            using (StreamReader sr = new StreamReader(xmlPath))
            {
                return sr.ReadToEnd();
            }
        }

        private static string InitLog(string logPath)
        {
            string logName = !String.IsNullOrEmpty(logPath) ?
                logPath : "NETCoreStaticTestConsole_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".log";

            StaticLogWriter.createInstance(logName);

            return logName;
        }

        private static void CloseLog(bool result)
        {
            //StaticLogWriter.Instance.TimestampOff = false;
            //StaticLogWriter.Instance.logMessage("\r\n**************************Test End**************************");
            StaticLogWriter.Instance.logMessage("*********************************************************");
            StaticLogWriter.Instance.logMessage("Overall test result for all updates --> " + (result ? "Pass" : "Fail"));
            StaticLogWriter.Instance.logMessage("*********************************************************");
            StaticLogWriter.Instance.close();
        }

        private static bool LogResults(string publishingXmlPath, List<TestResult> results)
        {
            bool result = true;

            StaticLogWriter.Instance.logMessage("*********************************************************");
            StaticLogWriter.Instance.logMessage(String.Format("* {0}", publishingXmlPath));
            StaticLogWriter.Instance.logMessage("*********************************************************");

            StaticLogWriter.Instance.TimestampOff = true;

            foreach (TestResult r in results)
            {
                StaticLogWriter.Instance.logMessage(String.Empty);
                StaticLogWriter.Instance.logScenario(String.Format("Executing case: {0}", r.CaseName));
                result &= r.Result;

                StaticLogWriter.Instance.logMessage(r.Log);

                if (!r.Result && r.Failures != null && r.Failures.Count > 0)
                {
                    StaticLogWriter.Instance.logMessage(String.Empty);

                    foreach (KeyValuePair<string, TestFailure> kv in r.Failures)
                    {
                        StaticLogWriter.Instance.logMessage("Expect " + kv.Key + ":");
                        StaticLogWriter.Instance.logMessage(kv.Value.ExpectResult);

                        StaticLogWriter.Instance.logMessage("Actual " + kv.Key + ":");
                        StaticLogWriter.Instance.logMessage(kv.Value.ActualResult);
                    }
                }

                StaticLogWriter.Instance.logMessage(StaticLogWriter.LineSeparator1);
                StaticLogWriter.Instance.logMessage("Case executing result --> " + (r.Result ? "Pass" : "Fail"));
            }

            StaticLogWriter.Instance.TimestampOff = false;

            StaticLogWriter.Instance.logScenario("Overall result --> " + (result ? "Pass" : "Fail"));

            return result;
        }
    }
}
