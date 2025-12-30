using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PubsuiteStaticTestLib;
using LoggerLibrary;
using ClosedXML.Excel;
using PubsuiteStaticTestLib.Testcases;
using PubsuiteStaticTestLib.Model;
using NetFxServicing.LogInfoLib;
using WorkerProcess;

namespace PubsuiteStaticTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            //WUTestProcess wUTestProcess = new WUTestProcess();
            //wUTestProcess.StatTest(true);
            #region Custom Test
            //CustomTest test = new CustomTest();
            //test.RunTest(args[0]);
            //return;
            #endregion
            LogInfo.CreateInstance("CaiPublisher");
            //Check arguments
            string excelPath;
            List<int> casesToExeute;
            string logPath;
            string xmlPath;
            if (!CheckArgs(args, out excelPath, out casesToExeute, out logPath, out xmlPath))
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
                List<InputData> expectedUpdateInfos = ReadExcel(excelPath);
                if (!String.IsNullOrEmpty(xmlPath))
                    ReadPublishingXml(expectedUpdateInfos, xmlPath);

                //Run tests for each updates
                foreach (InputData eu in expectedUpdateInfos)
                {
                    List<TestResult> testResults = PubsuiteStaticTestLib.PubsuiteStaticTestLib.RunTests(eu,0,casesToExeute);
                    overallResult &= LogResults(eu, testResults);
                }

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
            Console.WriteLine("PubsuiteStaticTestConsole.exe ExcelFilePath [/testcase:case_id1,case_id2] [/output:logpath] [/loc] [/xml]");
            Console.WriteLine("\tExcelFilePath: An excel file that is used to kick off WU runtime test");
            Console.WriteLine("\t/testcase: Specify cases to run, separated with commas. If not specified, all supported cases except localized cases will be executed");
            Console.WriteLine("\t/output: Specify the path & name of output log");
            Console.WriteLine("\t/loc: Specify if to include localized properties verification cases. (Localized cases have ID greater than 1000 and are not inlcuded by default)");
            Console.WriteLine("\tSwitch /loc has no effect when /testcase is specified");
            Console.WriteLine("\txml: Specify a folder that stores publishing xmls");
            Console.WriteLine("\tWhen such folder is specified, update will be built from it, instead of retriving with web API");
            Console.WriteLine("\tEach xml file should have name with this format: update id.out.xml");


            List<TTestCaseInfo> testcases = PubsuiteStaticTestLib.PubsuiteStaticTestLib.QuerySupportedCases();
            Console.WriteLine("For now, {0} cases supported", testcases.Count);
            Console.WriteLine("\tID\tName\tDescription");
            foreach (var s in testcases)
            {
                Console.WriteLine("\t{0}\t{1}\t{2}", s.ID, s.Name, s.Description);
            }
        }

        private static bool CheckArgs(string[] args, out string excelPath, out List<int> casesToExecute, out string logPath, out string xmlPath)
        {
            excelPath = null;
            casesToExecute = null;
            logPath = null;
            xmlPath = null;

            bool locCaseInclude = false;
            
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

                            case "/loc":
                                locCaseInclude = true;
                                break;

                            case "/xml":
                                xmlPath = s.Substring(s.IndexOf(':') + 1);
                                break;

                            default: break;
                        }
                    }
                }
                else
                {
                    if (File.Exists(s))
                    {
                        excelPath = s;
                    }
                    else
                        return false;
                }
            }

            if (!locCaseInclude && casesToExecute == null)
            {
                casesToExecute = PubsuiteStaticTestLib.PubsuiteStaticTestLib.QuerySupportedCases().Select(p => p.ID).ToList();
            }

            return !String.IsNullOrEmpty(excelPath);
        }

        private static List<InputData> ReadExcel(string excelPath)
        {
            List<WUTestManagerLib.ExcelData> lstExcelData = WUTestManagerLib.WUTestManagerLib.ReadExcel(excelPath);
            List<InputData> expectedUpdateInfos = new List<InputData>();

            foreach (WUTestManagerLib.ExcelData data in lstExcelData)
            {
                InputData expData = new InputData();

                expData.KB = data.KB;
                expData.SupersededKB = data.SSKBs;
                expData.TFSIDs = (from s in data.TFSID.Split(new char[] { '+', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                  select Convert.ToInt32(s)).Distinct().ToList();
                expData.Title = data.Title;
                expData.UpdateID = data.GUID;
                expData.IsCatalogOnly=data.IsCatalogOnly;

                if (!String.IsNullOrEmpty(data.OtherProperties))
                {
                    string[] temp = data.OtherProperties.Split(new char[] { ';', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    expData.OtherProperties = new Dictionary<string, string>();
                    try
                    {
                        for (int i = 0; i < temp.Length; i += 2)
                        {
                            expData.OtherProperties[temp[i]] = temp[i + 1];
                        }
                    }
                    catch
                    { }
                }

                expectedUpdateInfos.Add(expData);
            }

            return expectedUpdateInfos;
        }

        private static void ReadPublishingXml(List<InputData> inputData, string xmlPath)
        {
            foreach(var data in inputData)
            {
                string fileName = data.UpdateID + ".out.xml";
                string fileFullPath = Path.Combine(xmlPath, fileName);
                if (File.Exists(fileFullPath))
                {
                    using (StreamReader sr = new StreamReader(fileFullPath))
                    {
                        data.PublishingXmlContent = sr.ReadToEnd();
                    }
                }
            }
        }

        private static string InitLog(string logPath)
        {
            string logName = !String.IsNullOrEmpty(logPath) ?
                logPath : "PubsuiteStaticChecking" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".log";

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

        private static bool LogResults(InputData expectInfo, List<TestResult> results)
        {
            bool result = true;

            StaticLogWriter.Instance.logMessage("*********************************************************");
            StaticLogWriter.Instance.logMessage(String.Format("* {0}", expectInfo.UpdateID));
            StaticLogWriter.Instance.logMessage(String.Format("* {0}", expectInfo.Title));
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
