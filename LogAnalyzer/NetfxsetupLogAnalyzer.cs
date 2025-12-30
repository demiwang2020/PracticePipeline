using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogAnalyzer
{
    public class NetfxsetupLogAnalyzer : TestLogAnalyzer
    {
        private static readonly string MAIN_LOG_NAME = "dd_MainLog_NetFxSetup.txt";
        //private static readonly string VERSIONVERIFICATION_LOG_NAME = "dd_VersionVerification.log";
        private static readonly string EXTRACT_LOCATION = @"C:\SAFXExtracted\AutoAnalysis";
        private static readonly string[] LOG_SEPARATORS = new string[] { @"=============== Start to execute Sequence " };
        private static readonly string LOG_RESULT_KEYWORD = "=============== Executed step {0}, Result is ";
        private static readonly string RESTART_KEYWORD = "*************************** Restart Machine ***************************";

        public override Result Analyze(int runID)
        {
            Result result = new Result() {OverallResult = true, FailReason = String.Empty};

            string logPath = Utility.GetMDResultLogPath(runID);
            if (!Directory.Exists(logPath))
            {
                result.OverallResult = false;
                result.FailReason = String.Format("Log path does not exist: {0}, auto analysis blocked", logPath);
                return result;
            }

            string[] files = Directory.GetFiles(logPath, "vslogs*.cab");
            if (files == null || files.Length == 0)
            {
                result.OverallResult = false;
                result.FailReason = "Log cab file does not exist, auto analysis blocked";
                return result;
            }

            string logCabFile = files[0];

            //Extract CAB
            string mainLogPath = null;
            try
            {
                mainLogPath = Utility.ExtractCab(logCabFile, MAIN_LOG_NAME, Path.Combine(EXTRACT_LOCATION, runID.ToString()));

                AnalyzeLog(mainLogPath, result);
            }
            catch(Exception ex)
            {
                result.OverallResult = false;
                result.FailReason = ex.Message;
                result.FailReason += ex.StackTrace;
            }

            return result;
        }

        private void AnalyzeLog(string logPath, Result result)
        {
            string logContent = Utility.ReadLog(logPath);

            string[] steps = logContent.Split(LOG_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);

            string stepName;
            bool stepResult, stepReboot;
            int stepResultCode = 0;
            StringBuilder failReason = new StringBuilder();

            for (int i = 1; i < steps.Length; ++i)
            {
                stepName = GetStepName(steps[i]);
                stepResult = GetStepResult(steps[i]);
                stepResultCode = GetStepResultCode(steps[i]);
                stepReboot = StepRebooted(steps[i]);

                if (stepReboot)
                {
                    //When step reboots machine, it doesn't write step result to log
                    stepResult = (stepResultCode == 0 || stepResultCode == 3010);
                }
                else if (stepName == "Collect all logs")
                {
                    stepResult = true;
                }

                if (!stepResult)
                {
                    result.OverallResult = false;
                    if (stepResultCode != 0 && stepResultCode != 3010) //failure has an error code
                    {
                        string errorDesc = ErrorTranslator.TranslateErrorCode(stepResultCode);

                        failReason.AppendFormat("#Step{0}: '{1}' failed with exit code {2}", i, stepName, stepResultCode);

                        if (!String.IsNullOrEmpty(errorDesc))
                            failReason.AppendFormat(": {0}", errorDesc);
                    }
                    else //failure doesn't have an error code
                    {
                        failReason.AppendFormat("#Step{0}: '{1}' failed without exit code", i, stepName);
                    }
                }
            }

            result.FailReason = failReason.ToString();
        }

        private string GetStepName(string step)
        {
            string stepName = String.Empty;

            string keyword = "Name [";
            int index = step.IndexOf(keyword);
            if (index > 0)
            {
                int nextIndex = step.IndexOf(']', index);
                if (nextIndex > 0)
                {
                    stepName = step.Substring(index + keyword.Length, nextIndex - index - keyword.Length);
                }
            }

            return stepName;
        }

        private int GetStepResultCode(string step)
        {
            int resultCode = 0;

            string keyword = "Exit code is ";

            int index = step.IndexOf(keyword);
            if (index > 0)
            {
                index += keyword.Length;
                bool negative = false;

                if (step[index] == '-')
                {
                    negative = true;
                    ++index;
                }

                while (step[index] >= '0' && step[index] <= '9')
                {
                    resultCode = resultCode * 10 + step[index] - '0';
                    ++index;
                }

                if (negative)
                    resultCode = -resultCode;
            }

            return resultCode;
        }

        private string GetStepIndex(string step)
        {
            int index = step.IndexOf(']');
            if (index > 0)
            {
                return step.Substring(1, index - 1);
            }
            else
            {
                return String.Empty;
            }
        }

        private bool GetStepResult(string step)
        {
            string keyword = String.Format(LOG_RESULT_KEYWORD, GetStepIndex(step));

            int index = step.IndexOf(keyword);
            if (index > 0)
            {
                //Result string is pass or fail
                string result = step.Substring(index + keyword.Length, 4);

                return result == "Pass";
            }
            else
                return false;
        }

        private bool StepRebooted(string step)
        {
            return step.Contains(RESTART_KEYWORD);
        }
    }
}
