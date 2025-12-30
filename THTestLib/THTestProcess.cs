using Connect2TFS;
using LoggerLibrary;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib
{
    public class THTestProcess
    {
        public const string TFSServerURI = "https://vstfdevdiv.corp.microsoft.com/DevDiv";

        public static void StartTest()
        {
            //Create log
            string logPath = System.IO.Path.Combine(ConfigurationManager.AppSettings["BaseLogPath"], DateTime.Now.Date.ToString("yyyyMMdd") + "_Verbose.log");
            StaticLogWriter.createInstance(logPath);
            StaticLogWriter.Instance.logMessage(StaticLogWriter.LineSeparator2);

            //Refresh runtime status and send out report
            RuntimeStatusRefresher.UpdateRuntimeStatus();

            // If there is a speicified file, run test only for WI stored in the file
            List<WorkItem> specifiedItems = ReadSpecifiedTFSWIs();
            if (specifiedItems != null && specifiedItems.Count > 0)
            {
                StaticLogWriter.Instance.logMessage("Kickoff tests for specified WIs");
                foreach (WorkItem item in specifiedItems)
                {
                    StaticLogWriter.Instance.logMessage(String.Format("TFS ID - {0}", item.Id));
                   
                    THTestObject tester = new THTestObject(item);
                    tester.RunTest();
                } 
            }
            else
            {
                StaticLogWriter.Instance.logMessage("Kickoff tests for untested WIs from TFS");
                WorkItemCollection tfsItems = Connect2TFS.Connect2TFS.QueryCBSWorkItemsInSmokeTest(TFSServerURI);
                if (tfsItems.Count == 0)
                {
                    StaticLogWriter.Instance.logMessage("No work item found for smoke test in smoke test");
                }
                else
                {
                    foreach (WorkItem item in tfsItems)
                    {
                        StaticLogWriter.Instance.logMessage(String.Format("TFS ID - {0}", item.Id));

                        THTestObject tester = new THTestObject(item);
                        tester.RunTest();
                    }
                }
            }

            StaticLogWriter.Instance.logMessage(StaticLogWriter.LineSeparator2);
            StaticLogWriter.Instance.close();
        }

        private static List<WorkItem> ReadSpecifiedTFSWIs()
        {
            string wiFile = ConfigurationManager.AppSettings["SpecifiedWIFile"];
            int id;
            List<WorkItem> ret = null;
            string line;

            if (!String.IsNullOrEmpty(wiFile) && System.IO.File.Exists(wiFile))
            {
                ret = new List<WorkItem>();
                using (System.IO.StreamReader sr = new System.IO.StreamReader(wiFile))
                {

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (Int32.TryParse(line, out id))
                        {
                            ret.Add(Connect2TFS.Connect2TFS.GetWorkItem(id, TFSServerURI));
                        }
                    }
                }
            }

            return ret;
        }
    }
}
