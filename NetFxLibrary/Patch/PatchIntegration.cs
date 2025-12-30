using DefinitionInterpreter;
using NetFxSetupLibrary;
using Newtonsoft.Json;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using MDO = MadDogObjects;

namespace NetFxSetupLibrary.Patch
{
    public class PatchIntegration
    {
        private const string ownerName = "vsulab";
        public int TargetProductCPUID { get; set; }
        public string MatrixName { get; set; }
        public string PatchTechnology { get; set; }
        public bool AddStrongNameHijackPackage { get; set; }
        public long JobID { get; set; }
        public int NetFxSetupPatchInfoID { get; set; }

        private bool _THTestFlag;
        private string _THTestTargetProductName;

        public PatchIntegration(int targetProductCPUID, string matrixName, string patchTechnology, bool addStrongNameHijackPackage = false)
        {
            TargetProductCPUID = targetProductCPUID;
            MatrixName = matrixName;
            PatchTechnology = patchTechnology;
            AddStrongNameHijackPackage = addStrongNameHijackPackage;
            _THTestFlag = false;
        }

        public void OperatePatchRuns(bool isContainLDR, bool isHotfix, Dictionary<string, string> fileVersionDic, string runTitlePrefix, InputData inputData, long jobId, int netFxSetupPatchInfoID)
        {
            JobID = jobId;
            NetFxSetupPatchInfoID = netFxSetupPatchInfoID;

            List<InputDataItem> forHotFixOrGDRData = new List<InputDataItem>(inputData.Data);
            forHotFixOrGDRData.Add(new InputDataItem { FieldName = "PayloadType", FieldValue = "GDR", FieldType = "EnvironmentVariable" });

            string runTitle = runTitlePrefix + "-GDR-";
            if (isHotfix)
            {
                //runTitle = runTitlePrefix + "-Hotfix-";
                forHotFixOrGDRData.Add(new InputDataItem { FieldName = "VersionFilePath", FieldValue = fileVersionDic.FirstOrDefault(p => p.Key == "LDR").Value, FieldType = "EnvironmentVariable" });
            }
            else
            {
                forHotFixOrGDRData.Add(new InputDataItem { FieldName = "VersionFilePath", FieldValue = fileVersionDic.FirstOrDefault(p => p.Key == "GDR").Value, FieldType = "EnvironmentVariable" });
            }
            KickOffRuns(forHotFixOrGDRData, runTitle);

            if (isContainLDR && !PatchTechnology.Equals("CBS"))
            {
                runTitle = runTitlePrefix + "-LDR-";
                List<InputDataItem> forLDRData = new List<InputDataItem>(inputData.Data);
                forLDRData.Add(new InputDataItem { FieldName = "PayloadType", FieldValue = "LDR", FieldType = "EnvironmentVariable" });
                forLDRData.Add(new InputDataItem { FieldName = "VersionFilePath", FieldValue = fileVersionDic.FirstOrDefault(p => p.Key == "LDR").Value, FieldType = "EnvironmentVariable" });
                KickOffRuns(forLDRData, runTitle);
            }

        }

        public void OperatePatchRuns(string runTitlePrefix, InputData inputData, int thTestID, int parameterPathID, string defaultProductName)
        {
            _THTestFlag = true;
            _THTestTargetProductName = defaultProductName;

            JobID = thTestID;
            NetFxSetupPatchInfoID = parameterPathID;

            KickOffRuns(inputData.Data, runTitlePrefix);
        }

        private void KickOffRuns(List<InputDataItem> mdSelectionData, string runTitle)
        {
            //connect to maddog
            ConnectToMadDog(ownerName);
            List<TTestMatrix> lstMatrixes = PrepareTestMatrix();
            int count = 0;
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                foreach (TTestMatrix tm in lstMatrixes)
                {

                    //count = 0;
                    List<InputDataItem> inputDataItems = new List<InputDataItem>(mdSelectionData);
                    if ((runTitle.Contains("-ProductRefresh") && tm.TestCaseID == 2074922) || (runTitle.Contains("-ProductRefresh") && count == 1 && runTitle.Contains("x86")))
                    {
                        continue;
                    }
                    if (runTitle.Contains("-ProductRefresh"))
                    {
                        //AddInfoForPr(inputDataItems, count, runTitle);
                        if (inputDataItems.Any(p=>p.FieldValue == "4.8"))
                        {
                            inputDataItems.Add(new InputDataItem { FieldName = "Command", FieldValue = CreateBat(inputDataItems), FieldType = "Command" });
                        }
                    }
                    count++;
                    string prdValue = "";
                    foreach (var md in inputDataItems)
                    {
                        if (md.FieldName == "Product_File")
                        {
                            prdValue = md.FieldValue;
                        }
                    }
                    if (tm.MDOSID == 339 || tm.MDOSID == 211)
                    {
                        if (prdValue.Contains("NDP461.xml") || prdValue.Contains("NDP462.xml") ||
                             prdValue.Contains("NDP47.xml") || prdValue.Contains("NDP471.xml") ||
                             prdValue.Contains("NDP472.xml") || prdValue.Contains("NDP461_LatestHFR.xml"))
                            continue;
                    }
                    ConnectToMad connectToMad = new ConnectToMad()
                    {
                        RunTitle = runTitle,
                        PatchTech = PatchTechnology,
                        AddStrongNameToPackage = AddStrongNameHijackPackage,
                        THTestTargetProductName = _THTestTargetProductName,
                        JobId = JobID,
                        NetFxSetupPatchID = NetFxSetupPatchInfoID
                    };

                    SqlInfo sqlInfo = new SqlInfo()
                    {
                        TestCaseID = tm.TestCaseID,
                        MDOSID = tm.MDOSID,
                        ProductCPUID = tm.ProductCPUID,
                        OSImageID = tm.OSImageID,
                        TestMatrixName = tm.TestMatrixName,
                        ProductID = tm.ProductID
                    };


                    HttpClient client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(120);
                    //string url = "https://localhost:44355/api/KickoffRuntime";
                    //string url = "http://localhost:8081/api/KickoffRuntime";
                    string url = ConfigurationManager.AppSettings["ConnectToMadUrl"] + "api/KickoffRuntime";
                    var request = new KickoffRequest
                    {
                        tm = sqlInfo,
                        mdSelectionData = inputDataItems,
                        parameter = connectToMad

                    };

                    try
                    {
                        string json = JsonConvert.SerializeObject(request);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = client.PostAsync(url, content).Result;
                        response.EnsureSuccessStatusCode();

                        string result = response.Content.ReadAsStringAsync().Result;
                    }
                    catch (HttpRequestException e)
                    {
                       
                    }

                    #region


                    //    MDO.Run objRun = new MDO.Run();

                    //    //mapping test case id with test case query
                    //    var mappedCase = db.TMDQueryMappings.Where(p => p.MatrixTestcaseID == tm.TestCaseID && p.Technology == PatchTechnology).First();                  

                    //    MDO.QueryObject testcaseQuery = new MDO.QueryObject(mappedCase.QueryID);
                    //    objRun.TestcaseQuery = testcaseQuery;

                    //    //Set run title
                    //    objRun.Title = runTitle + mappedCase.TestcaseDesc;

                    //    //context query: default context
                    //    MDO.QueryObject contextQuery = new MDO.QueryObject(675434);
                    //    objRun.ContextQuery = contextQuery;

                    //    bool Is2003IAOS = db.TOs.Where(p => p.MDOSID == tm.MDOSID && p.OSCPUID == 3 && p.OSVersion == "5.2").Count() > 0;
                    //    bool IsWin8ARM = db.TOs.Where(p => p.MDOSID == tm.MDOSID && p.OSCPUID == 4 && p.OSVersion == "6.2").Count() > 0;
                    //    bool IsArmOS = tm.ProductCPUID == 4;

                    //    #region 2012R2 image doesn't have S14 installed, which should be installed before running test
                    //    bool is2012R2 = db.TOs.Where(p => p.MDOSID == tm.MDOSID && p.OSName.Contains("Blue")).Count() > 0;
                    //    //if (is2012R2)
                    //    //{
                    //    //    mdSelectionData.Insert(0, new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
                    //    //    mdSelectionData.Insert(0, new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["BluePreReq"].ToString(), FieldType = "Command" });
                    //    //}
                    //    #endregion



                    //    objRun.Owner = new MDO.Owner(ownerName);
                    //    objRun.AnalysisProfile = new MDO.AnalysisProfile(17);
                    //    objRun.AutoReserveMaxMachines = 2;
                    //    objRun.MaxMachines = 2;
                    //    //objRun.RunTimeOut = ((IsArmOS) ? 72 : 200);
                    //    objRun.RunTimeOut = 96;
                    //    objRun.Reimage = true;
                    //    objRun.Priority = MDO.Prioritization.enuPriority.VeryImportant;
                    //    objRun.OS = new MDO.OS(tm.MDOSID);
                    //    int mdOSImage = (int)db.TOSImages.Where(p => p.OSImageID == tm.OSImageID).First().MaddogOSImageID;
                    //    objRun.OSImage = new MDO.OSImage(mdOSImage);
                    //    objRun.Branch = new MDO.Branch(536);
                    //    SetRunMachineQuery(objRun, tm);
                    //    //Set machine optitons
                    //    var os = db.TOs.Where(p => p.MDOSID == tm.MDOSID).First();
                    //    //string osName = os.OSName;
                    //    //if (string.Equals(os.OSVersion, "10.0"))
                    //    //{
                    //    //    var osImage = db.TOSImages.Where(p => p.OSImageID == tm.OSImageID).First();
                    //    //    osName = osImage.OSSPLevel;
                    //    //    objRun.MachineOptions = Helper.MDMachineOptions.GetMDMachineOptions(osName);
                    //    //}

                    //    if (AddStrongNameHijackPackage)
                    //    {
                    //        MDO.Package objMDOPackageAddStrongNameHijack = new MDO.Package(10494);
                    //        Selection objSelectionAddStrongNameHijack = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageAddStrongNameHijack);
                    //        objSelectionAddStrongNameHijack.SetToken("CommandLine", ConfigurationManager.AppSettings["Preinstall"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionAddStrongNameHijack);
                    //    }

                    //    if (!IsArmOS)
                    //    {
                    //        if (!os.OSName.Contains("Core"))
                    //        {
                    //            //MDO.Package objMDOPackageUninstall47 = new MDO.Package(14188);
                    //            MDO.Package objMDOPackageUninstall47 = new MDO.Package(14565);
                    //            Selection objSelectionCommandUninstall47 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageUninstall47);
                    //            objRun.InstallSelections.InputSequence.Add(objSelectionCommandUninstall47);
                    //        }

                    //        //Add DevDiv-LUA-OFF
                    //        //MDO.Package objMDOPackageDevDivLUAOFF = new MDO.Package(8152);
                    //        //Selection objSelectionDevDivLUAOFF = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageDevDivLUAOFF);
                    //        //objRun.InstallSelections.InputSequence.Add(objSelectionDevDivLUAOFF);

                    //        MDO.Package objMDONugetConfigPath = new MDO.Package(10494);
                    //        Selection objSelectionCommandNugetConfigPath = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDONugetConfigPath);
                    //        objSelectionCommandNugetConfigPath.SetToken("CommandLine", ConfigurationManager.AppSettings["NugetConfigPath"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionCommandNugetConfigPath);

                    //        MDO.Package objMDONugetPackagePath = new MDO.Package(10494);
                    //        Selection objSelectionCommandNugetPackagePath = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDONugetPackagePath);
                    //        objSelectionCommandNugetPackagePath.SetToken("CommandLine", ConfigurationManager.AppSettings["NugetPackagePath"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionCommandNugetPackagePath);             

                    //        MDO.Package objMDOPackageInstallD3DCompiler = new MDO.Package(10494);
                    //        Selection objSelectionCommandInstallD3DCompiler = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageInstallD3DCompiler);
                    //        objSelectionCommandInstallD3DCompiler.SetToken("CommandLine", ConfigurationManager.AppSettings["InstallD3DCompiler"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionCommandInstallD3DCompiler);

                    //        MDO.Package disableWarningMDOPackage = new MDO.Package(10494);
                    //        Selection disableWarningSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(disableWarningMDOPackage);
                    //        disableWarningSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["DisableWarning"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(disableWarningSelectionCommand);

                    //        if (os.OSName.Contains("Core"))
                    //        {
                    //            MDO.Package objMDOPackage = new MDO.Package(10494);
                    //            Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    //            objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableNDP35OnServerCore"].ToString());
                    //            objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                    //        }

                    //        MDO.Package copyfilesMDOPackage = new MDO.Package(10494);
                    //        Selection copyfilesSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(copyfilesMDOPackage);
                    //        copyfilesSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["CopyFiles"].ToString());
                    //        copyfilesSelectionCommand.SetToken("SuccessfulExitCodes", ConfigurationManager.AppSettings["CopyFilesSuccessfulExitCodes"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(copyfilesSelectionCommand);

                    //        MDO.Package objMDOPackageDisableWastonLog = new MDO.Package(10494);
                    //        Selection objSelectionDisableWastonLog = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageDisableWastonLog);
                    //        objSelectionDisableWastonLog.SetToken("CommandLine", ConfigurationManager.AppSettings["DisableWastonLog"].ToString());
                    //        objSelectionDisableWastonLog.SetToken("SpawnWithNativeArchitecture", "True");
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionDisableWastonLog);


                    //        //if ((count == 0 && tm.OSImageID == 2260) || (count == 0 && tm.OSImageID == 2261))
                    //        //{
                    //        //    MDO.Package installEKBPackage = new MDO.Package(10494);
                    //        //    Selection installEKBSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(installEKBPackage);
                    //        //    installEKBSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["Upgrade2Win1022H2"].ToString());
                    //        //    objRun.InstallSelections.InputSequence.Add(installEKBSelectionCommand);
                    //        //    count++;
                    //        //}


                    //    }

                    //    if (_THTestFlag)
                    //    {
                    //        SetDisableWin10AU(objRun);

                    //        SetUseLatestCLR(objRun,is2012R2);

                    //        MDO.Package objMDOPackage = new MDO.Package(10420);
                    //        Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionCommand);

                    //        // Install the targetted product which is specified in matrix
                    //        if(!runTitle.Contains("-ProductRefresh"))
                    //        {
                    //            SetProductToInstallOnWin10(objRun, tm);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        EnableNetFx35InRun(objRun, db, inputDataItems, tm, Is2003IAOS, IsArmOS);
                    //        SetUseLatestCLR(objRun,is2012R2);
                    //    }

                    //    foreach (var item in inputDataItems)
                    //    {
                    //        if (item.FieldType.Equals("EnvironmentVariable"))
                    //        {
                    //            MDO.Package objMDOPackage = new MDO.Package(10428);
                    //            Selection objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    //            objSelection.SetToken("VariableName", item.FieldName);
                    //            //ARM cannot accept set a value as null or empty
                    //            objSelection.SetToken("VariableValue", string.IsNullOrEmpty(item.FieldValue) ? "Empty" : item.FieldValue);

                    //            objRun.InstallSelections.InputSequence.Add(objSelection);
                    //        }
                    //        else if (item.FieldType.Equals("Command"))
                    //        {
                    //            MDO.Package objMDOPackage = new MDO.Package(10494);
                    //            Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    //            objSelectionCommand.SetToken("CommandLine", item.FieldValue);

                    //            //Change copy bat return code from config, For special value.
                    //            if (item.FieldValue.Contains("CopyFiles.bat"))
                    //            {
                    //                objSelectionCommand.SetToken("SuccessfulExitCodes", ConfigurationManager.AppSettings["CopyFilesSuccessfulExitCodes"].ToString());
                    //            }

                    //            objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                    //        }
                    //        else if (item.FieldType.Equals("Reboot"))
                    //        {
                    //            MDO.Package objMDOPackage = new MDO.Package(10420);
                    //            Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    //            objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                    //        }
                    //    }
                    //    if(runTitle.Contains("4.7.2") )
                    //    {
                    //        MDO.Package objMDOPackageUnInstallNDP48 = new MDO.Package(10494);
                    //        Selection objSelectionCommandUnInstallNDP48 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageUnInstallNDP48);
                    //        objSelectionCommandUnInstallNDP48.SetToken("CommandLine", ConfigurationManager.AppSettings["UninstallNDP48"].ToString());
                    //        objRun.InstallSelections.InputSequence.Add(objSelectionCommandUnInstallNDP48);
                    //    }
                    //    objRun.Save();

                    //    if (IsArmOS)
                    //    {
                    //        objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = ConfigurationManager.AppSettings["TestSourcesLocation_ARM"].ToString();
                    //        //required binaries
                    //        objRun.InstallSelections.Defaults["TestReqBinPath"].Value = ConfigurationManager.AppSettings["RequiredBinariesLocation_ARM"].ToString();
                    //        //Test case flags - For CLR Drivers
                    //        objRun.Flags = new MDO.QueryObject(715949);
                    //    }
                    //    else
                    //    {
                    //        objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = ConfigurationManager.AppSettings["TestSourcesLocation"].ToString();
                    //        objRun.InstallSelections.Defaults["TestReqBinPath"].Value = @"\\cpvsbuild\drops\dev11\RTMRel\tst\50727.01\x86ret\suitebin";

                    //        // Driver flags for FXBVT Driver
                    //        objRun.Flags = new MDO.QueryObject(896247);
                    //    }

                    //    objRun.Save();

                    //    DefinitionInterpreter.Log.DebugMode = true;
                    //    DefinitionInterpreter.Log.Enabled = true;

                    //    objRun.GenerateInstallationSequence();
                    //    objRun.SetSecurityOnResultsFolder();
                    //    objRun.Save();

                    //    try
                    //    {
                    //        MDO.Run.RunHelpers.StartRun(objRun);
                    //    }
                    //    catch { }

                    //    if (_THTestFlag)
                    //    {
                    //        RecordTHRunInfo(db, objRun, tm);
                    //    }
                    //    else
                    //    {
                    //        RecordDownlevelRunInfo(db, objRun);
                    //    }
                    #endregion
                }

            }
        }

        private string CreateBat(string patch, string os)
        {
            string path = Path.Combine(ConfigurationManager.AppSettings["WorkFolder"], "Install" + os + ".bat");
            string[] lines =
            {
                "@echo off",
                "wusa.exe " + patch + " /quiet /norestart",
                "if errorlevel == 3010 exit 0"
            };
            File.WriteAllLines(path, lines);
            return path;
        }

        private string CreateBat(List<InputDataItem> inputData)
        {
            string path = Path.Combine(ConfigurationManager.AppSettings["BatPath"], "Install48.bat");
            string cmd = ConfigurationManager.AppSettings["ISVFor48"].ToString();
            cmd = cmd.Replace("04795.01", inputData.Where(p => p.FieldName == "BuildNumber").Select(p => p.FieldValue).First());
            string[] lines =
            {
                "@echo off",
                cmd,
                "if errorlevel == 3010 exit 0"
            };
            File.WriteAllLines(path, lines);
            return path;
        }

        //private void AddInfoForPr(List<InputDataItem> inputDatas, int count, string title)
        //{
        //    DownloadWSD wSD = new DownloadWSD();
        //    string arch = null;
        //    if (inputDatas.Any(p => p.FieldValue == "X86"))
        //        arch = "x86";
        //    else
        //        arch = "x64";
                    


        //    if (inputDatas.Any(p => p.FieldValue == "1709") && inputDatas.Any(p => p.FieldValue == "X86"))
        //    {
        //        string path = wSD.DownloadAndInstallPatches("17763", inputDatas.Where(p => p.FieldName == "Release").Select(p => p.FieldValue).First(), "4.8", arch);
        //        inputDatas.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["48RTM"], FieldType = "Command" });
        //        inputDatas.Add(new InputDataItem { FieldName = "Command", FieldValue = CreateBat(path, "Win101809-X86"), FieldType = "Command" });
        //    }
        //    else if (inputDatas.Any(p => p.FieldValue == "1709") && inputDatas.Any(p => p.FieldValue == "AMD64") && count == 0)
        //    {
        //        string path = wSD.DownloadAndInstallPatches("17763", inputDatas.Where(p => p.FieldName == "Release").Select(p => p.FieldValue).First(), "4.8", arch);
        //        inputDatas.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["48RTM"], FieldType = "Command" });
        //        inputDatas.Add(new InputDataItem { FieldName = "Command", FieldValue = CreateBat(path, "Win101809-X64"), FieldType = "Command" });

        //    }
        //    else if (inputDatas.Any(p => p.FieldValue == "1709") && inputDatas.Any(p => p.FieldValue == "AMD64") && count == 1)
        //    {
        //        inputDatas.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["48RTM"], FieldType = "Command" });
        //    }
        //}



        private void RecordDownlevelRunInfo(PatchTestDataClassDataContext db, MDO.Run objRun)
        {
            var runStatus = new TNetFxSetupRunStatus();
            runStatus.MDRunID = objRun.ID;
            runStatus.RunTitle = objRun.Title;
            runStatus.CreatedBy = objRun.Owner.Name;
            runStatus.CreatedDate = DateTime.Now;
            runStatus.LastModifiedBy = objRun.Owner.Name;
            runStatus.LastModifiedDate = DateTime.Now;
            runStatus.JobID = JobID;
            runStatus.RunStatusID = 1;
            runStatus.SubmissionID = NetFxSetupPatchInfoID;
            runStatus.SubmissionType = "Patch";

            db.TNetFxSetupRunStatus.InsertOnSubmit(runStatus);
            db.SubmitChanges();
        }

        private void RecordTHRunInfo(PatchTestDataClassDataContext db, MDO.Run objRun, TTestMatrix tm)
        {
            var runStatus = new TTHTestRunInfo();
            runStatus.MDRunID = objRun.ID;
            runStatus.THTestID = (int)JobID;
            runStatus.ParameterID = NetFxSetupPatchInfoID;
            runStatus.Title = objRun.Title;
            runStatus.OSImageID = tm.OSImageID;
            runStatus.TestCaseID = tm.TestCaseID;
            runStatus.RunStatusID = 1;
            runStatus.RunResultID = 4;
            runStatus.CreateDate = DateTime.Now;

            db.TTHTestRunInfos.InsertOnSubmit(runStatus);
            db.SubmitChanges();
        }

        private List<TTestMatrix> PrepareTestMatrix()
        {
            List<TTestMatrix> vTestMatrixes;
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                vTestMatrixes = (from tm in db.TTestMatrixes
                                 where tm.TestMatrixName == MatrixName
                                 && tm.MaddogDBID == 2  //TODO: Change this?
                                 && tm.Active == true
                                 && (TargetProductCPUID == 0 || tm.ProductCPUID == TargetProductCPUID)
                                 select tm).ToList();
            }
            return vTestMatrixes;
        }

        private void ConnectToMadDog(string strOwner)
        {
            try
            {
                string strUserName = strOwner;
                if (!string.IsNullOrEmpty(strOwner) && (strOwner.IndexOf("\\") >= 0))
                    strUserName = strOwner.Split('\\')[1]; //removing the domian Name

                MDO.Utilities.Security.AppName = "Setup";
                MDO.Utilities.Security.AppOwner = strUserName;
                MDO.Utilities.Security.SetDB("MDSQL3.corp.microsoft.com", "OrcasTS");
                //MDO.Branch.CurrentBranch = new MDO.Branch(536);

                //MDL.Utilities.Security.AppName = appApplicatonType.ToString();
                //MDL.Utilities.Security.AppOwner = strOwner;
                //MDL.Utilities.Security.SetDB(strExecutionSystemDatabaseName, strExecutionSystemName);
            }
            catch (Exception ex)
            {
                //Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, null, ex);
                throw (ex);
            }
        }

        private void EnableNetFx35InRun(MDO.Run objRun, PatchTestDataClassDataContext db, List<InputDataItem> mdSelectionData, TTestMatrix tm, bool Is2003IAOS, bool IsArmOS)
        {
            var win7MaxOSId = db.TOs.Where(p => p.OSVersion == "6.1").OrderByDescending(p => p.MDOSID).First().MDOSID;

            var os = db.TOs.FirstOrDefault(p => p.MDOSID == tm.MDOSID);

            //Win Server Core(for now, support 2008R2 server core only), Call 'EnableNDP35OnServerCore.bat'
            //WinBlue and above, Call 'EnableNetFx3Win8.bat'
            //IA64 2003 OS, install Dot Net Framework 4.0 and enable comptible with Dot Net Framework 3.5
            //ARM OS, use latest CLR
            //Others, Install Dot Net Framework 3.5 by package
            #region Make NDP3.5 is available in different OS

            InputDataItem productFile = mdSelectionData.FirstOrDefault(p => p.FieldName == "Product_File");

            if (os.OSName.Contains("Core"))
            {
                MDO.Package objMDOPackage = new MDO.Package(10494);
                Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableNDP35OnServerCore"].ToString());
                objRun.InstallSelections.InputSequence.Add(objSelectionCommand);

                //server core need a special test file
                if (productFile.FieldValue.Equals("NDP40.xml", StringComparison.OrdinalIgnoreCase))
                {
                    productFile.FieldValue = productFile.FieldValue.Replace(".xml", "-ServerCore.xml");
                }
            }
            else if (IsArmOS)
            {
                //we do nothing for ARM machine here since it will call Use latest CLR in following steps
                return;
            }
            else
            {
                if (productFile.FieldValue.Contains("-ServerCore.xml"))
                {
                    productFile.FieldValue = productFile.FieldValue.Replace("-ServerCore.xml", ".xml");
                }

                //For every msu run to make sure windows update service is enabled before case start.
                if (PatchTechnology.Equals("CBS"))
                {
                    MDO.Package objMDOPackage = new MDO.Package(10494);
                    Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableWUService"].ToString());
                    objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                }

                if (tm.MDOSID > win7MaxOSId)
                {
                    //Enable NetFx3 on os 8 and above
                    MDO.Package objMDOPackage = new MDO.Package(10494);
                    Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableNetFx3Win8"].ToString());
                    objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                }
                else
                {
                    //If IA64 2003 OS, install Dot Net Framework 4.0 and enable comptible with Dot Net Framework 3.5
                    if (Is2003IAOS)
                    {
                        MDO.Package objMDOPackage = new MDO.Package(10494);
                        Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                        objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["InstallNDP40ForIA64"].ToString());
                        objRun.InstallSelections.InputSequence.Add(objSelectionCommand);

                        //make that compatible with Net Framework 3.5
                        SetUseLatestCLR(objRun, false);
                    }
                    else
                    {
                        //Install Dot Net Framework 3.5
                        MDO.Package objMDOPackageNetFx35 = new MDO.Package(10381);
                        Selection objSelectionNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageNetFx35);
                        objRun.InstallSelections.InputSequence.Add(objSelectionNetFx35);
                    }
                }
            }
            #endregion
        }

        private void SetUseLatestCLR(MDO.Run objRun, bool isWinBlue)
        {
            MDO.Package objMDOPackageCompatibleNetFx35 = new MDO.Package(10427);
            Selection objSelectionCompatibleNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageCompatibleNetFx35);
            objSelectionCompatibleNetFx35.SetToken("File", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString());
            objSelectionCompatibleNetFx35.SetToken("SpawnWithNativeArchitecture", "True");
            objRun.InstallSelections.InputSequence.Add(objSelectionCompatibleNetFx35);

            if (isWinBlue)
            {
                MDO.Package objMDOPackageBlueReq = new MDO.Package(10494);
                Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageBlueReq);
                objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["BluePreReq"].ToString());
                objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
            }
        }

        private void SetRunMachineQuery(MDO.Run objRun, TTestMatrix tm)
        {
            int machineQueryID = 0;

            switch (tm.ProductCPUID)
            {
                case 3: // IA64
                    machineQueryID = 245570;
                    break;

                case 4: // ARM
                    machineQueryID = 712487;
                    break;

                case 5: // ARM64
                    machineQueryID = 909348;
                    break;

                default: // x86 and x64
                    if (tm.TestMatrixName.StartsWith("Win11") && tm.TestMatrixName != "Win11_22000" && !tm.TestMatrixName.Contains("ProductRefresh"))
                        machineQueryID = 909285;
                    else if (!tm.TestMatrixName.StartsWith("Win10") && !tm.TestMatrixName.StartsWith("Server_") && !tm.TestMatrixName.StartsWith("Win11"))
                    {
                        machineQueryID = 915030;
                    }
                    else
                        machineQueryID = 892116;
                    break;
            }

            objRun.VMRole = MDO.enuVMRoles.UsePhysical;
            MDO.QueryObject machineQuery = new MDO.QueryObject(machineQueryID);
            objRun.MachineQuery = machineQuery;
        }

        #region Methods only for Win10 test

        /// <summary>
        /// Add target product to installation list
        /// </summary>
        private void SetProductToInstallOnWin10(MDO.Run objRun, TTestMatrix tm)
        {
            // Product ID = 18 means no need to install product in matrix
            if (tm.ProductID == 18)
            {
                objRun.Title = objRun.Title.Replace("#SKU", _THTestTargetProductName);
                return;
            }

            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                string batchPath = db.TWUProductInstallMappings.Where(p => p.ProductID == tm.ProductID).FirstOrDefault().BatchPath;

                MDO.Package objMDOPackage = new MDO.Package(10494);
                Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                objSelectionCommand.SetToken("CommandLine", batchPath);

                objRun.InstallSelections.InputSequence.Add(objSelectionCommand);

                TProduct product = db.TProducts.Where(p => p.ProductID == tm.ProductID).SingleOrDefault();
                objRun.Title = objRun.Title.Replace("#SKU", product.ProductFriendlyName.Substring(25));
            }
        }

        //Disable auto update on Win10
        private void SetDisableWin10AU(MDO.Run objRun)
        {
            MDO.Package objMDOPackageDisableAU = new MDO.Package(10427);
            Selection objSelectionDisableAU = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageDisableAU);
            objSelectionDisableAU.SetToken("File", @"\\clrdrop\ddrelqa\WU_Enlist\PubsuiteClientConfig\Win10-Disable-AU.reg");
            objSelectionDisableAU.SetToken("SpawnWithNativeArchitecture", "True");
            objRun.InstallSelections.InputSequence.Add(objSelectionDisableAU);
        }

        #endregion




    }

    public class KickoffRequest
    {
        public SqlInfo tm { get; set; }
        public List<InputDataItem> mdSelectionData { get; set; }
        public ConnectToMad parameter { get; set; }
    }

    public class SqlInfo
    {
        public int TestCaseID { get; set; }
        public int MDOSID { get; set; }
        public int ProductCPUID { get; set; }
        public int OSImageID { get; set; }
        public string TestMatrixName { get; set; }
        public int ProductID { get; set; }
    }


    public class ConnectToMad
    {
        public string RunTitle { get; set; }
        public string PatchTech { get; set; }
        public bool AddStrongNameToPackage { get; set; }
        public string THTestTargetProductName { get; set; }
        public long JobId { get; set; }
        public int NetFxSetupPatchID { get; set; }
    }

}
