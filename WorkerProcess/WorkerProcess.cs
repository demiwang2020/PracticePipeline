using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DataAggregator;
using HotFixLibrary;
using NetFxSetupLibrary;
using NetFxSetupLibrary.Patch;
using NetFxSetupLibrary.ProductTesting;
using ScorpionDAL;
using Helper;
using System.Diagnostics;
using UploadAndDownloadFilesToWI;

namespace WorkerProcess
{
    public class WorkerProcess
    {
        #region Data Member
        public string LCUPatchLocations { get; set; }
        public string Custom1Data { get; set; }

        private static int MAXPATHLENGTH = 500;
        public DataBuilder objDataBuilder { get; set; }
        private List<TargetOS> lstTargetOS { get; set; }
        public string PatchTechnology { get; set; }

        private HotFixUtility.ApplicationType appType { get; set; }
        public long TFSID { get; private set; }
        public string TargetArchitecture { get; private set; }
        private Architecture arch { get; set; }
        private string Owner = "redmond\\vsulab";
        private string NewSharePath;

        public long JobID { get; set; }

        #region Patch Runtime Data Member

        public string PatchBuildNumber { get; private set; }
        //public string Owner { get; private set; }
        public string KBNumber { get; private set; }
        public string PatchLocation { get; private set; }
        public string MSPLocation { get; private set; }
        public TargetProduct TargetProduct { get; private set; }
        public string GeneratedPatchPath { get; private set; }
        public int TargetProductID { get; private set; }
        public string ProductSPLevel { get; private set; }
        public string PatchTargetArchitecture { get; private set; }
        public string TestGroup { get; private set; }
        public string RunNotes { get; private set; }
        public string ContextInfo { get; private set; }
        public string TestMatrixCreatedBy { get; private set; }
        public string TestMatrixName { get; private set; }
        public bool UseNebula { get; private set; }

        //add below 3 properties for kicking off LDR
        public bool IsHotfix { get; private set; }
        public bool IsGDRSetup { get; private set; }
        public string LDRBuildNumberSetup { get; private set; }
        public string LDRContextInfo { get; private set; }

        public string ListofFilesGDRVersionSetup { get; private set; }
        public string ListofFilesLDRVersionSetup { get; private set; }
        public string ListofFilesSetup { get; private set; }

        public string CurrentTargetProductName { get; set; }
        public short CurrentTargetProductID { get; set; }
        #endregion Patch Runtime Data Member

        #region Patch SAFX Data Member

        public string IsLDR { get; private set; }
        public string MSPFileName { get; private set; }
        public string KBNumberPrependKB { get; private set; }
        public string PatchBuildNumberSAFX { get; private set; }
        public string PatchTargetArchitectureSAFX { get; private set; }
        public string PatchFullPath { get; private set; }
        public string LDRBuildNumber { get; private set; }
        public string ListofFiles { get; private set; }
        public string ListofFilesGDRVersion { get; private set; }
        public string ListofFilesLDRVersion { get; private set; }
        public string FrameworkFamily { get; private set; }
        public string ReleaseType { get; private set; }

        #endregion Patch SAFX Data Member

        #region Patch SAFX Extra Data Member
        public SAFXIntegration.SAFXIntegration objSAFXIntegration { get; set; }
        #endregion

        #region Product Runtime Data Member

        public string ProductSchema { get; private set; }
        public string PreviousProduct { get; private set; }
        public string PreviousProduct_File { get; private set; }
        public string CurrentProduct { get; private set; }

        public string CurrentBuild { get; private set; }

        public string CurrentProduct_File { get; private set; }
        public string VariablesFilePath { get; private set; }
        public string InputFile { get; private set; }
        public string KBListFile { get; private set; }
        public string RRType { get; private set; }
        public string PackageType { get; private set; }
        public string PayloadType { get; private set; }
        public List<string> MatrixList { get; private set; }
        public NDP45x NDP45xProduct { get; private set; }
        public List<int> SubmissionIDList { get; private set; }
        public bool IsDualBranch { get; private set; }
        public string RunOwner { get; private set; }
        public bool IsPreinstall { get; private set; }

        #endregion

        #endregion Data Member

        #region Constructor

        //For patch testing
        public WorkerProcess(HotFixUtility.ApplicationType enumAppType, long lgTFSID, string strTargetArchitecture)
        {
            appType = enumAppType;
            TFSID = lgTFSID;
            TargetArchitecture = strTargetArchitecture;

            Architecture result = Architecture.X86;
            bool canParse = Enum.TryParse(TargetArchitecture.ToUpper(), out result);
            if (!canParse)
            {
                //statements for it cannot be parsed as Enum 
                throw new Exception("TargetArchitecture can't be parsed as Enum Architectures.");
            }
            else
            {
                arch = result;
            }

            RunOwner = "VSULAB";
        }

        //For product testing
        public WorkerProcess(HotFixUtility.ApplicationType enumAppType, string schema, string previousProduct, string product, string package, string payload, string inputFile, string kbListFile, string buildnumber = "0", string runOwner = "VSULAB", bool isPreinstall = false)
        {
            appType = enumAppType;
            ProductSchema = schema;
            PreviousProduct = previousProduct;
            RunOwner = runOwner;
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                PreviousProduct_File = db.TProductConfigMappings.Where(p => p.TargetProduct == PreviousProduct).First<TProductConfigMapping>().ProductFile;
            }
            CurrentProduct = product;
            PackageType = package;
            PayloadType = payload;
            InputFile = inputFile;
            KBListFile = kbListFile;
            NewSharePath = string.Empty;
            SubmissionIDList = new List<int>();
            IsPreinstall = isPreinstall;
            CurrentBuild = buildnumber;
        }

        //For product SAFX
        public WorkerProcess(HotFixUtility.ApplicationType enumAppType, string inputFile)
        {
            appType = enumAppType;
            InputFile = inputFile;
            RunOwner = "VSULAB";

            NDP45xProduct = new NDP45x();
            NDP45xProduct.GenerateSAFXInputs(inputFile);
        }

        #endregion Constructor

        #region Interface to UI or other callers like Job Handler

        public void LoadData()
        {
            switch (appType)
            {
                case HotFixUtility.ApplicationType.SetupTest:
                    LoadPatchRuntimeData();
                    break;

                case HotFixUtility.ApplicationType.SAFX:
                    LoadSAFXData();
                    break;

                case HotFixUtility.ApplicationType.ProductSetupTest:
                    LoadProductRuntimeData();
                    break;
                default:
                    break;
            }
            return;
        }

        public void KickOffRuns()
        {
            switch (appType)
            {
                case HotFixUtility.ApplicationType.SetupTest:
                    KickOffRuntimeTest();
                    break;

                case HotFixUtility.ApplicationType.SAFX:
                    KickOffSAFXTest();
                    break;

                case HotFixUtility.ApplicationType.ProductSetupTest:
                    KickOffProductRuntimeTest();
                    break;
                default:
                    break;
            }
            return;
        }

        public void LoadDataForPopulatePatch()
        {
            if (objDataBuilder == null)
                objDataBuilder = new DataBuilder(Convert.ToInt32(TFSID), arch);
            PatchSmoke objPatchSmoke = objDataBuilder.GetPatchSmokeObject(arch);
            if (objPatchSmoke != null)
            {
                TargetProduct = objPatchSmoke.TargetProd;
                ProductSPLevel = objPatchSmoke.TargetProd.ProductSPLevel;
                TargetProductID = this.GetProductID(string.Format("Microsoft {0} {1}", objPatchSmoke.TargetProd.ProductName, objPatchSmoke.TargetProd.SKU), objPatchSmoke.TargetProd.ProductSPLevel);
                lstTargetOS = objPatchSmoke.TargetOperatingSystems;
                PatchBuildNumber = objPatchSmoke.BuildNumber;
                KBNumber = objPatchSmoke.KbNumber.TrimStart(new char[] { 'K', 'B', 'k', 'b' });
                PatchLocation = objPatchSmoke.FullPath;
                MSPLocation = objPatchSmoke.MSPPath;
                PatchTechnology = objPatchSmoke.PatchTechnology;
                IsHotfix = objPatchSmoke.IsLdr;
                IsGDRSetup = !objPatchSmoke.IsLdr & objPatchSmoke.IsDualBranch;
                LDRBuildNumberSetup = objPatchSmoke.LdrBuildNumber;
                ListofFilesGDRVersionSetup = ConvertFileList(objPatchSmoke, false);
                ListofFilesLDRVersionSetup = ConvertFileList(objPatchSmoke, true);
                UseNebula = CanUseNebula(TargetProductID, ProductSPLevel, PatchTechnology);
                //Populate TestMatrixCreatedBy & TestMatrixName
                PopulateTargetTestMatrixDetails();

                CreateExtraContextInfo();

                GeneratePatchPath(arch);
            }
        }

        #endregion Interface to UI or other callers like Job Handler

        #region Interface to Data Aggregator layer

        //Calls Data Aggregator layer to populate Patch Runtime data members
        private void LoadPatchRuntimeData() {
            if (objDataBuilder == null)
                objDataBuilder = new DataBuilder(Convert.ToInt32(TFSID));
            PatchSmoke objPatchSmoke = objDataBuilder.GetPatchSmokeObject(arch);
            if (objPatchSmoke != null) {
                TargetProduct = objPatchSmoke.TargetProd;
                PatchBuildNumber = objPatchSmoke.BuildNumber;
                KBNumber = objPatchSmoke.KbNumber.TrimStart(new char[] { 'K', 'B', 'k', 'b' });
                PatchLocation = objPatchSmoke.FullPath;
                MSPLocation = objPatchSmoke.MSPPath;
                //#if DEBUG
                //                //Hard code
                //                MSPLocation = @"\\vsufile\Workspace\Current\SetupTest\ExtractLocation\KB2686827\30319.278\0\NDP40-KB2686827.msp";
                //#endif
                //
                //"Microsoft .NET Framework 3.0" 
                TargetProductID = this.GetProductID(string.Format("Microsoft {0} {1}", objPatchSmoke.TargetProd.ProductName, objPatchSmoke.TargetProd.SKU), objPatchSmoke.TargetProd.ProductSPLevel);
                ProductSPLevel = objPatchSmoke.TargetProd.ProductSPLevel;
                PatchTargetArchitecture = objPatchSmoke.TargetArch;
                TestGroup = objPatchSmoke.TestGroupName.Length > 45 ? objPatchSmoke.TestGroupName.Substring(0, 45) : objPatchSmoke.TestGroupName;
                //RunNotes = objPatchSmoke.;
                //ContextInfo = objPatchSmoke.;
                //TestMatrixCreatedBy = objPatchSmoke.;
                //TestMatrixName = objPatchSmoke.;
                lstTargetOS = objPatchSmoke.TargetOperatingSystems;
                PatchTechnology = objPatchSmoke.PatchTechnology;
                UseNebula = CanUseNebula(TargetProductID, objPatchSmoke.TargetProd.ProductSPLevel, PatchTechnology);
                IsHotfix = objPatchSmoke.IsLdr;
                IsGDRSetup = !objPatchSmoke.IsLdr & objPatchSmoke.IsDualBranch;
                LDRBuildNumberSetup = objPatchSmoke.LdrBuildNumber;
                ListofFilesGDRVersionSetup = ConvertFileList(objPatchSmoke, false);
                ListofFilesLDRVersionSetup = ConvertFileList(objPatchSmoke, true);
                if (String.IsNullOrEmpty(ListofFilesGDRVersionSetup)) {
                    ListofFilesGDRVersionSetup = ListofFilesLDRVersionSetup;
                }
                else if (String.IsNullOrEmpty(ListofFilesLDRVersionSetup)) {
                    ListofFilesLDRVersionSetup = ListofFilesGDRVersionSetup;
                }

                ListofFilesSetup = objPatchSmoke.FileList.Where(file => file.PatchArchitecture.ToLower().Equals(TargetArchitecture.ToLower())).Aggregate("", (current, file) => current + (file.FileName + ","));
                ListofFilesSetup = RemoveDupExtraFileList(ListofFilesSetup.TrimEnd(','), false);

                if (objPatchSmoke.MspFileName.Contains(@"NDP40-")) {
                    string argumentForCheckNDP40 = objPatchSmoke.WorkItemId+" "+ objPatchSmoke.MSPPath.ToString().Replace(" ", "");
                    Utility.ExecuteCommandSync(@"\\clrdrop\ddrelqa\v-wanqya\CheckNDP40ClientOrExtend\CheckNDP40ClientOrExtend.exe", argumentForCheckNDP40, 360000);
                }

                //LCU patch location
                LCUPatchLocations = objPatchSmoke.LCUPatchLocation;

                //Populate TestMatrixCreatedBy & TestMatrixName
                PopulateTargetTestMatrixDetails();

                CreateExtraContextInfo();

                GeneratePatchPath(arch);

            }

            return;
        }

        //Calls Data Aggregator layer to populate Patch SAFX data members
        private void LoadPatchSAFXData()
        {
            objSAFXIntegration = new SAFXIntegration.SAFXIntegration(1);

            if (objDataBuilder == null)
                objDataBuilder = new DataBuilder(Convert.ToInt32(TFSID));
            PatchSAFX objPatchSAFX = objDataBuilder.GetPatchSAFXObject(arch);

            if (objPatchSAFX != null)
            {
                LCUPatchLocations = objPatchSAFX.LCUPatchLocation;
                if (!string.IsNullOrEmpty(LCUPatchLocations) && LCUPatchLocations.EndsWith(".exe"))
                {
                    int kbnumIndex = LCUPatchLocations.ToLower().LastIndexOf("kb");
                    string kbNum = LCUPatchLocations.Substring(kbnumIndex, 9);
                    //string newLCU = LCUPatchLocations.Remove(LCUPatchLocations.ToLower().IndexOf("patch\\kb"));
                    //newLCU = newLCU.Replace("boxs", "msps");
                    //LCUPatchLocations = Path.Combine(newLCU, "msp_" + kbNum.ToLower() + "_net.msp");
                    string newLCU = LCUPatchLocations.Substring(0, LCUPatchLocations.ToLower().LastIndexOf("-"));
                    LCUPatchLocations = newLCU + ".msp";
                }
                Custom1Data = objPatchSAFX.Custom1Data;
                IsLDR = objPatchSAFX.IsLdr.ToString();
                MSPFileName = objPatchSAFX.MspFileName;
                MSPLocation = objPatchSAFX.MSPPath;
                //#if DEBUG
                //                //Hard code
                //                MSPFileName = @"NDP40-KB2686827.msp";
                //#endif
                KBNumberPrependKB = objPatchSAFX.KbNumber;
                //if (System.IO.Path.GetFileNameWithoutExtension(objPatchSAFX.FullPath).ToLower().Contains("v2"))
                //    KBNumberPrependKB += "v2";
                Regex rx = new Regex(@"-v\d{1,}-");
                Match match = rx.Match(System.IO.Path.GetFileNameWithoutExtension(objPatchSAFX.FullPath).ToLower());
                if (match.Success)
                {
                    KBNumberPrependKB += match.Value.Trim(new char[] { '-' });
                }

                PatchBuildNumberSAFX = objPatchSAFX.BuildNumber;
                PatchTargetArchitectureSAFX = objPatchSAFX.TargetArch;
                PatchFullPath = objPatchSAFX.FullPath;
                LDRBuildNumber = objPatchSAFX.LdrBuildNumber;
                ListofFiles = HandleLongPath(RemoveDupExtraFileList(objPatchSAFX.FileList, false), "SAFXFiles");

                string strFileListGdrVersion = objPatchSAFX.FileListGdrVersion;
                string strFileListLdrVersion = objPatchSAFX.FileListLdrVersion;
                if (String.IsNullOrEmpty(strFileListGdrVersion))
                {
                    strFileListGdrVersion = strFileListLdrVersion;
                }
                else if (String.IsNullOrEmpty(strFileListLdrVersion))
                {
                    strFileListLdrVersion = strFileListGdrVersion;
                }

                ListofFilesGDRVersion = HandleLongPath(RemoveDupExtraFileList(strFileListGdrVersion, true), "SAFXFilesGDRVersion");
                ListofFilesLDRVersion = HandleLongPath(RemoveDupExtraFileList(strFileListLdrVersion, true), "SAFXFilesLDRVersion");
                //UploadFileToWI(((int)TFSID),ListofFilesGDRVersion);
                //UploadFileToWI(((int)TFSID),ListofFilesLDRVersion);

                FrameworkFamily = objPatchSAFX.TargetFrameworkVersion;
                PatchTechnology = objPatchSAFX.PatchTechnology;
                ReleaseType = objPatchSAFX.ReleaseType;
            }

            return;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tfsid"></param>
        /// <param name="filePath"></param>
        public void UploadFileToWI(int tfsid, string filePath)
        {

            string fileName = Path.GetFileName(filePath);
            UploadFiles upload = new UploadFiles();
            string TFUrl = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
            upload.ConnectWithDefaultCreds(TFUrl);

            upload.AddAttachment(tfsid, filePath);

        }

        /// <summary>
        /// if the path is too long, save it to a txt file
        /// </summary>
        /// <param name="path">the path to handle</param>
        /// <param name="fileName">the file name, to save the long path if need</param>
        /// <returns></returns>
        private string HandleLongPath(string path, string fileName)
        {
            if (path.Length > MAXPATHLENGTH)
            {
                string fileLocation = System.IO.Path.GetDirectoryName(this.MSPLocation);
                string fullFilePath = Path.Combine(fileLocation, fileName + ".txt");
                WriteFile(fullFilePath, path);

                return fullFilePath;
            }
            else
            {
                return path;
            }
        }

        private void LoadProductRuntimeData()
        {
            // For HFR testing, input file and KBList file is not ready. 
            // So prepare them and then follow the usual product test workflow
            if (objDataBuilder != null && objDataBuilder.IsRefreshRedistHFR)
            {
                LoadHFRRuntimeData();
            }

            NDP45xProduct = new NDP45x(CurrentProduct);
            NDP45xProduct.GenerateNDP45xProduct(CurrentProduct, ProductSchema, InputFile, KBListFile);

            CurrentProduct_File = CopyToShareLocation(Path.Combine(new string[] { ConfigurationManager.AppSettings["ProductSharedPath"].ToString(), CurrentProduct }), Path.Combine(Path.GetTempPath(), CurrentProduct + ".xml"));
            VariablesFilePath = Path.GetDirectoryName(CurrentProduct_File);
            NDP45xProduct.GenerateNDP45xVariableFile(VariablesFilePath);
            RRType = NDP45xProduct.RRType;
            if (RRType.ToUpperInvariant() == "HFR" || RRType.ToUpperInvariant() == "HF")
                PayloadType = "LDR";
            IsDualBranch = NDP45xProduct.IsDualBranch;
            MatrixList = NDP45x.GetRefreshRedistMatrixList(RRType, PackageType);
        }

        /// <summary>
        /// generate patch config and fill KBNumberFile EnvironmentVariable
        /// </summary>
        private void GeneratePatchPath(Architecture arch)
        {
            GeneratedPatchPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.MSPLocation), string.Format("KB{0}.xml", KBNumber));
            ProductSkuPackage package = new ProductSkuPackage
            {
                Name = PatchTechnology,
                Path = PatchLocation
            };
            ProductSku sku = new ProductSku
            {
                Package = new ProductSkuPackage[] { package },
                Name = "Patch"
            };

            Product product = new Product
            {
                Name = "Patch",
                Schema = arch == Architecture.ARM ? "ARMPatchSchema.xml" : "PatchSchema.xml",
                Common = new ProductCommon(),
                Sku = new ProductSku[] { sku }
            };
            XMLHelper.XmlSerializeToFile(product, GeneratedPatchPath, typeof(Product));
            //UploadFileToWI(((int)TFSID), GeneratedPatchPath);

        }

        #endregion Interface to Data Aggregator layer

        #region Interface to PTAT Engine to Kick off Runtime Test

        public enum SPLevel
        {
            M3 = 0,
            BETA = 0,
            Preview = 0,
            RC = 0,
            RTM = 0,
            SP1 = 1,
            SP2 = 2,
            SP3 = 3
        }

        public enum TargettedCPU
        {
            all = 0,
            x86 = 1,
            amd64 = 2,
            ia64 = 3,
            arm
        }

        /// <summary>
        /// Kicks off runs for Setup test
        /// </summary>
        private void KickOffRuntimeTest()
        {
            if (IsKickoffViaNetFxSetup())
            {
                KickOffNetFxSetupRun();
            }
            else
            {
                KickOffBeaconRuntimeRun();
            }
        }


        /// <summary>
        /// Creates runs for Setup test
        /// </summary> 
        private void CreateRuns(bool isLDR)
        {
            #region Set local variales default value
            int MaddogDBID = 2;
            string VerificationOption = "PatchVerify";
            string VerificationScript = @"\\vsufile\Workspace\Current\HOTFIX_FIXED\PatchVerify.exe";
            //string Owner = "redmond\vsulab";
            bool PauseOnFailure = false;

            bool AddStrongNameHijack = false;
            bool CreateRuns = true;
            UseNebula = false;
            bool SaveToTPN = false;
            string SelectedLanguages = "All Languages;";
            ArrayList TargettedLanguageIDs = new ArrayList();
            TargettedLanguageIDs.Add("0");

            #endregion

            #region Set variables based on properties
            ArrayList TestMatrixNames = new ArrayList();
            TestMatrixNames.Add(TestMatrixName);

            int SPLevel = (int)Enum.Parse(typeof(SPLevel), ProductSPLevel);
            int TargettedCPU = (int)Enum.Parse(typeof(TargettedCPU), TargetArchitecture.ToLower());
            string buildNumber = string.Empty;
            string contextInfo = string.Empty;
            string strWorkItemFiledValue = string.Empty;
            #endregion

            #region Kick off runs

            if (!isLDR)
            {
                buildNumber = this.PatchBuildNumber;
                if (IsHotfix)
                {
                    contextInfo = this.LDRContextInfo;
                    strWorkItemFiledValue = string.Format("{0}-KB{1}-{2}-{3}", new object[] { TFSID, KBNumber, TargetArchitecture, "Hotfix" });
                }
                else
                {
                    contextInfo = this.ContextInfo;
                    strWorkItemFiledValue = string.Format("{0}-KB{1}-{2}-{3}", new object[] { TFSID, KBNumber, TargetArchitecture, "GDR" });
                }

            }
            else
            {
                buildNumber = this.LDRBuildNumberSetup;
                contextInfo = this.LDRContextInfo;
                strWorkItemFiledValue = string.Format("{0}-KB{1}-{2}-{3}", new object[] { TFSID, KBNumber, TargetArchitecture, "LDR" });
            }

            HotFixLibrary.Patch objPatch =
                new HotFixLibrary.Patch(KBNumber, buildNumber, Owner, strWorkItemFiledValue,
                                       PatchLocation, MSPLocation, VerificationOption,
                                       VerificationScript, TargetProductID, SPLevel, TargettedCPU,
                                       TargettedLanguageIDs, appType,
                                       TestMatrixNames, PauseOnFailure, RunNotes, contextInfo,
                                       SelectedLanguages);

            objPatch.AddStrongNameHijackPackage = AddStrongNameHijack;
            objPatch.ProcessAuthoringFiles(MaddogDBID);
            objPatch.PrepareTestMatrix();
            objPatch.CreateContext(appType);
            objPatch.CreateRunInfo(MaddogDBID, JobID);
            HotFixUtility.CreateContextFile(objPatch);
            HotFixUtility.SaveToDB(objPatch);

            if (CreateRuns)
            {
                UseNebula = CanUseNebula(TargetProductID, ProductSPLevel, PatchTechnology);

                ArrayList arlRunInfo = HotFixUtility.CreateRun(objPatch, UseNebula);

                if (SaveToTPN)
                {
                    HotFixUtility.SaveRunToTPN(arlRunInfo);
                }
            }

            #endregion

            //afterr Kick off Run in Worker Process is done, 
            //update the Patch Record in TPatch table with the 
            //JobID, Status ID, Result ID and Percent Complete
            UpdateTPatchDefaultStatus(objPatch.PatchInfo.TTestProdInfoID);
            //Update TPatchFiles
            UpdateTPatchFileDefaultStatus(objPatch.PatchInfo.TTestProdInfoID);
            //Update TRuns
            UpdateTRunDefaultStatus(objPatch.PatchInfo.TTestProdInfoID);
            return;
        }

        #endregion Interface to PTAT Engine to Kick off Runtime Test

        #region Interface to PTAT Engine to Kick off SAFX Test

        /// <summary>
        /// Kick off runs for SAFX test
        /// </summary>
        private void KickOffSAFXTest()
        {
            if (String.IsNullOrEmpty(InputFile))
            {
                KickOffPatchSAFXTest();
            }
            else
            {
                KickOffProductSAFXTest();
            }
        }

        /// <summary>
        /// Kicks off runs for patch SAFX test
        /// </summary>
        private void KickOffPatchSAFXTest()
        {
            /// Perform the same operation as per this function btnSubmit_Click, extracted from ServicingPortal\SAFXIntegration\SAFXInputData.aspx.cs
            if (objSAFXIntegration == null)
            {
                throw new Exception("objSAFXIntegration is null in \"KickOffSAFXTest\"");
            }

            #region Set local variables defalut value
            //string Owner = "redmond\vsulab";
            string strKBNumber = string.Empty;
            #endregion

            #region Update Field Value For SAFXProject
            TSAFXProject objTSAFXProject = objSAFXIntegration.TSAFXProject;
            List<TSAFXProjectInputData> lstTSAFXProjectInputData = objTSAFXProject.TSAFXProjectInputDatas.ToList();
            //Set PatchArchitecture value since there is a drodownlist type for this field

            string specificFiles30 = "";
            string specificFileVersions30 = "";
            if (!string.IsNullOrEmpty(Custom1Data))
            {
                var custom1DataArry = this.Custom1Data.Split(';');
                for (int i = 3; i < custom1DataArry.Length; i++)
                {
                    if (i == 3)
                    {
                        specificFiles30 += custom1DataArry[i].ToString();
                        specificFileVersions30 = specificFileVersions30 + "(" + custom1DataArry[i].ToString() + "-" + custom1DataArry[0].ToString() + "." + custom1DataArry[2].ToString() + ")";
                    }
                    else if (i > 3)
                    {
                        specificFiles30 = specificFiles30 + "," + custom1DataArry[i].ToString();
                        specificFileVersions30 = specificFileVersions30 + "," + "(" + custom1DataArry[i].ToString() + "-" + custom1DataArry[0].ToString() + "." + custom1DataArry[2].ToString() + ")";
                    }
                }
            }

            foreach (TSAFXProjectInputData _TSAFXProjectInputData in lstTSAFXProjectInputData)
            {
                if (_TSAFXProjectInputData.FieldName.ToUpper() == "MSPNAME")
                {
                    _TSAFXProjectInputData.FieldValue = this.MSPFileName;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "ISLDR")
                {
                    _TSAFXProjectInputData.FieldValue = this.IsLDR;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "LCUPATCHLOCATION")
                {
                    _TSAFXProjectInputData.FieldValue = this.LCUPatchLocations;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "CUSTOM1DATA")
                {
                    _TSAFXProjectInputData.FieldValue = this.Custom1Data;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "KBNUMBER")
                {
                    _TSAFXProjectInputData.FieldValue = this.KBNumberPrependKB;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "PATCHBUILDNUMBER")
                {
                    _TSAFXProjectInputData.FieldValue = this.PatchBuildNumberSAFX;
                }
                else if (_TSAFXProjectInputData.FieldValue.Contains("<REF TCPU DecaturCPU>"))
                {
                    _TSAFXProjectInputData.FieldValue = TargetArchitecture;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "ABSOLUTEPATCHLOCATION")
                {
                    _TSAFXProjectInputData.FieldValue = this.PatchFullPath;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "LDRPATCHBUILDNUMBER")
                {
                    _TSAFXProjectInputData.FieldValue = this.LDRBuildNumber;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "SPECIFICFILES")
                {
                    if (string.IsNullOrEmpty(specificFiles30))
                        _TSAFXProjectInputData.FieldValue = this.ListofFiles;
                    else if (!String.IsNullOrEmpty(this.ListofFiles))
                        _TSAFXProjectInputData.FieldValue = this.ListofFiles + "," + specificFiles30;
                    else
                        _TSAFXProjectInputData.FieldValue = specificFiles30;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "SPECIFICFILEVERSIONS")
                {
                    _TSAFXProjectInputData.FieldValue = this.ListofFilesGDRVersion;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "SPECIFICFILEVERSIONSLDRCAB")
                {
                    _TSAFXProjectInputData.FieldValue = this.ListofFilesLDRVersion;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "FRAMEWORKFAMILY")
                {
                    _TSAFXProjectInputData.FieldValue = this.FrameworkFamily;
                }
                else if (_TSAFXProjectInputData.FieldName.ToUpper() == "RELEASETYPE")
                {
                    _TSAFXProjectInputData.FieldValue = this.ReleaseType;
                }
            }
            #endregion

            #region Kick off Run
            int intRunID = objSAFXIntegration.KickOffSAFXRun(Owner, ref strKBNumber);

            //store the submitted data to DB after kicking off run successfully.
            if (intRunID > 0)
            {
                objSAFXIntegration.InsertTSAFXProjecSubmittedData(Owner, intRunID, strKBNumber, JobID, true);
            }
            #endregion

            return;
        }

        /// <summary>
        /// Kick off runs for product SAFX test
        /// </summary>
        private void KickOffProductSAFXTest()
        {
            string strKBNumber = string.Empty;

            int intRunID = objSAFXIntegration.KickOffProductSAFXRun(Owner, ref strKBNumber);

            if (intRunID > 0)
            {
                objSAFXIntegration.InsertTSAFXProjecSubmittedData(Owner, intRunID, strKBNumber, JobID, true);
            }
        }

        #endregion Interface to PTAT Engine to Kick off SAFX Test

        #region Interface to PTAT Engine to Kick off Product Runtime Test

        private void KickOffProductRuntimeTest()
        {
            if (RRType.ToUpperInvariant() == "RU")
            {
                foreach (string package in NDP45xProduct.PackageList)
                {
                    if (IsDualBranch)
                    {
                        SubmissionIDList.Add(KickOffNetFxSetupProductRuntimeRun(package, "GDR"));
                        SubmissionIDList.Add(KickOffNetFxSetupProductRuntimeRun(package, "LDR"));
                    }
                    else
                    {
                        SubmissionIDList.Add(KickOffNetFxSetupProductRuntimeRun(package, "GDR"));
                    }
                }
            }
            else
            {
                //For HFR, since each package has an independent TFS id, and they are tested seperately,
                //we kick off runs only for one package each time
                SubmissionIDList.Add(KickOffNetFxSetupProductRuntimeRun(PackageType, "LDR"));
            }
        }

        #endregion Interface to PTAT Engine to Kick off SAFX Test

        #region Private Member Function

        private void KickOffBeaconRuntimeRun()
        {
            /// Perform the same operation as per this function SaveButton_Click, extracted from ServicingPortal\HotfixPages\SetupTestSubmit.aspx.cs

            #region Kick off runs
            //Creates runs for GDR
            CreateRuns(false);
            if (IsGDRSetup && this.PatchTechnology.Equals("MSI"))
            {
                //Creates runs for LDR
                CreateRuns(true);
            }
            #endregion

            //afterr Kick off Run in Worker Process is done, 
            //update the Patch Record in TPatch table with the 
            //JobID, Status ID, Result ID and Percent Complete 
        }

        private void KickOffNetFxSetupRun()
        {
            int targettedCPU = (int)Enum.Parse(typeof(TargettedCPU), TargetArchitecture.ToLower());
            InputData inputData = new InputData() { Data = new List<InputDataItem>() };

            if (targettedCPU == 4)
            {
                //ARM machine
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["ARMMachineSetup1"].ToString(), FieldType = "Command" });
                inputData.Data.Add(new InputDataItem { FieldName = "Reboot", FieldValue = "Reboot", FieldType = "Reboot" });
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["ARMMachineSetup2"].ToString(), FieldType = "Command" });
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["CopyFiles_ARM"].ToString(), FieldType = "Command" });
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = string.Format("reg.exe Import {0}", ConfigurationManager.AppSettings["OnlyUseLatestCLR"].ToString()), FieldType = "Command" });
            }

            inputData.Data.Add(new InputDataItem { FieldName = "KBNumberFile", FieldValue = GeneratedPatchPath, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "RegistryFilePath", FieldValue = string.IsNullOrEmpty(verificationFilePath.RegistryFilePath) ? "empty" : verificationFilePath.RegistryFilePath, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "KBNumber", FieldValue = this.KBNumber, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "RunID", FieldValue = "[RunID]", FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = String.Format("reg.exe Import {0}", ConfigurationManager.AppSettings["DisablePerSessionTempDir"].ToString()), FieldType = "Command" });
            inputData.Data.Add(new InputDataItem { FieldName = "LCUScriptPath", FieldValue = this.verificationFilePath.LCUInstallScriptPath, FieldType = "EnvironmentVariable" });

            bool isContainLDR = IsGDRSetup && this.PatchTechnology.Equals("MSI");
            //bool tempIsHotfix = IsHotfix;
            int netFxSetupPatchInfoID = 0;
            string targetProduct, targetProductNameInTitle;
            targetProductNameInTitle = targetProduct = string.Join(" ", TargetProduct.ProductName, TargetProduct.SKU, TargetProduct.ProductSPLevel);

            if (!string.IsNullOrWhiteSpace(CurrentTargetProductName))
            {
                targetProductNameInTitle = CurrentTargetProductName;
                targetProduct = CurrentTargetProductName.Replace("Microsoft .NET Framework", ".NET Framework");
                #region comment out special codes for hotfix since all .NET SKUs are moving to single branch
                //if (CurrentTargetProductName.IndexOf("Hotfix Rollup") > 0 && CurrentTargetProductID < 33)
                //{
                //    isContainLDR = false;
                //    tempIsHotfix = true;
                //}
                #endregion
            }

            //For CBS packages, install their products if necessary
            if (PatchTechnology.Equals("CBS"))
            {
                inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = string.Format("{0} \"{1}\"", ConfigurationManager.AppSettings["InstallProductForCBSTest"].ToString(), targetProduct), FieldType = "Command" });
            }

            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                #region mapping target product config and fill Product_Sku/Product_File Environment Variable

                string architecture = arch.ToString();
                var configItem = db.TProductConfigMappings.FirstOrDefault(p => p.TargetProduct == targetProduct && p.Architecture == ((architecture == "X86" || architecture == "AMD64") ? "X86;AMD64" : architecture));
                if (configItem == null)
                    throw new Exception(string.Format("Miss the configration for target product {0} and architecture {1}, please fill the info into table TProductConfigMapping", targetProduct, architecture));
                inputData.Data.Add(new InputDataItem { FieldName = "Product_Sku", FieldValue = configItem.ProductSKU, FieldType = "EnvironmentVariable" });
                inputData.Data.Add(new InputDataItem { FieldName = "Product_File", FieldValue = configItem.ProductFile, FieldType = "EnvironmentVariable" });

                #endregion

                //as far as now, 4.5.1/4.5.2 need point to FullredistISV, 40 and 45 point to FullRedist
                string productPackage = "FullRedist";
                if (Utility.IsNDP45AboveFamework(targetProduct))
                {
                    productPackage = "FullRedistISV";
                }
                inputData.Data.Add(new InputDataItem { FieldName = "Product_Package", FieldValue = productPackage, FieldType = "EnvironmentVariable" });

                #region store patch info to db
                var patchInfo = new TNetFxSetupPatchInfo();
                patchInfo.KBNumber = KBNumber;
                patchInfo.PatchTechnology = PatchTechnology;
                patchInfo.PatchLocation = PatchLocation;
                patchInfo.TargetProduct = targetProduct;
                patchInfo.ProductPackage = productPackage;
                patchInfo.CPUID = targettedCPU;
                patchInfo.TestMatrixName = TestMatrixName;
                patchInfo.CreatedBy = "vsulab";
                patchInfo.CreatedDate = DateTime.Now;
                patchInfo.LastModifiedBy = patchInfo.CreatedBy;
                patchInfo.LastModifiedDate = patchInfo.CreatedDate;

                db.TNetFxSetupPatchInfos.InsertOnSubmit(patchInfo);
                db.SubmitChanges();
                netFxSetupPatchInfoID = patchInfo.ID;
                #endregion
            }

            string runTitlePrefix = string.Format("{0}-{1}- [Target {2}] ", TFSID.ToString(), System.IO.Path.GetFileNameWithoutExtension(PatchLocation), targetProductNameInTitle);
            PatchIntegration integration = new PatchIntegration(targettedCPU, TestMatrixName, PatchTechnology);

            integration.OperatePatchRuns(isContainLDR, IsHotfix, verificationFilePath.VersionFilePath, runTitlePrefix, inputData, JobID, netFxSetupPatchInfoID);
        }

        private bool IsKickoffViaNetFxSetup()
        {
            //switch for beacon
            int majorVersion = int.Parse(TargetProduct.SKU.Substring(0, 1));
            if (ConfigurationManager.AppSettings["Switch_KickoffRuntimeRunViaBeacon"].ToString().ToUpper().Equals("ON"))
            {
                return false;
            }
            return TargetProduct.ProductName == ".NET Framework" && (
                        (majorVersion >= 4)
                        || (TargetProduct.SKU.Equals("3.0") && TargetProduct.ProductSPLevel.Equals("SP2"))
                        || (TargetProduct.SKU.Equals("3.5") && TargetProduct.ProductSPLevel.Equals("SP1"))
                        || (TargetProduct.SKU.Equals("2.0") && TargetProduct.ProductSPLevel.Equals("SP2")));
        }

        private bool CanUseNebula(int intTargetProductID, string ProductSPLevel, string strPatchTechnology)
        {
            //Use Nebula for following products
            //if (
            //    (TargetProductID == 1 && ProductSPLevel.Equals("SP2")) || //Product = NDP2.0
            //    (TargetProductID == 2 && ProductSPLevel.Equals("SP2")) || //Product = NDP3.0
            //    (TargetProductID == 3 && ProductSPLevel.Equals("SP1")) || //Product = NDP3.5
            //    TargetProductID == 8 || //Product = NDP4.0
            //    TargetProductID == 17 || //Product = NDP4.5 RC
            //    TargetProductID == 18 //Product = NDP4.5 RTM
            //   )
            return true;
            //else
            //    return false;
        }

        /// <summary>
        /// TestMatrix used hard code in function PopulateTargetTestMatrixDetails
        /// </summary>
        public class TestMatrix
        {
            //Test Matrix Name
            public string TestMatrixName { get; set; }
            //Owner
            public string Owner { get; set; }
            //Target Product
            public string TargetProduct { get; set; }
            //Target OS
            public string TargetOS { get; set; }
            //Target Patch Type
            public string TargetPatchType { get; set; }
        }

        /// <summary>
        /// Populates target test matrix's createdby and textmatrix name
        /// </summary>
        private void PopulateTargetTestMatrixDetails()
        {
            ///ToDo: Populate TestMatrixCreatedBy & TestMatrixName based on the information in TargetProductID, ProductSPLevel & PatchLocation (MSI or CBS)?
            ///based in info at this wiki http://devdiv/sites/netfx/Servicing/test/NetFX%20Servicing%20Test%20Wiki/Servicing%20Test%20Matrices.aspx

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                if (PatchTechnology != "CBS" && PatchTechnology != "MSI" && PatchTechnology != "OCM")
                {
                    throw new Exception(string.Format("PatchTechnology:{0} now not support.", PatchTechnology));
                }

                var product = dataContext.TProducts.SingleOrDefault(c => c.ProductID == TargetProductID);

                if (product != null)
                {
                    string targetProduct = string.Format("{0} {1}", product.BriqsProduct, this.ProductSPLevel);

                    var testMatrixs = (from a in dataContext.TTestMatrixes
                                       join b in dataContext.TSmokeMatrixFilters on a.TestMatrixID equals b.TestMatrixID
                                       where b.TargetProduct == targetProduct
                                       && b.PatchTechnology == PatchTechnology
                                       && a.MaddogDBID == 2
                                       && a.Active == true
                                       select new TestMatrix { TargetOS = b.TargetOSes, TargetPatchType = PatchTechnology, TargetProduct = targetProduct, TestMatrixName = a.TestMatrixName });

                    if (PatchTechnology == "MSI")
                    {
                        var matrix = testMatrixs.FirstOrDefault();

                        if (matrix == null)
                        {
                            throw new Exception(string.Format("Matrix do not exist, PatchTechnology is {0} and target product is {1}", PatchTechnology, targetProduct));
                        }

                        this.TestMatrixName = matrix.TestMatrixName;
                        return;
                    }

                    #region For CBS and OCM Patches
                    if (PatchTechnology == "CBS" || PatchTechnology == "OCM")
                    {
                        foreach (var item in lstTargetOS)
                        {
                            var matrix = testMatrixs.Where(p => p.TargetOS.IndexOf(string.Format("{0} {1}", item.OSName, item.OSSPLevel)) >= 0).FirstOrDefault();
                            if (matrix != null)
                            {
                                this.TestMatrixName = matrix.TestMatrixName;
                                return;
                            }
                            else
                            {
                                throw new Exception(string.Format("Matrix do not exist, PatchTechnology is {0} and target product is {1} and target os is {2}", PatchTechnology, targetProduct, string.Format("{0} {1}", item.OSName, item.OSSPLevel)));
                            }
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// Gets Product ID with product friendly name
        /// </summary>
        /// <param name="strProductFriendlyName">product friendly name(e.g.:Microsoft .NET Framework 3.0)</param>
        /// <returns>Product ID</returns>
        private int GetProductID(string strProductFriendlyName, string strSPLevel)
        {
            ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext();

            var product = from prod in dataContext.TProducts
                          where prod.ProductFriendlyName.ToLower().Contains(strProductFriendlyName.ToLower())
                          select prod;

            //dataContext.TProducts.All(c => c.ProductFriendlyName.ToLower().Contains(strProductFriendlyName.ToLower()));
            if (product.Count() == 1)
            {
                return (product.ToList<TProduct>())[0].ProductID;
            }
            else
            {
                TProduct objProduct =
                    dataContext.TProducts.SingleOrDefault(
                        prod => prod.ProductFriendlyName.ToLower().Contains(strProductFriendlyName.ToLower()) &&
                        prod.ProductFriendlyName.ToLower().Contains(strSPLevel.ToLower()) &&
                        !prod.ProductFriendlyName.ToLower().Contains(strProductFriendlyName.ToLower() + ".")
                        );
                return objProduct.ProductID;
            }
        }

        public VerificationFilePath verificationFilePath;
        /// <summary>
        /// Creates extra context info( includes context info and binaries with its version)
        /// (Pending)
        /// </summary>
        private void CreateExtraContextInfo() {

            #region Create Version Verification file

            //Create Version Verification Directory 
            //Set these file location same as msp location
            string strFilePath = File.Exists(this.MSPLocation) ? Path.GetDirectoryName(this.MSPLocation) : this.MSPLocation;

            CreateFileDirectory(strFilePath);

            verificationFilePath = new VerificationFilePath();
            Dictionary<string, string> versionFilePath = new Dictionary<string, string>();

            //For registry
            if (PatchTechnology == "MSI") {
                //Registry
                string strRegistry = PopulateRegistryString();
                string strRegistryFileName = string.Format(@"Registry_{0}_{1}.txt", TargetArchitecture, DateTime.Now.ToString("mmssfff"));
                string strRegistryFileFullPath = System.IO.Path.Combine(strFilePath, strRegistryFileName);
                WriteFile(strRegistryFileFullPath, strRegistry);

                verificationFilePath.RegistryFilePath = strRegistryFileFullPath;
                //UploadFileToWI(((int)TFSID), strRegistryFileFullPath);

                //For GDR
                string strVerificationGDRString = PopulateBinariesString(false);
                string strGDRFileName = string.Format(@"FileVersion_{0}_GDR.txt", TargetArchitecture);
                string strGDRVersionFileFullPath = System.IO.Path.Combine(strFilePath, strGDRFileName);
                WriteFile(strGDRVersionFileFullPath, strVerificationGDRString);
                versionFilePath.Add("GDR", strGDRVersionFileFullPath);
                //UploadFileToWI(((int)TFSID), strGDRVersionFileFullPath);

                this.ContextInfo = string.Format(ContextInfoStringFormat.GDR_HOTFIX_MSI, strGDRVersionFileFullPath, strRegistryFileFullPath);

                if (IsGDRSetup || IsHotfix) {
                    string strVerificationLDRString = PopulateBinariesString(true);
                    string strLDRFileName = string.Format(@"FileVersion_{0}_LDR.txt", TargetArchitecture);
                    string strLDRVersionFileFullPath = System.IO.Path.Combine(strFilePath, strLDRFileName);
                    WriteFile(strLDRVersionFileFullPath, strVerificationLDRString);
                    this.LDRContextInfo = string.Format(ContextInfoStringFormat.LDR_MSI, strLDRVersionFileFullPath, strRegistryFileFullPath);
                    if (!IsHotfix)
                        this.LDRContextInfo += "#PROD0.MINOR=1";
                    versionFilePath.Add("LDR", strLDRVersionFileFullPath);
                    //UploadFileToWI(((int)TFSID), strLDRVersionFileFullPath);
                }
            }
            else if (PatchTechnology == "CBS") {
                //For GDR
                string strVerificationGDRString = PopulateBinariesString(false);
                string strGDRFileName = string.Format(@"FileVersion_{0}_GDR.txt", TargetArchitecture);
                string strGDRVersionFileFullPath = System.IO.Path.Combine(strFilePath, strGDRFileName);
                WriteFile(strGDRVersionFileFullPath, strVerificationGDRString);
                versionFilePath.Add("GDR", strGDRVersionFileFullPath);
                //UploadFileToWI(((int)TFSID), strGDRVersionFileFullPath);
                this.ContextInfo = string.Format(ContextInfoStringFormat.GDR_HOTFIX_CBS, strGDRVersionFileFullPath);

                if (IsGDRSetup || IsHotfix) {
                    string strVerificationLDRString = PopulateBinariesString(true);
                    string strLDRFileName = string.Format(@"FileVersion_{0}_LDR.txt", TargetArchitecture);
                    string strLDRVersionFileFullPath = System.IO.Path.Combine(strFilePath, strLDRFileName);
                    WriteFile(strLDRVersionFileFullPath, strVerificationLDRString);
                    this.LDRContextInfo = string.Format(ContextInfoStringFormat.LDR_CBS, strLDRVersionFileFullPath);
                    if (!IsHotfix)
                        this.LDRContextInfo += "#PROD0.MINOR=1";

                    versionFilePath.Add("LDR", strLDRVersionFileFullPath);
                    //UploadFileToWI(((int)TFSID), strLDRVersionFileFullPath);
                }
            }
            else if (PatchTechnology == "OCM") {
                string strVerificationGDRString = PopulateBinariesString(true);
                string strGDRFileName = string.Format(@"FileVersion_{0}_GDR.txt", TargetArchitecture);
                string strGDRVersionFileFullPath = System.IO.Path.Combine(strFilePath, strGDRFileName);
                WriteFile(strGDRVersionFileFullPath, strVerificationGDRString);
                //UploadFileToWI(((int)TFSID), strGDRVersionFileFullPath);

                this.ContextInfo = string.Format(ContextInfoStringFormat.GDR_HOTFIX_CBS, strGDRVersionFileFullPath);
            }

            verificationFilePath.VersionFilePath = versionFilePath;
            #endregion

            #region Generate LCU installation script

            string lCUScriptPath = Path.Combine(strFilePath, "InstallLCU.bat");
            if (String.IsNullOrEmpty(LCUPatchLocations)) //generate an empty script file
            {
                WriteFile(lCUScriptPath, "exit /B 0");
            }
            else {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("call {0} /quiet /norestart", LCUPatchLocations);
                sb.AppendLine();
                sb.AppendFormat("exit /B %ERRORLEVEL%");

                WriteFile(lCUScriptPath, sb.ToString());
            }
            verificationFilePath.LCUInstallScriptPath = lCUScriptPath;
            //UploadFileToWI(((int)TFSID), lCUScriptPath);
            #endregion

            return;
        }

        /// <summary>
        /// it can be removed when we can get the File list and its version info too in PatchSmokeObject
        /// </summary>
        /// <param name="forLDR"></param>
        /// <returns></returns>
        private string PopulateBinariesString(bool forLDR = false)
        {
            StringBuilder strVerificationString = new StringBuilder();
            string[] FileAndVersionList;
            if (!forLDR)
            {
                //For 2.0SP1 is not dual branch ,just include ldr payload 
                if (!IsGDRSetup && !IsHotfix)
                    FileAndVersionList = ListofFilesLDRVersionSetup.Split(new char[] { ',' });
                else
                    FileAndVersionList = ListofFilesGDRVersionSetup.Split(new char[] { ',' });
            }
            else
                FileAndVersionList = ListofFilesLDRVersionSetup.Split(new char[] { ',' });
            FileAndVersionList = FileAndVersionList.Distinct().ToArray();
            foreach (string file in FileAndVersionList)
            {
                string[] fileInfo = file.Trim(new char[] { '(', ')' }).Split(new char[] { '-' });

                if (fileInfo.Length < 2)
                    continue;

                var fileLoactions = SearchDBForFileLocations(fileInfo[0], fileInfo[1]);

                if (fileLoactions.Count() > 0)
                {
                    foreach (var f in fileLoactions)
                    {
                        strVerificationString.AppendLine(string.Format("{0}#{1}", System.IO.Path.Combine(f, fileInfo[0]), fileInfo[1]));
                    }
                }
                else
                {
                    strVerificationString.AppendLine(string.Format("File {0} is not exist in database", fileInfo[0]));
                }
            }

            return strVerificationString.ToString();
        }

        /// <summary>
        /// Search from table SANFileLocation for .net file path. Specially handling for 3.5/3.0/2.0 files
        /// </summary>
        private List<string> SearchDBForFileLocations(string fileName, string fileVersion)
        {
            //Product ID = 1: 2.0
            //Product ID = 2: 3.0
            //Product ID = 3: 3.5

            int productID = TargetProductID;
            string spLevel = ProductSPLevel;

            // for 2.0/3.0/3.5 CBS patches, they could be cumulative
            if (TargetProductID < 4 && this.PatchTechnology == "CBS")
            {
                switch (fileVersion.Substring(0, 3))
                {
                    case "2.0":
                        productID = 1;
                        spLevel = "SP2";
                        break;

                    case "3.0":
                        productID = 2;
                        spLevel = "SP2";
                        break;

                    case "3.5":
                        productID = 3;
                        spLevel = "SP1";
                        break;
                }
            }

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var files = (from r in dataContext.SANFileLocations
                             where r.ProductID == productID
                             && r.ProductSPLevel == spLevel
                             && r.CPUID == (int)Enum.Parse(typeof(TargettedCPU), TargetArchitecture.ToLower())
                             && r.FileName == fileName
                             && r.Active == true
                             select r.FileLocation).ToList();
                return files;
            }
        }

        /// <summary>
        /// Populate x86 binaries for x64 & ia64
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        private string PopulateX86BinariesString(string[] fileInfo)
        {
            StringBuilder strX86BinariesString = new StringBuilder();
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var files = from r in dataContext.SANFileLocations
                            where r.ProductID == TargetProductID
                            && r.ProductSPLevel == ProductSPLevel
                            && r.CPUID == (int)TargettedCPU.x86
                            && r.FileName == fileInfo[0]
                            select r;
                foreach (var f in files)
                {
                    strX86BinariesString.AppendLine(string.Format("{0}#{1}", System.IO.Path.Combine(f.FileLocation, f.FileName), fileInfo[1]));
                }
            }
            return strX86BinariesString.ToString();
        }

        /// <summary>
        /// Creates file directory
        /// </summary>
        /// <param name="strFilePath"></param>
        private void CreateFileDirectory(string strFilePath)
        {
            if (!System.IO.Directory.Exists(strFilePath))
            {
                System.IO.Directory.CreateDirectory(strFilePath);
            }
        }

        /// <summary>
        /// Writes file context
        /// </summary>
        /// <param name="strFileFullPath">file full path</param>
        /// <param name="strContext">context which want to write in</param>
        private void WriteFile(string strFileFullPath, string strContext)
        {
            using (System.IO.TextWriter textWriter = new System.IO.StreamWriter(strFileFullPath, false))
            {
                textWriter.Write(strContext);
                textWriter.Close();
            }
        }

        /// <summary>
        /// Updates TPatch's Status, Result, PercentCompleted with default values
        /// </summary>
        /// <param name="patchID">Patch ID</param>
        /// <returns></returns>
        private bool UpdateTPatchDefaultStatus(int patchID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var patch = dataContext.TTestProdInfos.SingleOrDefault(c => c.TTestProdInfoID == patchID);
                patch.JobID = this.JobID;
                patch.StatusID = (int)Helper.RunStatus.Running;
                patch.ResultID = (int)Helper.RunResult.Unknown;
                patch.PercentCompleted = 0;
                dataContext.SubmitChanges();
            }
            return true;
        }

        private bool UpdateTPatchFileDefaultStatus(int patchID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var patchFiles = from c in dataContext.TTestProdAttributes
                                 where c.TTestProdInfoID == patchID
                                 select c;
                foreach (var patchfile in patchFiles)
                {
                    patchfile.StatusID = (int)Helper.RunStatus.Running;
                    patchfile.ResultID = (int)Helper.RunResult.Unknown;
                    patchfile.PercentCompleted = 0;
                }
                dataContext.SubmitChanges();
            }
            return true;
        }

        private bool UpdateTRunDefaultStatus(int patchID)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                var patchFiles = from c in dataContext.TTestProdAttributes
                                 where c.TTestProdInfoID == patchID
                                 select c;
                foreach (var patchfile in patchFiles)
                {
                    var runs = from c in dataContext.TRuns
                               where c.TTestProdAttributesID == patchfile.TestProdAttributesID
                               select c;
                    foreach (var run in runs)
                    {
                        run.RunStatusID = (int)Helper.RunStatus.Running;
                        run.RunResultID = (int)Helper.RunResult.Unknown;
                    }
                }
                dataContext.SubmitChanges();
            }
            return true;
        }


        /// <summary>
        /// Get File list
        /// </summary>
        /// <param name="objPatchSmoke">PatchSmoke</param>
        /// <param name="isLDR">is for LDR</param>
        /// <returns></returns>
        private string ConvertFileList(PatchSmoke objPatchSmoke, bool isLDR)
        {
            string filesWithVersions = "";
            if (!isLDR && objPatchSmoke.IsDualBranch) // means this is a GDR
                filesWithVersions = objPatchSmoke.FileList.Where(file => !file.IsLDRPayload && file.PatchArchitecture.ToLower().Equals(TargetArchitecture.ToLower() == "amd64" ? "x64" : TargetArchitecture.ToLower())).Aggregate(filesWithVersions, (current, file) => current + string.Format("{0}-{1},", file.FileName, file.FileVersion));
            else if (isLDR && objPatchSmoke.IsDualBranch) // this is the LDR case for the GDR
                filesWithVersions = objPatchSmoke.FileList.Where(file => file.IsLDRPayload && file.PatchArchitecture.ToLower().Equals(TargetArchitecture.ToLower() == "amd64" ? "x64" : TargetArchitecture.ToLower())).Aggregate(filesWithVersions, (current, file) => current + string.Format("{0}-{1},", file.FileName, file.FileVersion));
            else if (isLDR && !objPatchSmoke.IsDualBranch) // this is the hotfix case
                filesWithVersions = objPatchSmoke.FileList.Where(file => file.PatchArchitecture.ToLower().Equals(TargetArchitecture.ToLower() == "amd64" ? "x64" : TargetArchitecture.ToLower())).Aggregate(filesWithVersions, (current, file) => current + string.Format("{0}-{1},", file.FileName, file.FileVersion));

            return RemoveDupExtraFileList(filesWithVersions.TrimEnd(','), true);
        }

        /// <summary>
        /// remove duplicates and extra files
        /// </summary>
        /// <param name="strFileList">
        /// strFileList format:
        /// 1.without version: 
        /// system.dll,system.web.dll,clr.dll
        /// 2.with version:
        /// case 1 : system.dll-2.0.30729.3688,system.web.dll-2.0.30729.3688
        /// case 2 : (system.dll-2.0.30729.3688),(system.web.dll-2.0.30729.3688)
        /// </param>
        /// <param name="IsFileWithVersion"></param>
        /// <returns></returns>
        private string RemoveDupExtraFileList(string strFileList, bool IsFileWithVersion)
        {
            string result = "";
            strFileList = strFileList.Replace("(", "").Replace(")", "");
            string[] fileInfoList = strFileList.Split(new char[] { ',' });
            fileInfoList = fileInfoList.Distinct().ToArray();
            List<string> lst = new List<string>();
            if (IsFileWithVersion)
            {
                for (int i = 0; i < fileInfoList.Length; i++)
                {
                    string[] fileInfo = fileInfoList[i].Split(new char[] { '-' });
                    //filter extra file
                    if (fileInfo[0].ToLower() == "setup.exe"
                        || fileInfo[0].ToLower() == "setupengine.dll"
                        || fileInfo[0].ToLower() == "setupui.dll"
                        || fileInfo[0].ToLower() == "netfxupdate.exe"
                        || fileInfo[0].ToLower() == "setregni.exe"
                        || fileInfo[0].ToLower() == "togac.exe")
                    {
                        continue;
                    }

                    //change sy52106.dll to system.dll for 1.1 and 1.0
                    if (fileInfo[0].ToLower() == "sy52106.dll")
                    {
                        fileInfoList[i] = fileInfoList[i].Replace("sy52106.dll", "system.dll");
                    }

                    //only for dll and exe files
                    if (System.IO.Path.GetExtension(fileInfo[0]).ToLower() != ".dll"
                        && System.IO.Path.GetExtension(fileInfo[0]).ToLower() != ".exe")
                    {
                        continue;
                    }

                    //resource file also except
                    if (fileInfo[0].ToLower().IndexOf(".resources.dll") > 0)
                    {
                        continue;
                    }

                    lst.Add(fileInfoList[i]);
                }
                result = lst.Aggregate(result, (current, s) => current + string.Format("({0}),", s));
            }
            else
            {
                for (int i = 0; i < fileInfoList.Length; i++)
                {
                    //filter extra file
                    if (fileInfoList[i].ToLower() == "setup.exe"
                        || fileInfoList[i].ToLower() == "setupengine.dll"
                        || fileInfoList[i].ToLower() == "setupui.dll"
                        || fileInfoList[i].ToLower() == "netfxupdate.exe"
                        || fileInfoList[i].ToLower() == "setregni.exe"
                        || fileInfoList[i].ToLower() == "togac.exe")
                    {
                        continue;
                    }

                    //change sy52106.dll to system.dll for 1.1 and 1.0
                    if (fileInfoList[i].ToLower() == "sy52106.dll")
                    {
                        fileInfoList[i].Replace("sy52106.dll", "system.dll");
                    }

                    //only for dll and exe files
                    if (System.IO.Path.GetExtension(fileInfoList[i]).ToLower() != ".dll"
                        && System.IO.Path.GetExtension(fileInfoList[i]).ToLower() != ".exe")
                    {
                        continue;
                    }

                    //resource file also except
                    if (fileInfoList[i].ToLower().IndexOf(".resources.dll") > 0)
                    {
                        continue;
                    }

                    lst.Add(fileInfoList[i]);
                }
                result = lst.Aggregate(result, (current, s) => current + string.Format("{0},", s));
            }

            return result.TrimEnd(',');
        }

        private class ContextInfoStringFormat
        {
            public static string GDR_HOTFIX_CBS = "VERSIONVERIFICATION={0}";
            public static string LDR_CBS = "VERSIONVERIFICATION={0}";
            public static string GDR_HOTFIX_MSI = "VERSIONVERIFICATION={0}#REGISTRYVERIFICATION={1}";
            public static string LDR_MSI = "VERSIONVERIFICATION={0}#REGISTRYVERIFICATION={1}";
        }

        /// <summary>
        /// Populate registry string by product
        /// </summary>
        /// <returns></returns>
        private string PopulateRegistryString() {
            StringBuilder strRegistryString = new StringBuilder();
            string strNotSupportException = "Product ID: {0} - Product SP Level: {1} - Architecture: {2} doesn't support.";

            //some patch's name called like 'Windows8.1-KB2908850-v3.msu', and registry node is named with its KB number plus version
            //thus, we need to catch the version to generate currect verify item
            //registry example: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Updates\Microsoft .NET Framework 4.5.1\KB{0}\ThisVersionInstalled#Y
            string kbNumber = this.KBNumber + Regex.Match(System.IO.Path.GetFileNameWithoutExtension(this.PatchLocation), @"v\d+", RegexOptions.IgnoreCase).Value.Trim();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext()) {
                //if this patch need to target multi product, currentTargetProductID would be set a value but zero
                //other wise, this patch is only target one product set in RM
                if (CurrentTargetProductID == 0) {
                    CurrentTargetProductID = (short)TargetProductID;
                }

                var queryNeededRegistryVarify = (from a in dataContext.TPatchVerifyRegistries
                                                 where a.ProductID == CurrentTargetProductID
                                                 && a.ProductSPLevel.Equals(ProductSPLevel.ToUpper())
                                                 && a.Arch.Equals(TargetArchitecture.ToUpper())
                                                 select a).ToList();

                if (queryNeededRegistryVarify.Count() == 0) {
                    throw new Exception(string.Format(strNotSupportException, CurrentTargetProductID, ProductSPLevel, TargetArchitecture));
                }

                //do specific trigger in NDP40
                //if (CurrentTargetProductID == 8 && ProductSPLevel.ToUpper() == "RTM") {
                //    Dictionary<string, string> dicProducts = GetTargetProductCodeMSIs_MSP(MSPLocation);
                //    if (dicProducts.Count <= 0) {
                //        throw new Exception(string.Format(strNotSupportException, CurrentTargetProductID, ProductSPLevel, TargetArchitecture));
                //    }

                //    queryNeededRegistryVarify = (from a in queryNeededRegistryVarify
                //                                 join b in dicProducts on a.SpecificTrigger equals b.Value.ToUpper()
                //                                 select a).ToList();
                //}

                foreach (var item in queryNeededRegistryVarify) {
                    strRegistryString.AppendLine(string.Format(item.RegistryKey, kbNumber));
                }
            }
            return strRegistryString.ToString();
        }

        /// <summary>
        /// Get associated ProductCode and MSIs of the MSP -- MSP
        /// </summary>
        /// <param name="strMSPPath"></param> 
        /// <returns></returns>
        //private Dictionary<string, string> GetTargetProductCodeMSIs_MSP(string strMSPPath) {
        //    string[] targetProductCodes = MsiUtils.GetTargetProductCodes(strMSPPath);
        //    Dictionary<string, string> dictTargetProductCodeMSI = new Dictionary<string, string>();

        //    PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
        //    foreach (string targetProductcode in targetProductCodes) {
        //        string _targetProductcode = targetProductcode.Trim('{', '}');
        //        var varProductMSIs = from productMSI in db.TProductMSIs
        //                             where productMSI.MSIProductCode == _targetProductcode
        //                             select productMSI;
        //        if (varProductMSIs != null && varProductMSIs.Count() > 0) {
        //            dictTargetProductCodeMSI.Add(targetProductcode, ((TProductMSI)(varProductMSIs.First())).MSIName);

        //        }
        //    }
        //    return dictTargetProductCodeMSI;
        //}

        private string CopyToShareLocation(string share, string xmlPath)
        {
            if (String.IsNullOrEmpty(NewSharePath))
                NewSharePath = CalculateSharePath(share);

            string destination = Path.Combine(NewSharePath, new FileInfo(xmlPath).Name);
            try
            {
                if (!Directory.Exists(NewSharePath)) // if this share already exists then don't create it again.
                    Directory.CreateDirectory(NewSharePath);

                File.Copy(xmlPath, destination, true);
            }
            catch (Exception eX)
            {
                throw new Exception(eX.Message);
            }

            return destination;
        }

        /// <summary>
        /// Calculate share path based on \\share\[folder name 0, 1, 2, ... n]
        /// </summary>
        /// <param name="share">share path</param>
        /// <returns>share path with the folder name of the highest non-existent directory</returns>
        private string CalculateSharePath(string share)
        {
            string sharePath;
            int counter = 0;
            for (; ; counter++) // this goes on until we find a directory that does not exist
            {
                sharePath = Path.Combine(share, counter.ToString(CultureInfo.InvariantCulture));
                if (!Directory.Exists(sharePath))
                    break;
            }
            return sharePath;
        }

        private int KickOffNetFxSetupProductRuntimeRun(string package, string payloadType)
        {
            int submissionID = 0;
            InputData inputData = new InputData() { Data = new List<InputDataItem>() };
            inputData.Data.Add(new InputDataItem { FieldName = "CurrentProduct", FieldValue = CurrentProduct, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "CurrentProduct_File", FieldValue = CurrentProduct_File, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "PreviousProduct", FieldValue = PreviousProduct, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "PreviousProduct_File", FieldValue = PreviousProduct_File, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "Package", FieldValue = package, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "RefreshRedistReleaseTypes", FieldValue = RRType, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "PayloadType", FieldValue = payloadType, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "VariablesFilePath", FieldValue = VariablesFilePath, FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "RunID", FieldValue = "[RunID]", FieldType = "EnvironmentVariable" });
            inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["CopyFiles"].ToString(), FieldType = "Command" });
            inputData.Data.Add(new InputDataItem { FieldName = "Command", FieldValue = ConfigurationManager.AppSettings["DisableWarning"].ToString(), FieldType = "Command" });
            inputData.Data.Add(new InputDataItem { FieldName = "BuildNumber", FieldValue = CurrentBuild, FieldType = "EnvironmentVariable" });

            string runTitle = string.Format("AutoKickOff-{0}-{3}-{1}-{2}-", CurrentProduct, package, payloadType, CurrentBuild);
            List<string> matrixNamesList = NDP45x.GetRefreshRedistMatrixList(RRType, package);

            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                string matrixNames = string.Empty;
                foreach (string matrix in matrixNamesList)
                {
                    matrixNames += matrix + ';';
                }

                var prodInfo = new TNetFxSetupProductInfo();
                prodInfo.CurrentProduct = CurrentProduct;
                prodInfo.CurrentProduct_File = CurrentProduct_File;
                prodInfo.PreviousProduct = PreviousProduct;
                prodInfo.PreviousProduct_File = PreviousProduct_File;
                prodInfo.Package = package;
                prodInfo.RefreshRedistReleaseTypes = RRType;
                prodInfo.PayloadType = payloadType;
                prodInfo.Matrix_Names = matrixNames;
                prodInfo.CreatedBy = RunOwner;
                prodInfo.CreatedDate = DateTime.Now;
                prodInfo.LastModifiedBy = prodInfo.CreatedBy;
                prodInfo.LastModifiedDate = prodInfo.CreatedDate;
                prodInfo.BuildNumber = GetBuildNumberFromVariableFile(VariablesFilePath, CurrentProduct, payloadType);

                db.TNetFxSetupProductInfos.InsertOnSubmit(prodInfo);
                db.SubmitChanges();
                submissionID = prodInfo.ID;
            }

            ProductIntegration intergration = new ProductIntegration(matrixNamesList, CurrentProduct, PreviousProduct, IsPreinstall);

            //Set special data for HFR
            if (objDataBuilder != null && objDataBuilder.IsRefreshRedistHFR)
            {
                intergration.JobID = JobID;
                intergration.IsHotfixRollup = true;

                //generate a special title for HFR
                if (TFSID != 0)
                {
                    runTitle = string.Format("{0}-{1}-{2}-", TFSID, CurrentProduct, package);
                }
            }

            intergration.OperateManualProductRun(inputData, runTitle, submissionID, RunOwner);
            return submissionID;
        }

        private string GetBuildNumberFromVariableFile(string variableFolder, string currentProduct, string payload)
        {
            string buildNumber = string.Empty;
            string path = Path.Combine(variableFolder, string.Format("Variables_{0}_{1}.txt", currentProduct, payload));
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (String.IsNullOrEmpty(line))
                            continue;
                        string[] temp = line.Split('=');
                        if (temp.Length == 2 && !string.IsNullOrEmpty(temp[0].Trim()) && !string.IsNullOrEmpty(temp[1].Trim()))
                        {
                            if ("BUILDNUMBER" == temp[0].Trim().ToUpperInvariant())
                            {
                                buildNumber = temp[1].Trim();
                                break;
                            }
                        }
                    }
                }
            }
            return buildNumber;
        }

        #region HFR related private functions

        private void LoadHFRRuntimeData()
        {
            RefreshRedistHFRInfo hfrInfo = objDataBuilder.GetRefreshRedistHFRInfo();
            string sku = hfrInfo.TargetProd.SKU;

            //schema
            ProductSchema = QueryHFRConfigurationValue(sku, "Schema");

            //Previous Product
            PreviousProduct = QueryHFRConfigurationValue(sku, "PreviousProduct");
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                PreviousProduct_File = db.TProductConfigMappings.Where(p => p.TargetProduct == PreviousProduct).First<TProductConfigMapping>().ProductFile;
            }

            //Current Product
            CurrentProduct = String.Format("NDP{0}_HFR_{1}", sku.Replace(".", String.Empty), hfrInfo.KbNumber);

            //Package type
            if (hfrInfo.PackageType == RefreshRedistHFRType.RefreshRedistMSU)
            {
                foreach (var os in hfrInfo.TargetOperatingSystems)
                {
                    PackageType = QueryHFRConfigurationValue(sku, "InputPackageType", os.OSName);
                    if (!String.IsNullOrEmpty(PackageType))
                    {
                        break;
                    }
                }
            }
            else
            {
                PackageType = hfrInfo.PackageType.ToString();
            }

            PayloadType = "LDR";
            NewSharePath = string.Empty;
            SubmissionIDList = new List<int>();

            GenerateInputAndKBListFile(sku, hfrInfo);
        }

        private void GenerateInputAndKBListFile(string sku, RefreshRedistHFRInfo hfrInfo)
        {
            string fullRedistPackagePath, isvPackagePath, webPackagePath;
            fullRedistPackagePath = isvPackagePath = webPackagePath = null;
            List<string> lstMsuPackagePath = new List<string>();

            // Fullredist package is always needed
            fullRedistPackagePath = GetHFRPackagePath(sku, RefreshRedistHFRType.FullRedist, hfrInfo.BuildNumber);

            if (hfrInfo.PackageType == RefreshRedistHFRType.RefreshRedistMSU)
            {
                lstMsuPackagePath.Add(hfrInfo.PackagePath);
            }
            else
            {
                isvPackagePath = GetHFRPackagePath(sku, RefreshRedistHFRType.FullRedistISV, hfrInfo.BuildNumber);
                webPackagePath = GetHFRPackagePath(sku, RefreshRedistHFRType.Webbootstrapper, hfrInfo.BuildNumber);

                //Also get msu packages since SAFX may need it
                if (!String.IsNullOrEmpty(isvPackagePath))
                {
                    lstMsuPackagePath.AddRange(GetMsuPackagePathFromISVPath(isvPackagePath));
                }
            }

            string destPath = System.IO.Path.Combine(hfrInfo.ConfigSharePath, hfrInfo.KbNumber, hfrInfo.BuildNumber);
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            // Backup packages to shared folder
            if (hfrInfo.PackageType == RefreshRedistHFRType.FullRedist && File.Exists(fullRedistPackagePath))
            {
                File.Copy(fullRedistPackagePath, Path.Combine(destPath, Path.GetFileName(fullRedistPackagePath)), true);
            }
            else if (hfrInfo.PackageType == RefreshRedistHFRType.FullRedistISV && File.Exists(isvPackagePath))
            {
                File.Copy(isvPackagePath, Path.Combine(destPath, Path.GetFileName(isvPackagePath)), true);
            }
            else if (hfrInfo.PackageType == RefreshRedistHFRType.Webbootstrapper && File.Exists(webPackagePath))
            {
                File.Copy(webPackagePath, Path.Combine(destPath, Path.GetFileName(webPackagePath)), true);
            }

            InputFile = Path.Combine(destPath, String.Format("InputFile_{0}.txt", CurrentProduct));
            KBListFile = Path.Combine(destPath, String.Format("KBList_{0}.txt", CurrentProduct));

            using (StreamWriter sw = new StreamWriter(InputFile, false))
            {
                string line = GetInputFileItemString(sku, RefreshRedistHFRType.FullRedist, fullRedistPackagePath);
                if (!String.IsNullOrEmpty(line))
                    sw.WriteLine(line);

                line = GetInputFileItemString(sku, RefreshRedistHFRType.FullRedistISV, isvPackagePath);
                if (!String.IsNullOrEmpty(line))
                    sw.WriteLine(line);

                line = GetInputFileItemString(sku, RefreshRedistHFRType.Webbootstrapper, webPackagePath);
                if (!String.IsNullOrEmpty(line))
                    sw.WriteLine(line);

                foreach (string msuPath in lstMsuPackagePath)
                {
                    line = GetInputFileItemString(sku, RefreshRedistHFRType.RefreshRedistMSU, msuPath);
                    if (!String.IsNullOrEmpty(line))
                        sw.WriteLine(line);
                }

                sw.WriteLine("RefreshRedistReleaseTypes=HFR");
            }

            using (StreamWriter sw = new StreamWriter(KBListFile, false))
            {
                string line = GetKBListItemString(sku, RefreshRedistHFRType.FullRedist, fullRedistPackagePath);
                if (!String.IsNullOrEmpty(line))
                    sw.WriteLine(line);

                line = GetKBListItemString(sku, RefreshRedistHFRType.FullRedistISV, isvPackagePath);
                if (!String.IsNullOrEmpty(line))
                    sw.WriteLine(line);

                line = GetKBListItemString(sku, RefreshRedistHFRType.Webbootstrapper, webPackagePath);
                if (!String.IsNullOrEmpty(line))
                    sw.WriteLine(line);

                foreach (string msuPath in lstMsuPackagePath)
                {
                    line = GetKBListItemString(sku, RefreshRedistHFRType.RefreshRedistMSU, msuPath);
                    if (!String.IsNullOrEmpty(line))
                        sw.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Querty configuration value in table THFRConfigurations
        /// </summary>
        /// <param name="sku"></param>
        /// <param name="type"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        private string QueryHFRConfigurationValue(string sku, string type, string hint = null)
        {
            using (PatchTestDataClassDataContext db = new PatchTestDataClassDataContext())
            {
                var queryResult = String.IsNullOrEmpty(hint) ? db.THFRSmokeConfigurations.Where(p => p.SKU == sku && p.Type == type) : db.THFRSmokeConfigurations.Where(p => p.SKU == sku && p.Type == type && p.Hint == hint);
                return queryResult.Count() > 0 ? queryResult.First().Value : null;
            }
        }

        private string GetFirstFileOfDir(string path)
        {
            return Directory.GetFiles(path)[0];
        }

        private string GetHFRPackagePath(string sku, RefreshRedistHFRType hfrType, string buildNumber)
        {
            string[] buildNumberSplit = buildNumber.Split(new char[] { '.' });
            if (buildNumberSplit.Length != 2 && buildNumberSplit.Length != 3)
                return null;

            string value;

            try
            {
                switch (hfrType)
                {
                    case RefreshRedistHFRType.FullRedist:
                        value = QueryHFRConfigurationValue(sku, "FullRedistPath");
                        return GetFirstFileOfDir(String.Format(value, String.Format("{0}.{1}", buildNumberSplit[0], buildNumberSplit[1])));

                    case RefreshRedistHFRType.FullRedistISV:
                        if (buildNumberSplit.Length == 2)
                            return null;
                        value = QueryHFRConfigurationValue(sku, "FullRedistISVPath");
                        return GetFirstFileOfDir(String.Format(value, buildNumber));

                    case RefreshRedistHFRType.Webbootstrapper:
                        if (buildNumberSplit.Length == 2)
                            return null;
                        value = QueryHFRConfigurationValue(sku, "WebbootstrapperPath");
                        return GetFirstFileOfDir(String.Format(value, buildNumber));
                }
            }
            catch
            { }

            return null;
        }

        /// <summary>
        /// Get MSU packages from ISV path
        /// </summary>
        /// <param name="isvPath"></param>
        /// <returns></returns>
        private List<string> GetMsuPackagePathFromISVPath(string isvPath)
        {
            List<string> listPath = new List<string>();

            string msuPath = Path.Combine(Path.GetDirectoryName(isvPath), "MSU");
            foreach (string file in Directory.GetFiles(msuPath))
            {
                if (Path.GetFileName(file).ToLowerInvariant().Contains("x86"))
                {
                    listPath.Add(file);
                }
            }

            return listPath;
        }

        /// <summary>
        /// Retrive one line of InputFile.txt, based on hfr type and package path
        /// </summary>
        /// <param name="sku"></param>
        /// <param name="hfrType"></param>
        /// <param name="packagePath"></param>
        /// <param name="keyOS"></param>
        /// <returns></returns>
        private string GetInputFileItemString(string sku, RefreshRedistHFRType hfrType, string packagePath)
        {
            if (String.IsNullOrEmpty(packagePath))
                return null;

            if (hfrType == RefreshRedistHFRType.RefreshRedistMSU)
            {
                string msuInputName = QueryHFRConfigurationValue(sku, "InputPackageType", GetOSShortName(Path.GetFileName(packagePath).Split(new char[] { '-' })[0]));
                if (String.IsNullOrEmpty(msuInputName))
                    return null;

                return String.Format("{0}={1}", msuInputName, packagePath);
            }
            else
            {
                return String.Format("{0}={1}", hfrType.ToString(), packagePath);
            }
        }

        /// <summary>
        /// Retrive one line of KBList.txt, based on hfr type and package path
        /// </summary>
        /// <param name="sku"></param>
        /// <param name="hfrType"></param>
        /// <param name="packagePath"></param>
        /// <returns></returns>
        private string GetKBListItemString(string sku, RefreshRedistHFRType hfrType, string packagePath)
        {
            if (String.IsNullOrEmpty(packagePath))
                return null;

            string hint;
            if (hfrType == RefreshRedistHFRType.RefreshRedistMSU)
            {
                hint = Path.GetFileName(packagePath).Split(new char[] { '-' })[0];
                hint = GetOSShortName(hint);
            }
            else
            {
                hint = hfrType.ToString();
            }

            string kbName = QueryHFRConfigurationValue(sku, "KBName", hint);
            if (String.IsNullOrEmpty(kbName))
                return null;

            Regex rx = new Regex(@"-KB\d{5,}");
            Match match = rx.Match(System.IO.Path.GetFileNameWithoutExtension(packagePath));
            if (!match.Success)
                return null;
            string kbNumber = match.Value.Substring(3);


            return String.Format("{0}={1}", kbName, kbNumber);
        }

        private static string GetOSShortName(string osName)
        {
            return osName.Replace("Windows", "Win");
        }

        private void LoadSAFXData()
        {
            if (String.IsNullOrEmpty(InputFile)) //Input file is specially for product test 
            {
                LoadPatchSAFXData();
            }
            else
            {
                if (objDataBuilder != null && objDataBuilder.IsRefreshRedistHFR)
                {
                    LoadRefreshRedistHFRSAFXData();
                }
                else
                {
                    //For non-HFR product, so far it is kicked off from outside of WorkerProcess
                }
            }
        }

        private void LoadRefreshRedistHFRSAFXData()
        {
            objSAFXIntegration = new SAFXIntegration.SAFXIntegration(3);

            TSAFXProject objTSAFXProject = objSAFXIntegration.TSAFXProject;
            List<TSAFXProjectInputData> lstTSAFXProjectInputData = objTSAFXProject.TSAFXProjectInputDatas.ToList();
            string absolutePatchLocation = string.Empty;
            string anotherAbsolutePatchLocation = string.Empty;
            if (String.IsNullOrEmpty(NDP45xProduct.FullRedistISVPath))
            {
                absolutePatchLocation = NDP45xProduct.FullRedistPath;
                anotherAbsolutePatchLocation = string.Empty;
            }
            else
            {
                absolutePatchLocation = NDP45xProduct.FullRedistISVPath;
                anotherAbsolutePatchLocation = NDP45xProduct.FullRedistPath;
            }

            Regex regex = new Regex(@"KB\d*");
            string kbNumber = regex.Match(Path.GetFileNameWithoutExtension(NDP45xProduct.FullRedistPath).ToUpperInvariant()).Value;

            foreach (TSAFXProjectInputData TSAFXProjectInputData in lstTSAFXProjectInputData)
            {
                if (TSAFXProjectInputData.FieldName == "Release")
                {
                    TSAFXProjectInputData.FieldValue = "RTM";
                }
                else if (TSAFXProjectInputData.FieldName == "IsLDR")
                {
                    TSAFXProjectInputData.FieldValue = NDP45xProduct.IsLDR.ToString();
                }
                else if (TSAFXProjectInputData.FieldName == "REMOTEENLISTMENT")
                {
                    //For now, just simply hard code enlistment location
                    TSAFXProjectInputData.FieldValue = @"\\clrdrop\ddrelqa\drte\RahimB\SAFX_Servicing\SAFX_CommandLine\TestResources_RefreshRedistHotfix";
                }

                if (TSAFXProjectInputData.Active && TSAFXProjectInputData.DisplayToFrontEnd && !TSAFXProjectInputData.ReadOnly)
                {
                    if (TSAFXProjectInputData.FieldValue == "<UI>" || TSAFXProjectInputData.FieldValue == "<INTERNAL>")
                    {
                        switch (TSAFXProjectInputData.FieldName.ToLowerInvariant())
                        {
                            case "absolutepatchlocation":
                                TSAFXProjectInputData.FieldValue = absolutePatchLocation;
                                break;
                            case "anotherabsolutepatchlocation":
                                TSAFXProjectInputData.FieldValue = anotherAbsolutePatchLocation;
                                break;
                            case "ldrpatchbuildnumber":
                                TSAFXProjectInputData.FieldValue = NDP45xProduct.LDRVersion;
                                break;
                            case "releasetype":
                                TSAFXProjectInputData.FieldValue = "Hotfix Rollup";
                                break;
                            case "refreshredistsku":
                                TSAFXProjectInputData.FieldValue = String.IsNullOrEmpty(anotherAbsolutePatchLocation) ? "FullRedist" : "FullRedistISV";
                                break;
                            case "kbnumber":
                                TSAFXProjectInputData.FieldValue = kbNumber;
                                break;
                            case "patchbuildnumber":
                                TSAFXProjectInputData.FieldValue = NDP45xProduct.GDRVersion;
                                break;

                            case "mtpackpath":
                                TSAFXProjectInputData.FieldValue = NDP45xProduct.MTPackPath;
                                break;
                            case "sdkpath":
                                TSAFXProjectInputData.FieldValue = NDP45xProduct.SDKPath;
                                break;
                        }
                    }
                }
            }
        }

        #endregion

        #endregion Private Member Function

    }
}
