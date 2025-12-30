using DefinitionInterpreter;
using Helper;
//using MadDogObjects.BuildWebServices;
using NetFxSetupLibrary;
using Newtonsoft.Json;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using MDO = MadDogObjects;

namespace WUTestManagerLib
{
    public class WUIntegration
    {
        #region Data Member

        public long JobID { get; private set; }
        private string currentUser;

        #endregion

        #region Constructor

        public WUIntegration(long lgJobID)
        {
            JobID = lgJobID;
            this.currentUser = "redmond\\vsulab";
        }

        public WUIntegration(long lgJobID, string strCurrentUser)
        {
            JobID = lgJobID;
            this.currentUser = strCurrentUser;
        }

        #endregion

        #region Public Member Function

        /// <summary>
        /// Kick off WU runs for a WU job
        /// </summary>
        /// <returns></returns>
        public void KickOffRuns()
        {
            ConnectToMadDog();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var WURuns = from c in dataContext.TWURuns
                             where c.JobID == JobID
                             select c;
                RunInfo runInfo = new RunInfo();
                PatchDetail patch = new PatchDetail();
                WuRequest wuRequest = new WuRequest();
                foreach (TWURun run in WURuns)
                {
                    TWUSubPatchDetail subPatchDetail = dataContext.TWUSubPatchDetails.SingleOrDefault(p => p.ID == run.PatchDetailID);
                    TWUPatchDetail patchDetail = dataContext.TWUPatchDetails.SingleOrDefault(p => p.ID == subPatchDetail.ParentID);

                    //Get Install sequence data
                    InputData data = LoadInstallSequenceData(run, subPatchDetail, patchDetail);

                    //Kick off one run each time
                    try
                    {
                        KickOffRun(data, run, patchDetail, dataContext);
                        //runInfo.Title = run.Title;
                        //runInfo.OSImageID = run.OSImageID;
                        //runInfo.TestCaseID = run.TestCaseID;
                        //runInfo.JobID = run.JobID;
                        //runInfo.PatchDetailID = run.PatchDetailID;
                        //patch.CPUID = patchDetail.CPUID;
                        //patch.ProductLayer = patchDetail.ProductLayer;
                        //wuRequest.patchD = patch;
                        //wuRequest.runInf = runInfo;
                        //wuRequest.inputData = data;

                        //HttpClient client = new HttpClient();
                        //client.Timeout = TimeSpan.FromSeconds(120);
                        ////string url = "https://localhost:44355/api/KickoffRuntime";
                        ////string url = "http://localhost:8081/api/KickoffRuntime";
                        //string url = ConfigurationManager.AppSettings["ConnectToMadUrl"] + "api/KickoffRunForWu";

                        //string json = JsonConvert.SerializeObject(wuRequest);
                        //var content = new StringContent(json, Encoding.UTF8, "application/json");
                        //HttpResponseMessage response = client.PostAsync(url, content).Result;
                        //response.EnsureSuccessStatusCode();                       

                    }
                    catch(Exception ex) {
                        CreateBat(ex.Message);
                        CreateBat(ex.ToString());

                    }
                }
            }
        }

        #endregion

        private void CreateBat(string e)
        {
            string path = Path.Combine("E:\\App\\ConnectToMad\\Logs", "Exception.txt");
            using (StreamWriter sw = new StreamWriter(path,append:true)) { 
                sw.WriteLine(e);
            }
        }

        #region Private Member Function

        // Prepare necessary data for WU runs
        private InputData LoadInstallSequenceData(TWURun run, TWUSubPatchDetail subPatchDetail, TWUPatchDetail patchDetail)
        {
            InputData data = new InputData() { Data = new List<InputDataItem>() };

            Architecture arch = (Architecture)patchDetail.CPUID;

            #region Add S14 packages for 2012R2 and 8.1

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["NugetPackagePath"].ToString(), FieldType = "Command" });
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["NugetConfigPath"].ToString(), FieldType = "Command" });
                bool is2012R2 = dataContext.TWUOS.Where(p => p.OSImageID == run.OSImageID && p.OSName.Contains("Blue")).Count() > 0;
                if (is2012R2)
                {
                    data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["BluePreReq"].ToString(), FieldType = "Command" });
                    data.Data.Add(new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
                }
                bool is2008R2 = dataContext.TWUOS.Where(p => p.OSImageID == run.OSImageID && p.OSName=="Windows Server 2008 R2").Count() > 0;
                if (is2008R2)
                {
                    string a = ConfigurationManager.AppSettings["Server2K8R2ActivateCommand"].ToString();
                    string b = ConfigurationManager.AppSettings["Installkb5016892ForServer2K8R2"].ToString();
                    data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["Server2K8R2ActivateCommand"].ToString(), FieldType = "Command" });
                    data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["Installkb5016892ForServer2K8R2"].ToString(), FieldType = "Command" });
                }
            }

            #endregion

            if (arch == Architecture.ARM)
            {
                //ARM machine
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["ARMMachineSetup1"].ToString(), FieldType = "Command" });
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["WUNetConnect_ARM"].ToString(), FieldType = "Command" });
                data.Data.Add(new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["ARMMachineSetup2"].ToString(), FieldType = "Command" });
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = string.Format("reg.exe Import {0}", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString()), FieldType = "Command" });
            }
            else if (arch == Architecture.IA64) //IA64 machine
            {
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["WUNetConnect"].ToString(), FieldType = "Command" });
                data.Data.Add(new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
            }
            
            //data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["NugetConfigPath"].ToString(), FieldType = "Command" });
            //data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = "powershell -command \"Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile %systemdrive%\\Nuget\\nuget.exe\"", FieldType = "Command" });
            //data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = "nuget install TestScripts -o %systemdrive%\\NugetPackage -ExcludeVersion  ", FieldType = "Command" });
            data.Data.Add(new InputDataItem { FieldName = "RunID", FieldValue = "[RunID]", FieldType = "EnvironmentVariable" });
            data.Data.Add(new InputDataItem { FieldName = "ParameterFile", FieldValue = run.ParameterFilePath, FieldType = "EnvironmentVariable" });
            data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["EnableWUService"].ToString(), FieldType = "Command" });
            data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["DisableWarning"].ToString(), FieldType = "Command" });
            data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = String.Format("reg.exe Import {0}", ConfigurationManager.AppSettings["DisablePerSessionTempDir"].ToString()), FieldType = "Command" });
            data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["CopyFiles_WU"].ToString(), FieldType = "Command" });

            // We need to install EKB to upgrade to 22H2
            //if (patchDetail.Title.Contains("Windows 10 Version 22H2"))
            //{
            //    data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["Upgrade2Win1022H2"].ToString(), FieldType = "Command" });
            //}

            if (IsNetfx35ToBeInstalled(subPatchDetail))
            {
                InputData dataEnableNetfx35 = LoadEnableNetfx35Data(run, arch);
                if (dataEnableNetfx35 != null && data.Data.Count > 0)
                    data.Data.AddRange(dataEnableNetfx35.Data);
            }

            // add test target product
            data.Data.Add(new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });

            if (run.ActualProductID != null)
            {
                InputDataItem item = LoadTargetProduct(Convert.ToInt32(run.ActualProductID));
                if (item != null && !String.IsNullOrEmpty(item.FieldName))
                    data.Data.Add(item);
            }
            //
            if(patchDetail.Title.Contains("Windows 7") || patchDetail.Title.Contains("Server 2008 R2"))
                data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["FixWin7WUCBug"].ToString(), FieldType = "Command" });

            return data;
        }

        private bool IsNetfx35ToBeInstalled(TWUSubPatchDetail subPatchDetail)
        {
            string[] targetProducts = subPatchDetail.ProductLayer.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (targetProducts.Length > 1)
                return true;

            return subPatchDetail.ProductLayer[0] < '4';
        }

        private InputData LoadEnableNetfx35Data(TWURun run, Architecture arch)
        {
            if (arch == Architecture.ARM) //arm machine
                return null;

            InputData data = new InputData() { Data = new List<InputDataItem>() };

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var os = (from c in dataContext.TOs
                          join image in dataContext.TOSImages on c.MDOSID equals image.MDOSID
                          where image.OSImageID == run.OSImageID
                          select c).First();
                var win7MaxOSId = dataContext.TOs.Where(p => p.OSVersion == "6.1").OrderByDescending(p => p.MDOSID).First().MDOSID;
                bool Is2003IAOS = os.OSCPUID == 3 && os.OSVersion == "5.2";

                if (os.OSName.Contains("Core"))
                {
                    data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["EnableNDP35OnServerCore"].ToString(), FieldType = "Command" });
                }
                else
                {
                    if (os.MDOSID > win7MaxOSId)
                    {
                        //Enable NetFx3 on os 8.1 and above
                        data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["EnableNetFx3Win8"].ToString(), FieldType = "Command" });
                    }
                    else
                    {
                        //If IA64 2003 OS, install Dot Net Framework 4.0 and enable comptible with Dot Net Framework 3.5
                        if (Is2003IAOS)
                        {
                            data.Data.Add(new InputDataItem { FieldName = "MDPackage", FieldValue = 10494.ToString(), FieldType = "MDPackage" });

                            //make that compatible with Net Framework 3.5
                            data.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = string.Format("reg.exe Import {0}", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString()), FieldType = "Command" });
                        }
                        else
                        {
                            //Install Dot Net Framework 3.5
                            data.Data.Add(new InputDataItem { FieldName = "MDPackage", FieldValue = 10381.ToString(), FieldType = "MDPackage" });
                        }
                    }
                }
            }

            return data;
        }

        private InputDataItem LoadTargetProduct(int productID)
        {
            InputDataItem item = new InputDataItem();
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var productMapping = from c in dataContext.TWUProductInstallMappings
                                     where c.ProductID == productID
                                     select c;
                if (productMapping.Count() > 0)
                {
                    TWUProductInstallMapping product = productMapping.First();
                    if (product.MDPackageID > 0)
                    {
                        //A Maddog package specified
                        item.FieldName = item.FieldType = "MDPackage";
                        item.FieldValue = product.MDPackageID.ToString();
                    }
                    else if (!String.IsNullOrEmpty(product.BatchPath))
                    {
                        //A custom batch is specified to install product
                        item.FieldName = item.FieldType = "Command";
                        item.FieldValue = product.BatchPath;
                    }
                }

            }

            return item;
        }

        private void KickOffRun(InputData installSequence, TWURun run, TWUPatchDetail patchDetail, ScorpionDAL.PatchTestDataClassDataContext dataContext)
        {
            MDO.Run objRun = new MDO.Run();

            TO os = (from c in dataContext.TOs
                     join image in dataContext.TOSImages on c.MDOSID equals image.MDOSID
                     where image.OSImageID == run.OSImageID
                     select c).First();

            //Run title
            objRun.Title = run.Title;

            //mapping test case id with test case query
            var mappedCase = dataContext.TMDQueryMappings.First(p => p.MatrixTestcaseID == run.TestCaseID);

            if (ISUseDownlevelCase(run, dataContext, os, mappedCase.TestcaseDesc))
            {
                mappedCase = dataContext.TMDQueryMappings.First(p => p.TestcaseDesc == mappedCase.TestcaseDesc + "Downlevel");
                if (mappedCase == null)
                {
                    throw new Exception(string.Format("case {0} do not exist", mappedCase.TestcaseDesc + "Downlevel"));
                }
            }

            MDO.QueryObject testcaseQuery = new MDO.QueryObject(mappedCase.QueryID);
            objRun.TestcaseQuery = testcaseQuery;

            //context query: default context
            MDO.QueryObject contextQuery = new MDO.QueryObject(675434);
            objRun.ContextQuery = contextQuery;



            objRun.MachineOptions = Helper.MDMachineOptions.GetMDMachineOptions(os.OSName);

            //OS and image
            objRun.Owner = new MDO.Owner(currentUser);
            objRun.AnalysisProfile = new MDO.AnalysisProfile(17);
            objRun.AutoReserveMaxMachines = 2;
            objRun.MaxMachines = 2;
            //objRun.RunTimeOut = (((Architecture)patchDetail.CPUID == Architecture.ARM64) ? 72 : 200);
            objRun.RunTimeOut = 96;
            objRun.Reimage = true;
            objRun.OS = new MDO.OS(os.MDOSID);
            int mdOSImage = (int)dataContext.TOSImages.Where(p => p.OSImageID == run.OSImageID).First().MaddogOSImageID;
            objRun.OSImage = new MDO.OSImage(mdOSImage);
            objRun.Branch = new MDO.Branch(536);

            //set machine query
            SetRunMachineQuery(objRun, (Architecture)patchDetail.CPUID, os);

            //test source location
            objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = ConfigurationManager.AppSettings["TestSourcesLocation_WUAutomation"].ToString();
            //required binaries
            objRun.InstallSelections.Defaults["TestReqBinPath"].Value = ConfigurationManager.AppSettings["RequiredBinariesLocation_WUAutomation"].ToString();

            //MDO.Package objMDOPackageUninstall47 = new MDO.Package(14188);
            MDO.Package objMDOPackageUninstall47 = new MDO.Package(14565);
            Selection objSelectionCommandUninstall47 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageUninstall47);
            objRun.InstallSelections.InputSequence.Add(objSelectionCommandUninstall47);

            //Test case flags - For CLR Drivers
            //if (String.Compare(os.OSVersion, "6.0") < 0) //for xp and 2003 
            //{
            //    objRun.RunBuildFlags = new MDO.QueryObject(740136);
            //}
            //else //for Vista and above OSes
            //{
            //    objRun.RunBuildFlags = new MDO.QueryObject(715949);
            //}

            // Driver flags for FXBVT Driver
            objRun.Flags = new MDO.QueryObject(896247);

            //Config installation sequence
            foreach (InputDataItem item in installSequence.Data)
            {
                if (item.FieldType.Equals("EnvironmentVariable"))
                {
                    MDO.Package objMDOPackage = new MDO.Package(10428);
                    Selection objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    objSelection.SetToken("VariableName", item.FieldName);
                    //ARM cannot accept set a value as null or empty
                    objSelection.SetToken("VariableValue", string.IsNullOrEmpty(item.FieldValue) ? "Empty" : item.FieldValue);

                    objRun.InstallSelections.InputSequence.Add(objSelection);
                }
                else if (item.FieldType.Equals("Command"))
                {
                    MDO.Package objMDOPackage = new MDO.Package(10494);
                    Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    objSelectionCommand.SetToken("CommandLine", item.FieldValue);
                    objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                }
                else if (item.FieldType.Equals("Reboot"))
                {
                    MDO.Package objMDOPackage = new MDO.Package(10420);
                    Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                }
                else if (item.FieldType.Equals("MDPackage"))
                {
                    MDO.Package objMDOPackage = new MDO.Package(Convert.ToInt32(item.FieldValue));
                    Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);

                    objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                }
            }

            //Set use latest CLR
            {
                MDO.Package objMDOPackageCompatibleNetFx35 = new MDO.Package(10427);
                Selection objSelectionCompatibleNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageCompatibleNetFx35);
                objSelectionCompatibleNetFx35.SetToken("File", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString());
                objSelectionCompatibleNetFx35.SetToken("SpawnWithNativeArchitecture", "True");
                objRun.InstallSelections.InputSequence.Add(objSelectionCompatibleNetFx35);
            }

            objRun.Save();

            DefinitionInterpreter.Log.Enabled = true;
            objRun.GenerateInstallationSequence();
            objRun.SetSecurityOnResultsFolder();
            objRun.Save();

            // starting run may fail due to permission issue. Catch any exceptions so we can start them manually instead
            try
            {
                MDO.Run.RunHelpers.StartRun(objRun);
            }
            catch
            { }

            run.MDRunID = objRun.ID;
            run.CreateDate = DateTime.Now;
            run.RunStatusID = 1;

            dataContext.SubmitChanges();
        }

        private bool ISUseDownlevelCase(TWURun run, ScorpionDAL.PatchTestDataClassDataContext dataContext, TO os, string testcaseName)
        {
            if (!testcaseName.Contains("BasicIUR"))
                return false;

            TWUSubPatchDetail patchDetail = dataContext.TWUSubPatchDetails.Where(a => a.ID == run.PatchDetailID).FirstOrDefault();

            if (patchDetail.ProductLayer.Contains("MSI") || patchDetail.ProductLayer.Contains("OCM"))
                return true;

            //use downlevel basic IUR case on Vista and downlevel OS
            if (String.Compare(os.OSVersion, "6.1") < 0 || os.OSVersion == "10.0")
                return true;

            return false;
        }

        private void ConnectToMadDog()
        {
            try
            {
                string strUserName = currentUser;
                if (!string.IsNullOrEmpty(currentUser) && (currentUser.IndexOf("\\") >= 0))
                    strUserName = currentUser.Split('\\')[1]; //removing the domian Name

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

        private void SetRunMachineQuery(MDO.Run objRun, Architecture arch, TO os)
        {
            int machineQueryID = 0;

            switch (arch)
            {
                case Architecture.IA64: // IA64
                    machineQueryID = 245570;
                    break;

                case Architecture.ARM: // ARM
                    machineQueryID = 712487;
                    break;

                case Architecture.ARM64: // ARM64
                    machineQueryID = 909348;
                    break;

                default: // x86 and x64
                    //if (os.OSName.StartsWith("Windows 11"))
                    //    machineQueryID = 909285;
                    //else
                    //    machineQueryID = 892116;
                    //change by jiacheng
                    if (os.OSVersion.StartsWith("10") && os.MDOSID >= 4055)
                        machineQueryID = 909285;
                    else if (os.OSVersion.StartsWith("10") && (3523 <= os.MDOSID && os.MDOSID < 4055))
                        machineQueryID = 892116;
                    else
                    {
                        machineQueryID = 915030;
                    }
                    break;
            }

            objRun.VMRole = MDO.enuVMRoles.UsePhysical;
            MDO.QueryObject machineQuery = new MDO.QueryObject(machineQueryID);
            objRun.MachineQuery = machineQuery;
        }

        #endregion
    }

    public class RunInfo
    {
        public int OSImageID { get; set; }
        public string Title { get; set; }
        public int TestCaseID { get; set; }
        public int JobID { get; set; }
        public int PatchDetailID { get; set; }
    }

    public class PatchDetail
    {
        public int CPUID { get; set; }
        public string ProductLayer { get; set; }
    }

    public class WuRequest
    {
        public PatchDetail patchD { get; set; }
        public RunInfo runInf { get; set; }
        public InputData inputData { get; set; }
    }

}
