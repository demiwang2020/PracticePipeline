using DefinitionInterpreter;
using MadDogObjects;
using MadDogObjects.RunManagement;
using Microsoft.SqlServer.Server;
using NetFxServicing.LogInfoLib;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using MDO = MadDogObjects;

namespace FxbvtLibrary
{
    public class FxbvtIntegration
    {
        public string Release { get; set; }
        public string ReleaseOriginalForm { get; set; }
        public string ReleaseType { get; set; }
        public string TestPatchType { get; set; }
        public string WIPKBNumber { get; set; }
        public string TestTeam { get; set; }
        public List<int> TeamIDs { get; set; }
        public MDO.Owner Owner { get; set; }
        private static List<RunInfo> RunInfos;
        public LogInfo LogInstance { get; set; }

        public FxbvtIntegration()
        {
            ConnectToMadDog("VSULAB");
            Owner = new MDO.Owner("VSULAB");
        }

        public FxbvtIntegration(string release, string releaseType, string testPatchType, string testTeam, List<int> teamIDs, string wipKBNumber, string owner = "VSULAB")
        {
            ReleaseType = releaseType;
            Release = ReleaseType == "B" ? release : release + "D";
            ReleaseOriginalForm = Release.ToUpperInvariant().Replace("REL", "20").Replace("-", ".") + ReleaseType;
            TestPatchType = testPatchType;
            TestTeam = testTeam;
            TeamIDs = teamIDs;
            WIPKBNumber = wipKBNumber;
            ConnectToMadDog(owner);
            Owner = new MDO.Owner(owner);
            LogInfo.CreateInstance("FxbvtMTPAutomation");
            LogInstance = LogInfo.Instance;
        }

        public void Kickoff()
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                MDO.RunManagement.RMValidatorType dateValidatorType = new MDO.RunManagement.RMValidatorType(2);
                MDO.RunManagement.RMValidatorType timeValidatorType = new MDO.RunManagement.RMValidatorType(1);
                MDO.RunManagement.RMValidator dateValidator = new MDO.RunManagement.RMValidator();
                MDO.RunManagement.RMValidator timeValidator = new MDO.RunManagement.RMValidator();

                dateValidator.Name = string.Format("Start Date: {0}_{1}", Release, DateTime.Now.Ticks);
                dateValidator.Owner = Owner;
                dateValidator.ValidatorType = dateValidatorType;
                DateTime kickoffTime = TimeZoneInfo.ConvertTime(DateTime.Now.AddMinutes(10), TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
                dateValidator.Data = string.Format(@"<Schedule xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Years><Year>{0}</Year></Years><Months><Month>{1}</Month></Months><DaysOfMonth><DayOfMonth>{2}</DayOfMonth></DaysOfMonth></Schedule>", kickoffTime.Year, kickoffTime.Month, kickoffTime.Day);
                dateValidator.Save();
                timeValidator.Name = string.Format("Start Time: {0}_{1}", Release, DateTime.Now.Ticks);
                timeValidator.Owner = Owner;
                timeValidator.ValidatorType = timeValidatorType;
                timeValidator.Data = string.Format(@"<TimeOfDay xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Tolerance>15</Tolerance><Time>{0}</Time></TimeOfDay>", kickoffTime.ToString("Hmm"));
                timeValidator.Save();

                foreach (int teamID in TeamIDs)
                {
                    string teamName = db.FXBVTTeams.Where(x => x.ID == teamID).Select(x => x.TeamName).FirstOrDefault().Trim();
                    string scheduleName = string.Format("{0}: MTP {1}-{2}", TestTeam, Release, teamName);

                    MDO.RunManagement.RMSchedule rmSchedule = new MDO.RunManagement.RMSchedule();
                    rmSchedule.Name = scheduleName;
                    rmSchedule.Owner = Owner;

                    List<string> templates = db.FXBVTTeams.Where(x => x.ID == teamID && x.ReleaseType == ReleaseType).Select(x => x.Templates).FirstOrDefault().Split(',').ToList();
                    foreach (string templateId in templates)
                    {
                        MDO.RunManagement.RMRunTemplate template = new MDO.RunManagement.RMRunTemplate(int.Parse(templateId));
                        UpdateTemplate(teamName, template);
                        rmSchedule.AddRunTemplate(template);
                    }

                    rmSchedule.AddValidator(dateValidator);
                    rmSchedule.AddValidator(timeValidator);
                    rmSchedule.Save();

                    FXBVTSchedule fXBVTSchedule = new FXBVTSchedule();
                    fXBVTSchedule.ScheduleID = rmSchedule.ID.ToString();
                    fXBVTSchedule.Name = scheduleName;
                    fXBVTSchedule.Release = Release;
                    fXBVTSchedule.TeamID = teamID;
                    fXBVTSchedule.TestTeam = TestTeam;
                    fXBVTSchedule.StartTime = DateTime.Now;
                    db.FXBVTSchedules.InsertOnSubmit(fXBVTSchedule);
                    db.SubmitChanges();
                }
            }
        }

        public void AddRunsToSchedule(string scheduleID, List<string> runIDs)
        {
            MDO.RunManagement.RMSchedule rmSchedule = new MDO.RunManagement.RMSchedule(int.Parse(scheduleID));
            runIDs.ForEach(x => rmSchedule.AddRun(int.Parse(x)));
            rmSchedule.Save();
        }

        public void ChangeTokens(int queryID, string tokenName, string tokenValue)
        {
            MDO.QueryObject queryObject = new MDO.QueryObject(queryID);
            List<MDO.Run> runs = queryObject.GetCollection<MDO.Run>();
            foreach (MDO.Run run in runs)
            {
                run.InstallSelections.SetDefaultToken(tokenName, tokenValue);
                run.Save();
            }
        }

        public static List<RunInfo> GetMaddogRuns(List<string> scheduleIDs)
        {
            RunInfos = new List<RunInfo>();
            int totalTests = 0;
            int totalTestsExecuted = 0;
            int totalTestsPassed = 0;
            int totalTestsFailed = 0;
            int totalUnanalyzedFailures = 0;
            int totalTestsScenariosExecuted = 0;
            int totalTestsScenariosPassed = 0;
            int totalTestsScenariosFailed = 0;
            int totalScenarios = 0;
            foreach (string scheduleID in scheduleIDs)
            {
                MDO.RunManagement.RMSchedule rmSchedule = new MDO.RunManagement.RMSchedule(int.Parse(scheduleID));
                List<MDO.Run> runs = rmSchedule.GetAllRuns;

                foreach (MDO.Run run in runs)
                {
                    RunInfo info = new RunInfo();
                    info.Team = GetTeamNameByScheduleID(scheduleID);
                    info.ScheduleID = scheduleID;
                    info.Title = run.Title;
                    if (info.Title.ToLowerInvariant().StartsWith("clone of"))
                        continue;
                    info.ID = run.ID.ToString();
                    MDO.QueryObject queryObject = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.Results);
                    queryObject.QueryAdd("RunID", "=", run.ID);
                    queryObject.QueryAdd("ResultTypeID", "=", 2);
                    queryObject.QueryAdd("AnalyzedBit", "=", 0);
                    int UnanalyzedFailures = queryObject.Count;
                    MatchCollection dataCollection = Regex.Matches(run.GetResultsSummary().Replace(",", ""), @"([1-9]\d*(\.[0-9]*[0-9])?)|(0\.\d*[0-9])|[0]");
                    if (dataCollection.Count == 0)
                    {
                        continue;
                    }
                    info.TestsExecuted = Int32.Parse(dataCollection[0].ToString()) + Int32.Parse(dataCollection[2].ToString());//executed=passed + failed
                    totalTestsExecuted += Int32.Parse(dataCollection[0].ToString()) + Int32.Parse(dataCollection[2].ToString());
                    if (run.RunState.Name == "Stopped" && info.TestsExecuted == 0)
                    {
                        continue;
                    }
                    info.TestsPassRate = float.Parse(dataCollection[0].ToString()) / (float.Parse(dataCollection[0].ToString()) + float.Parse(dataCollection[2].ToString()));//Pass Rate=(passed/(passed+failed))
                    info.TestsPassed = Int32.Parse(dataCollection[0].ToString());
                    totalTestsPassed += Int32.Parse(dataCollection[0].ToString());
                    info.TestsFailed = Int32.Parse(dataCollection[2].ToString());
                    totalTestsFailed += Int32.Parse(dataCollection[2].ToString());
                    info.UnanalyzedFailures = UnanalyzedFailures;
                    totalUnanalyzedFailures += UnanalyzedFailures;
                    info.TestsScenariosPassRate = float.Parse(dataCollection[16].ToString()) / float.Parse(dataCollection[20].ToString());// Scenarios Pass Rate=(Scenarios Passed/Scenarios Total)
                    totalScenarios += Int32.Parse(dataCollection[20].ToString());
                    info.TestsScenariosExecuted = Int32.Parse(dataCollection[16].ToString()) + Int32.Parse(dataCollection[18].ToString());//Scenarios Pass Executed=(Scenarios passed + Scenarios failed)
                    totalTestsScenariosExecuted += Int32.Parse(dataCollection[16].ToString()) + Int32.Parse(dataCollection[18].ToString());
                    info.TestsScenariosPassed = Int32.Parse(dataCollection[16].ToString());
                    totalTestsScenariosPassed += Int32.Parse(dataCollection[16].ToString());
                    info.TestsScenariosFailed = Int32.Parse(dataCollection[18].ToString());
                    totalTestsScenariosFailed += Int32.Parse(dataCollection[18].ToString());
                    info.CompletionRate = (float.Parse(dataCollection[0].ToString()) + float.Parse(dataCollection[2].ToString())) / float.Parse(dataCollection[12].ToString());//completion rate=(passed+failed)/Total
                    totalTests += Int32.Parse(dataCollection[12].ToString());
                    info.TestsTotal = Int32.Parse(dataCollection[12].ToString());
                    info.ScenariosTotal = Int32.Parse(dataCollection[20].ToString());
                    RunInfos.Add(info);
                }
            }

            return RunInfos;
        }

        public void ProcessMonitor(string testTeam)
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                List<string> scheduleIDs = db.FXBVTSchedules.Where(x => x.TestTeam == testTeam && x.StartTime > DateTime.Now.AddDays(-21)).Select(x => x.ScheduleID).ToList();
                foreach (string scheduleID in scheduleIDs)
                {
                    LogInstance.LogScenario($"Processing schedule {scheduleID} ...");
                    MDO.RunManagement.RMSchedule rmSchedule = new MDO.RunManagement.RMSchedule(int.Parse(scheduleID));
                    foreach (Run run in rmSchedule.GetAllRuns)
                    {
                        if (run.RunState.Name == "Running" || run.RunState.Name == "Execution Completed")
                        {
                            if (!db.FXBVTFailedCases.Where(x => x.RunID == run.ID && x.IsMailSent).Any())
                            {
                                LogInstance.LogMessage($"Processing Run {run.ID} ...");
                                try
                                {
                                    ResetFailedTests(run, testTeam);
                                }
                                catch (Exception ex)
                                {
                                    LogInstance.LogError($"Failed to reset cases! Error: {ex.Message}");
                                    LogInstance.LogError(ex.StackTrace);
                                }
                            }
                        }
                        if (run.RunState.Name == "Running" && testTeam == "FXBVT")
                        {
                            if (!db.FXBVTBlockedRuns.Where(x => x.RunID == run.ID).Any())
                            {
                                try
                                {
                                    CheckMachineStatus(run);
                                }
                                catch (Exception ex)
                                {
                                    LogInstance.LogError($"Failed to check machine status! Error: {ex.Message}");
                                    LogInstance.LogError(ex.StackTrace);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ResetFailedTests(Run run, string testTeam)
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                QueryObject queryObject = new QueryObject(QueryConstants.BaseObjectTypes.Results);
                queryObject.QueryAdd("RunID", "=", run.ID);
                queryObject.QueryAdd("ResultTypeID", "=", 2);
                queryObject.QueryAdd("AnalyzedBit", "=", 0);
                queryObject.DisplayFields = new string[] { Tables.ResultTable.TESTCASEIDFIELD, Tables.ResultTable.TESTCASENAMEFIELD, Tables.ResultTable.CONTEXTIDFIELD, Tables.ResultTable.CONTEXTNAMEFIELD, Tables.ResultTable.RESETCOUNTFIELD, Tables.ResultTable.MACHINEIDFIELD };
                if (queryObject.Count == 0)
                {
                    return;
                }
                else
                {
                    LogInstance.LogMessage($"Run {run.ID}: {queryObject.Count} failed case detected");
                }
                List<int> machineIDs = GetMachinesCurrentlyInUse(run);
                DataTable failedTestTable = queryObject.GetDataSet().Tables[0];
                foreach (DataRow row in failedTestTable.Rows)
                {
                    int resetCount = int.Parse(row["ResetCount"].ToString());
                    int caseID = int.Parse(row["TestcaseID"].ToString());
                    string caseName = row["TestcaseName"].ToString();
                    int contextID = int.Parse(row["ContextID"].ToString());
                    string contextName = row["ContextName"].ToString();
                    Result result = new Result(run.ID, contextID, caseID);
                    var presentFailedCases = db.FXBVTFailedCases.Where(x => x.RunID == run.ID && x.TestCaseID == caseID && x.ContextID == contextID);
                    //Insert new failed case
                    if (!presentFailedCases.Any())
                    {
                        FXBVTFailedCase fXBVTFailedCase = new FXBVTFailedCase();
                        fXBVTFailedCase.RunID = run.ID;
                        fXBVTFailedCase.TestCaseID = caseID;
                        fXBVTFailedCase.TestCaseName = caseName;
                        fXBVTFailedCase.ContextID = contextID;
                        fXBVTFailedCase.ContextName = contextName;
                        fXBVTFailedCase.ResetCount = resetCount;
                        fXBVTFailedCase.IsMailSent = false;
                        db.FXBVTFailedCases.InsertOnSubmit(fXBVTFailedCase);
                    }
                    //Update reset count for present failed case
                    else
                    {
                        FXBVTFailedCase failedCase = presentFailedCases.FirstOrDefault();
                        failedCase.ResetCount = resetCount;
                    }
                    db.SubmitChanges();
                    if (resetCount < 3)
                    {
                        try
                        {
                            LogInstance.LogMessage($"Run {run.ID}: Reset test case: {caseID} {caseName} context: {contextID} {contextName}. Current reset count: {resetCount}");
                            int previousUsedMachineID = int.Parse(row["MachineID"].ToString());
                            if (!machineIDs.Contains(previousUsedMachineID))
                            {
                                previousUsedMachineID = machineIDs.FirstOrDefault();
                            }
                            ResultResetOptions resetOptions = new ResultResetOptions(TestcaseFailureReason.UnknownReason, previousUsedMachineID);
                            result.Reset(resetOptions);
                        }
                        catch (Exception ex)
                        {
                            LogInstance.LogError($"Error when reset case! Skip to next item.\r\n {ex.Message}");
                            LogInstance.LogError(ex.StackTrace);
                            continue;
                        }
                    }
                    else
                    {
                        try
                        {
                            LogInstance.LogMessage($"Run {run.ID}: {caseID} {caseName} context: {contextID} {contextName} Still failed after 3 reset");
                            FXBVTFailedCase failedCase = presentFailedCases.FirstOrDefault();
                            failedCase.ResetCount = resetCount;
                            db.SubmitChanges();
                            if (IsRunExecutionFinished(run))
                            {
                                SendMailForFailedTests(run, testTeam);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogInstance.LogError($"Error when send mail for failed case! Skip to next item.\r\n {ex.Message}");
                            LogInstance.LogError(ex.StackTrace);
                            continue;
                        }
                    }
                }
            }
        }

        public List<int> GetMachinesCurrentlyInUse(Run run)
        {
            List<int> machineIDs = new List<int>();
            QueryObject queryObject = new QueryObject(QueryConstants.BaseObjectTypes.Machines);
            queryObject.QueryAdd("RunID", "=", run.ID);
            queryObject.DisplayFields = new string[] { Tables.MachineTable.IDFIELD };
            DataTable machineTable = queryObject.GetDataSet().Tables[0];
            foreach (DataRow row in machineTable.Rows)
            {
                machineIDs.Add(int.Parse(row[Tables.MachineTable.IDFIELD].ToString()));
            }
            return machineIDs;
        }

        public bool IsRunExecutionFinished(Run run)
        {
            QueryObject totalCaseQuery = new QueryObject(QueryConstants.BaseObjectTypes.Results);
            totalCaseQuery.QueryAdd("RunID", "=", run.ID);
            QueryObject passedCaseQuery = new QueryObject(QueryConstants.BaseObjectTypes.Results);
            passedCaseQuery.QueryAdd("RunID", "=", run.ID);
            passedCaseQuery.QueryAdd("ResultTypeID", "=", 1);
            QueryObject failedCaseQuery = new QueryObject(QueryConstants.BaseObjectTypes.Results);
            failedCaseQuery.QueryAdd("RunID", "=", run.ID);
            failedCaseQuery.QueryAdd("ResultTypeID", "=", 2);
            if (totalCaseQuery.Count == passedCaseQuery.Count + failedCaseQuery.Count)
            {
                QueryObject queryObject = new QueryObject(QueryConstants.BaseObjectTypes.Results);
                queryObject.QueryAdd("RunID", "=", run.ID);
                queryObject.QueryAdd("ResultTypeID", ">=", 2);
                queryObject.QueryAdd("ResultTypeID", "<=", 4);
                queryObject.QueryAdd("AnalyzedBit", "=", 0);
                queryObject.QueryAdd("ResetCount", "<", 3);
                if (queryObject.Count == 0)
                {
                    using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
                    {
                        List<FXBVTFailedCase> failedCaseList = db.FXBVTFailedCases.Where(x => x.RunID == run.ID && !x.IsMailSent && x.ResetCount >= 3).ToList();
                        if (failedCaseList.Count == failedCaseQuery.Count)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void CheckMachineStatus(Run run, string testTeam = "FXBVT")
        {
            if (run.StartTime.AddHours(2) > DateTime.Now)
                return;
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                QueryObject queryObject = new QueryObject(QueryConstants.BaseObjectTypes.Machines);
                queryObject.QueryAdd("CurrentRunID", "=", run.ID);
                if (queryObject.Count == 0)
                {
                    if (!db.FXBVTBlockedRuns.Where(x => x.RunID == run.ID && x.BlockReason == "LackOfMachines").Any())
                    {
                        FXBVTBlockedRun data = new FXBVTBlockedRun();
                        data.RunID = run.ID;
                        data.BlockReason = "LackOfMachines";
                        db.FXBVTBlockedRuns.InsertOnSubmit(data);
                        db.SubmitChanges();
                        MailHelper mailHelper = new MailHelper();
                        mailHelper.SendMailAboutAbnormalRun(run, 0, "LackOfMachines", testTeam);
                        LogInstance.LogMessage($"Run {run.ID} {run.Title} didn't get test machines after 2 hours");
                    }
                }
                else
                {
                    queryObject.QueryAdd("MachineStates.MachineStateName", "=", "Hung/Broken");
                    queryObject.DisplayFields = new string[] { Tables.ResultTable.MACHINEIDFIELD };
                    foreach (DataRow row in queryObject.GetDataSet().Tables[0].Rows)
                    {
                        int machineID = int.Parse(row["MachineID"].ToString());
                        if (!db.FXBVTBlockedRuns.Where(x => x.RunID == run.ID && x.HBMachineID == machineID && x.BlockReason == "MachineH/B").Any())
                        {
                            FXBVTBlockedRun data = new FXBVTBlockedRun();
                            data.RunID = run.ID;
                            data.HBMachineID = machineID;
                            data.BlockReason = "MachineH/B";
                            db.FXBVTBlockedRuns.InsertOnSubmit(data);
                            db.SubmitChanges();
                            MailHelper mailHelper = new MailHelper();
                            mailHelper.SendMailAboutAbnormalRun(run, machineID, "MachineH/B", testTeam);
                            LogInstance.LogMessage($"Machine {machineID} turned into Hung/Broken. Run ID: {run.ID}");
                        }
                    }
                }
            }
        }

        public void SendMailForFailedTests(Run run, string testTeam)
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                try
                {
                    LogInstance.LogMessage($"Send abnormal mail for run {run.ID} {run.Title}");
                    List<FXBVTFailedCase> failedCaseList = db.FXBVTFailedCases.Where(x => x.RunID == run.ID && !x.IsMailSent && x.ResetCount >= 3).ToList();
                    MailHelper mailHelper = new MailHelper();
                    mailHelper.SendMailForFailedTests(run, failedCaseList, testTeam);
                    failedCaseList.ForEach(x => x.IsMailSent = true);
                    db.SubmitChanges();
                }
                catch (Exception ex)
                {
                    LogInstance.LogError($"Failed to send mail! Error: {ex.Message}");
                    LogInstance.LogError(ex.StackTrace);
                }
            }
        }

        public void SendSummaryMail()
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                string currentRelease = db.FXBVTSchedules.Where(x => x.TestTeam == "FXBVT").OrderByDescending(x => x.ID).FirstOrDefault().Release;
                Dictionary<int, List<string>> teamSchedulesPairs = new Dictionary<int, List<string>>();
                foreach (FXBVTSchedule item in db.FXBVTSchedules.Where(x => x.Release == currentRelease && x.TestTeam == "FXBVT").ToList())
                {
                    if (teamSchedulesPairs.ContainsKey(item.TeamID))
                    {
                        teamSchedulesPairs[item.TeamID].Add(item.ScheduleID);
                    }
                    else
                    {
                        teamSchedulesPairs.Add(item.TeamID, new List<string>() { item.ScheduleID });
                    }
                }

                foreach (KeyValuePair<int, List<string>> teamSchedules in teamSchedulesPairs)
                {
                    if (db.FXBVTSchedules.Where(x => teamSchedules.Value.Contains(x.ScheduleID)).Select(x => x.IsSummaryMailSent).Contains(true))
                        continue;
                    if (CheckSchedulesExecuting(teamSchedules.Value))
                        continue;
                    try
                    {
                        Dictionary<Run, List<FXBVTFailedCase>> runFailedCasePairs = GetFailedCasesFromSchedules(teamSchedules.Value);
                        if (runFailedCasePairs.Count > 0)
                        {
                            string teamName = db.FXBVTTeams.Where(x => x.ID == teamSchedules.Key).FirstOrDefault().TeamName;
                            MailHelper mailHelper = new MailHelper();
                            mailHelper.SendSummaryMailForFailedTests(teamName, runFailedCasePairs);
                        }
                        List<FXBVTSchedule> schedules = db.FXBVTSchedules.Where(x => teamSchedules.Value.Contains(x.ScheduleID)).ToList();
                        schedules.ForEach(x => x.IsSummaryMailSent = true);
                        db.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        LogInstance.LogError($"Failed to send summary mail! Error: {ex.Message}");
                        LogInstance.LogError(ex.StackTrace);
                    }
                }
            }
        }

        public bool CheckSchedulesExecuting(List<string> scheduleIDs)
        {
            foreach (string scheduleID in scheduleIDs)
            {
                RMSchedule rmSchedule = new RMSchedule(int.Parse(scheduleID));
                foreach (Run run in rmSchedule.GetAllRuns)
                {
                    if (run.Running)
                        return true;
                    if (run.RunState.Name == "Execution Completed")
                    {
                        if (!IsRunExecutionFinished(run))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public Dictionary<Run, List<FXBVTFailedCase>> GetFailedCasesFromSchedules(List<string> scheduleIDs)
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                Dictionary<Run, List<FXBVTFailedCase>> runFailedCasePairs = new Dictionary<Run, List<FXBVTFailedCase>>();
                foreach (string scheduleID in scheduleIDs)
                {
                    RMSchedule rmSchedule = new RMSchedule(int.Parse(scheduleID));
                    foreach (Run run in rmSchedule.GetAllRuns)
                    {
                        List<FXBVTFailedCase> failedCaseList = db.FXBVTFailedCases.Where(x => x.RunID == run.ID && x.ResetCount >= 3).ToList();
                        if (failedCaseList.Count > 0)
                        {
                            runFailedCasePairs.Add(run, failedCaseList);
                        }
                    }
                }
                return runFailedCasePairs;
            }
        }
        private void UpdateTemplate(string teamName, MDO.RunManagement.RMRunTemplate template)
        {
            template.Owner = Owner;
            string title = string.Format("{0} {1} $OSName $OSArchitecture", template.Name, Release);
            template.Data.StaticFields.Title = title;
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(template.Data.StaticFields.Selections);
            XmlNode parentNode = xml.SelectSingleNode("//Selections/Input/Defaults");
            xml.SelectSingleNode("//Selections/Input/Defaults/Token[@Name='RunTitle']").InnerText = title;
            XmlNode mtpNode = xml.SelectSingleNode("//Selections/Input/Defaults/Token[@Name='MTP']");
            XmlNode patchTypeNode = xml.SelectSingleNode("//Selections/Input/Defaults/Token[@Name='TestPatchType']");
            XmlNode wipKBNode = xml.SelectSingleNode("//Selections/Input/Defaults/Token[@Name='WIPKBNumber']");
            XmlNode release = xml.SelectSingleNode("//Selections/Input/Defaults/Token[@Name='ReleaseOriginalForm']");
            if (mtpNode != null)
            {
                mtpNode.InnerText = Release;
            }
            else
            {
                parentNode.AppendChild(CreateMissingToken(xml, "MTP", Release));
            }
            if (patchTypeNode != null)
            {
                patchTypeNode.InnerText = TestPatchType;
            }
            else
            {
                parentNode.AppendChild(CreateMissingToken(xml, "TestPatchType", TestPatchType));
            }
            if (wipKBNode != null)
            {
                wipKBNode.InnerText = WIPKBNumber;
            }
            else
            {
                parentNode.AppendChild(CreateMissingToken(xml, "WIPKBNumber", WIPKBNumber));
            }
            if (release != null)
            {
                release.InnerText = ReleaseOriginalForm;
            }
            else
            {
                parentNode.AppendChild(CreateMissingToken(xml, "ReleaseOriginalForm", ReleaseOriginalForm));
            }
            template.Data.StaticFields.Selections = xml.InnerXml;

            template.Save();
        }
        private static string GetTeamNameByScheduleID(string id)
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                int teamID = db.FXBVTSchedules.Where(x => x.ScheduleID == id).FirstOrDefault().TeamID;
                return db.FXBVTTeams.Where(x => x.ID == teamID).FirstOrDefault().TeamName;
            }
        }
        private void ConnectToMadDog(string strOwner)
        {
            try
            {
                string strUserName = strOwner;
                if (!string.IsNullOrEmpty(strOwner) && (strOwner.IndexOf("\\") >= 0))
                    strUserName = strOwner.Split('\\')[1];

                MDO.Utilities.Security.AppName = "Setup";
                MDO.Utilities.Security.AppOwner = strUserName;
                MDO.Utilities.Security.SetDB("MDSQL3.corp.microsoft.com", "OrcasTS");
                MDO.Branch.CurrentBranch = new MDO.Branch(536);

            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private XmlNode CreateMissingToken(XmlDocument xml, string key, string value)
        {
            XmlNode node = xml.CreateNode(XmlNodeType.Element, "Token", "");
            XmlAttribute attr = xml.CreateAttribute("Name");
            attr.Value = key;
            node.Attributes.Append(attr);
            node.InnerText = value;
            return node;
        }
    }
}
