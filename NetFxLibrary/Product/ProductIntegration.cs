using DefinitionInterpreter;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;
using MDO = MadDogObjects;

namespace NetFxSetupLibrary.ProductTesting
{
    public class ProductIntegration
    {
        public List<string> MatrixName { get; set; }
        public bool AddStrongNameHijackPackage { get; set; }
        public long JobID { get; set; }
        public int SubmissionID { get; set; }
        public bool IsHotfixRollup { get; set; }
        public string CurrentProduct { get; set; }
        public string PreviousProduct { get; set; }

        public ProductIntegration(List<string> matrixName, string curProduct, string preProduct, bool addStrongNameHijackPackage = false)
        {
            MatrixName = matrixName;
            CurrentProduct = curProduct;
            PreviousProduct = preProduct;
            AddStrongNameHijackPackage = addStrongNameHijackPackage;
            JobID = 0;
            IsHotfixRollup = false;
        }

        public void OperateManualProductRun(InputData inputData, string runTitlePrefix, int submissionID, string runOwner)
        {
            SubmissionID = submissionID;
            KickOffRuns(inputData.Data, runTitlePrefix, runOwner);
        }

        private void KickOffRuns(List<InputDataItem> mdSelectionData, string runTitle, string runOwner)
        {

            //connect to maddog
            ConnectToMadDog(runOwner);
            List<TTestMatrix> lstMatrixes = PrepareTestMatrix();

            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                foreach (TTestMatrix tm in lstMatrixes)
                {
                    MDO.Run objRun = new MDO.Run();

                    //mapping the new case id
                    var mappedCase = db.TMDQueryMappings.Where(p => p.MatrixTestcaseID == tm.TestCaseID).First();
                    MDO.QueryObject testcaseQuery = new MDO.QueryObject(mappedCase.QueryID);
                    objRun.TestcaseQuery = testcaseQuery;

                    objRun.Title = runTitle + mappedCase.TestcaseDesc + "-";
                    objRun.Title = objRun.Title.Replace("CurrentProduct", CurrentProduct).Replace("PreviousProduct", PreviousProduct);
                    //context query: default context
                    MDO.QueryObject contextQuery = new MDO.QueryObject(675434);
                    objRun.ContextQuery = contextQuery;

                    MDO.QueryObject machineQuery = new MDO.QueryObject(892116);
                    objRun.MachineQuery = machineQuery;

                    #region 2012R2 image doesn't have S14 installed, which should be installed before running test
                    //bool is2012R2 = db.TOs.Where(p => p.MDOSID == tm.MDOSID && p.OSCPUID == 2 && p.OSVersion == "6.3" && p.OSName.Contains("Windows Server Blue")).Count() > 0;
                    bool is2012R2 = db.TOs.Where(p => p.MDOSID == tm.MDOSID && p.OSName.Contains("Blue")).Count() > 0;
                    if (is2012R2)
                    {
                        mdSelectionData.Insert(0, new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
                        mdSelectionData.Insert(0, new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["BluePreReq"].ToString(), FieldType = "Command" });
                    }
                    #endregion

                    MDO.Owner ownRunOwner;
                    try
                    {
                        ownRunOwner = new MDO.Owner(runOwner);
                    }
                    catch (Exception ex)
                    {
                        //Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, objPatch.PatchInfo, ex);
                        throw (ex);
                    }

                    objRun.Owner = ownRunOwner;
                    objRun.AnalysisProfile = new MDO.AnalysisProfile(17);
                    objRun.AutoReserveMaxMachines = 1;
                    objRun.MaxMachines = 1;
                    objRun.RunTimeOut = ((tm.ProductCPUID == 4) ? 72 : 200);
                    objRun.VMRole = MDO.enuVMRoles.UsePhysical;
                    objRun.Priority = MDO.Prioritization.enuPriority.VeryImportant;
                    objRun.Reimage = true;
                    objRun.OS = new MDO.OS(tm.MDOSID);
                    int mdOSImage = (int)db.TOSImages.Where(p => p.OSImageID == tm.OSImageID).First().MaddogOSImageID;
                    int mdOSID = (int)db.TOSImages.Where(p => p.OSImageID == tm.OSImageID).First().MDOSID;
                    var OSInfo = db.TOs.Where(p => p.MDOSID == mdOSID).First();
                    objRun.Title += OSInfo.OSName;
                    objRun.OSImage = new MDO.OSImage(mdOSImage);

                    //Set machine optitons
                    var os = db.TOs.Where(p => p.MDOSID == tm.MDOSID).First();
                    string osName = os.OSName;
                    if (string.Equals(os.OSVersion, "10.0"))
                    {
                        var osImage = db.TOSImages.Where(p => p.OSImageID == tm.OSImageID).First();
                        osName = osImage.OSSPLevel;
                    }

                    //XmlDocument doc = new XmlDocument();
                    //if (osName.StartsWith("RS"))
                    //{
                    //    doc.LoadXml(Helper.MDMachineOptions.RS1_XML);
                    //}

                    //objRun.MachineOptions = doc;
                    objRun.InstallSelections.Defaults["TestReqBinPath"].Value = @"\\cpvsbuild\drops\dev11\RTMRel\tst\50727.01\x86ret\suitebin";
                    
                    // Driver flags for FXBVT Driver
                    objRun.Flags = new MDO.QueryObject(896247);

                    if (AddStrongNameHijackPackage)
                    {
                        MDO.Package objMDOPackageAddStrongNameHijack = new MDO.Package(10494);
                        Selection objSelectionAddStrongNameHijack = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageAddStrongNameHijack);
                        objSelectionAddStrongNameHijack.SetToken("CommandLine", ConfigurationManager.AppSettings["Preinstall"].ToString());
                        objRun.InstallSelections.InputSequence.Add(objSelectionAddStrongNameHijack);
                    }

                    //Add DevDiv-LUA-OFF
                    //MDO.Package objMDOPackageDevDivLUAOFF = new MDO.Package(8152);
                    //Selection objSelectionDevDivLUAOFF = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageDevDivLUAOFF);
                    //objRun.InstallSelections.InputSequence.Add(objSelectionDevDivLUAOFF);

                    MDO.Package objMDOPackageEnableWUService = new MDO.Package(10494);
                    Selection objSelectionCommandEnableWUService = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageEnableWUService);
                    objSelectionCommandEnableWUService.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableWUService"].ToString());
                    objRun.InstallSelections.InputSequence.Add(objSelectionCommandEnableWUService);

                    MDO.Package objMDOPackageDNSSuffix = new MDO.Package(10427);
                    Selection objSelectionDNSSuffix = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageDNSSuffix);
                    objSelectionDNSSuffix.SetToken("File", @"\\clrdrop\ddrelqa\v-zhehu\Scripts\DNSSuffix.reg");
                    objSelectionDNSSuffix.SetToken("SpawnWithNativeArchitecture", "True");
                    objRun.InstallSelections.InputSequence.Add(objSelectionDNSSuffix);
                    SetUseLatestCLR(objRun);

                    MDO.Package objMDOPackageDisableWastonLog = new MDO.Package(10494);
                    Selection objSelectionDisableWastonLog = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageDisableWastonLog);
                    objSelectionDisableWastonLog.SetToken("CommandLine", ConfigurationManager.AppSettings["DisableWastonLog"].ToString());
                    objSelectionDisableWastonLog.SetToken("SpawnWithNativeArchitecture", "True");
                    objRun.InstallSelections.InputSequence.Add(objSelectionDisableWastonLog);

                    //MDO.Package objMDOPackageInstallD3DCompiler = new MDO.Package(10494);
                    //Selection objSelectionCommandInstallD3DCompiler = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageInstallD3DCompiler);
                    //objSelectionCommandInstallD3DCompiler.SetToken("CommandLine", ConfigurationManager.AppSettings["InstallD3DCompiler"].ToString());
                    //objRun.InstallSelections.InputSequence.Add(objSelectionCommandInstallD3DCompiler);

                    if (os.OSName.Contains("Core"))
                    {
                        MDO.Package objMDOPackage = new MDO.Package(10494);
                        Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                        objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableNDP35OnServerCore"].ToString());
                        objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                    }
                    else
                    {
                        if (tm.MDOSID > 2410)
                        {
                            //Enable win8 and above compatible with Net Framework 3.5
                            MDO.Package objMDOPackageCompatibleNetFx35 = new MDO.Package(10427);
                            Selection objSelectionCompatibleNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageCompatibleNetFx35);
                            objSelectionCompatibleNetFx35.SetToken("File", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString());
                            objSelectionCompatibleNetFx35.SetToken("SpawnWithNativeArchitecture", "True");
                            objRun.InstallSelections.InputSequence.Add(objSelectionCompatibleNetFx35);
                            SetUseLatestCLR(objRun);
                        }
                        else
                        {
                            //Add Dot Net Framework 3.5
                            MDO.Package objMDOPackageNetFx35 = new MDO.Package(10381);
                            Selection objSelectionNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageNetFx35);
                            objRun.InstallSelections.InputSequence.Add(objSelectionNetFx35);
                            SetUseLatestCLR(objRun);
                        }
                    }

                    foreach (var item in mdSelectionData)
                    {
                        if (item.FieldType.Equals("EnvironmentVariable"))
                        {
                            MDO.Package objMDOPackage = new MDO.Package(10428);
                            Selection objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                            objSelection.SetToken("VariableName", item.FieldName);
                            objSelection.SetToken("VariableValue", item.FieldValue);

                            objRun.InstallSelections.InputSequence.Add(objSelection);
                        }
                        else if (item.FieldType.Equals("Command"))
                        {
                            MDO.Package objMDOPackage = new MDO.Package(10494);
                            Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                            objSelectionCommand.SetToken("CommandLine", item.FieldValue);

                            if (item.FieldValue.Contains("CopyFiles.bat"))
                            {
                                objSelectionCommand.SetToken("SuccessfulExitCodes", ConfigurationManager.AppSettings["CopyFilesSuccessfulExitCodes"].ToString());
                            }

                            objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                        }
                    }

                    if (!string.IsNullOrEmpty(tm.TestCaseSpecificData))
                    {
                        string[] specificData = tm.TestCaseSpecificData.Split('#');
                        foreach (string data in specificData)
                        {
                            string[] singleData = data.Split('=');
                            MDO.Package objMDOPackage = new MDO.Package(10428);
                            Selection objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                            objSelection.SetToken("VariableName", singleData[0]);
                            objSelection.SetToken("VariableValue", singleData[1]);

                            objRun.InstallSelections.InputSequence.Add(objSelection);
                        }
                    }

                    objRun.Save();

                    //we have no permission to write file in the path of "\\ddrelqa\EnlistmentSAN\Dev10\DTG_1\src"
                    //now relpace that with "\\vsufile\Workspace\Current\SetupTest\ClickOnce_Enlistment\src"
                    objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = ConfigurationManager.AppSettings["TestSourcesLocation"].ToString();
                    objRun.Save();

                    DefinitionInterpreter.Log.Enabled = true;
                    objRun.GenerateInstallationSequence();
                    objRun.SetSecurityOnResultsFolder();
                    objRun.Save();

                    if (!Helper.RunHelper.SecurityOSList.Contains(osName))
                    {
                        MDO.Run.RunHelpers.StartRun(objRun);
                    }

                    var runStatus = new TNetFxSetupRunStatus();
                    runStatus.MDRunID = objRun.ID;
                    runStatus.RunTitle = objRun.Title;
                    runStatus.CreatedBy = objRun.Owner.Name;
                    runStatus.CreatedDate = DateTime.Now;
                    runStatus.LastModifiedBy = objRun.Owner.Name;
                    runStatus.LastModifiedDate = DateTime.Now;
                    runStatus.JobID = JobID;
                    runStatus.RunStatusID = 1;
                    runStatus.SubmissionID = SubmissionID;
                    runStatus.SubmissionType = IsHotfixRollup ? "HFR" : "Product";
                    runStatus.OS = OSInfo.OSName;
                    runStatus.Architecture = (OSInfo.OSCPUID == 1) ? "X86" : "AMD64";

                    db.TNetFxSetupRunStatus.InsertOnSubmit(runStatus);
                    db.SubmitChanges();
                }
            }
        }

        private void SetUseLatestCLR(MDO.Run objRun)
        {
            MDO.Package objMDOPackageCompatibleNetFx35 = new MDO.Package(10427);
            Selection objSelectionCompatibleNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageCompatibleNetFx35);
            objSelectionCompatibleNetFx35.SetToken("File", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString());
            objSelectionCompatibleNetFx35.SetToken("SpawnWithNativeArchitecture", "True");
            objRun.InstallSelections.InputSequence.Add(objSelectionCompatibleNetFx35);
        }

        private List<TTestMatrix> PrepareTestMatrix()
        {
            List<TTestMatrix> vTestMatrixes = new List<TTestMatrix>();
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                foreach (string matrix in MatrixName)
                {
                    vTestMatrixes.AddRange((from tm in db.TTestMatrixes
                                            where tm.TestMatrixName == matrix
                                              && tm.MaddogDBID == 2  //TODO: Change this?
                                              && tm.Active == true
                                            select tm).ToList());
                }
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
                MDO.Utilities.Security.SetDB("MDSQL3", "OrcasTS");
                MDO.Branch.CurrentBranch = new MDO.Branch(536);

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

    }
}
