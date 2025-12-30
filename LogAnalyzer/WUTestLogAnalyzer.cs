using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScorpionDAL;
using PubsuiteStaticTestLib;

namespace LogAnalyzer
{
    public class WUTestLogAnalyzer : TestLogAnalyzer
    {
        private static readonly string MAIN_LOG_NAME = "WUTestMainLog.log";
        private static readonly string[] LOG_SEPARATORS = new string[] { @"=========================================" };
        private static readonly string KEY_EXPECT = "ExpectTitleFragment";
        private static readonly string KEY_UNEXPECT = "UnExpectTitleFragment";
        private static readonly string ERROR_PREFIX = "Error:";
        private static readonly string STEP_INTERNAL_SEP = "-----------------------------------------\r\n";
        private static readonly string FINAL_RESULT_FLAG = "Test completed, final result: ";

        private List<string> _expectTitleFragment;
        private List<string> _unExpectTitleFragment;
        private List<string> _ignorableErrors;

        public override Result Analyze(int runID)
        {
            Result result = new Result() { OverallResult = true, FailReason = String.Empty };

            string logPath = Utility.GetMDResultLogPath(runID);
            if (!Directory.Exists(logPath))
            {
                result.OverallResult = false;
                result.FailReason = String.Format("Log path does not exist: {0}, auto analysis blocked", logPath);
                return result;
            }

            string mainLog = Path.Combine(logPath, MAIN_LOG_NAME);
            if (!File.Exists(mainLog))
            {
                result.OverallResult = false;
                result.FailReason = String.Format("Log file does not exist: {0}, auto analysis blocked", mainLog);
                return result;
            }

            AnalyzeLog(mainLog, result);

            return result;
        }

        private void AnalyzeLog(string logPath, Result result)
        {
            RetriveExtraData();
            
            //Read log
            string logContent = Utility.ReadLog(logPath);

            // Read test result, if pass, further analysis is not needed
            if (ReadOverallTestResult(logContent))
            {
                return;
            }

            //Split log to steps
            List<string> steps = SplitLog2Steps(logContent);

            string stepName;
            bool stepResult;

            foreach(string step in steps)
            {
                stepResult = GetStepResult(step);
                if (stepResult) // if this step is passing, no need to do further analysis
                    continue;
                
                //split step to lines
                List<string> lines = SplitStep2Lines(step);
                stepName = lines.First();

                //Find each errors in this step
                List<WUTestFailureInfo> failures = FindAllErrorsInStep(lines);
                if (String.IsNullOrEmpty(result.FailReason))
                    result.FailReason = stepName;
                else
                    result.FailReason = String.Format("{0}#{1}", result.FailReason, stepName);

                foreach (WUTestFailureInfo fail in failures)
                {
                    AnalyzeError(stepName, lines, fail, result);
                }
            }
        }

        /// <summary>
        /// Analyze one failure
        /// </summary>
        /// <param name="stepLines"></param>
        /// <param name="fail"></param>
        private void AnalyzeError(string stepName, List<string> stepLines, WUTestFailureInfo fail, Result result)
        {
            // Check ignorable error list
            if (_ignorableErrors != null && _ignorableErrors.Count > 0)
            {
                foreach (var s in _ignorableErrors)
                {
                    if (fail.ErrorMsg.Equals(s))
                    {
                        result.FailReason = String.Format("{0};;{1}", result.FailReason, "Find ignorable error: " + s);
                        return;
                    }
                }
            }
            
            switch (fail.ErrorMsg)
            {
                case "Expected differences don't match actual":
                    if (stepName.Contains(") Scan"))
                    {
                        ScanAnalysis(stepLines, fail, result);
                    }
                    else
                    {
                        DefaltAnalysis(fail, result);
                    }
                    break;

                default: //defalut handler
                    DefaltAnalysis(fail, result);
                    break;
            }
        }

        private void DefaltAnalysis(WUTestFailureInfo fail, Result result)
        {
            result.OverallResult = false;
            result.FailReason = String.Format("{0};;{1}", result.FailReason, fail.ErrorMsg);
        }

        private void ScanAnalysis(List<string> stepLines, WUTestFailureInfo fail, Result result)
        {
            bool analyzedResult = true;

            bool expectAppearFound = false;

            for (int i = fail.LineNo + 1; i < stepLines.Count; ++i)
            {
                if (!stepLines[i].StartsWith("-------"))
                    break;

                if (stepLines[i].Contains("-------Additional NOT offered KBs are:"))
                    continue;
                if (stepLines[i].Contains("-------Additional NEWly offered KBs are:"))
                    continue;

                string[] splitStr = stepLines[i].TrimStart(new char[] { '-' }).Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                string title = splitStr[0];
                string guid = splitStr[1];
                
                //exclude Catalog only guid
                if (IsCatalogUpdate(guid, title))
                    continue;

                // Expect appear updates
                if (_expectTitleFragment != null && _expectTitleFragment.Count > 0)
                {
                    foreach(var s in _expectTitleFragment)
                    {
                        if (title.Contains(s))
                        {
                            expectAppearFound = true;
                            break;
                        }
                    }
                }

                // Expect appear updates
                if (_unExpectTitleFragment != null && _unExpectTitleFragment.Count > 0)
                {
                    foreach (var s in _unExpectTitleFragment)
                    {
                        if (title.Contains(s))
                        {
                            analyzedResult = false;
                            break;
                        }
                    }
                }
            }

            if (analyzedResult && _expectTitleFragment != null && _expectTitleFragment.Count > 0 && !expectAppearFound)
                analyzedResult = false;

            result.OverallResult &= analyzedResult;

            if (!analyzedResult)
            {
                result.FailReason = String.Format("{0};;{1}", result.FailReason, "Find unexpect title fragments, or not find expect title fragments in the results of comparing scan logs");
            }
            else 
            {
                result.FailReason = String.Format("{0};;{1}", result.FailReason, "Failed, but analyze passed");
            }
        }


        /// <summary>
        /// Get step result
        /// </summary>
        /// <returns></returns>
        private bool GetStepResult(string step)
        {
            string keyword = "--> ";

            int index = step.IndexOf(keyword);
            if (index > 0)
            {
                index += keyword.Length;
                if (step[index] == 'P')
                {
                    return true;
                }
            }

            return false;
        }

        private string GetErrorStep(string step)
        {
            string error = string.Empty;

            string keyword = "-->";
            int index = 0;
            int nextIndex = step.IndexOf(keyword);
            error = step.Substring(index, nextIndex - index);
            return error;
        }

        private bool IsCatalogUpdate(string id, string title)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext db = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                // 1. Search from DB
                TDotNetUpdate update = db.TDotNetUpdates.Where(p => p.UpdateID.Equals(id)).FirstOrDefault();

                if (update != null)
                {
                    return update.Destination.Equals("Catalog");
                }
                else
                {
                    string dest = PubsuiteStaticTestLib.PubsuiteStaticTestLib.GetUpdateDestination(id);

                    if (!String.IsNullOrEmpty(dest))
                    {
                        TDotNetUpdate newRecord = new TDotNetUpdate();
                        newRecord.UpdateID = id;
                        newRecord.Destination = dest;
                        newRecord.Title = title;
                        db.TDotNetUpdates.InsertOnSubmit(newRecord);

                        db.SubmitChanges();

                        return dest.Equals("Catalog");
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Read overall test result from end of the log
        /// </summary>
        private bool ReadOverallTestResult(string log)
        {
            int index = log.IndexOf(FINAL_RESULT_FLAG);
            if (index > 0)
            {
                string str = log.Substring(index + FINAL_RESULT_FLAG.Length);
                if (str.StartsWith("Pass"))
                    return true;
            }

            return false;
        }

        private List<string> SplitLog2Steps(string log)
        {
            //Remove timestamps
            //Sample: [2019-05-10 04:21:38] 
            string timestamp = @"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] ";
            log = Regex.Replace(log, timestamp, String.Empty);

            log = log.Replace(STEP_INTERNAL_SEP, String.Empty);
            
            List<string> steps = log.Split(LOG_SEPARATORS, StringSplitOptions.RemoveEmptyEntries).ToList();

            steps.RemoveAt(0); // fist is not needed

            if (steps.Last().Contains(FINAL_RESULT_FLAG)) //remove last
                steps.RemoveAt(steps.Count - 1);

            return steps;
        }

        private List<string> SplitStep2Lines(string stepContent)
        {
            return stepContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Find the lines that error/failure occurs
        /// </summary>
        /// <param name="stepLines"></param>
        /// <returns></returns>
        private List<WUTestFailureInfo> FindAllErrorsInStep(List<string> stepLines)
        {
            List<WUTestFailureInfo> failures = new List<WUTestFailureInfo>();

            for (int i = 0; i < stepLines.Count; ++i)
            {
                if(stepLines[i].StartsWith(ERROR_PREFIX))
                {
                    failures.Add(new WUTestFailureInfo() { ErrorMsg = stepLines[i].Substring(ERROR_PREFIX.Length), LineNo = i });
                }
            }

            return failures;
        }

        private void RetriveExtraData()
        {
            object obj = base.GetExtraData("KEY_EXPECT");
            if (obj != null)
                _expectTitleFragment = (List<string>)(obj);

            obj = base.GetExtraData("KEY_UNEXPECT");
            if (obj != null)
                _unExpectTitleFragment = (List<string>)(obj);

            obj = base.GetExtraData("KEY_IGNORABLE");
            if (obj != null)
                _ignorableErrors = (List<string>)(obj);
        }
    }

    class WUTestFailureInfo
    {
        public string ErrorMsg {get; set;}
        public int LineNo {get; set;}

        public WUTestFailureInfo()
        {
            ErrorMsg = String.Empty;
            LineNo = -1;
        }
    }
}
