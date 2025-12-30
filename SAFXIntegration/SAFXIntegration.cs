//using MDOF = MadDogObjects.Forms;
using DefinitionInterpreter;
using ScorpionDAL;
using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Xml;
using MDO = MadDogObjects;

namespace SAFXIntegration
{
    public enum FieldType { EnvironmentVariable, Command }
    public enum VariableType { User_specific, System_wide }

    public class SAFXIntegration
    {
        public TSAFXProject TSAFXProject { get; set; }
        public DataTable dtDecaturCPU { get; set; }
        public DataTable dtDecaturLanguage { get; set; }

        public SAFXIntegration(int intSAFXProjectID)
        {
            TSAFXProject = GetInputData(intSAFXProjectID);
            //lstTSAFXProjectInputData = myTSAFXProject.TSAFXProjectInputDatas.ToList();           

            dtDecaturCPU = GetDecaturCPUsFromTCPU();
            dtDecaturLanguage = GetDecaturLanguagesFromTLanguage();
        }

        private TSAFXProject GetInputData(int intSAFXProjectID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TSAFXProject objSAFXProject = new TSAFXProject();

            var varSAFXProject = from safx in db.TSAFXProjects
                                 where safx.SAFXProjectID == intSAFXProjectID
                                 select safx;

            foreach (var obj in varSAFXProject)
                objSAFXProject = (TSAFXProject)obj;

            return objSAFXProject;
        }

        private FieldType GetFieldTypeByString(string strFieldType)
        {
            if (strFieldType.Equals(FieldType.EnvironmentVariable.ToString()))
            {
                return FieldType.EnvironmentVariable;
            }
            else if (strFieldType.Equals(FieldType.Command.ToString()))
            {
                return FieldType.Command;
            }
            else
            {
                throw new ArgumentException("FieldType is not one of the two valid formats which predefined in T_TSAFXProjectInputData.");
            }
        }

        private VariableType GetVariableTypeByString(string strVariableType)
        {
            if (strVariableType.Equals(VariableType.System_wide.ToString().Replace('_', '-')))
            {
                return VariableType.System_wide;
            }
            else if (strVariableType.Equals(VariableType.User_specific.ToString().Replace('_', '-')))
            {
                return VariableType.User_specific;
            }
            else
            {
                throw new ArgumentException("VariableType is not one of the two valid formats which predefined in T_TSAFXProjectInputData.");
            }
        }

        private DataTable GetDecaturCPUsFromTCPU()
        {
            DataTable dtRtn = new DataTable();
            dtRtn.Columns.Add("CPUID");
            dtRtn.Columns.Add("DecaturCPU");

            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            var varCPUs = from cpu in db.TCPUs
                          select cpu;
            foreach (var obj in varCPUs)
            {
                TCPU objCPU = (TCPU)obj;
                DataRow dr = dtRtn.NewRow();
                dr["CPUID"] = objCPU.CPUID;
                dr["DecaturCPU"] = objCPU.DecaturCPU;
                dtRtn.Rows.Add(dr);
            }

            return dtRtn;
        }

        private DataTable GetDecaturLanguagesFromTLanguage()
        {
            DataTable dtRtn = new DataTable();
            dtRtn.Columns.Add("LanguageID");
            dtRtn.Columns.Add("DecaturLanguage");

            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            var varLanguages = from language in db.TLanguages
                               select language;
            foreach (var obj in varLanguages)
            {
                TLanguage objLanguage = (TLanguage)obj;
                DataRow dr = dtRtn.NewRow();
                dr["LanguageID"] = objLanguage.LanguageID;
                dr["DecaturLanguage"] = objLanguage.DecaturLanguage;
                dtRtn.Rows.Add(dr);
            }

            return dtRtn;
        }

        public int KickOffSAFXRun(string strUser, ref string strKBNumber)
        {

            int intRunID = -1;

            try
            {
                ConnectToMadDog(strUser);
                string stringOwnerAlias = strUser.Split(@"\".ToCharArray())[strUser.Split(@"\".ToCharArray()).Length - 1];

                MDO.Owner ownRunOwner;
                MDO.Run objRun = new MDO.Run();
                objRun.Title = "SAFX Run";

                //Values hardcoded for the demo purpose.
                //ToDo: Need to get values from DB and UI.
                MDO.QueryObject testcaseQuery = new MDO.QueryObject(327410);
                objRun.TestcaseQuery = testcaseQuery;

                MDO.QueryObject machineQuery = new MDO.QueryObject(892116);
                objRun.MachineQuery = machineQuery;
                objRun.Branch = new MDO.Branch(536);

                #region determine which platform will be used for SAFX test

                //NDP20, 30, 35 MSI patches need to be tested on Win7. They are not supportted by Win8.
                bool Win7Needed_TestPlatform = false;
                string strFrameworkFamily = string.Empty;
                var InputDatasFrameFamily = this.TSAFXProject.TSAFXProjectInputDatas.Where(obj => obj.FieldName == "FrameworkFamily");
                if (InputDatasFrameFamily.Count() == 1)
                {
                    strFrameworkFamily = this.TSAFXProject.TSAFXProjectInputDatas.Where(obj => obj.FieldName == "FrameworkFamily").Single().FieldValue;
                    string strPatchTechnology = this.TSAFXProject.TSAFXProjectInputDatas.Where(obj => obj.FieldName == "MspName").Single().FieldValue.Trim().EndsWith(".Cab", StringComparison.OrdinalIgnoreCase) ? "MSU" : "MSI/OCM";

                    if ((strFrameworkFamily.ToUpperInvariant().StartsWith("NDP10") ||
                        strFrameworkFamily.ToUpperInvariant().StartsWith("NDP11") ||
                        strFrameworkFamily.ToUpperInvariant().StartsWith("NDP20") ||
                        strFrameworkFamily.ToUpperInvariant().StartsWith("NDP30") ||
                        strFrameworkFamily.ToUpperInvariant().StartsWith("NDP35")) && strPatchTechnology.Equals("MSI/OCM"))
                    {
                        Win7Needed_TestPlatform = true;
                    }
                }

                #endregion

                #region Get CPU type for run title
                string arch = (this.TSAFXProject.TSAFXProjectInputDatas.Where(obj => obj.FieldName == "PatchArchitecture")).First().FieldValue;
                #endregion

                if (Win7Needed_TestPlatform)
                {
                    objRun.OS = new MDO.OS(1545); // Windows 7 Enterprise
                    objRun.OSImage = new MDO.OSImage(9734); // RTM Image
                    objRun.MachineOptions = Helper.MDMachineOptions.GetMDMachineOptions("Windows 7 Enterprise");
                }
                else
                {
                    objRun.OS = new MDO.OS(3230); // Windows 8.1 Enterprise
                    objRun.OSImage = new MDO.OSImage(69330); // RTM Image
                    objRun.MachineOptions = Helper.MDMachineOptions.GetMDMachineOptions("Windows 8.1 Enterprise");
                }

                //objRun.Product = new MDO.Product(843430);

                try
                {
                    ownRunOwner = new MDO.Owner(stringOwnerAlias);
                }
                catch (Exception ex)
                {
                    //Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, objPatch.PatchInfo, ex);
                    throw (ex);
                }

                objRun.Owner = ownRunOwner;
                objRun.AnalysisProfile = new MDO.AnalysisProfile(17);
                objRun.AutoReserveMaxMachines = 2;
                objRun.MaxMachines = 2;
                objRun.RunTimeOut = 200;
                //ToDo: Set RunBuildFlags & RunRequirementBuildFlags & RunTypes
                //objRun.RunTypes
                objRun.VMRole = MDO.enuVMRoles.UsePhysical;
                objRun.Priority = MDO.Prioritization.enuPriority.VeryImportant;
                objRun.Reimage = true;

                MDO.QueryObject qFlagQuery = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.Flags);
                qFlagQuery.QueryAdd("FlagID", MDO.QueryConstants.EQUALTO.ToString(), 1938, MDO.QueryConstants.OR_OPERATOR);
                qFlagQuery.Name = System.Guid.NewGuid().ToString();
                qFlagQuery.Owner = MDO.Owner.CurrentOwner;
                qFlagQuery.Save();
                objRun.RunBuildFlags = qFlagQuery;

                objRun.Save();

                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(Helper.MDMachineOptions.PreWin10_XML);
                    objRun.MachineOptions = doc;
                }

                {//DevDiv-LUA-OFF
                    MDO.Package objMDOPackage1 = new MDO.Package(8152);
                    Selection objSelection1 = new Selection();
                    objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage1);
                    objRun.InstallSelections.InputSequence.Add(objSelection1); // InstallSelections.InputSequence.Add(objSelection1);
                }

                {//.Net Framework 4.0 Full
                    MDO.Package objMDOPackage1 = new MDO.Package(10379);
                    Selection objSelection1 = new Selection();
                    objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage1);
                    objRun.InstallSelections.InputSequence.Add(objSelection1);
                }

                {
                    MDO.Package objMDOPackageCompatibleNetFx35 = new MDO.Package(10427);
                    Selection objSelectionCompatibleNetFx35 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackageCompatibleNetFx35);
                    objSelectionCompatibleNetFx35.SetToken("File", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString());
                    objSelectionCompatibleNetFx35.SetToken("SpawnWithNativeArchitecture", "True");
                    objRun.InstallSelections.InputSequence.Add(objSelectionCompatibleNetFx35);
                }

                // Turn on UAC once machine is released
                {
                    MDO.Package objMDOPackage = new MDO.Package(10494);
                    Selection objSelection1 = new Selection();
                    objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    objSelection1.SetToken("CommandLine", @"\\clrdrop\ddrelqa\v-zhehu\Tools\StaticVMReleaseHelper\StaticVMReleaseHelper.bat");
                    objRun.InstallSelections.InputSequence.Add(objSelection1);
                }

                foreach (TSAFXProjectInputData objTSAFXProjectInputData in this.TSAFXProject.TSAFXProjectInputDatas.Where(obj => obj.Active == true).OrderBy(obj => obj.Sequence))
                {
                    if (objTSAFXProjectInputData.FieldType.Equals("EnvironmentVariable"))
                    {
                        MDO.Package objMDOPackage = new MDO.Package(10428); //Later this value will also be provided from the DB as one of the Field.
                        Selection objSelection = new Selection();
                        objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                        objSelection.SetToken("VariableName", objTSAFXProjectInputData.FieldName);
                        if (objTSAFXProjectInputData.FieldName.Equals("RunID"))
                        {
                            objSelection.SetToken("VariableValue", objRun.ID.ToString());
                        }
                        else if (objTSAFXProjectInputData.FieldName.Equals("PatchArchitecture"))
                        {
                            objSelection.SetToken("VariableValue", objTSAFXProjectInputData.FieldValue + "ret");
                        }
                        else
                        {
                            if (objTSAFXProjectInputData.FieldName.ToLower().Equals("kbnumber"))
                            {
                                strKBNumber = objTSAFXProjectInputData.FieldValue;
                            }
                            objSelection.SetToken("VariableValue", objTSAFXProjectInputData.FieldValue);
                        }
                        objRun.InstallSelections.InputSequence.Add(objSelection);
                    }
                    else if (objTSAFXProjectInputData.FieldType.Equals("Command"))
                    {
                        MDO.Package objMDOPackage = new MDO.Package(10494);
                        Selection objSelection1 = new Selection();
                        objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                        objSelection1.SetToken("CommandLine", objTSAFXProjectInputData.FieldValue);
                        objRun.InstallSelections.InputSequence.Add(objSelection1);
                    }
                }


                objRun.Save();
                //objRun.InstallSelections.Defaults["TestSourcesLocation"] = new Token("TestSourcesLocation", @"\\ddrelqa\EnlistmentSAN\Dev10\DTG_1\src");
                objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = ConfigurationManager.AppSettings["SAFXTestSourcesLocation"].ToString();
                //objRun.InstallSelections = objRun.InstallSelections;
                objRun.Save();
                //objRun.Title = string.Format("SAFX Run for {0} Target {1}", strKBNumber, strFrameworkFamily);
                objRun.Title = string.Format("SAFX Run for {0} {1}", strKBNumber, arch);
                objRun.Priority = MDO.Prioritization.enuPriority.VeryImportant;
                objRun.Save();
                DefinitionInterpreter.Log.Enabled = true;
                objRun.GenerateInstallationSequence();
                objRun.SetSecurityOnResultsFolder();
                //objRun.SetSecurityOnResultsFolder();
                objRun.Save();
                MDO.Run.RunHelpers.StartRun(objRun);
                return intRunID = objRun.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int KickOffProductSAFXRun(string strUser, ref string strKBNumber)
        {

            int intRunID = -1;

            try
            {

                ConnectToMadDog("redmond\\vsulab");
                string stringOwnerAlias = "vsulab";

                MDO.Run objRun = new MDO.Run();
                //ToDo: Give a more meaningful name
                objRun.Title = "SAFX Run";

                //Values hardcoded for the demo purpose.
                //ToDo: Need to get values from DB and UI.
                //We use a spcial case query for hotfix which excludes cases of SDK and MTPack
                bool isHotfix = ((from input in this.TSAFXProject.TSAFXProjectInputDatas
                                 where input.Active &&
                                 input.FieldName.Equals("releasetype", StringComparison.InvariantCultureIgnoreCase) &&
                                 input.FieldValue.ToLowerInvariant().StartsWith("hotfix")
                                 select input).Count()) > 0;

                MDO.QueryObject testcaseQuery = new MDO.QueryObject(isHotfix? 847188 : 541142);
                objRun.TestcaseQuery = testcaseQuery;


                MDO.QueryObject machineQuery = new MDO.QueryObject(892116);
                objRun.MachineQuery = machineQuery;

                objRun.OS = new MDO.OS(3298);
                objRun.OSImage = new MDO.OSImage(69397);

                MDO.Owner ownRunOwner;
                try
                {
                    ownRunOwner = new MDO.Owner(stringOwnerAlias);
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
                objRun.RunTimeOut = 200;
                //ToDo: Set RunBuildFlags & RunRequirementBuildFlags & RunTypes
                //objRun.RunTypes
                objRun.VMRole = MDO.enuVMRoles.UsePhysical;
                objRun.Priority = MDO.Prioritization.enuPriority.VeryImportant;
                objRun.Reimage = true;

                MDO.QueryObject qFlagQuery = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.Flags);
                qFlagQuery.QueryAdd("FlagID", MDO.QueryConstants.EQUALTO.ToString(), 1938, MDO.QueryConstants.OR_OPERATOR);
                qFlagQuery.Name = System.Guid.NewGuid().ToString();
                qFlagQuery.Owner = MDO.Owner.CurrentOwner;
                qFlagQuery.Save();
                objRun.RunBuildFlags = qFlagQuery;

                objRun.Save();

                {//DevDiv-LUA-OFF
                    MDO.Package objMDOPackage1 = new MDO.Package(8152);
                    Selection objSelection1 = new Selection();
                    objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage1);
                    objRun.InstallSelections.InputSequence.Add(objSelection1); // InstallSelections.InputSequence.Add(objSelection1);
                }

                {
                    //Enable NetFx3 on os 8.1 and above 
                    MDO.Package objMDOPackage = new MDO.Package(10494);
                    Selection objSelectionCommand = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    objSelectionCommand.SetToken("CommandLine", ConfigurationManager.AppSettings["EnableNetFx3Win8"].ToString());
                    objRun.InstallSelections.InputSequence.Add(objSelectionCommand);
                }

                // Turn on UAC once machine is released
                {
                    MDO.Package objMDOPackage = new MDO.Package(10494);
                    Selection objSelection1 = new Selection();
                    objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                    objSelection1.SetToken("CommandLine", @"\\clrdrop\ddrelqa\v-zhehu\Tools\StaticVMReleaseHelper\StaticVMReleaseHelper.bat");
                    objRun.InstallSelections.InputSequence.Add(objSelection1);
                }

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(Helper.MDMachineOptions.PreWin10_XML);
                objRun.MachineOptions = doc;
                objRun.InstallSelections.Defaults["TestReqBinPath"].Value = @"\\cpvsbuild\drops\dev11\RTMRel\tst\50727.01\x86ret\suitebin";

                foreach (TSAFXProjectInputData objTSAFXProjectInputData in this.TSAFXProject.TSAFXProjectInputDatas.Where(obj => obj.Active == true).OrderBy(obj => obj.Sequence))
                {

                    if (objTSAFXProjectInputData.FieldType.Equals("EnvironmentVariable"))
                    {
                        MDO.Package objMDOPackage = new MDO.Package(10428); //Later this value will also be provided from the DB as one of the Field.
                        Selection objSelection = new Selection();
                        objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                        objSelection.SetToken("VariableName", objTSAFXProjectInputData.FieldName);
                        if (objTSAFXProjectInputData.FieldName.Equals("RunID"))
                        {
                            objSelection.SetToken("VariableValue", objRun.ID.ToString());
                        }
                        else
                        {
                            if (objTSAFXProjectInputData.FieldName.ToLower().Equals("kbnumber"))
                            {
                                strKBNumber = objTSAFXProjectInputData.FieldValue;
                            }
                            objSelection.SetToken("VariableValue", objTSAFXProjectInputData.FieldValue);
                        }
                        objRun.InstallSelections.InputSequence.Add(objSelection);
                    }
                    else if (objTSAFXProjectInputData.FieldType.Equals("Command"))
                    {
                        MDO.Package objMDOPackage = new MDO.Package(10494);
                        Selection objSelection1 = new Selection();
                        objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                        objSelection1.SetToken("CommandLine", objTSAFXProjectInputData.FieldValue);
                        objRun.InstallSelections.InputSequence.Add(objSelection1);
                    }
                }

                objRun.Save();
                //objRun.InstallSelections.Defaults["TestSourcesLocation"] = new Token("TestSourcesLocation", @"\\ddrelqa\EnlistmentSAN\Dev10\DTG_1\src");
                objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = ConfigurationManager.AppSettings["SAFXTestSourcesLocation"].ToString();
                //objRun.InstallSelections = objRun.InstallSelections;
                objRun.Save();
                objRun.Title += " for " + strKBNumber;
                objRun.Save();
                DefinitionInterpreter.Log.Enabled = true;
                objRun.GenerateInstallationSequence();
                objRun.SetSecurityOnResultsFolder();
                //objRun.SetSecurityOnResultsFolder();
                objRun.Save();
                MDO.Run.RunHelpers.StartRun(objRun);
                return intRunID = objRun.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void ConnectToMadDog(string strOwner)
        {
            try
            {
                string strUserName = "";
                if ((strOwner != null) && (strOwner.Split('\\') != null) && (strOwner.Split('\\')).Count() > 0)
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

        /// <summary>
        /// Insert new data to TSAFXProjecSubmittedData table
        /// </summary>
        /// <param name="strUser"></param>
        public void InsertTSAFXProjecSubmittedData(string strUser, int intRunID, string strKBNumber, long lgJobID = 0, bool IsForWorkProcess = false)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            string strSubmissionUniqueIdentifier = string.Empty;

            strSubmissionUniqueIdentifier = strKBNumber + "|" + DateTime.Now.ToString() + "|" + intRunID.ToString();
            foreach (TSAFXProjectInputData objTSAFXProjectInputData in this.TSAFXProject.TSAFXProjectInputDatas.OrderBy(obj => obj.Sequence))
            {
                var objTSAFXProjectSubmittedData = new TSAFXProjectSubmittedData
                {
                    SAFXProjectID = objTSAFXProjectInputData.SAFXProjectID,
                    SAFXProjectInputDataID = objTSAFXProjectInputData.ID,
                    FieldValue = objTSAFXProjectInputData.FieldValue,
                    CreatedBy = strUser,
                    CreatedDate = DateTime.Now,
                    LastModifiedBy = null,
                    LastModifiedDate = null,
                    SubmissionUniqueIdentifier = strSubmissionUniqueIdentifier
                };

                if (IsForWorkProcess)
                {
                    objTSAFXProjectSubmittedData.RunID = intRunID;
                    objTSAFXProjectSubmittedData.StatusID = 1;//Running
                    objTSAFXProjectSubmittedData.ResultID = 1;//Unknown
                    objTSAFXProjectSubmittedData.JobID = lgJobID;
                }

                db.TSAFXProjectSubmittedDatas.InsertOnSubmit(objTSAFXProjectSubmittedData);
            }

            db.SubmitChanges();
        }

        /// <summary>
        /// Update TSAFXProjecSubmittedData table by (SAFXProjectID and SAFXProjectInputDataID) or TSAFXProjecSubmittedData.ID
        /// </summary>
        /// <param name="strUser"></param>
        /// <param name="intTSAFXProjectSubmittedDataID"></param>
        public void UpdateTSAFXProjecSubmittedData(string strUser, int intTSAFXProjectSubmittedDataID = -1)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            foreach (TSAFXProjectInputData objTSAFXProjectInputData in this.TSAFXProject.TSAFXProjectInputDatas.OrderBy(obj => obj.Sequence))
            {
                TSAFXProjectSubmittedData objTSAFXProjectSubmittedData = null;
                if (intTSAFXProjectSubmittedDataID == -1)
                {
                    objTSAFXProjectSubmittedData = db.TSAFXProjectSubmittedDatas.First(c => c.SAFXProjectID == objTSAFXProjectInputData.SAFXProjectID && c.SAFXProjectInputDataID == objTSAFXProjectInputData.ID);
                }
                else
                {
                    objTSAFXProjectSubmittedData = db.TSAFXProjectSubmittedDatas.Single(c => c.ID == intTSAFXProjectSubmittedDataID);
                }

                if (objTSAFXProjectSubmittedData == null)
                {
                    throw new ArgumentException("No available data in table TSAFXProjectSubmittedData for operation -- update");
                }

                objTSAFXProjectSubmittedData.FieldValue = objTSAFXProjectInputData.FieldValue;
                objTSAFXProjectSubmittedData.LastModifiedBy = strUser;
                objTSAFXProjectSubmittedData.LastModifiedDate = DateTime.Now;
            }

            db.SubmitChanges();
        }
    }


}
