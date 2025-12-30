using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Data;
using System.Collections;

using ScorpionDAL;
using Wix = Microsoft.Tools.WindowsInstallerXml;

using LoggerLibrary;
namespace HotFixLibrary
{
    public class Patch
    {
        #region Data Members

        public bool AddStrongNameHijackPackage { set; get; }
        public bool TargetX86 { set; get; }
        public bool TargetX64 { set; get; }
        public bool TargetIA64 { set; get; }

        public string ExtraRunTokens = string.Empty;
        public string ExtraTestCaseSpecificData = string.Empty;

        public bool IsNDP45Patch = false;
        public bool IsNDP40Patch = false;
        public bool IsVS2010Patch = false;
        public bool IsVCRedistPatch = false;
        public string TFSServerURI { get; private set; }

        public HotFixUtility.ChainerType PatchChainerType;
 
        public bool IsPauseOnFailureOn { get; private set; }

        public HotFixUtility.ApplicationType appApplicationType { get; private set; }

        public TTestProdInfo PatchInfo { get; private set; }
       
        private string ProductBuild { get; set; }
        private string SignedPatchLocation { get; set; }
        private string VerificationScript { get; set; }
        private string Owner { get; set; }
        private string DDSFilePath { get; set; }
        private string SKUListPath { get; set; }
        public string MSPDDSFilePath { get; set; }

        public string Brand { get; private set; }
        public string ProductName { get; private set; }
        private int ProductID { get; set; }
        private string BeaconProductCode { get; set; }

        private ArrayList ChipList { get; set; }
        private Dictionary<string, string> ChipPackageName { get; set; }

        private short PatchID {get; set; }

        private string PU { get; set; }
        private int PUID { get; set; }

        private string WorkItem { get; set; }
        private ArrayList MatrixIDs { get; set; }
        private ArrayList TestMatrixNames { get; set; }
        private ArrayList TargettedProductLanguageIDs { get; set; }

        private string FileLockName { get; set; }
        private string ProductRepairFileName { get; set; }
        //TP TestCase specific.
        private string PayLoadFilex86 { get; set; }
        //Superseding TestCase Specifc
        public string SupersedingPatch { get; private set; }
        public string SupersedingMSP { get; private set; }
        public string SupersedingPatchKBNumber { get; private set; }

        public int MaddogDBID { get; private set; }

        #region Specific to SetupTest
        private string MSPLocation { get; set; }
        private string VerificationOption { get; set; }
        private int TargettedProductID { get; set; }
        private int TargettedProductSPLevel { get; set; }
        private int TargettedProductCPUID { get; set; }
        #endregion Specific to SetupTest

        #endregion Data Members

        #region Constructor

        public Patch(TTestProdInfo objPatch)
        {
            this.PatchInfo = objPatch;
        }

        //This Constructor is used for SetupTest (SetupTestSubmit.aspx.cs) 
        public Patch(string strKBNumber, string strPatchBuildNumber, string strOwner, string strWorkItem, string strPatchLocation,
            string strMSPLocation, string strVerificationOption, string strVerificationScript, int intTargettedProductID,
            int intTargettedProductSPLevel, int intTargettedProductCPUID, ArrayList arrIntTargettedLanguageIDs, HotFixUtility.ApplicationType appType,
            ArrayList arrStrTestMatrixName, bool blnIsPauseOnFailureOn, string strExtraRunTokens, string strExtraTestCaseSpecificData,
            string strSelectedLanguages)
        {
            MaddogDBID = 1; //Default to Whidbey.
            PatchInfo = new TTestProdInfo();
            PatchInfo.TestIdentifier = strKBNumber;
            PatchInfo.BuildNumber = strPatchBuildNumber;
            ProductBuild = strPatchBuildNumber;
            
            SignedPatchLocation = strPatchLocation;
            VerificationScript = strVerificationScript;
            PatchInfo.ProdVerificationScript = strVerificationScript;
            PatchInfo.DDSFilePath = "";
            PatchInfo.SKUFilePath = "";
            PatchInfo.ProdSPLevel = intTargettedProductSPLevel.ToString();
            
            Owner = strOwner;


            ChipList = new ArrayList();
            ChipPackageName = new Dictionary<string, string>();
            PatchID = -1;

            appApplicationType = appType;
            WorkItem = strWorkItem;

            ProductID = intTargettedProductID;

            MSPLocation = strMSPLocation;
            VerificationOption = strVerificationOption;
            TargettedProductID = intTargettedProductID;
            TargettedProductSPLevel = intTargettedProductSPLevel;
            TargettedProductCPUID = intTargettedProductCPUID;
            if(arrIntTargettedLanguageIDs != null)
                TargettedProductLanguageIDs = (ArrayList)arrIntTargettedLanguageIDs.Clone();

            IsPauseOnFailureOn = blnIsPauseOnFailureOn;

            if (arrStrTestMatrixName != null)
                TestMatrixNames = (ArrayList)arrStrTestMatrixName.Clone();

            Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.USER_INPUT_ACCEPTED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);
            ExtraRunTokens = strExtraRunTokens;
            ExtraTestCaseSpecificData = strExtraTestCaseSpecificData;

            PatchInfo.TargetArchitecture = intTargettedProductCPUID;
            PatchInfo.TargetLanguage = strSelectedLanguages;
            PatchInfo.ExtraRunTokens = ExtraRunTokens;
            PatchInfo.ExtraContextInfo = ExtraTestCaseSpecificData;

        }

        //This Constructor is used for HotFix (OrcasSubmit.aspx.cs) & Dev10ServicingReadiness (Dev10ServicingSubmit.aspx.cs)
        public Patch(string strProductBuild, string strSignedPatchLocation,
            string strVerificationScript, string strDDSFilePath, string strSKUListPath,
            string strOwner, HotFixUtility.ApplicationType appType, string strPU, string strWorkItem, bool blnIsPauseOnFailureOn,
            string strTFSServerURI, ArrayList arrIntMatrixIDs, string strFileLockName, string strProductRepairFileName, int intPUID, string strPayLoadFilex86,
            string strSupersedingPatch, string strSupersedingPatchMSP, string strSupersedingPatchKBNumber)
        {
            TFSServerURI = strTFSServerURI;

            PatchInfo = new TTestProdInfo();
            ProductBuild = strProductBuild;
            SignedPatchLocation = strSignedPatchLocation;
            VerificationScript = strVerificationScript;
            Owner = strOwner;
            DDSFilePath = strDDSFilePath;
            SKUListPath = strSKUListPath;


            ChipList = new ArrayList();
            ChipPackageName = new Dictionary<string, string>();
            PatchID = -1;

            appApplicationType = appType;
            PU = strPU;
            PUID = intPUID;
            WorkItem = strWorkItem;
            IsPauseOnFailureOn = blnIsPauseOnFailureOn;
            if(arrIntMatrixIDs != null)
                MatrixIDs = (ArrayList) arrIntMatrixIDs.Clone();
            FileLockName = strFileLockName;
            ProductRepairFileName = strProductRepairFileName;
            PayLoadFilex86 = strPayLoadFilex86;
            SupersedingPatch = strSupersedingPatch;
            SupersedingMSP = strSupersedingPatchMSP;
            SupersedingPatchKBNumber = strSupersedingPatchKBNumber;
            Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.USER_INPUT_ACCEPTED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);

        }

        #endregion Constructor

        #region Process Authoring Files

        public void ProcessAuthoringFiles(int intMaddogDBID)
        {
            try
            {
                MaddogDBID = intMaddogDBID;
                if (appApplicationType == HotFixUtility.ApplicationType.SetupTest || appApplicationType == HotFixUtility.ApplicationType.DeploymentTest || appApplicationType == HotFixUtility.ApplicationType.ProductSetupTest)
                {
                    PopulatePatchInfo(intMaddogDBID);
                    LoadPatchSKUnValidCPUs();
                    LoadPatchFile(1);
                }
                else
                {
                    XmlDocument xmlDocDDSFile = new XmlDocument();
                    xmlDocDDSFile.Load(DDSFilePath);

                    XmlDocument xmlDocSKUList = new XmlDocument();
                    if(!(SKUListPath.Equals("NA")))
                        xmlDocSKUList.Load(SKUListPath);

                    XmlDocument xmlDocMSPDDSFile = new XmlDocument();
                    if (MSPDDSFilePath != null && MSPDDSFilePath != string.Empty)
                        xmlDocMSPDDSFile.Load(MSPDDSFilePath);

                    Logger.Instance.AddLogMessage("Loaded Authoring Files", LogHelper.LogLevel.INFORMATION, this.PatchInfo);

                    PopulatePatchInfo(xmlDocDDSFile, xmlDocMSPDDSFile, intMaddogDBID);

                    Logger.Instance.AddLogMessage("Populated Patch Info", LogHelper.LogLevel.INFORMATION, this.PatchInfo);
                    
                    if (!(SKUListPath.Equals("NA")))
                        LoadPatchSKUnValidCPUs(xmlDocSKUList, xmlDocDDSFile);
                    LoadPatchSKUnValidCPUsFromMSPDDS(xmlDocMSPDDSFile, xmlDocDDSFile);

                    Logger.Instance.AddLogMessage("Loaded Patch SKUs", LogHelper.LogLevel.INFORMATION, this.PatchInfo);

                    PopulatePackageName();
                    PatchInfo.ProdSPLevel = GetTargetProductSPLevel();

                    Logger.Instance.AddLogMessage("Populated Package Names", LogHelper.LogLevel.INFORMATION, this.PatchInfo);

                    LoadPatchFile();

                }
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.PROCESSING_AUTHORING_FILES_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);

                if ((PatchInfo.TargetProductName.ToLower().Contains("framework") && PatchInfo.TargetProductName.ToLower().Contains("4.5")))
                    IsNDP45Patch = true;
                if ((PatchInfo.TargetProductName.ToLower().Contains("framework") && PatchInfo.TargetProductName.ToLower().Contains("4.0")))
                    IsNDP40Patch = true;
                if (PatchInfo.TargetProductName.ToLower().Contains("studio") && PatchInfo.TargetProductName.ToLower().Contains("2010"))
                    IsVS2010Patch = true;
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.PROCESSING_AUTHORING_FILES_FAILED.ToString(), LogHelper.LogLevel.ERROR, this.PatchInfo, ex);
                //Exception ex1 = new Exception("TestMe");

                Exception ex1 = new Exception("PatchID:" + this.PatchInfo.TTestProdInfoID.ToString() +
                    ";KBNumber:" + (this.PatchInfo.TestIdentifier == null ? "" : this.PatchInfo.TestIdentifier) +
                    ";BuildNumber:" + (this.PatchInfo.BuildNumber == null ? "" : this.PatchInfo.BuildNumber) +
                    ";CreationDate:" + (this.PatchInfo.CreatedDate == DateTime.MinValue ? "" : this.PatchInfo.CreatedDate.ToString()) +
                    ";CreatedBy:" + (this.PatchInfo.CreatedBy == null ? "" : this.PatchInfo.CreatedBy) + ";", ex);

                throw (ex1); ;
                //throw (ex);
            }
            return;
        }

      
        #region Business Object Loader

        private void PopulatePatchInfo(XmlDocument xmlDocDDSFile, XmlDocument xmlDocMSPDDSFile, int intMaddogDBID)
        {
            PatchInfo = new TTestProdInfo();

            #region Commented Code
            //bool blnFound = false;

            //XmlNodeList xmlNdListParameter = xmlDocDDSFile.DocumentElement.GetElementsByTagName("Parameter");
            //for (int i = 0; i < xmlNdListParameter.Count; i++)
            //{
            //    foreach (XmlAttribute xmlAttParameter in xmlNdListParameter[i].Attributes)
            //    {
            //        if (blnFound == true || xmlAttParameter.Value.Equals("KBNumber"))
            //        {
            //            blnFound = true;
            //            if (xmlAttParameter.Name.Equals("Value"))
            //            {
            //                objPatchDropInfo.PatchKB = xmlAttParameter.Value;
            //                break;
            //            }
            //        }
            //    }
            //    if (blnFound == true)
            //        break;
            //}

            #endregion Commented Code
            string strKBNumber = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "KBNumber");
            int intKBNumber = int.MinValue;
            if (int.TryParse(strKBNumber, out intKBNumber))
                PatchInfo.TestIdentifier = strKBNumber;
            else
                PatchInfo.TestIdentifier = strKBNumber.Substring(2);
            PatchInfo.BuildNumber = ProductBuild;
            PatchInfo.ProdVerificationScript = VerificationScript;
            PatchInfo.DDSFilePath = DDSFilePath;
            PatchInfo.SKUFilePath = SKUListPath;

            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            string strBaseLineID = string.Empty;
            if (xmlDocMSPDDSFile.InnerXml != string.Empty)
            {
                Brand = XMLHelper.GetAttributeValue(xmlDocMSPDDSFile, "Parameter", "Brand");
                ProductName = XMLHelper.GetAttributeValue(xmlDocMSPDDSFile, "Parameter", "ProductName");
                strBaseLineID = XMLHelper.GetAttributeValue(xmlDocMSPDDSFile, "Parameter", "BaselineId");
            }
            else
            {
                Brand = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "Brand");
                ProductName = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "ProductName");
                strBaseLineID = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "BaselineId");
            }
            
            PatchInfo.TargetProductName = ProductName;

            if (ProductName.Length == 0)
            {
                throw(new Exception("Product Name not found from dds file"));
            }
            
            PatchInfo.BaselineID = strBaseLineID;

            string strProductName = ProductName;
            if (System.Text.RegularExpressions.Regex.Match(ProductName, "SP(0|1|2|3|4|5|6|7|8|9)").Success)
            {
                strProductName = ProductName.Remove(ProductName.IndexOf(System.Text.RegularExpressions.Regex.Match(ProductName, "SP(0|1|2|3|4|5|6|7|8|9)").ToString())).Trim();
            }

            TProduct objProduct = db.TProducts.SingleOrDefault(product => product.ProductFriendlyName.ToLower().Contains(strProductName.ToLower()));
            BeaconProductCode = objProduct.BriqsProduct;
            PatchInfo.ProductID = objProduct.ProductID;

            //string strSPLevel = "SP0";
            //if (strProductName.Contains("SP1"))
            //    strSPLevel = "SP1";
            //if (strProductName.Contains("SP2"))
            //    strSPLevel = "SP2";
            //if (strProductName.Contains("SP3"))
            //    strSPLevel = "SP3";
            //if (strProductName.Contains("SP4"))
            //    strSPLevel = "SP4";

            //SPLevel will be updated later. Look for GetTargetProductSPLevel()
            //PatchInfo.ProdSPLevel = "SP0"; //"SP2"; // strBaseLineID; // strSPLevel;

            //PatchInfo.ExecutionSystemID = db.TExecutionSystems.Single(exs => exs.ApplicationType == this.appApplicationType.ToString()).ExecutionSystemID;

            PatchInfo.ExecutionSystemID = db.TExecutionSystems.Single
                (exs => (exs.ApplicationType == this.appApplicationType.ToString() && exs.MaddogDBID == intMaddogDBID)).ExecutionSystemID;

            //PatchInfo.TExecutionSystem = (TExecutionSystem) db.TExecutionSystems.Cast<TExecutionSystem>();

            PatchInfo.CreatedBy = Owner;
            PatchInfo.CreatedDate = DateTime.Now;
            PatchInfo.LastModifiedBy = Owner;
            PatchInfo.LastModifiedDate = PatchInfo.CreatedDate;

            PatchInfo.PU = PU;
            if(PUID != -1)
                PatchInfo.PUID = PUID;
            PatchInfo.ApplicationType = appApplicationType.ToString();
            PatchInfo.WorkItem = WorkItem;
            PatchInfo.MSPDDSFilePath = MSPDDSFilePath;
        }

        //Setup Test specific
        public void PopulatePatchInfo(int MaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TProduct objProduct = db.TProducts.SingleOrDefault(product => product.ProductID == TargettedProductID);

            Brand = objProduct.DecaturProduct;

            ProductName = objProduct.ProductFriendlyName;
            PatchInfo.TargetProductName = ProductName;

            string strBaseLineID = TargettedProductSPLevel.ToString();
            PatchInfo.BaselineID = strBaseLineID;

            BeaconProductCode = objProduct.BriqsProduct;
            PatchInfo.ProductID = objProduct.ProductID;

            
            PatchInfo.ExecutionSystemID = db.TExecutionSystems.Single
                (exs => (exs.ApplicationType == this.appApplicationType.ToString() && exs.MaddogDBID == MaddogDBID)).ExecutionSystemID;
          
           
            PatchInfo.CreatedBy = Owner;
            PatchInfo.CreatedDate = DateTime.Now;
            PatchInfo.LastModifiedBy = Owner;
            PatchInfo.LastModifiedDate = PatchInfo.CreatedDate;

            PatchInfo.PU = PU;
            if (PUID != -1)
                PatchInfo.PUID = PUID;
            PatchInfo.ApplicationType = appApplicationType.ToString();
            PatchInfo.WorkItem = WorkItem;
        }

        private string GetTargetProductSPLevel()
        {
            string strSPLevel = "SP0";
            foreach (string strChipLanguage in ChipPackageName.Keys)
            {
                string strPackageName = ChipPackageName[strChipLanguage];
                if (strPackageName.Contains("SP1"))
                {
                    strSPLevel = "SP1";
                    break;
                }
                if (strPackageName.Contains("SP2"))
                {
                    strSPLevel = "SP2";
                    break;
                }
                if (strPackageName.Contains("SP3"))
                {
                    strSPLevel = "SP3";
                    break;
                }
                if (strPackageName.Contains("SP4"))
                {
                    strSPLevel = "SP4";
                    break;
                }
            }
            return strSPLevel;
        }

        private void LoadPatchSKUnValidCPUs(XmlDocument xmlDocSKUList, XmlDocument xmlDocDDSFile)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TPatchSKU objPatchSKU;

            string strDecaturSKU = string.Empty;

            XmlNodeList xmlNdListSKU = xmlDocSKUList.DocumentElement.GetElementsByTagName("SKU");

            TSKU objSKU;
            TLanguage objLanguage;
            TCPU objCPU;

            string strChipLanguage = string.Empty;
            
            bool blnFound = false;
            bool blnInValidRecord = false;
            for (int i = 0; i < xmlNdListSKU.Count; i++)
            {
                strChipLanguage = string.Empty;
                blnFound = false;
                blnInValidRecord = false;
                objPatchSKU = new TPatchSKU();
                foreach (XmlAttribute xmlAttSKU in xmlNdListSKU[i].Attributes)
                {
                    if (blnFound == true || xmlAttSKU.Value.Contains("Patch_"))
                    {
                        blnFound = true;
                        if (xmlAttSKU.Name.Equals("Chip"))
                            strChipLanguage = xmlAttSKU.Value;
                        if (xmlAttSKU.Name.Equals("Lang"))
                            strChipLanguage += "-" + xmlAttSKU.Value;
                        continue;
                    }
                    else
                    {
                        try
                        {
                            switch (xmlAttSKU.Name)
                            {
                                case "Name":
                                    strDecaturSKU = xmlAttSKU.Value;
                                    objSKU = db.TSKUs.SingleOrDefault(sku => (sku.DecaturSKU == xmlAttSKU.Value && PatchInfo.ProductID == sku.ProductID));
                                    objPatchSKU.SKUID = objSKU.SKUID;
                                    if (strDecaturSKU.Equals("vc_red"))
                                        IsVCRedistPatch = true;
                                    break;

                                case "Chip":
                                    objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.DecaturCPU == xmlAttSKU.Value);
                                    objPatchSKU.CPUID = objCPU.CPUID;
                                    break;

                                case "Lang":
                                    objLanguage = db.TLanguages.SingleOrDefault(lang => lang.DecaturLanguage == xmlAttSKU.Value);
                                    objPatchSKU.LanguageID = objLanguage.LanguageID;
                                    break;
                            }
                        }
                        catch
                        {
                            blnInValidRecord = true;
                            break;
                        }
                    }
                }
                if (blnFound == false)
                {
                    if (blnInValidRecord == false)
                    {

                        objPatchSKU.ProductID = PatchInfo.ProductID;
                        ProductID = objPatchSKU.ProductID;

                        objPatchSKU.CreatedBy = PatchInfo.CreatedBy;
                        objPatchSKU.CreatedDate = PatchInfo.CreatedDate;
                        objPatchSKU.LastModifiedBy = PatchInfo.LastModifiedBy;
                        objPatchSKU.LastModifiedDate = PatchInfo.LastModifiedDate;

                        PatchInfo.TPatchSKUs.Add(objPatchSKU);
                    }
                }
                else
                {
                    if (!(ChipList.Contains(strChipLanguage)))
                        ChipList.Add(strChipLanguage);
                }
            }
            if (PatchInfo.TPatchSKUs.Count == 0)
            {
                throw new Exception("Either SKU List is missing or not supported");
            }

        }

        private void LoadPatchSKUnValidCPUsFromMSPDDS(XmlDocument xmlDocMSPDDSFile, XmlDocument xmlDocDDSFile)
        {
            ChipList = new ArrayList();
            string strChipLanguage = string.Empty;
            if (TargetX86)
            {
                strChipLanguage = "x86-enu";
                ChipList.Add(strChipLanguage);
            }
            if (TargetX64)
            {
                strChipLanguage = "amd64-enu";
                ChipList.Add(strChipLanguage);
            }
            if (TargetIA64)
            {
                strChipLanguage = "ia64-enu";
                ChipList.Add(strChipLanguage);
            }

            if (!(xmlDocMSPDDSFile.InnerXml.Equals(string.Empty)))
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                string strDecaturSKU = string.Empty;
                XmlNodeList xmlNdListSKU = xmlDocMSPDDSFile.DocumentElement.GetElementsByTagName("SKURef");

                TSKU objSKU;
                TLanguage objLanguage;
                TCPU objCPU;

                bool blnRepeatForAllArchitecture = false;
                for (int i = 0; i < xmlNdListSKU.Count; i++)
                {
                    short shCPUID = 0;
                    short shSKUID = 0;
                    short shLanguageID = 0;
                    blnRepeatForAllArchitecture = false;
                    bool blnSKUNotFound = false;
                    foreach (XmlAttribute xmlAttSKU in xmlNdListSKU[i].Attributes)
                    {
                        switch (xmlAttSKU.Name)
                        {
                            case "Name":
                                strDecaturSKU = xmlAttSKU.Value;
                                objSKU = db.TSKUs.SingleOrDefault(sku => (sku.DecaturSKU == xmlAttSKU.Value && PatchInfo.ProductID == sku.ProductID));
                                if (objSKU == null)
                                {
                                    blnSKUNotFound = true;
                                    break;
                                }
                                shSKUID = objSKU.SKUID;
                                if (strDecaturSKU.Equals("vc_red"))
                                    IsVCRedistPatch = true;
                                break;

                            case "Chip":
                                if (xmlAttSKU.Value.ToUpper().Equals("$(var.Chip)".ToUpper()))
                                    blnRepeatForAllArchitecture = true;
                                else
                                {
                                    objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.DecaturCPU == xmlAttSKU.Value);
                                    shCPUID = objCPU.CPUID;
                                }
                                break;

                            case "Lang":
                                objLanguage = db.TLanguages.SingleOrDefault(lang => lang.DecaturLanguage == xmlAttSKU.Value);
                                shLanguageID = objLanguage.LanguageID;
                                break;
                        }
                        if (blnSKUNotFound == true)
                            break;
                    }

                    if (blnSKUNotFound == false)
                    {
                        if (blnRepeatForAllArchitecture)
                        {
                            if (TargetX86)
                            {
                                objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.DecaturCPU == "x86");
                                AddTargetSKUToPatchSKUList(objCPU.CPUID, shSKUID, shLanguageID);
                            }
                            if (TargetX64)
                            {
                                objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.DecaturCPU == "amd64");
                                AddTargetSKUToPatchSKUList(objCPU.CPUID, shSKUID, shLanguageID);
                            }
                            if (TargetIA64)
                            {
                                objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.DecaturCPU == "ia64");
                                AddTargetSKUToPatchSKUList(objCPU.CPUID, shSKUID, shLanguageID);
                            }
                        }
                        else
                        {
                            AddTargetSKUToPatchSKUList(shCPUID, shSKUID, shLanguageID);
                        }
                    }
                }

                if (PatchInfo.TPatchSKUs.Count == 0)
                {
                    throw new Exception("Either SKU List is missing or not supported");
                }
            }
        }

        private void AddTargetSKUToPatchSKUList(short shCPUID, short shSKUID, short shLanguageID)
        {
            TPatchSKU objPatchSKU = new TPatchSKU();
            
            objPatchSKU.SKUID = shSKUID;
            objPatchSKU.LanguageID = shLanguageID;
            objPatchSKU.CPUID = shCPUID;

            objPatchSKU.ProductID = PatchInfo.ProductID;
            ProductID = objPatchSKU.ProductID;

            objPatchSKU.CreatedBy = PatchInfo.CreatedBy;
            objPatchSKU.CreatedDate = PatchInfo.CreatedDate;
            objPatchSKU.LastModifiedBy = PatchInfo.LastModifiedBy;
            objPatchSKU.LastModifiedDate = PatchInfo.LastModifiedDate;

            PatchInfo.TPatchSKUs.Add(objPatchSKU);
        }

        //Setup Test specific
        public void LoadPatchSKUnValidCPUs()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            string strChipLanguage = string.Empty;

            TLanguage objLanguage;
            TCPU objCPU;

            if (TargettedProductCPUID != 0)
            {
                objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.CPUID == TargettedProductCPUID);
                strChipLanguage = objCPU.DecaturCPU;
            }
            else
            {
                strChipLanguage = "All";
            }

            //if (TargettedProductLanguageID != 0)
            //{
            //    objLanguage = db.TLanguages.SingleOrDefault(lang => lang.LanguageID == TargettedProductLanguageID);
            //    strChipLanguage += "-" + objLanguage.DecaturLanguage;
            //}
            //else
            //{
                strChipLanguage += "-" + "NA";
            //}

            ChipList.Add(strChipLanguage);
        }

        private void PopulatePackageName()
        {
            XmlDocument xmlDoc;
            Wix.Preprocessor wixPreprocessor = new Wix.Preprocessor();
            Hashtable hstParameters;
            foreach (string strChipLanguage in ChipList)
            {
                xmlDoc = new XmlDocument();
                hstParameters = new Hashtable();
                hstParameters.Add("Chip", strChipLanguage.Split("-".ToCharArray())[0]);

                hstParameters.Add("ProductFamily", "NA");
                hstParameters.Add("BuildType", "NA");
                hstParameters.Add("Lang", "NA");

                hstParameters.Add("IronMan_VSSupportedLanguages", "NA");
                hstParameters.Add("IronMan_NDPSupportedLanguages", "NA");
                hstParameters.Add("IronMan_VSTOSupportedLanguages", "NA");

                xmlDoc = wixPreprocessor.Process(DDSFilePath, hstParameters);

                XmlNodeList xmlNdListPackage = xmlDoc.DocumentElement.GetElementsByTagName("DefaultPackage");
                for (int i = 0; i < xmlNdListPackage.Count; i++)
                {
                    foreach (XmlAttribute xmlAttPackage in xmlNdListPackage[i].Attributes)
                    {
                        if (xmlAttPackage.Name.Equals("Name"))
                        {
                            ChipPackageName.Add(strChipLanguage, xmlAttPackage.Value);
                            break;
                        }
                    }
                }
            }
        }

        private void LoadPatchFile()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            string strPatchLocationFirstPart = SignedPatchLocation + "\\" + PatchInfo.BuildNumber + "\\setup\\sfxs\\";
            if (!System.IO.Directory.Exists(strPatchLocationFirstPart))
            {
                strPatchLocationFirstPart = SignedPatchLocation + "\\" + PatchInfo.BuildNumber + "\\patch\\setup\\sfxs\\";
                if (!System.IO.Directory.Exists(strPatchLocationFirstPart))
                {
                    strPatchLocationFirstPart = SignedPatchLocation + "\\" + PatchInfo.BuildNumber + "\\patchdual\\setup\\sfxs\\";
                }
            }

            TCPU objCPU;
            TLanguage objLanguage;

            TTestProdAttribute objPatchFile;
            foreach (string strChipLanguage in ChipPackageName.Keys)
            {
                objPatchFile = new TTestProdAttribute();

                objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.DecaturCPU == strChipLanguage.Split("-".ToCharArray())[0]);
                objPatchFile.CPUID = objCPU.CPUID;

                objLanguage = db.TLanguages.SingleOrDefault(lang => lang.DecaturLanguage == strChipLanguage.Split("-".ToCharArray())[1]);
                objPatchFile.LanguageID = objLanguage.LanguageID;

                objPatchFile.TestProdLocation = strPatchLocationFirstPart + objCPU.BriqsCPU.ToUpper() + "ret\\" + /*enu*/ strChipLanguage.Split("-".ToCharArray())[1] + "\\patch\\KB" + PatchInfo.TestIdentifier;
                objPatchFile.TestProdName = ChipPackageName[strChipLanguage];
                if (!(System.IO.File.Exists(objPatchFile.TestProdLocation + @"\" + objPatchFile.TestProdName)))
                    continue;
                objPatchFile.CreatedBy = PatchInfo.CreatedBy;
                objPatchFile.CreatedDate = PatchInfo.CreatedDate;
                objPatchFile.LastModifiedBy = PatchInfo.LastModifiedBy;
                objPatchFile.LastModifiedDate = PatchInfo.LastModifiedDate;
                try
                {
                    objPatchFile.VerificationScript = HotFixUtility.GetMSPFile(objPatchFile, ProductName, PatchInfo.ProdSPLevel, PatchInfo.TestIdentifier, PatchInfo.BuildNumber, appApplicationType, strChipLanguage, ref PatchChainerType);
                }
                catch (Exception ex)
                {
                    Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, PatchInfo, ex);
                    throw (ex);
                }

//                objPatchFile.VerificationFilePath = "asd";

                PatchInfo.TTestProdAttributes.Add(objPatchFile);
            }
            if (PatchInfo.TTestProdAttributes.Count == 0)
                throw new Exception("Patch not found at the specified location (" + SignedPatchLocation + "). Please contact support.");
            return;
        }

        //Setup Test specific
        public void LoadPatchFile(int intA)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TTestProdAttribute objPatchFile = new TTestProdAttribute();

            try
            {

                if (TargettedProductCPUID != 0)
                {
                    objPatchFile.CPUID = Convert.ToInt16(TargettedProductCPUID);
                }
                else
                {
                    objPatchFile.CPUID = 1;
                }

                //if (TargettedProductLanguageID != 0)
                //{
                //    objPatchFile.LanguageID = Convert.ToInt16(TargettedProductLanguageID);
                //}
                //else
                //{
                    objPatchFile.LanguageID = 1;
                //}

                    objPatchFile.TestProdLocation = System.IO.Path.GetDirectoryName(SignedPatchLocation);
                    objPatchFile.TestProdName = System.IO.Path.GetFileName(SignedPatchLocation);
                if (!(System.IO.File.Exists(SignedPatchLocation)))
                    throw new Exception("Patch not found at the specified location (" + SignedPatchLocation + "). Please contact support.");

                objPatchFile.CreatedBy = PatchInfo.CreatedBy;
                objPatchFile.CreatedDate = PatchInfo.CreatedDate;
                objPatchFile.LastModifiedBy = PatchInfo.LastModifiedBy;
                objPatchFile.LastModifiedDate = PatchInfo.LastModifiedDate;

                if (VerificationOption.Equals("Sitzmark"))
                {
                    objPatchFile.VerificationScript = VerificationScript;
                }
                else
                {
                    if (!(System.IO.File.Exists(MSPLocation)))
                        throw new Exception("MSP File not found at the specified location (" + MSPLocation + "). Please contact support.");
                    objPatchFile.VerificationScript = MSPLocation;

                }
                PatchInfo.TTestProdAttributes.Add(objPatchFile);
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, PatchInfo, ex);
                throw (ex);
            }
        }

        #endregion Business Object Loader

        #endregion Process Authoring Files

        #region TestMatrix

        #region CreateTestMatrix - Hotfix

        public void CreateTestMatrix()
        {
            try
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                Context objContext;
                
                int intMaxLimit = -1;
                foreach (TTestProdAttribute objPatchFile in PatchInfo.TTestProdAttributes)
                {
                    //To avoid duplicate test scenario being added to objContext.ListTestMatrix
                    Hashtable hstAlreadyAddedSKUs = new Hashtable();

                    intMaxLimit = -1;
                    if (IsNDP40Patch == false && IsNDP45Patch == false)
                    {
                        if (PatchInfo.TargetProductName.ToLower().Contains("framework"))
                        {
                            //if (!PatchInfo.TargetProductName.ToLower().Contains("4.0"))
                            {
                                if (objPatchFile.TestProdName.ToUpper().Contains("X86"))
                                    intMaxLimit = HotFixUtility.NDP_X86_CONTEXT_LIMIT;
                                if (objPatchFile.TestProdName.ToUpper().Contains("X64"))
                                    intMaxLimit = HotFixUtility.NDP_AMD64_CONTEXT_LIMIT;
                                if (objPatchFile.TestProdName.ToUpper().Contains("IA64"))
                                    intMaxLimit = HotFixUtility.NDP_IA64_CONTEXT_LIMIT;
                            }
                        }
                    }
                    if (IsVS2010Patch == false)
                    {
                        if (PatchInfo.TargetProductName.ToLower().Contains("studio"))
                        {
                            if (objPatchFile.TestProdName.ToUpper().Contains("X86"))
                                intMaxLimit = HotFixUtility.VS_X86_CONTEXT_LIMIT;
                            if (objPatchFile.TestProdName.ToUpper().Contains("X64"))
                                intMaxLimit = HotFixUtility.VS_AMD64_CONTEXT_LIMIT;
                            if (objPatchFile.TestProdName.ToUpper().Contains("IA64"))
                                intMaxLimit = HotFixUtility.VS_IA64_CONTEXT_LIMIT;
                        }
                    }

                    objContext = new Context() { TestFramework = "Beacon" };
                    foreach (TPatchSKU objPatchSKU in PatchInfo.TPatchSKUs)
                    {
                        //To avoid duplicate test scenario being added to objContext.ListTestMatrix. Check if already added, skip it.
                        if (hstAlreadyAddedSKUs.Contains(objPatchSKU.ProductID.ToString() + "-" + objPatchSKU.SKUID.ToString() + "-" + objPatchSKU.CPUID.ToString()))
                        {
                            continue;
                        }
                        hstAlreadyAddedSKUs.Add(objPatchSKU.ProductID.ToString() + "-" + objPatchSKU.SKUID.ToString() + "-" + objPatchSKU.CPUID.ToString(), objPatchSKU.ProductID.ToString() + "-" + objPatchSKU.SKUID.ToString() + "-" + objPatchSKU.CPUID.ToString());

                        var vTestMatrix = from t in db.TMatrixes
                                          //join tc in db.TTestCaseWithPriorities on t.TestCaseCode equals tc.TestCaseCode
                                          join tc in db.TTestCaseWithPriorities on t.TestCaseID equals tc.TestCaseID
                                          where t.ProductID == objPatchSKU.ProductID
                                          && t.SKUID == objPatchSKU.SKUID
                                              //&& t.LanguageID == objPatchSKU.LanguageID //As most of the decatur patches are Language independent
                                          && t.CPUID == objPatchSKU.CPUID
                                          && objPatchFile.CPUID == objPatchSKU.CPUID
                                          && (t.ProductSPLevel == "*" || t.ProductSPLevel == objPatchSKU.TTestProdInfo.ProdSPLevel.ToString())
                                          && tc.PatchType == appApplicationType.ToString()
                                          //To avoid selecting Matrix from Servicing Readiness
                                          && t.PUID == null
                                          && t.Active == true
                                          && tc.MaddogDBID == MaddogDBID
                                          && t.MaddogDBID == MaddogDBID
                                          orderby tc.TestCaseCode
                                          select new { t.MatrixID, t.MDOSID, t.OSSPLevel, t.ProductID, t.SKUID, t.LanguageID, t.CPUID, t.ProductSPLevel, t.OSDetails, tc.TestCaseCode, t.TestCaseName, tc.TestCaseID, t.MaddogDBID, t.TOSImage };

                        foreach (var t in vTestMatrix)
                        {
                            TMatrix objMatrix = new TMatrix()
                                                    {
                                                        MatrixID = t.MatrixID,
                                                        MDOSID = t.MDOSID,
                                                        OSSPLevel = t.OSSPLevel,
                                                        ProductID = t.ProductID,
                                                        SKUID = t.SKUID,
                                                        LanguageID = t.LanguageID,
                                                        CPUID = t.CPUID,
                                                        ProductSPLevel = t.ProductSPLevel,
                                                        TestCaseID = t.TestCaseID,
                                                        OSDetails = t.OSDetails,
                                                        TestCaseCode = t.TestCaseCode,
                                                        TestCaseName = t.TestCaseName,
                                                        MaddogDBID = t.MaddogDBID,
                                                        TOSImage = t.TOSImage
                                                    };

                            if (objPatchSKU.TTestProdInfo.ProdSPLevel.Equals("SP0") && (IsNDP45Patch || IsNDP40Patch) && (objMatrix.TestCaseCode.Equals("C1") || objMatrix.TestCaseCode.Equals("C")))
                            {
                                objMatrix.ProductSPLevel = "0";
                                objMatrix.ProductID = 3; //NDP35 RTM
                                objMatrix.SKUID = 3; //NDP35 RTM

                            }
                            else if (objPatchSKU.TTestProdInfo.ProdSPLevel.Equals("SP0") && IsVS2010Patch && (objMatrix.TestCaseCode.Equals("C1") || objMatrix.TestCaseCode.Equals("C")))
                            {
                                objMatrix.ProductSPLevel = "0";
                                objMatrix.ProductID = 5; //VS2008 RTM
                                objMatrix.SKUID = 22; //VSTS
                            }
                            else
                            {
                                objMatrix.ProductSPLevel = GetProductSPLevel(objMatrix.TestCaseCode, objPatchSKU.TTestProdInfo.ProdSPLevel, db.TProducts.Single(p => p.ProductID == objPatchSKU.ProductID).DecaturProduct);
                            }
                            if (objMatrix.ProductSPLevel.Equals(string.Empty))
                                continue;
                            objContext.ListTestMatrix.Add(objMatrix);
                        }
                    }
                    //Adding priority Test scenrios (outside from predefined test matrix). 
                    //Keep the count of OS Index. To avoid adding same OS always.
                    int intMDOSCounter = 0;
                    int intTCCounter = 0;
                    for (int intCount = 0; intCount < 10; intCount++) // Iternate this for loop for a max of 10 times (number is based on performance)
                    {
                        bool blnIsScenarioAdded = true;
                        //Modification done so that only one priority test scenario is added in one loop of SKU.
                        foreach (TPatchSKU objPatchSKU in PatchInfo.TPatchSKUs)
                        {
                            blnIsScenarioAdded = AddOnePriorityScenario(objContext, objPatchSKU, objPatchFile.CPUID, objPatchFile.LanguageID, ref intMDOSCounter, ref intTCCounter);
                            //Exit the loop if no more test scenario can be added
                            if (blnIsScenarioAdded == false)
                                break;
                        }
                        //Exit the loop if no more test scenario can be added
                        if (blnIsScenarioAdded == false)
                            break;
                    }
                    if (!(intMaxLimit == -1 || objContext.ListTestMatrix.Count <= intMaxLimit))
                        objContext.ListTestMatrix.RemoveRange(intMaxLimit, objContext.ListTestMatrix.Count - intMaxLimit);
                    objPatchFile.ContextObject = objContext;
                }
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.IDENTIFICATION_TEST_MATRIX_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);
            }
            catch(Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.IDENTIFICATION_TEST_MATRIX_FAILED.ToString(), LogHelper.LogLevel.ERROR, this.PatchInfo, ex);
                throw (ex);
            }
        }

        private string GetProductSPLevel(string strTestCode, string strPatchSPLevel, string strDecaturProductCode)
        {
            if ((strTestCode.Equals("C") || strTestCode.Equals("C1")) && strPatchSPLevel.Equals("SP0"))
                return "";

            string strProductSPLevel = string.Empty;
            if (strTestCode.Equals("C") || strTestCode.Equals("C1"))
            {
                switch (strPatchSPLevel)
                {
                    case "SP1":
                        strProductSPLevel = "SP0";
                        break;
                    case "SP2":
                        strProductSPLevel = "SP1";
                        break;
                    case "SP3":
                        strProductSPLevel = "SP2";
                        break;
                    case "SP4":
                        strProductSPLevel = "SP3";
                        break;
                }
            }
            else if(strTestCode.Equals("COS") && (strDecaturProductCode.Equals("NDP20") || strDecaturProductCode.Equals("NDP30")))
            {
                strProductSPLevel = "SP0";
            }
            else
            {
                strProductSPLevel = strPatchSPLevel.ToString();
            }
            strProductSPLevel = (strProductSPLevel == "RTM" ? "0" : strProductSPLevel.Substring(2, 1));
            return strProductSPLevel;
        }

        //Modification done so that only one priority test scenario is added in one loop of SKU.
        private bool AddOnePriorityScenario(Context objContext, TPatchSKU objPatchSKU, short intPatchCPUID, short intPatchLanguageID,  ref int intMDOSCounter, ref int intTCCounter)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            //List<TMDOSWithPriority> lstMDOSWithPriorities = (List<TMDOSWithPriority>)
            var varMDOSWithPriorities = from os in db.TMDOSWithPriorities
                                        where os.PatchType == appApplicationType.ToString() //"Hotfix"
                                        && os.ProductID == objPatchSKU.ProductID
                                            //&& os.LanguageID == objPatchSKU.LanguageID //As most of the decatur patches are Language independent
                                        && os.CPUID == intPatchCPUID
                                        && os.Active == true
                                        orderby os.Priority
                                        select os;

            List<TMDOSWithPriority> lstMDOSWithPriorities = varMDOSWithPriorities.ToList<TMDOSWithPriority>();
            if (lstMDOSWithPriorities.Count == 0)
                return false; //No Scenario added. Exit as no priority OS is available for this Product & CPU

            //Choose only those TestCases which are already part of that product's test matrix. So that Rollback and Re-install are not used for Orcas products.
            var varTestCase = (from tc in db.TTestCaseWithPriorities
                              //join t in db.TMatrixes on tc.TestCaseCode equals t.TestCaseCode
                               join t in db.TMatrixes on tc.TestCaseID equals t.TestCaseID
                              where tc.PatchType == appApplicationType.ToString() //"Hotfix"
                              && tc.TestCaseCode != "COS" && tc.TestCaseCode != "B1" && tc.TestCaseCode != "B"
                              && t.ProductID == objPatchSKU.ProductID
                              && t.Active == true
                              && t.MaddogDBID == MaddogDBID
                              && tc.MaddogDBID == MaddogDBID
                              orderby tc.Priority
                              select tc).Distinct();

            List<TTestCaseWithPriority> lstTestCaseWithPriority = varTestCase.ToList<TTestCaseWithPriority>();

            DataSet dstContextDetails = new DataSet();
            string strTestCaseName = string.Empty;
            string strTestCaseDescription = string.Empty;

            int intPreviousCount = 0;
            int intPreviousTCCount = 0;
            //int intMDOSCounter = 0;
            //foreach (TTestCaseWithPriority objTestCaseWithPriority in varTestCase)
            for (int intCounter = 0; intCounter < lstTestCaseWithPriority.Count; intCounter++)
            {
                TTestCaseWithPriority objTestCaseWithPriority = lstTestCaseWithPriority[(intCounter + intTCCounter) % lstTestCaseWithPriority.Count];
                var varCountUsedContext = from ts in objContext.ListTestMatrix // ListTestSenario
                                          where ts.TestCaseID == objTestCaseWithPriority.TestCaseID
                                          //group ts by ts.TestCaseID into ts_count
                                          select ts;
                
                int intCountUsedContext = varCountUsedContext.Count();

                dstContextDetails = HotFixUtility.GetAssociatedContextsDetails(objTestCaseWithPriority.TestCaseID, ref strTestCaseName, ref strTestCaseDescription,
                    appApplicationType, PatchInfo.CreatedBy, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == PatchInfo.ExecutionSystemID).ExecutionSystemName,
                    db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == PatchInfo.ExecutionSystemID).DatabaseName);
                int intCountTotalContext = dstContextDetails.Tables[0].Rows.Count;

                for (int intCount = 0; intCount < intCountTotalContext - intCountUsedContext; intCount++)
                {
                    //if (intCount < lstMDOSWithPriorities.Count)
                    //{
                        TMDOSWithPriority objMDOSWthPriority = lstMDOSWithPriorities[(intCount + intMDOSCounter) % lstMDOSWithPriorities.Count];

                        TMatrix objTMatrix = new TMatrix();
                        objTMatrix.MDOSID = objMDOSWthPriority.MDOSID;
                        objTMatrix.OSSPLevel = objMDOSWthPriority.OSSPLevel;
                        objTMatrix.ProductID = objPatchSKU.ProductID;
                        objTMatrix.SKUID = objPatchSKU.SKUID;
                        objTMatrix.LanguageID = intPatchLanguageID; // objPatchSKU.LanguageID;
                        objTMatrix.CPUID = intPatchCPUID; // objPatchSKU.CPUID;

                        objTMatrix.ProductSPLevel = GetProductSPLevel(objTestCaseWithPriority.TestCaseCode, objPatchSKU.TTestProdInfo.ProdSPLevel, db.TProducts.Single(p => p.ProductID == objPatchSKU.ProductID).DecaturProduct);
                        if (objTMatrix.ProductSPLevel.Equals(string.Empty))
                            continue;
                        objTMatrix.TestCaseID = objTestCaseWithPriority.TestCaseID;
                        objTMatrix.OSDetails = objMDOSWthPriority.OSDetails;
                        objTMatrix.TestCaseCode = objTestCaseWithPriority.TestCaseCode;
                        objTMatrix.TestCaseName = strTestCaseName;

                        objTMatrix.TOSImage = objMDOSWthPriority.TOSImage;
                        objTMatrix.MaddogDBID = MaddogDBID;

                        objContext.ListTestMatrix.Add(objTMatrix);
                        intPreviousCount = intCount + 1;

                        intMDOSCounter += intPreviousCount;

                        intPreviousTCCount = intTCCounter + 1;
                        intTCCounter = intPreviousTCCount;
                        return true; //One Scenario added. Exit as soon as one scenario is added.
                    //}
                }
                intMDOSCounter += intPreviousCount;
            }
            return false; //No Scenario added
        }

        #endregion CreateTestMatrix

        #region LoadTestMatrix - Dev10Servicing Readiness Specific (Loading UserSelected Matrix info)

        public void LoadTestMatrix()
        {
            try
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                Context objContext;

                List<string> lstMatrixIDs = new List<string>();

                foreach (string strMatrixID in MatrixIDs)
                {
                    lstMatrixIDs.Add(strMatrixID);
                    TPatchMatrixMapping objPatchMatrixMapping = new TPatchMatrixMapping();
                    objPatchMatrixMapping.PatchID = PatchInfo.TTestProdInfoID;
                    objPatchMatrixMapping.MatrixID = Convert.ToInt16(strMatrixID);
                    PatchInfo.TPatchMatrixMappings.Add(objPatchMatrixMapping);
                }

                foreach (TTestProdAttribute objPatchFile in PatchInfo.TTestProdAttributes)
                {
                    objContext = new Context() { TestFramework = "Beacon" };
                    var vTestMatrix = from t in db.TMatrixes
                                      //join tc in db.TTestCaseWithPriorities on t.TestCaseCode equals tc.TestCaseCode
                                      join tc in db.TTestCaseWithPriorities on t.TestCaseID equals tc.TestCaseID
                                      where lstMatrixIDs.Contains(t.MatrixID.ToString())
                                          //where Convert.ToInt32(t.MatrixID).Contains(strMatrixID)
                                      && (t.CPUID == objPatchFile.CPUID || (Brand.ToLower().Contains("vs") && IsVCRedistPatch == false))
                                      && tc.PatchType == appApplicationType.ToString()
                                      orderby tc.TestCaseCode
                                      select new { t.MatrixID, t.MDOSID, t.OSSPLevel, t.ProductID, t.SKUID, t.LanguageID, t.CPUID, t.ProductSPLevel, t.OSDetails, tc.TestCaseCode, t.TestCaseName, tc.TestCaseID };

                    foreach (var t in vTestMatrix)
                    {
                        TMatrix objMatrix = new TMatrix()
                                                {
                                                    MatrixID = t.MatrixID,
                                                    MDOSID = t.MDOSID,
                                                    OSSPLevel = t.OSSPLevel,
                                                    ProductID = t.ProductID,
                                                    SKUID = t.SKUID,
                                                    LanguageID = t.LanguageID,
                                                    CPUID = t.CPUID,
                                                    ProductSPLevel = t.ProductSPLevel,
                                                    TestCaseID = t.TestCaseID,
                                                    OSDetails = t.OSDetails,
                                                    TestCaseCode = t.TestCaseCode,
                                                    TestCaseName = t.TestCaseName
                                                };

                        //objMatrix.ProductSPLevel = GetProductSPLevel(objMatrix.TestCaseCode, objPatchSKU.TTestProdInfo.ProdSPLevel, db.TProducts.Single(p => p.ProductID == objPatchSKU.ProductID).DecaturProduct);
                        //if (objMatrix.ProductSPLevel.Equals(string.Empty))
                        //    continue;
                        objContext.ListTestMatrix.Add(objMatrix);
                    }

                    objPatchFile.ContextObject = objContext;
                }
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.IDENTIFICATION_TEST_MATRIX_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);

            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.IDENTIFICATION_TEST_MATRIX_FAILED.ToString(), LogHelper.LogLevel.ERROR, this.PatchInfo, ex);
                throw (ex);
            }
        }
        
        #endregion Dev10Servicing Readiness Specific (Loading UserSelected Matrix info)

        #region PrepareTestMatrix - Setup Test Specific

        //Setup Test specific
        public void PrepareTestMatrix()
        {
            try
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                Context objContext;

                List<string> lstTestMatrixIDs = new List<string>();

                foreach (string strTestMatrixName in TestMatrixNames)
                {
                    var vTestMatrixID = from tm in db.TTestMatrixes
                                        where tm.TestMatrixName == strTestMatrixName
                                        && tm.MaddogDBID == MaddogDBID
                                        && tm.Active == true
                                        select tm.TestMatrixID;


                    foreach (int intTestMatrixID in vTestMatrixID)
                    {
                        lstTestMatrixIDs.Add(intTestMatrixID.ToString());

                        //ToDo: Add a Table to store PatchID 

                        TPatchTestMatrixMapping objPatchTestMatrixMapping = new TPatchTestMatrixMapping();
                        objPatchTestMatrixMapping.PatchID = PatchInfo.TTestProdInfoID;
                        objPatchTestMatrixMapping.TestMatrixID = intTestMatrixID;
                        objPatchTestMatrixMapping.TestMatrixName = strTestMatrixName;
                        PatchInfo.TPatchTestMatrixMappings.Add(objPatchTestMatrixMapping);

                    }

                }

                List<string> lstTargettedLanguageIDs = new List<string>();

                foreach (string strTargettedLanguage in TargettedProductLanguageIDs)
                {
                    lstTargettedLanguageIDs.Add(strTargettedLanguage);
                }


                    objContext = new Context() { TestFramework = "Beacon" };
                    var vSetupTestMatrix = from t in db.TTestMatrixes
                                           //join tc in db.TTestCaseWithPriorities on t.TestCaseID equals tc.TestCaseID
                                      //join os in db.TOs on t.MDOSID equals os.MDOSID
                                      //join osi in db.TOSImages on t.OSImageID equals osi.OSImageID
                                      //join p in db.TProducts on t.ProductID equals p.ProductID
                                      //join sku in db.TSKUs on t.ProductSKUID equals sku.SKUID
                                      //join cpu in db.TCPUs on t.ProductCPUID equals cpu.CPUID
                                      //join lang in db.TLanguages on t.ProductLanguageID equals lang.LanguageID
                                      where /*tc.PatchType.Equals(appApplicationType.ToString())
                                      &&  */ /*t.ProductID == TargettedProductID
                                      && t.ProductSPLevel == TargettedProductSPLevel.ToString() */
                                      //t.TestProdIdentifier == PatchInfo.TestProdIdentifier
                                      lstTestMatrixIDs.Contains(t.TestMatrixID.ToString())
                                      && (TargettedProductCPUID == 0 || t.ProductCPUID == TargettedProductCPUID)
                                      && (lstTargettedLanguageIDs.Contains("0") || lstTargettedLanguageIDs.Contains(t.ProductLanguageID.ToString()))
                                      //&& t.TestMatrixPriority == 0
                                      && t.Active == true
                                        && t.MaddogDBID == MaddogDBID
                                        && t.Active == true
                                      orderby t.TestCaseID
                                      select t;
                                               /*new { t.TestMatrixID, t.MDOSID, t.OSImageID, t.ProductID,  t.ProductSKUID, t.ProductLanguageID, t.ProductCPUID, 
                                          t.ProductSPLevel, os.OSDetails, tc.TestCaseCode, tc.TestCaseName, t.TestCaseID, t.TestCaseSpecificData, t.TestMatrixPriority };
                                              */

                    objContext.ListSetupTestMatrix = vSetupTestMatrix.ToList<TTestMatrix>();
                if(ExtraTestCaseSpecificData != null && (!(ExtraTestCaseSpecificData.Equals(string.Empty))))
                {
                    foreach (TTestMatrix objTestMatrix in objContext.ListSetupTestMatrix)
                    {
                        if (objTestMatrix.TestCaseSpecificData != null && (!(objTestMatrix.TestCaseSpecificData.Trim().Equals(string.Empty))))
                        {
                            objTestMatrix.TestCaseSpecificData += "#" + ExtraTestCaseSpecificData;
                        }
                        else
                        {
                            objTestMatrix.TestCaseSpecificData += ExtraTestCaseSpecificData;
                        }
                    }
                }
                    //foreach (var t in vSetupTestMatrix)
                    //{
                    //    //TTestMatrix objSetupTestMatrix = new TTestMatrix()
                    //    //{
                    //    //    TestMatrixID = t.TestMatrixID,
                    //    //    MDOSID = t.MDOSID,
                    //    //    OSImageID = t.OSImageID.,
                    //    //    ProductID = t.ProductID,
                    //    //    ProductSKUID = t.ProductSKUID,
                    //    //    ProductLanguageID = t.ProductLanguageID,
                    //    //    ProductCPUID = t.ProductCPUID,
                    //    //    ProductSPLevel = t.ProductSPLevel,
                    //    //    TestCaseID = t.TestCaseID,
                    //    //    TestCaseSpecificData = t.TestCaseSpecificData,
                    //    //    TestMatrixPriority = t.TestMatrixPriority
                    //    //};

                    //    objContext.ListSetupTestMatrix.Add(t);
                    //}

                    PatchInfo.TTestProdAttributes[0].ContextObject = objContext;
                
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.IDENTIFICATION_TEST_MATRIX_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);

            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.IDENTIFICATION_TEST_MATRIX_FAILED.ToString(), LogHelper.LogLevel.ERROR, this.PatchInfo, ex);
                throw (ex);
            }
        }

        #endregion Setup Test Specific

        #endregion TestMatrix

        #region CreateContext

        public void CreateContext(HotFixUtility.ApplicationType appType)
        {
            try
            {
                string strDirectoryLocation = string.Empty;//(appType == HotFixUtility.ApplicationType.Hotfix ? HotFixUtility.HotfixWorkDirectory : HotFixUtility.Dev10ServicingWorkDirectory) + @"CONTEXTS\" + ProductName + @"\" + PatchInfo.ProdSPLevel + @"\";
                switch (appType)
                {
                    case HotFixUtility.ApplicationType.Hotfix:
                        strDirectoryLocation = HotFixUtility.HotfixWorkDirectory;
                        break;
                    case HotFixUtility.ApplicationType.Dev10Servicing:
                        strDirectoryLocation = HotFixUtility.Dev10ServicingWorkDirectory;
                        break;
                    case HotFixUtility.ApplicationType.SetupTest:
                        strDirectoryLocation = HotFixUtility.SetupTestWorkDirectory;
                        break;
                    case HotFixUtility.ApplicationType.DeploymentTest:
                        strDirectoryLocation = HotFixUtility.DeploymentTestWorkDirectory;
                        break;
                    case HotFixUtility.ApplicationType.ProductSetupTest:
                        strDirectoryLocation = HotFixUtility.ProductSetupTestWorkDirectory;
                        break;

                }
                strDirectoryLocation += @"CONTEXTS\" + ProductName + @"\" + PatchInfo.ProdSPLevel + @"\";

                if (!(System.IO.Directory.Exists(strDirectoryLocation)))
                    System.IO.Directory.CreateDirectory(strDirectoryLocation);
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                TCPU objCPU;
                foreach (TTestProdAttribute objPatchFile in PatchInfo.TTestProdAttributes)
                {
                    Context objContext = objPatchFile.ContextObject;

                    objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.CPUID == objPatchFile.CPUID);

                    objContext.FileName = strDirectoryLocation + "Context_KB" + PatchInfo.TestIdentifier + "_" + objCPU.BriqsCPU + ".ini";
                    objContext.FileName = HotFixUtility.GetUniqueFileName(objContext.FileName, false);

                    if (appType == HotFixUtility.ApplicationType.SetupTest || appType == HotFixUtility.ApplicationType.DeploymentTest || appType == HotFixUtility.ApplicationType.ProductSetupTest)
                        PopulateTestSenariosForSetupTest(objContext, ProductID);
                    else
                        PopulateTestSenarios(objContext, ProductID);
                }
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_CONTEXT_BLOCK_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);
            }
            catch(Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_CONTEXT_BLOCK_FAILED.ToString(), LogHelper.LogLevel.ERROR, this.PatchInfo, ex);
                throw (ex);
            }
        }

        private void PopulateTestSenarios(Context objContext, int intProductID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TestSenario objTestSenario;
            DataSet dstContextDetails = new DataSet();

            int intTestCaseID = int.MinValue;
            string strTestCaseName = string.Empty;
            string strTestCaseDescription = string.Empty;
            int intMaddogOSImageID = -1;

            foreach (TMatrix objTestMatrix in objContext.ListTestMatrix)
            {
                intMaddogOSImageID = -1;
                if (objTestMatrix.TOSImage != null && objTestMatrix.TOSImage.MaddogOSImageID != null)
                    intMaddogOSImageID = (int)objTestMatrix.TOSImage.MaddogOSImageID;
                if (Convert.ToInt32(objTestMatrix.TestCaseID) != intTestCaseID)
                {
                    intTestCaseID = Convert.ToInt32(objTestMatrix.TestCaseID);
                    dstContextDetails = HotFixUtility.GetAssociatedContextsDetails(intTestCaseID, ref strTestCaseName, ref strTestCaseDescription,
                        appApplicationType, PatchInfo.CreatedBy, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == PatchInfo.ExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == PatchInfo.ExecutionSystemID).DatabaseName);
                }
                objTestSenario = new TestSenario() { TestCaseID = intTestCaseID, TestCaseCode = objTestMatrix.TestCaseCode, TestCaseName = strTestCaseName, TestCaseDescripton = strTestCaseDescription, MDOSCPUID = -1,
                                                     MaddogOSImageID = intMaddogOSImageID,
                                                     MaddogDBID = (int)objTestMatrix.MaddogDBID};
                
                foreach (DataRow drwContextDetails in dstContextDetails.Tables[0].Rows)
                    if (HotFixUtility.IsContextInUse(objContext, drwContextDetails["ContextName"].ToString()) == false)
                    {
                        objTestSenario.ContextBlockName = drwContextDetails["ContextName"].ToString();
                        break;
                    }

                if (objTestSenario.ContextBlockName == null || objTestSenario.ContextBlockName.Equals(string.Empty))
                    continue;
                
                objContext.ListTestSenario.Add(objTestSenario);

                HotFixUtility.PopulateContextBlock(objTestSenario, objTestMatrix, intProductID, BeaconProductCode, objContext.TestFramework, FileLockName, ProductRepairFileName, PayLoadFilex86, "KB" + PatchInfo.TestIdentifier, PatchInfo.ProdVerificationScript,
                    MaddogDBID);
            }
            return;
        }

        //Setup Test specific
        private void PopulateTestSenariosForSetupTest(Context objContext, int intProductID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TestSenario objTestSenario;
            DataSet dstContextDetails = new DataSet();

            int intTestCaseID = int.MinValue;
            string strTestCaseName = string.Empty;
            string strTestCaseDescription = string.Empty;
            int intMaddogOSImageID = -1;
          
            foreach (TTestMatrix objTestMatrix in objContext.ListSetupTestMatrix)
            {
                if (objTestMatrix.TOSImage.MaddogOSImageID != null)
                    intMaddogOSImageID = (int)objTestMatrix.TOSImage.MaddogOSImageID;
                if (Convert.ToInt32(objTestMatrix.TestCaseID) != intTestCaseID)
                {
                    intTestCaseID = Convert.ToInt32(objTestMatrix.TestCaseID);
                    dstContextDetails = HotFixUtility.GetAssociatedContextsDetails(intTestCaseID, ref strTestCaseName, ref strTestCaseDescription,
                        appApplicationType, PatchInfo.CreatedBy, db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == PatchInfo.ExecutionSystemID).ExecutionSystemName,
                        db.TExecutionSystems.Single(exs => exs.ExecutionSystemID == PatchInfo.ExecutionSystemID).DatabaseName);
                }
                string strTestCode = db.TTestCaseWithPriorities.SingleOrDefault(tc => (tc.TestCaseID == objTestMatrix.TestCaseID) && tc.PatchType.ToLower().Contains(appApplicationType.ToString().ToLower())).TestCaseCode;
                
                objTestSenario = new TestSenario() { TestCaseID = intTestCaseID, TestCaseCode = strTestCode, TestCaseName = strTestCaseName,
                    TestCaseDescripton = strTestCaseDescription, MDOSCPUID = objTestMatrix.TO.OSCPUID , MaddogOSImageID = intMaddogOSImageID, MaddogDBID = (int)objTestMatrix.MaddogDBID };

                objTestSenario.MDOSID = objTestMatrix.MDOSID;
                objTestSenario.LabName = objTestMatrix.TOSImage.LabName;
                objTestSenario.MaddogOSImageName = objTestMatrix.TOSImage.MaddogOSImageName;
                objTestSenario.OSImage = objTestMatrix.TOSImage.OSImage;
                objTestSenario.OSSPLevel = objTestMatrix.TOSImage.OSSPLevel;
                
                foreach (DataRow drwContextDetails in dstContextDetails.Tables[0].Rows)
                    if (HotFixUtility.IsContextInUse(objContext, drwContextDetails["ContextName"].ToString()) == false)
                    {
                        objTestSenario.ContextBlockName = drwContextDetails["ContextName"].ToString();
                        break;
                    }

                if (objTestSenario.ContextBlockName == null || objTestSenario.ContextBlockName.Equals(string.Empty))
                    continue;

                objContext.ListTestSenario.Add(objTestSenario);

                HotFixUtility.PopulateContextBlockForSetupTest(objTestSenario, objTestMatrix, intProductID, BeaconProductCode, objContext.TestFramework, FileLockName, ProductRepairFileName, PayLoadFilex86, "KB" + PatchInfo.TestIdentifier, PatchInfo.ProdVerificationScript);
            }
            return;
        }

        #endregion CreateContext

        #region Create Run Information

        public void CreateRunInfo(int MaddogDBID, long? jobID = null)
        {
            try
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                TRun objRun;

                TCPU objCPU;
                foreach (TTestProdAttribute objPatchFile in PatchInfo.TTestProdAttributes)
                {
                    Context objContext = objPatchFile.ContextObject;

                    foreach (TestSenario objTestSenario in objContext.ListTestSenario)
                    {
                        objRun = new TRun();

                        //objRun.PatchFileID = objPatchFile.PatchFileID;
                        if (((appApplicationType == HotFixUtility.ApplicationType.SetupTest || appApplicationType == HotFixUtility.ApplicationType.DeploymentTest || appApplicationType == HotFixUtility.ApplicationType.ProductSetupTest) && objContext.ListSetupTestMatrix.Count != 0)
                            || ((appApplicationType != HotFixUtility.ApplicationType.SetupTest || appApplicationType != HotFixUtility.ApplicationType.DeploymentTest || appApplicationType != HotFixUtility.ApplicationType.ProductSetupTest) && objContext.ListTestMatrix.Count != 0))
                        //if (objContext.ListTestMatrix.Count != 0)
                        {

                            //if ((PatchInfo.TargetProductName.ToLower().Contains("framework") && PatchInfo.ProductName.ToLower().Contains("4.0")) ||
                            //    (PatchInfo.ProductName.ToLower().Contains("studio") && PatchInfo.ProductName.ToLower().Contains("2010")))
                            if (IsNDP45Patch) //Take the Template run for Dev 11 Servicing Readiness for NDP45 patch as they have pre-install (striongname hijack) package to be installed. Remove this check after RTM release
                            {
                                objRun.RunTemplateID = db.TRunTemplates.SingleOrDefault
                                    (t => t.RunTemplateName.Equals(HotFixUtility.ApplicationType.Dev10Servicing.ToString()) && t.MaddogDBID == MaddogDBID).RunTemplateID;
                                if(appApplicationType.Equals(HotFixUtility.ApplicationType.ProductSetupTest))
                                    objRun.RunTemplateID = db.TRunTemplates.SingleOrDefault(t => t.RunTemplateName.Equals(appApplicationType.ToString()) && t.MaddogDBID == MaddogDBID).RunTemplateID;
                            }
                            else
                            {
                                objRun.RunTemplateID = db.TRunTemplates.SingleOrDefault(t => t.RunTemplateName.Equals(appApplicationType.ToString()) && t.MaddogDBID == MaddogDBID ).RunTemplateID;
                            }
                            objRun.TestCaseQueryID = db.TTestCaseQueries.SingleOrDefault(q => q.TestCaseQueryName.Equals(appApplicationType.ToString()) && q.MaddogDBID == MaddogDBID ).TestCaseQueryID;
                            if (Brand.ToUpper().Contains("NDP"))
                            {
                                if (objTestSenario.MDOSCPUID == -1)
                                {
                                    objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.CPUID == objPatchFile.CPUID);
                                    objRun.MachineQueryID = db.TMachineQueries.SingleOrDefault
                                        (mq => mq.MachineQueryName.ToUpper().Equals(objCPU.DecaturCPU.ToUpper() + " MACHINES") 
                                            && mq.PatchType.Equals(appApplicationType.ToString()) && mq.MaddogDBID == MaddogDBID).MachineQueryID;
                                }
                                else
                                {
                                    objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.CPUID == objTestSenario.MDOSCPUID);
                                    //objRun.MachineQueryID = db.TMachineQueries.SingleOrDefault(mq => mq.MachineQueryName.ToUpper().Equals("X86 and AMD64 Machines") && mq.PatchType.Equals(appApplicationType.ToString())).MachineQueryID;
                                    objRun.MachineQueryID = db.TMachineQueries.SingleOrDefault
                                        (mq => mq.MachineQueryName.ToUpper().Equals(objCPU.DecaturCPU.ToUpper() + " MACHINES") 
                                            && mq.PatchType.Equals(appApplicationType.ToString()) && mq.MaddogDBID == MaddogDBID ).MachineQueryID;
                                }
                            }
                            else
                            {
                                if (objTestSenario.MDOSCPUID == -1)
                                    objRun.MachineQueryID = db.TMachineQueries.SingleOrDefault
                                        (mq => mq.MachineQueryName.Equals("X86 and AMD64 Machines") && mq.PatchType.Equals(appApplicationType.ToString()) && mq.MaddogDBID == MaddogDBID ).MachineQueryID;
                                else
                                {
                                    objCPU = db.TCPUs.SingleOrDefault(cpu => cpu.CPUID == objTestSenario.MDOSCPUID);
                                    //objRun.MachineQueryID = db.TMachineQueries.SingleOrDefault(mq => mq.MachineQueryName.ToUpper().Equals("X86 and AMD64 Machines") && mq.PatchType.Equals(appApplicationType.ToString())).MachineQueryID;
                                    objRun.MachineQueryID = db.TMachineQueries.SingleOrDefault
                                        (mq => mq.MachineQueryName.ToUpper().Equals(objCPU.DecaturCPU.ToUpper() + " MACHINES") && mq.PatchType.Equals(appApplicationType.ToString()) && mq.MaddogDBID == MaddogDBID).MachineQueryID;
                                }
                            }
                            //objRun.MachineQueryID = (Brand.ToUpper().Contains("NDP") ? db.TMachineQueries.SingleOrDefault(mq => mq.MachineQueryName.ToUpper().Equals(objCPU.DecaturCPU.ToUpper() + " MACHINES")).MachineQueryID : db.TMachineQueries.SingleOrDefault(mq => mq.MachineQueryName.Equals("X86 and AMD64 Machines")).MachineQueryID);
                            objRun.WorkSpaceLocationID = db.TWorkSpaceLocations.SingleOrDefault(w => (w.WorkSpaceName.ToLower().Contains(objContext.TestFramework.ToLower()) && w.WorkSpaceName.ToLower().Contains("-" + appApplicationType.ToString().ToLower()) && w.MaddogDBID == MaddogDBID )).WorkSpaceLocationID;

                            objRun.ContextFilePath = objContext.FileName;
                            objRun.ContextBlockName = objTestSenario.ContextBlockName;

                            objRun.TestCaseCode = objTestSenario.TestCaseCode;
                            if (objRun.TestCaseCode.Equals("FLK"))
                                objRun.FileLockName = FileLockName;
                            if (objRun.TestCaseCode.Equals("REP"))
                                objRun.ProductRepairFileName = ProductRepairFileName;
                            if (objRun.TestCaseCode.Equals("TP"))
                                objRun.PayLoadFilex86 = PayLoadFilex86;
                            if (objRun.TestCaseCode.StartsWith("FP"))
                            {
                                objRun.SupersedingPatch = SupersedingPatch;
                                objRun.SupersedingMSP = SupersedingMSP;
                            }
                        }

                        objRun.CreatedBy = PatchInfo.CreatedBy;
                        objRun.CreatedDate = PatchInfo.CreatedDate;
                        objRun.LastModifiedBy = PatchInfo.LastModifiedBy;
                        objRun.LastModifiedDate = PatchInfo.LastModifiedDate;

                        objRun.JobID=jobID;
                        
                        objPatchFile.TRuns.Add(objRun);
                    }

                }
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_RUN_INFO_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, this.PatchInfo);
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_RUN_INFO_FAILED.ToString(), LogHelper.LogLevel.ERROR, this.PatchInfo, ex);
                throw (ex);
            }
            //CheckForMSPFile();
        }

        #endregion Create Run Information

    }
}
