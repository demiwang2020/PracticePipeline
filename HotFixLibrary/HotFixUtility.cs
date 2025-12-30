using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Collections;
using ScorpionDAL;
using Wix = Microsoft.Tools.WindowsInstallerXml;

using System.IO;
using System.Data;

//For MadDog APIs
//
// Legacy MadDog APIs (Whidbey and lower)
//
using MDL = MaddogObjects.Legacy;
using MDLF = MadDogObjects.Legacy.Forms;
//
// New MadDog APIs (Orcas and above)
//
using MDO = MadDogObjects;
using MDOF = MadDogObjects.Forms;

using LoggerLibrary;

// For INI Files
using SE.Utilities;
using SE.Lab;

// For HotfixBvtWebService
//using HotFixLibrary.HotfixBvtWebService;
using System.Configuration;

namespace HotFixLibrary
{
    public class HotFixUtility
    {
        static HotFixUtility()
        {
            HOTIRON_EXE = System.Configuration.ConfigurationManager.AppSettings["HOTIRON_EXE"].ToString();
            IRONSPIGOT_EXE = System.Configuration.ConfigurationManager.AppSettings["IRONSPIGOT_EXE"].ToString();
            IRONMAN_EXE = System.Configuration.ConfigurationManager.AppSettings["IRONMAN_EXE"].ToString();

            HOTIRON_PATCH_TYPE = System.Configuration.ConfigurationManager.AppSettings["HOTIRON_PATCH_TYPE"].ToString();
            IRONSPIGOT_PATCH_TYPE = System.Configuration.ConfigurationManager.AppSettings["IRONSPIGOT_PATCH_TYPE"].ToString();
            IRONMAN_PATCH_TYPE = System.Configuration.ConfigurationManager.AppSettings["IRONMAN_PATCH_TYPE"].ToString();

            NDP_X86_CONTEXT_LIMIT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["NDP_X86_CONTEXT_LIMIT"].ToString());
            NDP_AMD64_CONTEXT_LIMIT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["NDP_AMD64_CONTEXT_LIMIT"].ToString());
            NDP_IA64_CONTEXT_LIMIT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["NDP_IA64_CONTEXT_LIMIT"].ToString());

            VS_X86_CONTEXT_LIMIT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["VS_X86_CONTEXT_LIMIT"].ToString());
            VS_AMD64_CONTEXT_LIMIT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["VS_AMD64_CONTEXT_LIMIT"].ToString());
            VS_IA64_CONTEXT_LIMIT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["VS_IA64_CONTEXT_LIMIT"].ToString());

            MADDOG_FLAG_BEACONF_NoReImage = System.Configuration.ConfigurationManager.AppSettings["MADDOG_FLAG_BEACONF_NoReImage"].ToString();
            MADDOG_FLAG_BEACONF_PauseOnFaiure = System.Configuration.ConfigurationManager.AppSettings["MADDOG_FLAG_BEACONF_PauseOnFaiure"].ToString();

            HotfixWorkDirectory = System.Configuration.ConfigurationManager.AppSettings["HotfixWorkDirectory"].ToString();
            Dev10ServicingWorkDirectory = System.Configuration.ConfigurationManager.AppSettings["Dev10ServicingWorkDirectory"].ToString();
            SetupTestWorkDirectory = System.Configuration.ConfigurationManager.AppSettings["SetupTestWorkDirectory"].ToString();
            DeploymentTestWorkDirectory = System.Configuration.ConfigurationManager.AppSettings["DeploymentTestWorkDirectory"].ToString();
            ProductSetupTestWorkDirectory = System.Configuration.ConfigurationManager.AppSettings["ProductSetupTestWorkDirectory"].ToString();

            MaddogImageFile = System.Configuration.ConfigurationManager.AppSettings["MaddogImageFile"].ToString();
            iniMaddogImageFile = new IniFile(MaddogImageFile);

            //MaddogDBOwner = System.Configuration.ConfigurationSettings.AppSettings["MaddogDBOwner"].ToString();
        }

        public enum ChainerType { HotIron = 0, IronSpigot = 1, IronMan = 2 }

        public static string HOTIRON_EXE;// = "HotFixInstaller.exe";
        public static string IRONSPIGOT_EXE;// = "Spinstaller.exe";
        public static string IRONMAN_EXE;// = "Setup.exe";

        public static string HOTIRON_PATCH_TYPE;// = "Decatur";
        public static string IRONSPIGOT_PATCH_TYPE;// = "Decatur2";
        public static string IRONMAN_PATCH_TYPE;// = "Decatur3";

        public static int NDP_X86_CONTEXT_LIMIT;// = 8;
        public static int NDP_AMD64_CONTEXT_LIMIT;// = 8;
        public static int NDP_IA64_CONTEXT_LIMIT;// = 2;

        public static int VS_X86_CONTEXT_LIMIT;// = 8;
        public static int VS_AMD64_CONTEXT_LIMIT;// = 8;
        public static int VS_IA64_CONTEXT_LIMIT;// = 2;

        static string MADDOG_FLAG_BEACONF_NoReImage;// = "931";
        static string MADDOG_FLAG_BEACONF_PauseOnFaiure;// = "919";

        public static string HotfixWorkDirectory;// = @"\\vsufile\workspace\Current\HOTFIX_FIXED\Test1\";
        public static string Dev10ServicingWorkDirectory;// = @"\\vsufile\Workspace\Current\Dev10Servicing\Test2\";
        public static string SetupTestWorkDirectory;// = @"\\vsufile\Workspace\Current\Dev10Servicing\Test2\";
        public static string DeploymentTestWorkDirectory;// = @"\\vsufile\Workspace\Current\Dev10Servicing\Test2\";
        public static string ProductSetupTestWorkDirectory;// = @"\\vsufile\Workspace\Current\Dev10Servicing\Test2\";

        public enum ApplicationType { Hotfix = 1, Dev10Servicing = 2, SetupTest = 3, DeploymentTest = 4, ProductSetupTest = 5, SAFX = 6 };
        public enum DatabaseName { WHIDBEY = 1, ORCASTS = 2 };

        

        public static string MaddogImageFile;// = @"\\vsufile\workspace\Current\HOTFIX_FIXED\images.ini";
        static IniFile iniMaddogImageFile;// = new IniFile(MaddogImageFile);

        //public static string MaddogDBOwner;

        struct RunInfo
        {
            public int intRunID;
            public int intExecutionSystemRunID;
            public string strRunInfo;
            public string strExecutionSystemName;
            public string strApplicationType;

            public int intTestPassID;
            public int intTechnologyID;
        };

        #region GetPatch Information

        public static List<PatchInfo> GetPatchInfo(Patch objPatch)
        {
            //List<TPatch> lstPatchInfo = new List<TPatch>();
            //lstPatchInfo.Add(objPatch.PatchInfo);

            List<PatchInfo> lstPatchInfo = new List<PatchInfo>();
            lstPatchInfo.Add(new PatchInfoEx()
            {
                PatchID = objPatch.PatchInfo.TTestProdInfoID,
                KBNumber = objPatch.PatchInfo.TestIdentifier,
                BuildNumber = objPatch.PatchInfo.BuildNumber,
                DDSFilePath = objPatch.PatchInfo.DDSFilePath,
                SKUFilePath = objPatch.PatchInfo.SKUFilePath,
                VerificationScript = objPatch.PatchInfo.ProdVerificationScript,
                ProductName = objPatch.PatchInfo.TargetProductName,
                SPLevel = objPatch.PatchInfo.ProdSPLevel,
                CreatedBy = objPatch.PatchInfo.CreatedBy,
                LastModifyDate = objPatch.PatchInfo.LastModifiedDate.ToString("yyyy/M/d h:m:s"),
                StatusName = HotFixUtility.GetStatusName(objPatch.PatchInfo.StatusID),
                ResultName = HotFixUtility.GetResultName(objPatch.PatchInfo.ResultID),
                PercentCompleted = objPatch.PatchInfo.PercentCompleted == null ? 0 : (int)objPatch.PatchInfo.PercentCompleted
            });

            return lstPatchInfo;
        }

        public static List<PatchInfoEx> GetPatchInfoEx(Patch objPatch)
        {
            //List<TPatch> lstPatchInfo = new List<TPatch>();
            //lstPatchInfo.Add(objPatch.PatchInfo);
            List<PatchInfo> lstPatchInfo = GetPatchInfo(objPatch);

            List<PatchInfoEx> lstPatchInfoEx = new List<PatchInfoEx>();

            foreach (PatchInfo patchinfo in lstPatchInfo)
            {
                lstPatchInfoEx.Add((PatchInfoEx)patchinfo);
            }

            return lstPatchInfoEx;
        }

        public static List<PatchFile> GetPatchFiles(Patch objPatch)
        {
            //List<TPatchFile> lstPatchFile = new List<TPatchFile>(objPatch.PatchInfo.TTestProdAttributes);

            List<PatchFile> lstPatchFile1 = new List<PatchFile>();
            foreach (TTestProdAttribute objTPatchFile in objPatch.PatchInfo.TTestProdAttributes)
            {
                lstPatchFile1.Add(new PatchFileEx()
                {
                    PatchLocation = objTPatchFile.TestProdLocation,
                    PatchFileName = objTPatchFile.TestProdName,
                    TargetCPU = objTPatchFile.TCPU.CPUFriendlyName,
                    TargetLanguage = objTPatchFile.TLanguage.LanguageFriendlyName,
                    VerificationFilePath = objTPatchFile.VerificationScript,
                    LastModifyDate = objTPatchFile.LastModifiedDate.ToString("yyyy/M/d h:m:s"),
                    StatusName = HotFixUtility.GetStatusName(objTPatchFile.StatusID),
                    ResultName = HotFixUtility.GetResultName(objTPatchFile.ResultID),
                    PercentCompleted = objTPatchFile.PercentCompleted == null ? 0 : (int)objTPatchFile.PercentCompleted
                });
            }

            return lstPatchFile1;
        }

        public static List<PatchFileEx> GetPatchFilesEx(Patch objPatch)
        {
            List<PatchFile> lstPatchFile = GetPatchFiles(objPatch);
            List<PatchFileEx> lstPatchFileEx = new List<PatchFileEx>();
            foreach (PatchFile patchFile in lstPatchFile)
            {
                lstPatchFileEx.Add((PatchFileEx)patchFile);
            }
            return lstPatchFileEx;
        }

        public static List<PatchSKU> GetTPatchSKUs(Patch objPatch)
        {
            //List<TPatchSKU> lstProductInfo = new List<TPatchSKU>(objPatch.PatchInfo.TPatchSKUs);
            List<PatchSKU> lstProductInfo = new List<PatchSKU>();
            foreach (TPatchSKU objTPatchSKU in objPatch.PatchInfo.TPatchSKUs)
            {
                lstProductInfo.Add(new PatchSKU()
                {
                    Product = objTPatchSKU.TProduct.ProductFriendlyName,
                    CPU = objTPatchSKU.TCPU.CPUFriendlyName,
                    Language = objTPatchSKU.TLanguage.LanguageFriendlyName,
                    SKU = objTPatchSKU.TSKU.SKUFriendlyName
                });

            }

            return lstProductInfo;
        }

        public static List<Run> GetTPatchRUNs(Patch objPatch)
        {
            List<Run> lstRuns = new List<Run>();
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            foreach (TTestProdAttribute objPatchFile in objPatch.PatchInfo.TTestProdAttributes)
            {
                var varRun = from r in db.TRuns
                             where r.TTestProdAttributesID == objPatchFile.TestProdAttributesID
                             select r;

                foreach (TRun objRun in varRun)
                {
                    RunEx run = new RunEx();
                    run.PatchFileName = objPatchFile.TestProdName;
                    run.ContextFilePath = objRun.ContextFilePath;
                    run.RunID = objRun.MDRunID.ToString();
                    run.ContextBlockName = objRun.ContextBlockName;
                    run.Status = ((objRun.MDRunID > 0) ? MaddogHelper.GetRunStatus(objRun.MDRunID, objPatch.PatchInfo.CreatedBy, (int)objRun.TRunTemplate.MaddogDBID, true) : "N/A");
                    run.LogFile = ((objRun.MDRunID > 0) ? GetLogFilePath(objRun.MDRunID, objPatch.PatchInfo.CreatedBy, (int)objRun.TRunTemplate.MaddogDBID, run.Status) : "N/A");
                    run.ResultName = HotFixUtility.GetResultName(objRun.RunResultID);
                    run.LastModifyDate = objRun.LastModifiedDate.ToString("yyyy/M/d h:m:s");
                    //set title, status & runID(format:Run -> ID: {0},Run -> Goto: {1})
                    MaddogHelper.SetRunPropertiesbyMaddogRun(objRun.MDRunID, objPatch.PatchInfo.CreatedBy, (int)objRun.TRunTemplate.MaddogDBID, run);
                    lstRuns.Add(run);
                    //lstRuns.Add(new Run()
                    //{
                    //    PatchFileName = objPatchFile.PatchFileName,
                    //    ContextFilePath = objRun.ContextFilePath,
                    //    RunID = objRun.ExecutionSystemRunID.ToString(),
                    //    ContextBlockName = objRun.ContextBlockName,
                    //    Status = ((objRun.ExecutionSystemRunID > 0) ? MaddgHelper.GetRunStatus(objRun.ExecutionSystemRunID, objPatch.PatchInfo.CreatedBy, (int)objRun.TRunTemplate.MaddogDBID) : "N/A")
                    //    //((objRun.ExecutionSystemRunID > 0)? "Started":"N/A")
                    //});
                }
            }

            //List<TRun> lstRuns = new List<TRun>();
            //PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            //foreach (TPatchFile objPatchFile in objPatch.PatchInfo.TTestProdAttributes)
            //{
            //    var varRun = from r in db.TRuns
            //                 where r.PatchFileID == objPatchFile.PatchFileID
            //                 select r;

            //    lstRuns.AddRange(varRun.ToList<TRun>());
            //}

            return lstRuns;
        }

        public static List<RunEx> GetTPatchRUNsEx(Patch objPatch)
        {
            List<Run> lstRun = GetTPatchRUNs(objPatch);
            List<RunEx> lstRunEx = new List<RunEx>();
            foreach (Run patchFile in lstRun)
            {
                lstRunEx.Add((RunEx)patchFile);
            }
            return lstRunEx;
        }

        public static string GetLogFilePath(int intMaddogRunID, string strOwner, int MaddogDBID, string runStatus)
        {
            string logPath = "N/A"; //for deleted status return value : NA

            //            string runStatus = MaddgHelper.GetRunStatus(intMaddogRunID, strOwner, MaddogDBID);

            if (runStatus.ToLower() == "running")
            {
                logPath = "No Log";
            }
            else if (runStatus.ToLower() == "passed" || runStatus.ToLower() == "failed") // Passed & Failed
            {
                string logFoderPath = MaddogHelper.GetRunResultsFolder(intMaddogRunID, strOwner, MaddogDBID);
                //return value begin with '?NA?' that mean get failed.
                if (logFoderPath.IndexOf("N/A") == 0)
                {
                    logPath = logFoderPath;
                }
                else
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(logFoderPath);
                    if (dirInfo.Exists)
                    {
                        //get logs.zip file path
                        FileInfo[] fileList = dirInfo.GetFiles("*.zip");
                        foreach (FileInfo fi in dirInfo.GetFiles("*.zip"))
                        {
                            logPath = fi.FullName;
                            break;//So far in result foder there is one zip file.
                        }
                    }
                }
            }

            return logPath;
        }

        public static List<TestMatrixOwner> GetPatchTestMatrixOwner(Patch objPatch)
        {
            List<TestMatrixOwner> lstTestMtrixOwner = new List<TestMatrixOwner>();

            //get patch ID by Patch
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            var varOwners = (from t1 in db.TPatchTestMatrixMappings
                             join t2 in db.TTestMatrixes
                             on t1.TestMatrixID equals t2.TestMatrixID
                             where t1.PatchID == objPatch.PatchInfo.TTestProdInfoID
                             select new TestMatrixOwner
                             {
                                 TestMatrixName = t1.TestMatrixName,
                                 Owner = t2.CreatedBy
                             }).Distinct();

            lstTestMtrixOwner.AddRange(varOwners.ToList());

            if (lstTestMtrixOwner.Count <= 0)
            {
                TestMatrixOwner tmo = new TestMatrixOwner();
                tmo.TestMatrixName = "N/A";
                tmo.Owner = "N/A";
                lstTestMtrixOwner.Add(tmo);
            }

            return lstTestMtrixOwner;

        }

        #endregion GetPatch Information

        public static void SaveToDB(Patch objPatch)
        {
            try
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

                db.TTestProdInfos.InsertOnSubmit(objPatch.PatchInfo);
                db.SubmitChanges();
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.SAVING_DATA_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, objPatch.PatchInfo);
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.SAVING_DATA_FAILED.ToString(), LogHelper.LogLevel.ERROR, objPatch.PatchInfo, ex);
                throw (ex);
            }
        }

        public static void CreateContextFile(Patch objPatch)
        {
            try
            {
                //string strFileName = "Context_KB" + objPatch.PatchDropInfo.PatchKB + ".ini";
                //string strDirectoryLocation = "\\\\vsufile\\workspace\\Current\\HOTFIX_FIXED\\CONTEXTS\\" + objPatch.Brand + "\\" + objPatch.BaseLine + "\\";
                StreamWriter swWriter;
                foreach (TTestProdAttribute objPatchFile in objPatch.PatchInfo.TTestProdAttributes)
                {
                    Context objContextBO = objPatchFile.ContextObject;
                    swWriter = File.CreateText(objContextBO.FileName);
                    string[] strArray;
                    foreach (TestSenario objTestSenario in objContextBO.ListTestSenario)
                    {
                        strArray = objTestSenario.ContextBlock.Split("\n".ToCharArray());
                        foreach (string strData in strArray)
                        {
                            swWriter.WriteLine(strData);
                        }
                    }
                    swWriter.Close();
                }
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_CONTEXT_FILE_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, objPatch.PatchInfo);
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_CONTEXT_FILE_FAILED.ToString(), LogHelper.LogLevel.ERROR, objPatch.PatchInfo, ex);
                throw (ex);
            }
        }

        //public static List<TestSenario> GetAllContextBlock(Context objContext, int intProductID)
        //{
        //    List<TestSenario> lstTestSenario = new List<TestSenario>();
        //    TestSenario objTestSenario;
        //    DataSet dstContextDetails = new DataSet();
        //    string strUsedContextName = string.Empty;

        //    int intTestCaseID = int.MinValue;
        //    string strTestCaseName = string.Empty;
        //    string strTestCaseDescription = string.Empty;

        //    foreach (TMatrix objTestMatrix in objContext.ListTestMatrix)
        //    {
        //        if (Convert.ToInt32(objTestMatrix.TestCaseID) != intTestCaseID)
        //        {
        //            intTestCaseID = Convert.ToInt32(objTestMatrix.TestCaseID);
        //            dstContextDetails = GetAssociatedContextsDetails(intTestCaseID, ref strTestCaseName, ref strTestCaseDescription);
        //        }
        //        objTestSenario = new TestSenario() { TestCaseID = intTestCaseID, TestCaseName = strTestCaseName, TestCaseDescripton = strTestCaseDescription};

        //        foreach (DataRow drwContextDetails in dstContextDetails.Tables[0].Rows)
        //            if(strUsedContextName.Contains(drwContextDetails["ContextName"].ToString()) == false)
        //                objTestSenario.ContextBlockName = drwContextDetails["ContextName"].ToString();

        //        if (objTestSenario.ContextBlockName == null || objTestSenario.ContextBlockName.Equals(string.Empty))
        //            continue;
        //        strUsedContextName += "|" + objTestSenario.ContextBlockName;
        //        lstTestSenario.Add(objTestSenario);

        //        PopulateContextBlock(objTestSenario, objTestMatrix, intProductID, objContext.TestFramework);
        //    }
        //    return lstTestSenario;
        //}

        public static bool IsContextInUse(Context objContext, string strContextBlockName)
        {
            var var = from ts in objContext.ListTestSenario
                      where ts.ContextBlockName == strContextBlockName
                      select ts;
            if (var.Count() == 0)
                return false;
            return true;
        }

        public static DataSet GetAssociatedContextsDetails(int intTestCaseID, ref string strTestCaseName, ref string strTestCaseDescription,
            ApplicationType appApplicatonType, string strOwner, string strExecutionSystemName, string strExecutionSystemDatabaseName)
        {
            ConnectToMadDog(appApplicatonType, strOwner, strExecutionSystemName, strExecutionSystemDatabaseName);
            if (strExecutionSystemName.ToUpper() == DatabaseName.WHIDBEY.ToString())
            {
                MDL.Testcase objMDLTestCase;
                objMDLTestCase = new MDL.Testcase(intTestCaseID);

                strTestCaseName = ((MaddogObjects.Legacy.NamedObject)(objMDLTestCase)).Name;
                strTestCaseDescription = objMDLTestCase.Description;
                return objMDLTestCase.Contexts.GetDataSet();
            }
            else if (strExecutionSystemName.ToUpper() == DatabaseName.ORCASTS.ToString()) //Orcas
            {
                MDO.Testcase objMDOTestCase;
                objMDOTestCase = new MDO.Testcase(intTestCaseID);

                strTestCaseName = ((MDO.Testcase)(objMDOTestCase)).Name;
                strTestCaseDescription = objMDOTestCase.Description;
                return objMDOTestCase.Contexts.GetDataSet();
            }

            return null; //if both conditions are not met


        }

        //ToDo: Merge PopulateContextBlock & PopulateContextBlockForSetupTest
        public static void PopulateContextBlock(TestSenario objTestSenario, TMatrix objTestMatrix, int intProductID, string strBeaconProductCode, string txtFramework, string strFileLockName, string strProductRepairFileName, string strPayLoadFilex86, string strKBNumber, string strVerificationXML,
            int intMaddogDBID)
        {
            string strOSName = string.Empty;
            string strOSLanguageLocale = string.Empty;
            string strOSPlatform = string.Empty;

            if (intMaddogDBID == 1)
            {
                MDL.OS objMDLOS = new MaddogObjects.Legacy.OS(objTestMatrix.MDOSID);

                strOSName = objMDLOS.Name;
                strOSLanguageLocale = objMDLOS.LanguageLocale;
                strOSPlatform = objMDLOS.Platform;
            }
            else
            {
                MDO.OS objMDOOS = new MDO.OS(objTestMatrix.MDOSID);

                strOSName = objMDOOS.Name;
                strOSLanguageLocale = objMDOOS.LanguageLocale;
                strOSPlatform = objMDOOS.Platform;
            }

            string strOSDetails = strOSName + " " + strOSLanguageLocale + " " + strOSPlatform + " " + objTestMatrix.OSSPLevel;
            objTestSenario.ContextBlock = GetContextBlock(intProductID, objTestSenario.TestCaseID, objTestMatrix.MDOSID, objTestSenario.TestCaseName, objTestSenario.TestCaseDescripton, txtFramework, null,
                objTestSenario.ContextBlockName, strBeaconProductCode, strFileLockName, strProductRepairFileName, strPayLoadFilex86, strOSDetails,
                objTestMatrix.MDOSID + "-" + objTestMatrix.OSSPLevel, objTestMatrix.ProductID, objTestMatrix.SKUID, objTestMatrix.LanguageID, objTestMatrix.CPUID, objTestMatrix.ProductSPLevel,
                strKBNumber, strVerificationXML);


            /*
            List<TContextAttribute> lstContextAttribute = GetContextAttributes(intProductID, objTestSenario.TestCaseID);
            MDL.OS objMDLOS = new MaddogObjects.Legacy.OS(objTestMatrix.MDOSID);

            objTestSenario.ContextBlock = "/// Context running test " + objTestSenario.TestCaseName + " - " + objTestSenario.TestCaseDescripton + "\n";
            objTestSenario.ContextBlock += "/// " + LinqHelper.GetMappingObjectName(objTestMatrix.SKUID, txtFramework, LinqHelper.MappingObject.SKU) + " " + LinqHelper.GetMappingObjectName(objTestMatrix.LanguageID, txtFramework, LinqHelper.MappingObject.Language) + " on " + objMDLOS.Name + " " + objMDLOS.LanguageLocale + " " + objMDLOS.Platform + " " + objTestMatrix.OSSPLevel + "\n";


            foreach (TContextAttribute objContextAttrbute in lstContextAttribute)
            {
                if ((objContextAttrbute.NamePart.Equals("PROD0.NAME") || objContextAttrbute.NamePart.Equals("SETUP.BEHAVIOR")) && !strBeaconProductCode.Equals("NDP4.0"))
                    continue;

                if ((objContextAttrbute.NamePart.Equals("PREREQ0.PATH") || objContextAttrbute.NamePart.Equals("PREREQ0.ARG") || objContextAttrbute.NamePart.Equals("PREREQ0.NAME")))
                {
                    if (objTestMatrix.MDOSID > 310 || objTestMatrix.MDOSID == 211 || objTestMatrix.MDOSID == 296)
                        continue;
                    //Prereq not required for other Porducts other than VS10 & NDP40
                    if (!(strBeaconProductCode.Equals("VS10") || strBeaconProductCode.Equals("NDP4.0")))
                        continue;
                }

                objTestSenario.ContextBlock += "/// #[" + objTestSenario.ContextBlockName + "]." + objContextAttrbute.NamePart + "=";
                if (objContextAttrbute.ValuePart.StartsWith("<"))
                    switch (objContextAttrbute.ValuePart)
                    {
                        case "<MDOSID>": objTestSenario.ContextBlock += objTestMatrix.MDOSID;
                            break;
                        case "<MDOSID-SPLEVEL>": objTestSenario.ContextBlock += objTestMatrix.MDOSID + "-" + objTestMatrix.OSSPLevel;
                            break;
                        case "<PRODKEY>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductID, txtFramework, LinqHelper.MappingObject.ProductKey);
                            break;
                        case "<PRODFAM>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.SKUID, txtFramework, LinqHelper.MappingObject.ProductFamily);
                            break;
                        case "<PRODEDT>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.SKUID, txtFramework, LinqHelper.MappingObject.SKU);
                            break;
                        case "<PRODLANG>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.LanguageID, txtFramework, LinqHelper.MappingObject.Language);
                            break;
                        case "<PRODCPU>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.CPUID, txtFramework, LinqHelper.MappingObject.CPU);
                            break;
                        case "<PRODSPLEVEL>": objTestSenario.ContextBlock += objTestMatrix.ProductSPLevel;
                            break;
                        //case "<PROD0VERIFY>": objTestSenario.ContextBlock += "1";
                        //    break;
                        //case "<PATCHNAME>": objTestSenario.ContextBlock += "VS90.PATCH.HOTFIX";
                        //    break;
                        //case "<PATCHVERIFY>": objTestSenario.ContextBlock += "1";
                        //    break;
                        case "<PRODFILE>": objTestSenario.ContextBlock += strFileLockName;
                            break;
                        case "<PATCHFILE>": objTestSenario.ContextBlock += strProductRepairFileName;
                            break;
                        case "<PRODNAME>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.SKUID, txtFramework, LinqHelper.MappingObject.SKUFriendlyName);
                            break;
                        case "<SILENT>": objTestSenario.ContextBlock += "SILENT";
                            break;
                        //TP TestCase specific.
                        case "<PAYLOADFILE>": objTestSenario.ContextBlock += strPayLoadFilex86;
                            break;
                    }
                else
                    objTestSenario.ContextBlock += objContextAttrbute.ValuePart.Replace("<CPU>", LinqHelper.GetMappingObjectName(objTestMatrix.CPUID, txtFramework, LinqHelper.MappingObject.CPU));
                objTestSenario.ContextBlock += "\n";

            }
             */

        }

        public static string GetContextBlock(int intProductID, int intTestCaseID, int? intMDOSID, string strTestCaseName, string strTestCaseDescripton, string txtFramework,
            string strTestCaseSpecificData, string strContextBlockName, string strBeaconProductCode, string strFileLockName, string strProductRepairFileName, string strPayLoadFilex86,
            string strOSDetails, string strOSImage, int? intTestMatrixProductID, short? srtProductSKUID, short? srtProductLanguageID, short? srtProductCPUID, string strProductSPLevel,
            string strKBNumber, string strVerificationXML)
        {
            string strContextBlock = string.Empty;
            List<TContextAttribute> lstContextAttribute = GetContextAttributes(intTestMatrixProductID, intTestCaseID);
            if (intMDOSID != null)
            {
                MDL.OS objMDLOS = new MaddogObjects.Legacy.OS((int)intMDOSID);
            }

            strContextBlock = "/// Context running test " + strTestCaseName + " - " + strTestCaseDescripton + "\n";
            strContextBlock += "/// " + LinqHelper.GetMappingObjectName(srtProductSKUID, txtFramework, LinqHelper.MappingObject.SKU) + " " + LinqHelper.GetMappingObjectName(srtProductLanguageID, txtFramework, LinqHelper.MappingObject.Language) + " on " + strOSDetails + "\n";

            string[] arrString = new string[100];
            if (strTestCaseSpecificData != null)
            {
                arrString = strTestCaseSpecificData.Split("'#".ToCharArray());
            }

            foreach (TContextAttribute objContextAttrbute in lstContextAttribute)
            {
                if (strTestCaseSpecificData != null && strTestCaseSpecificData.Contains(objContextAttrbute.NamePart + "="))
                {
                    for (int i = 0; i < arrString.Length; i++)
                    {
                        string strString = arrString[i];
                        if (strString.Contains(objContextAttrbute.NamePart))
                        {
                            strContextBlock += "/// #[" + strContextBlockName + "]." + strString;
                            strContextBlock += "\n";

                            arrString[i] = string.Empty;
                            break;
                        }
                    }
                    continue;
                }
                if ((objContextAttrbute.NamePart.Equals("PROD0.NAME") || objContextAttrbute.NamePart.Equals("SETUP.BEHAVIOR")) && (!strBeaconProductCode.Equals("NDP4.0") && !strBeaconProductCode.Equals("NDP4.5")))
                    continue;

                if ((objContextAttrbute.NamePart.Equals("PREREQ0.PATH") || objContextAttrbute.NamePart.Equals("PREREQ0.ARG") || objContextAttrbute.NamePart.Equals("PREREQ0.NAME")))
                {
                    if (intMDOSID > 310 || intMDOSID == 211 || intMDOSID == 296)
                        continue;
                    //Prereq not required for other Porducts other than VS10 & NDP40
                    if (!(strBeaconProductCode.Equals("VS10") || strBeaconProductCode.Equals("NDP4.0") || strBeaconProductCode.Equals("NDP4.5")))
                        continue;
                }

                strContextBlock += "/// #[" + strContextBlockName + "]." + objContextAttrbute.NamePart + "=";
                if (objContextAttrbute.ValuePart.StartsWith("<"))
                    switch (objContextAttrbute.ValuePart)
                    {
                        case "<MDOSID>": strContextBlock += ((intMDOSID == null) ? "<MDOSID>" : intMDOSID.ToString());
                            break;
                        case "<MDOSID-SPLEVEL>": strContextBlock += ((strOSImage == null) ? "<MDOSID-SPLEVEL>" : strOSImage);
                            break;
                        case "<PRODKEY>": strContextBlock += ((intTestMatrixProductID == null) ? "<PRODKEY>" : LinqHelper.GetMappingObjectName(intTestMatrixProductID, txtFramework, LinqHelper.MappingObject.ProductKey));
                            break;
                        case "<PRODFAM>": strContextBlock += ((srtProductSKUID == null) ? "<PRODFAM>" : LinqHelper.GetMappingObjectName(srtProductSKUID, txtFramework, LinqHelper.MappingObject.ProductFamily));
                            break;
                        case "<PRODEDT>": strContextBlock += ((srtProductSKUID == null) ? "<PRODEDT>" : LinqHelper.GetMappingObjectName(srtProductSKUID, txtFramework, LinqHelper.MappingObject.SKU));
                            break;
                        case "<PRODLANG>": strContextBlock += ((srtProductLanguageID == null) ? "<PRODLANG>" : LinqHelper.GetMappingObjectName(srtProductLanguageID, txtFramework, LinqHelper.MappingObject.Language));
                            break;
                        case "<PRODCPU>": strContextBlock += ((srtProductCPUID == null) ? "<PRODCPU>" : LinqHelper.GetMappingObjectName(srtProductCPUID, txtFramework, LinqHelper.MappingObject.CPU));
                            break;
                        case "<PRODSPLEVEL>": strContextBlock += ((strProductSPLevel == null) ? "<PRODSPLEVEL>" : strProductSPLevel);
                            break;
                        //case "<PROD0VERIFY>": objTestSenario.ContextBlock += "1";
                        //    break;
                        //case "<PATCHNAME>": objTestSenario.ContextBlock += "VS90.PATCH.HOTFIX";
                        //    break;
                        //case "<PATCHVERIFY>": objTestSenario.ContextBlock += "1";
                        //    break;
                        case "<PRODFILE>": strContextBlock += ((strFileLockName == null) ? "<PRODFILE>" : strFileLockName);
                            break;
                        case "<PATCHFILE>": strContextBlock += ((strProductRepairFileName == null) ? "<PATCHFILE>" : strProductRepairFileName);
                            break;
                        case "<PRODNAME>": strContextBlock += ((srtProductSKUID == null) ? "<PRODNAME>" : LinqHelper.GetMappingObjectName(srtProductSKUID, txtFramework, LinqHelper.MappingObject.SKUFriendlyName));
                            break;
                        case "<SILENT>": strContextBlock += "SILENT";
                            break;
                        //TP TestCase specific.
                        case "<PAYLOADFILE>": strContextBlock += ((strPayLoadFilex86 == null) ? "<PAYLOADFILE>" : strPayLoadFilex86);
                            break;
                        case "<KBNUMBER>": strContextBlock += ((strKBNumber == null) ? "<KBNUMBER>" : strKBNumber);
                            break;
                        case "<VERIFICATION_XML>": strContextBlock += ((strVerificationXML == null) ? "<VERIFICATION_XML>" : strVerificationXML);
                            break;

                    }
                else
                    strContextBlock += ((srtProductCPUID == null) ? objContextAttrbute.ValuePart : objContextAttrbute.ValuePart.Replace("<CPU>", LinqHelper.GetMappingObjectName(srtProductCPUID, txtFramework, LinqHelper.MappingObject.CPU)));
                strContextBlock += "\n";
            }

            if (strTestCaseSpecificData != null)
            {
                for (int i = 0; i < arrString.Length; i++)
                {
                    if (arrString[i] != string.Empty && arrString[i] != null)
                    {
                        strContextBlock += "/// #[" + strContextBlockName + "]." + arrString[i];
                        strContextBlock += "\n";
                    }
                }
            }

            return strContextBlock;

        }

        //Setup Test specific
        public static void PopulateContextBlockForSetupTest(TestSenario objTestSenario, TTestMatrix objTestMatrix, int intProductID, string strBeaconProductCode, string txtFramework, string strFileLockName, string strProductRepairFileName, string strPayLoadFilex86, string strKBNumber, string strVerificationXML)
        {
            objTestSenario.ContextBlock = GetContextBlock(intProductID, objTestSenario.TestCaseID, objTestMatrix.MDOSID, objTestSenario.TestCaseName, objTestSenario.TestCaseDescripton, txtFramework, objTestMatrix.TestCaseSpecificData,
                objTestSenario.ContextBlockName, strBeaconProductCode, strFileLockName, strProductRepairFileName, strPayLoadFilex86, objTestMatrix.TO.OSDetails,
                objTestMatrix.TOSImage.OSImage, objTestMatrix.ProductID, objTestMatrix.ProductSKUID, objTestMatrix.ProductLanguageID, objTestMatrix.ProductCPUID, objTestMatrix.ProductSPLevel,
                strKBNumber, strVerificationXML);

            /*
            List<TContextAttribute> lstContextAttribute = GetContextAttributes(intProductID, objTestSenario.TestCaseID);
            MDL.OS objMDLOS = new MaddogObjects.Legacy.OS(objTestMatrix.MDOSID);

            objTestSenario.ContextBlock = "/// Context running test " + objTestSenario.TestCaseName + " - " + objTestSenario.TestCaseDescripton + "\n";
            objTestSenario.ContextBlock += "/// " + LinqHelper.GetMappingObjectName(objTestMatrix.ProductSKUID, txtFramework, LinqHelper.MappingObject.SKU) + " " + LinqHelper.GetMappingObjectName(objTestMatrix.ProductLanguageID, txtFramework, LinqHelper.MappingObject.Language) + " on " + objTestMatrix.TO.OSDetails + "\n";


            string[] arrString = new string[100];
            if (objTestMatrix.TestCaseSpecificData != null)
            {
                arrString = objTestMatrix.TestCaseSpecificData.Split("'#".ToCharArray());

            }

            foreach (TContextAttribute objContextAttrbute in lstContextAttribute)
            {
                if (objTestMatrix.TestCaseSpecificData != null && objTestMatrix.TestCaseSpecificData.Contains(objContextAttrbute.NamePart))
                {
                    //string[] arrString = objTestMatrix.TestCaseSpecificData.Split("'#".ToCharArray());

                    for(int i = 0;i < arrString.Length; i++)
                    {
                        string strString = arrString[i];
                        if (strString.Contains(objContextAttrbute.NamePart))
                        {
                            objTestSenario.ContextBlock += "/// #[" + objTestSenario.ContextBlockName + "]." + strString;
                            objTestSenario.ContextBlock += "\n";
                         
                            arrString[i] = string.Empty;
                            break;
                        }
                    //foreach (string strString in arrString)
                    //{
                    //    if (strString.Contains(objContextAttrbute.NamePart))
                    //    {
                    //        objTestSenario.ContextBlock += "/// #[" + objTestSenario.ContextBlockName + "]." + strString;
                    //        objTestSenario.ContextBlock += "\n";
                            
                    //        break;
                    //    }
                    //}
                    }
                    continue;
                }
                if ((objContextAttrbute.NamePart.Equals("PROD0.NAME") || objContextAttrbute.NamePart.Equals("SETUP.BEHAVIOR")) && !strBeaconProductCode.Equals("NDP4.0"))
                    continue;

                if ((objContextAttrbute.NamePart.Equals("PREREQ0.PATH") || objContextAttrbute.NamePart.Equals("PREREQ0.ARG") || objContextAttrbute.NamePart.Equals("PREREQ0.NAME")) && (objTestMatrix.MDOSID > 310 || objTestMatrix.MDOSID == 211 || objTestMatrix.MDOSID == 296))
                    continue;

                objTestSenario.ContextBlock += "/// #[" + objTestSenario.ContextBlockName + "]." + objContextAttrbute.NamePart + "=";
                if (objContextAttrbute.ValuePart.StartsWith("<"))
                    switch (objContextAttrbute.ValuePart)
                    {
                        case "<MDOSID>": objTestSenario.ContextBlock += objTestMatrix.MDOSID;
                            break;
                        case "<MDOSID-SPLEVEL>": objTestSenario.ContextBlock += objTestMatrix.TOSImage.OSImage;
                            break;
                        case "<PRODKEY>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductID, txtFramework, LinqHelper.MappingObject.ProductKey);
                            break;
                        case "<PRODFAM>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductSKUID, txtFramework, LinqHelper.MappingObject.ProductFamily);
                            break;
                        case "<PRODEDT>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductSKUID, txtFramework, LinqHelper.MappingObject.SKU);
                            break;
                        case "<PRODLANG>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductLanguageID, txtFramework, LinqHelper.MappingObject.Language);
                            break;
                        case "<PRODCPU>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductCPUID, txtFramework, LinqHelper.MappingObject.CPU);
                            break;
                        case "<PRODSPLEVEL>": objTestSenario.ContextBlock += objTestMatrix.ProductSPLevel;
                            break;
                        //case "<PROD0VERIFY>": objTestSenario.ContextBlock += "1";
                        //    break;
                        //case "<PATCHNAME>": objTestSenario.ContextBlock += "VS90.PATCH.HOTFIX";
                        //    break;
                        //case "<PATCHVERIFY>": objTestSenario.ContextBlock += "1";
                        //    break;
                        case "<PRODFILE>": objTestSenario.ContextBlock += strFileLockName;
                            break;
                        case "<PATCHFILE>": objTestSenario.ContextBlock += strProductRepairFileName;
                            break;
                        case "<PRODNAME>": objTestSenario.ContextBlock += LinqHelper.GetMappingObjectName(objTestMatrix.ProductSKUID, txtFramework, LinqHelper.MappingObject.SKUFriendlyName);
                            break;
                        case "<SILENT>": objTestSenario.ContextBlock += "SILENT";
                            break;
                        //TP TestCase specific.
                        case "<PAYLOADFILE>": objTestSenario.ContextBlock += strPayLoadFilex86;
                            break;
                    }
                else
                    objTestSenario.ContextBlock += objContextAttrbute.ValuePart.Replace("<CPU>", LinqHelper.GetMappingObjectName(objTestMatrix.ProductCPUID, txtFramework, LinqHelper.MappingObject.CPU));
                objTestSenario.ContextBlock += "\n";

            }

            if (objTestMatrix.TestCaseSpecificData != null)
            {
                for (int i = 0; i < arrString.Length; i++)
                {
                    if (arrString[i] != string.Empty && arrString[i] != null)
                    {
                        objTestSenario.ContextBlock += "/// #[" + objTestSenario.ContextBlockName + "]." + arrString[i];
                        objTestSenario.ContextBlock += "\n";
                    }
                }
            }
             */

        }

        private static List<TContextAttribute> GetContextAttributes(int? intProductID, int intTestCaseID)
        {
            List<TContextAttribute> lstContextAttribute = new List<TContextAttribute>();
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            var ProductSpecific = from contextvalue in db.TContextAttributeProductSpecificValues
                                  where contextvalue.ProductID == intProductID && contextvalue.Active == true
                                  select contextvalue;

            var v = from context in db.TContextAttributes
                    join testcontext in db.TTestCaseContextAttributeMappings on context.ContextAttributeID equals testcontext.ContextAttributeID
                    join contextvalue in ProductSpecific on context.ContextAttributeID equals contextvalue.ContextAttributeID into g
                    //&& intProductID.ToString().Equals(contextvalue.ProductID.ToString()) into g
                    //contextvalue.ProductID.ToString().Equals(intProductID.ToString()) into g
                    from o in g.DefaultIfEmpty()
                    //join contextvalue in db.tbl_ContextAttributeProductSpecificValues on intProductKey equals contextvalue.ProductKeyID
                    where testcontext.TestCaseID == intTestCaseID && (o.ProductID == null || o.ProductID == intProductID) && context.Active == true && testcontext.Active == true
                    select new { context.ContextAttributeID, context.NamePart, Value = (o.AttributeValue == null ? context.ValuePart : o.AttributeValue) };

            //var v = from context in db.TContextAttributes
            //        join testcontext in db.TTestCaseContextAttributeMappings on context.ContextAttributeID equals testcontext.ContextAttributeID
            //        join contextvalue in db.TContextAttributeProductSpecificValues on context.ContextAttributeID equals contextvalue.ContextAttributeID into g
            //        //&& intProductID.ToString().Equals(contextvalue.ProductID.ToString()) into g
            //        //contextvalue.ProductID.ToString().Equals(intProductID.ToString()) into g
            //        from o in g.DefaultIfEmpty()
            //        //join contextvalue in db.tbl_ContextAttributeProductSpecificValues on intProductKey equals contextvalue.ProductKeyID
            //        where testcontext.TestCaseID == intTestCaseID && (o.ProductID == null || o.ProductID == intProductID)
            //        select new { context.ContextAttributeID, context.NamePart, Value = (o.AttributeValue == null ? context.ValuePart : o.AttributeValue) };

            TContextAttribute objContextAttribute;
            foreach (var item in v)
            {
                objContextAttribute = new TContextAttribute() { ContextAttributeID = item.ContextAttributeID, NamePart = item.NamePart, ValuePart = item.Value };
                lstContextAttribute.Add(objContextAttribute);
            }
            //List<tbl_ContextAttribute> lstContextAttribute = from context in db.tbl_ContextAttributes
            //                                                 where context.tbl_TestCaseContextAttributeMappings.tbl_TestCaseContextAttributeMappings.Select(x => x.TestCaseID) < TestCaseID
            //                                                 select context;
            return lstContextAttribute;
        }

        public static void ConnectToMadDog(ApplicationType appApplicatonType, string strOwner, string strExecutionSystemName, string strExecutionSystemDatabaseName)
        {
            //try
            //{
                if (strExecutionSystemName.ToUpper() == DatabaseName.WHIDBEY.ToString())
                {
                    MDL.Utilities.Security.AppName = appApplicatonType.ToString();
                    MDL.Utilities.Security.AppOwner = strOwner;
                    MDL.Utilities.Security.SetDB(strExecutionSystemDatabaseName, strExecutionSystemName);
                }
                if (strExecutionSystemName.ToUpper() == DatabaseName.ORCASTS.ToString())
                {
                    string strUserName = strOwner;
                    if ((strOwner != null) && (strOwner.Split('\\') != null) && (strOwner.Split('\\')).Count() > 1)
                        strUserName = strOwner.Split('\\')[1]; //removing the domian Name

                    MDO.Utilities.Security.AppName = appApplicatonType.ToString();
                    MDO.Utilities.Security.AppOwner = strUserName;  //HardCodeing just for Testing
                    MDO.Utilities.Security.SetDB(strExecutionSystemDatabaseName, strExecutionSystemName);

                    MDO.Branch.CurrentBranch = new MDO.Branch(536);
                    //if (appApplicatonType.Equals(ApplicationType.ProductSetupTest))
                    // MDO.Branch.CurrentBranch = new MDO.Branch(742);
                    MDO.Branch.CurrentBranch.SetNoLocalEnlistment();

                }
            //}
            //catch (Exception ex)
            //{
            //    Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, null, ex);
            //    throw (ex);
            //}
        }

        public static string GetMSPFile(TTestProdAttribute objPatchFile, string strProductName, string strSPLevel, string strKBNumber, string strBuildNumber, ApplicationType appType, string strChipLanguage, ref ChainerType PatchChainerType)
        {
            try
            {
                string strExtractLocation = (appType == ApplicationType.Hotfix ? HotFixUtility.HotfixWorkDirectory : HotFixUtility.Dev10ServicingWorkDirectory)
                    + @"MSPs\" + strProductName.Replace(' ', '_') + @"\" + strSPLevel + @"\KB" + strKBNumber + @"\" + strBuildNumber + @"\" + strChipLanguage + @"\";

                string strOriginalExtractLocation = strExtractLocation;
                int intCounter = 0;
                while (System.IO.Directory.Exists(strExtractLocation))
                {
                    strExtractLocation = (appType == ApplicationType.Hotfix ? HotFixUtility.HotfixWorkDirectory : HotFixUtility.Dev10ServicingWorkDirectory)
                    + @"MSPs\" + strProductName.Replace(' ', '_') + @"\" + strSPLevel + @"\KB" + strKBNumber + @"\" + strBuildNumber + @"\" + strChipLanguage + "_" + intCounter.ToString() + @"\";

                    intCounter++;
                }

                string strPatchLocation = objPatchFile.TestProdLocation;

                ExtractMSP(strPatchLocation + @"\" + objPatchFile.TestProdName, strExtractLocation);
                //while (!(procExtraction.HasExited))
                //{

                if (File.Exists(strExtractLocation + HOTIRON_EXE))
                    PatchChainerType = ChainerType.HotIron;
                else if (File.Exists(strExtractLocation + IRONSPIGOT_EXE))
                    PatchChainerType = ChainerType.IronSpigot;
                else if (File.Exists(strExtractLocation + IRONMAN_EXE))
                    PatchChainerType = ChainerType.IronMan;

                string[] astrListOfFiles = Directory.GetFiles(strExtractLocation, "*" + strKBNumber + "*.msp");
                if (astrListOfFiles.Length == 1)
                {
                    return astrListOfFiles[0];
                }
                else if (astrListOfFiles.Length > 1)
                {
                    throw new Exception("More than one MSP file found with the same KB Number");
                }
                //}
                throw new Exception("MSP file not found");
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, null, ex);
                throw (ex);
            }

            //return "";
        }

        public static void ExtractMSP(string strFullPatchFilePath, string strExtractLocation)
        {
            System.Diagnostics.Process procExtraction = new System.Diagnostics.Process();
            procExtraction.StartInfo.FileName = strFullPatchFilePath;
            procExtraction.StartInfo.Arguments = @"/q /x:" + "\"" + strExtractLocation + "\"";
            procExtraction.Start();
            procExtraction.WaitForExit();
            if (procExtraction.ExitCode != 0)
            {
                throw new Exception("Patch extraction failed. Process Exit Code: " + procExtraction.ExitCode.ToString());
            }
        }

        public static void Extract_10Or11_MSI(string strFullPatchFilePath, string strExtractLocation)
        {
            System.Diagnostics.Process procExtraction = new System.Diagnostics.Process();
            procExtraction.StartInfo.FileName = strFullPatchFilePath;
            procExtraction.StartInfo.Arguments = @"/q /extract " + "\"" + strExtractLocation + "\"";
            procExtraction.Start();
            procExtraction.WaitForExit();
            if (procExtraction.ExitCode != 0)
            {
                throw new Exception("Patch extraction failed. Process Exit Code: " + procExtraction.ExitCode.ToString());
            }
        }

        public static void Extract_10Or11_OCM(string strFullPatchFilePath, string strExtractLocation)
        {
            System.Diagnostics.Process procExtraction = new System.Diagnostics.Process();
            procExtraction.StartInfo.FileName = strFullPatchFilePath;
            procExtraction.StartInfo.Arguments = @"/q /extract:" + "\"" + strExtractLocation + "\"";
            procExtraction.Start();
            procExtraction.WaitForExit();
            if (procExtraction.ExitCode != 0)
            {
                throw new Exception("Patch extraction failed. Process Exit Code: " + procExtraction.ExitCode.ToString());
            }
        }

        public static ArrayList CreateRun(Patch objPatch, bool IsNebula = false, ApplicationType appType = ApplicationType.SetupTest)
        {
            iniMaddogImageFile.AllowDuplicateSections = true;
            iniMaddogImageFile.AllowDuplicateKeys = true;
            int RunID = 0;
            int MaddogDBID = 1; //by Default pointing to Whideby
            ArrayList runList = new ArrayList();
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TTestProdInfo objPatchInfo = objPatch.PatchInfo;
            List<TVariable> lstVariables = GetMaddogVariables(objPatch.PatchInfo.ProductID);

            try
            {
                foreach (TTestProdAttribute objPatchFile in objPatchInfo.TTestProdAttributes)
                {
                    var varRun = from r in db.TRuns
                                 where r.TTestProdAttributesID == objPatchFile.TestProdAttributesID
                                 select r;

                    List<TRun> lstRuns = varRun.ToList<TRun>();
                    foreach (TRun objRun in lstRuns)
                    {
                        //if (objRun.ContextBlockName.CompareTo("DDCPX: 0033") < 0)
                        //    continue;
                        //List<TRun> lstRun = db.TRuns.Select(r => r.PatchFileID == objPatchFile.PatchFileID);
                        //SingleOrDefault(r => r.RunID == objPatchFile.TRuns.RunID);
                        if (objRun.RunTemplateID == null || objRun.TestCaseQueryID == null
                            || objRun.MachineQueryID == null || objRun.WorkSpaceLocationID == null)
                        {
                            return runList;
                        }

                        string stringOwnerAlias = objPatch.PatchInfo.CreatedBy.Split(@"\".ToCharArray())[objPatch.PatchInfo.CreatedBy.Split(@"\".ToCharArray()).Length - 1];


                        StringBuilder sbNotes = new StringBuilder();
                        string strVariableValue = string.Empty;

                        sbNotes.Append(objPatch.ExtraRunTokens + System.Environment.NewLine);

                        foreach (TVariable objVariable in lstVariables)
                        {
                            if (sbNotes.ToString().ToLower().Contains(objVariable.VariableName.ToLower()))
                                continue;

                            switch (objVariable.VariableName)
                            {
                                case "@Beacon.WorkSpace":
                                    if (appType.Equals(ApplicationType.ProductSetupTest))
                                        continue;
                                    strVariableValue = objRun.TWorkSpaceLocation.WorkSpaceLocation;
                                    break;
                                case "@Context.Run":
                                    strVariableValue = objRun.ContextFilePath;
                                    break;
                                case "@PATCH0.PATH":
                                    if (appType.Equals(ApplicationType.ProductSetupTest))
                                        continue;
                                    strVariableValue = objPatchFile.TestProdLocation + @"\" + objPatchFile.TestProdName;
                                    break;
                                case "@PATCH0.NAME":
                                    if (appType.Equals(ApplicationType.ProductSetupTest))
                                        continue;
                                    strVariableValue = objPatchFile.TestProdName;
                                    break;
                                case "@PATCH0.VERIFICATION":
                                    if (appType.Equals(ApplicationType.ProductSetupTest))
                                        continue;
                                    strVariableValue = objPatchFile.VerificationScript;
                                    break;
                                case "@PATCH0.TYPE":
                                    if (appType.Equals(ApplicationType.ProductSetupTest))
                                        continue;
                                    if (objPatch.PatchChainerType == ChainerType.HotIron)
                                        strVariableValue = HOTIRON_PATCH_TYPE;
                                    else if (objPatch.PatchChainerType == ChainerType.IronSpigot)
                                        strVariableValue = IRONSPIGOT_PATCH_TYPE;
                                    else if (objPatch.PatchChainerType == ChainerType.IronMan)
                                        strVariableValue = IRONMAN_PATCH_TYPE;
                                    break;

                            }

                            sbNotes.Append(objVariable.VariableName + "=" + strVariableValue + System.Environment.NewLine + System.Environment.NewLine);
                        }


                        if (objRun.TestCaseCode.StartsWith("FP"))
                        {
                            sbNotes.Append("@PATCH1.PATH=" + objPatch.SupersedingPatch + System.Environment.NewLine + System.Environment.NewLine);
                            sbNotes.Append("@PATCH1.NAME=" + System.IO.Path.GetFileName(objPatch.SupersedingPatch) + System.Environment.NewLine + System.Environment.NewLine);
                            sbNotes.Append("@PATCH1.VERIFICATION=" + objPatch.SupersedingMSP + System.Environment.NewLine + System.Environment.NewLine);
                        }
                        TestSenario objTestSenario = objPatchFile.ContextObject.ListTestSenario.SingleOrDefault(TS => TS.ContextBlockName.Equals(objRun.ContextBlockName));

                        string strOSIDLocator = "#[" + objRun.ContextBlockName + "].OS.ID=";
                        int intStartIndexOSIDLocator = objTestSenario.ContextBlock.IndexOf(strOSIDLocator) + strOSIDLocator.Length;
                        string strOSID = objTestSenario.ContextBlock.Substring(intStartIndexOSIDLocator, objTestSenario.ContextBlock.IndexOf("\n", intStartIndexOSIDLocator) - intStartIndexOSIDLocator);

                        string strOSImageLocator = "#[" + objRun.ContextBlockName + "].OS.IMAGE=";
                        int intStartIndexOSImageLocator = objTestSenario.ContextBlock.IndexOf(strOSImageLocator) + strOSImageLocator.Length;
                        string strOSImage = objTestSenario.ContextBlock.Substring(intStartIndexOSImageLocator, objTestSenario.ContextBlock.IndexOf("\n", intStartIndexOSImageLocator) - intStartIndexOSImageLocator);


                        #region Whidbey Run
                        if (objPatch.PatchInfo.TExecutionSystem.ExecutionSystemName.ToUpper() == DatabaseName.WHIDBEY.ToString())
                        {
                            //Only Load the INI File in Whideby
                            iniMaddogImageFile.LoadLines();
                            IList<IniValue> iniValues = iniMaddogImageFile.Values;

                            MaddogDBID = 1;

                            MDL.Run runTemplate = new MaddogObjects.Legacy.Run(objRun.TRunTemplate.RunID);// ((int)reader["RunTemplateId"]);
                            MDL.Run run = runTemplate.Clone();
                            MaddogObjects.Legacy.Owner ownRunOwner;
                            try
                            {
                                ownRunOwner = new MaddogObjects.Legacy.Owner(stringOwnerAlias);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, objPatch.PatchInfo, ex);
                                throw (ex);

                                //ownRunOwner = MaddogObjects.Legacy.Owner.CurrentOwner.Clone();
                                //ownRunOwner.Name = stringOwnerAlias;
                            }

                            run.Owner = ownRunOwner;

                            //Re-image option should always be selected
                            run.Reimage = true;


                            run.Notes = sbNotes.ToString();

                            run.Title = run.Title + " - " + objPatchFile.TestProdName + " - " + DateTime.Now.ToString();

                            MDL.QueryObject machineQuery = new MaddogObjects.Legacy.QueryObject(objRun.TMachineQuery.QueryID /*(int)reader["MachineQueryId"]*/);
                            run.MachineQuery = machineQuery;

                            MDL.QueryObject testcaseQuery = new MaddogObjects.Legacy.QueryObject(objRun.TTestCaseQuery.QueryID /*(int)reader["TestcaseQueryId"]*/);
                            run.TestcaseQuery = testcaseQuery;

                            MDL.QueryObject qContextQuery = new MaddogObjects.Legacy.QueryObject(MDL.QueryConstants.BaseObjectTypes.Contexts);
                            //foreach (TestSenario objTestSenario in objPatchFile.ContextObject.ListTestSenario)
                            //{
                            //  string strContextName = objTestSenario.ContextBlockName;
                            qContextQuery.QueryAdd("ContextName", MDL.QueryConstants.EQUALTO.ToString(), objRun.ContextBlockName, MDL.QueryConstants.OR_OPERATOR);
                            //}
                            qContextQuery.Name = System.Guid.NewGuid().ToString();
                            qContextQuery.Owner = MDL.Owner.CurrentOwner; // objPatch.PatchInfo.CreatedBy;
                            qContextQuery.Save();
                            run.ContextQuery = qContextQuery;

                            //run.ma

                            MDL.Flag runFlag = new MaddogObjects.Legacy.Flag(931);

                            MDL.QueryObject qFlagQuery = new MaddogObjects.Legacy.QueryObject(MDL.QueryConstants.BaseObjectTypes.Flags);
                            qFlagQuery.QueryAdd("FlagID", MDL.QueryConstants.EQUALTO.ToString(), MADDOG_FLAG_BEACONF_NoReImage, MDL.QueryConstants.OR_OPERATOR);
                            if (objPatch.IsPauseOnFailureOn)
                                qFlagQuery.QueryAdd("FlagID", MDL.QueryConstants.EQUALTO.ToString(), MADDOG_FLAG_BEACONF_PauseOnFaiure, MDL.QueryConstants.OR_OPERATOR);
                            qFlagQuery.Name = System.Guid.NewGuid().ToString();
                            qFlagQuery.Owner = MDL.Owner.CurrentOwner;
                            qFlagQuery.Save();
                            run.RunBuildFlags = qFlagQuery;

                            run.OS = new MaddogObjects.Legacy.OS(Convert.ToInt32(strOSID));

                            IList<IniValue> lstIniValues = (from v in iniValues
                                                            where v.SectionName.ToLower().StartsWith("ddcpx " + strOSImage.Split("-".ToCharArray())[0].ToLower())
                                                            && v.KeyName.ToLower().Contains(strOSImage.Split("-".ToCharArray())[0].ToLower())
                                                            && (strOSImage.Split("-".ToCharArray()).Length == 1 || v.KeyName.ToLower().Contains(strOSImage.Split("-".ToCharArray())[1].ToLower()))
                                                            && v.KeyName.ToLower().Trim().Equals(strOSImage.ToLower().Trim())
                                                            orderby v.LineNumber ascending
                                                            select v).ToList();

                            if (lstIniValues.Count == 1)
                            {
                                run.OSImage = new MaddogObjects.Legacy.OS.OSImage("DDCPX:" + strOSImage, Convert.ToInt32(strOSID));
                            }
                            else
                            {
                                lstIniValues = (from v in iniValues
                                                where v.SectionName.ToLower().StartsWith(strOSID.ToLower())
                                                && v.KeyName.ToLower().Contains(strOSImage.Split("-".ToCharArray())[0].ToLower())
                                                && v.KeyName.ToLower().Contains(strOSImage.Split("-".ToCharArray())[1].ToLower())
                                                && (strOSImage.Split("-".ToCharArray()).Length == 1 || v.KeyName.ToLower().Trim().Equals(strOSImage.ToLower().Trim()))
                                                orderby v.LineNumber ascending
                                                select v).ToList();

                                if (lstIniValues.Count == 1)
                                {
                                    run.OSImage = new MaddogObjects.Legacy.OS.OSImage(lstIniValues[0].KeyName, Convert.ToInt32(strOSID));
                                }
                                else
                                {
                                    throw new Exception("No match for OS Image found for " + strOSImage);
                                }
                            }

                            //run.Product = new MaddogObjects.Legacy.Product(

                            run.Save();
                            RunID = run.ID;
                            MDL.Run.RunHelpers.StartRun(run);
                        }
                        #endregion Whidbey Run

                        #region Orcas Run
                        else   //Orcas
                        {
                            MaddogDBID = 2;
                            MDO.Run runTemplate = new MDO.Run(objRun.TRunTemplate.RunID);// ((int)reader["RunTemplateId"]);
                            //if(appType.Equals(ApplicationType.ProductSetupTest))
                            //    runTemplate = new MDO.Run(1668939);

                            MDO.Run run = runTemplate.Clone();
                            MDO.Owner ownRunOwner;
                            try
                            {
                                ownRunOwner = new MDO.Owner(stringOwnerAlias);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, objPatch.PatchInfo, ex);
                                throw (ex);
                            }

                            run.Owner = ownRunOwner;
                            run.Reimage = true;


                            run.Notes = sbNotes.ToString();

                            run.Title = objPatch.PatchInfo.WorkItem + " - " + run.Title + " - " + objPatchFile.TestProdName + " - " + DateTime.Now.ToString();

                            MDO.QueryObject machineQuery = new MDO.QueryObject(objRun.TMachineQuery.QueryID);
                            run.MachineQuery = machineQuery;

                            if (objPatchFile.CPUID != 3) //Don't use Nebula for IA64 runs
                            {
                                if (IsNebula)
                                    run.VMRole = MDO.enuVMRoles.UsePhysical;
                            }

                            MDO.QueryObject testcaseQuery = new MDO.QueryObject(objRun.TTestCaseQuery.QueryID);
                            run.TestcaseQuery = testcaseQuery;

                            MDO.QueryObject qContextQuery = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.Contexts);
                            qContextQuery.QueryAdd("ContextName", MDO.QueryConstants.EQUALTO.ToString(), objRun.ContextBlockName, MDO.QueryConstants.OR_OPERATOR);
                            qContextQuery.Name = System.Guid.NewGuid().ToString();
                            qContextQuery.Owner = MDO.Owner.CurrentOwner; // objPatch.PatchInfo.CreatedBy;
                            qContextQuery.Save();
                            run.ContextQuery = qContextQuery;

                            MDO.Flag runFlag = new MDO.Flag(931);

                            MDO.QueryObject qFlagQuery = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.Flags);
                            qFlagQuery.QueryAdd("FlagID", MDO.QueryConstants.EQUALTO.ToString(), MADDOG_FLAG_BEACONF_NoReImage, MDO.QueryConstants.OR_OPERATOR);
                            if (objPatch.IsPauseOnFailureOn)
                                qFlagQuery.QueryAdd("FlagID", MDO.QueryConstants.EQUALTO.ToString(), MADDOG_FLAG_BEACONF_PauseOnFaiure, MDO.QueryConstants.OR_OPERATOR);
                            qFlagQuery.Name = System.Guid.NewGuid().ToString();
                            qFlagQuery.Owner = MDO.Owner.CurrentOwner;
                            qFlagQuery.Save();
                            run.RunBuildFlags = qFlagQuery;

                            MDO.Branch.CurrentBranch = new MDO.Branch(536);
                            if (appType.Equals(ApplicationType.ProductSetupTest))
                                MDO.Branch.CurrentBranch = new MDO.Branch(742);

                            if (objPatch.appApplicationType == ApplicationType.ProductSetupTest)
                                MDO.Branch.CurrentBranch = new MDO.Branch(742);

                            run.OS = new MDO.OS(Convert.ToInt32(strOSID));
                            run.OSImage = new MDO.OSImage(objTestSenario.MaddogOSImageID); // MaddogOSImageID is Unique

                            run.Save();

                            if (objPatch.AddStrongNameHijackPackage)
                            {
                                MDO.Package objMDOPackage = new MDO.Package(10494);
                                DefinitionInterpreter.Selection objSelection1 = new DefinitionInterpreter.Selection();
                                objSelection1 = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
                                objSelection1.SetToken("CommandLine", ConfigurationManager.AppSettings["Preinstall"].ToString());
                                run.InstallSelections.InputSequence.Add(objSelection1);
                                run.Save();
                                //run.InstallSelections = run.InstallSelections;
                                //DefinitionInterpreter.Log.Enabled = true;
                                run.GenerateInstallationSequence();
                                //run.SetSecurityOnResultsFolder();
                                run.Save();
                            }

                            RunID = run.ID;
                            MDO.Run.RunHelpers.StartRun(run);
                        }

                        objRun.MDRunID = RunID;
                        objRun.RunStatusID = 2;
                        objRun.RunResultID = 3;
                        db.SubmitChanges();

                        RunInfo runInfo = new RunInfo();

                        runInfo.intRunID = objPatchFile.TestProdAttributesID /*(int)reader["RunId"]*/;
                        runInfo.intExecutionSystemRunID = RunID;
                        runInfo.strRunInfo = "KBNumber=" + objPatch.PatchInfo.TestIdentifier
                            + ";BuildNumber=" + objPatch.PatchInfo.BuildNumber
                            + ";ProductName=" + objPatch.PatchInfo.TargetProductName
                            + ";PatchFileName=" + objPatchFile.TestProdName
                            + ";ContextFilePath=" + objRun.ContextFilePath
                            + ";ContextBlockName=" + objRun.ContextBlockName
                            + ";";
                        runInfo.strExecutionSystemName = objPatch.PatchInfo.TExecutionSystem.ExecutionSystemName;
                        runInfo.strApplicationType = objPatch.PatchInfo.ApplicationType;

                        runInfo.intTestPassID = Convert.ToInt32(db.TTPNDatas.Single(tpnd =>
                            (tpnd.ApplicationType.Equals(objPatch.PatchInfo.ApplicationType) && tpnd.TPNDataName.Equals("TestPassID")) && tpnd.MaddogDBID == MaddogDBID).TPNDataValue);
                        runInfo.intTechnologyID = Convert.ToInt32(db.TTPNDatas.Single(tpnd => (tpnd.ApplicationType.Equals(objPatch.PatchInfo.ApplicationType) && tpnd.TPNDataName.Equals("TechnologyID")) && tpnd.MaddogDBID == MaddogDBID).TPNDataValue);

                        runList.Add(runInfo);
                        #endregion Orcas Run;
                    }
                    Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.MADDOG_RUN_STARTED_PASSED.ToString(), LogHelper.LogLevel.INFORMATION, objPatchInfo);
                }
            }
            catch (Exception ex)
            {

                Logger.Instance.AddLogMessage(LogHelper.PredefinedLogMessages.CREATION_RUN_INFO_FAILED.ToString(), LogHelper.LogLevel.ERROR, objPatchInfo, ex);
                throw (ex);
            }

            return runList;
        }

        private static List<TVariable> GetMaddogVariables(short intProductID)
        {
            List<TVariable> lstVariables = new List<TVariable>();
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            //var varVPM = db.TVariableProductMappings.Select(vpm => vpm.ProductID == intProductID);
            //var varVariable = from variable in db.TVariables.Select(vpm => vpm. ProductID == intProductID)
            //                  where variable.TVariableProductMappings. TVariableProductMappings.Select(vpm => vpm.ProductID == intProductID)

            var v = from variable in db.TVariables
                    join vpm in db.TVariableProductMappings on variable.VariableID equals vpm.VariableID
                    where vpm.ProductID == intProductID
                    select variable;


            foreach (TVariable objVariable in v)
            {
                lstVariables.Add(objVariable);
            }
            return lstVariables;
        }

        public static void UpdateRunResult(int intPatchDropInfoID)
        {

        }

        #region Interaction with TestPassNet

        public static void SaveRunToTPN(ArrayList arlRunInfo)
        {
            //foreach (RunInfo objRunInfo in arlRunInfo)
            //{
            //    TPNMatrix.MatrixSoapClient objMatrixSoapClient = new HotFixLibrary.TPNMatrix.MatrixSoapClient();
            //    TPNMatrix.MatrixRowOpResult objMatrixRowOpResult = objMatrixSoapClient.CreateRow_BasedOnMdRun(objRunInfo.strExecutionSystemName, objRunInfo.intExecutionSystemRunID, objRunInfo.intTestPassID, objRunInfo.intTechnologyID, TPNMatrix.RunAssociation.RunToTrack);
            //    objMatrixSoapClient.LogToRow((uint)objMatrixRowOpResult.RowID, objRunInfo.strApplicationType, objRunInfo.strRunInfo);
            //}
        }

        //public static DataPu[] GetPUsList(string strUserName)
        //{
        //    string strFilterUserName = string.IsNullOrEmpty(strUserName) ? null : strUserName;
        //    //string strFilterUserName = null;
        //    //strFilterUserName = "REDMOND\ashk";
        //    HotfixBvtInteropSoapClient soapClientHotfixBVT = new HotfixBvtInteropSoapClient();
        //    ResultPus resPUs = soapClientHotfixBVT.List_Pus(strFilterUserName);
        //    return resPUs.DataPUs;
        //}

        #endregion Interaction with TestPassNet

        public static List<TestScenario> GetDev10ServicingTestMatrix(int intPUID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TestScenario> lstTestScenario = new List<TestScenario>();

            var varMatrix = from m in db.TMatrixes
                            //where m.PatchFileID == objPatchFile.PatchFileID
                            where /* m.TProduct.ProductFriendlyName == "Dev10Servicing" 
                            && */ ((intPUID == 1 && m.PUID != null) || (m.PUID == intPUID))
                            && m.Active == true
                            select new TestScenario() { MatrixIDTestCaseCode = m.MatrixID.ToString() + "|" + m.TestCaseCode, TestScenarioText = m.TestCaseName + "; " + m.OSDetails + ";", TestCaseCode = m.TestCaseCode };


            lstTestScenario = varMatrix.ToList<TestScenario>();

            //foreach (var objMatrix in varMatrix)
            //{
            //    lstTestScenario.Add(new TestScenario()
            //    {
            //        MatrixID = objMatrix.MatrixID,
            //        TestScenarioText = objMatrix.OSDetails
            //    });
            //}
            return lstTestScenario;

        }


        public static string GetUniqueFileName(string strFullFilePath, bool blnIsDirectory)
        {
            int intCounter = 0;
            string strOriginalFullFilePath = strFullFilePath;
            if (blnIsDirectory == false)
            {
                while (System.IO.File.Exists(strFullFilePath))
                {
                    strFullFilePath = System.IO.Path.GetDirectoryName(strOriginalFullFilePath) + @"\" +
                        System.IO.Path.GetFileNameWithoutExtension(strOriginalFullFilePath) + "_" + intCounter.ToString() +
                        System.IO.Path.GetExtension(strOriginalFullFilePath);
                    intCounter++;
                }
            }
            else
            {
                //string strFullFilePath = @"\\vsufile\Workspace\Current\Dev10Servicing\SKUList\Partner, NCL  (ID33)\REDMOND\neerajw\310952\MSP";

                string strOneLevelUpDirectory = strFullFilePath.Substring(0, strFullFilePath.LastIndexOf(@"\"));
                string strLastDirectory = strFullFilePath.Substring(strFullFilePath.LastIndexOf(@"\") + 1, strFullFilePath.Length - strFullFilePath.LastIndexOf(@"\") - 1);

                while (System.IO.Directory.Exists(strFullFilePath))
                {
                    strFullFilePath = strOneLevelUpDirectory + @"\" + strLastDirectory + "_" + intCounter.ToString();
                    intCounter++;
                }
            }
            return strFullFilePath;
        }

        public static string[] RemoveUnwantedLinesFromDDS(string strDDSFilePath)
        {
            string[] arrStrDDSData = File.ReadAllLines(strDDSFilePath);

            string strData = "";
            for (int i = 0; i < arrStrDDSData.Length; i++)
            {
                strData = arrStrDDSData[i];
                if (
                    (strData.Contains("$(env._NTDRIVE)") || strData.Contains("$(var.BuildType)") || strData.Contains("$(var.Lang)") || strData.Contains("$(var.IronMan_VSSupportedLanguages)") || strData.Contains("$(var.IronMan_NDPSupportedLanguages)"))
                    && (strData.Contains("<") || strData.Contains("<?"))
                    && (strData.Contains("/>") || strData.Contains("?>"))
                    && !(strData.Contains("<!--") || strData.Contains("-->"))
                    )
                    arrStrDDSData[i] = string.Empty;

            }

            return arrStrDDSData;
        }

        public static List<TProduct> GetProductList()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TProduct> lstProduct = new List<TProduct>();

            var varProducts = from p in db.TProducts
                              select p;

            lstProduct = varProducts.ToList<TProduct>();

            return lstProduct;
        }

        public static TProduct GetProduct(int intProductID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TProduct objProduct = db.TProducts.SingleOrDefault(product => product.ProductID == intProductID);
            return objProduct;
        }

        public static int GetMaddogOSImageID(string strImageName, int maddogDB)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TOSImage objOSImage = db.TOSImages.SingleOrDefault(image => (image.OSImage == strImageName && image.MaddogDBID == maddogDB));
            return objOSImage.MaddogOSImageID.Value;
        }

        public static int ValidateIntegerValue(object objSessionValue)
        {
            if ((objSessionValue == null)) return 2; //Default to Orcas if the Session is not Set
            else return int.Parse(objSessionValue.ToString());

        }

        public static bool isValidDB(string strApplicationName, int intMaddogDBID)
        {

            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            TExecutionSystem objExecutionSystem = db.TExecutionSystems.SingleOrDefault(exeSystem => (exeSystem.ApplicationType == strApplicationName && exeSystem.MaddogDBID == intMaddogDBID));

            if (objExecutionSystem == null) return false;
            else return true;
        }

        public static string RemoveQuotesFromstring(string inputString)
        {
            return inputString.Replace("\"", "");

        }

        public static string GetStatusName(int? statusID)
        {
            string statusName = "";
            //Running = 1, Completed = 2, NotStarted = 3, Pending = 4, Analyzing = 5, Error = 6 
            //Unknown = 1, Passed = 2, Failed = 3, Pending = 4, Error = 5  
            switch (statusID)
            {
                case 1:
                    statusName = "Running";
                    break;
                case 2:
                    statusName = "Completed";
                    break;
                case 3:
                    statusName = "NotStarted";
                    break;
                case 4:
                    statusName = "Pending";
                    break;
                case 5:
                    statusName = "Analyzing";
                    break;
                case 6:
                    statusName = "Error";
                    break;
                default:
                    statusName = "Unknown Status Type";
                    break;
            }
            return statusName;
        }

        public static string GetResultName(int? resultID)
        {
            string resultName = "";
            //Unknown = 1, Passed = 2, Failed = 3, Pending = 4, Error = 5  
            switch (resultID)
            {
                case 1:
                    resultName = "Unknown";
                    break;
                case 2:
                    resultName = "Passed";
                    break;
                case 3:
                    resultName = "Failed";
                    break;
                case 4:
                    resultName = "Pending";
                    break;
                case 5:
                    resultName = "Error";
                    break;

                default:
                    resultName = "Unknown Result Type";
                    break;
            }
            return resultName;
        }

        public static string GetMAXBranchIDForSpecificMaddogDB(string strMDDBID)
        {
            string strMAXBranchID = null;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varBranchIDs = from Branch in db.MDDBandBranchDetails
                               where Branch.TMaddogDBID == Convert.ToInt32(strMDDBID)
                               select Branch.id;
            if (varBranchIDs.Count() > 0)
            {
                strMAXBranchID = varBranchIDs.Max().ToString();
            }
            else
            {
                strMAXBranchID = "null";
            }
            return strMAXBranchID;
        }

        public static string GetMaddogDBNamebyID(string strMDDBID)
        {
            string strMaddogDBName = null;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varMaddogDBName = from DB in db.TMaddogDBs
                                  where DB.ID == Convert.ToInt32(strMDDBID)
                                  select DB.MaddogSystemName;
            if (varMaddogDBName.Count() > 0)
            {
                strMaddogDBName = varMaddogDBName.First();
            }
            return strMaddogDBName;
        }

        public static string GetMaddogDBServerNamebyID(string strMDDBID)
        {
            string strMaddogDBName = null;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varMaddogDBName = from DB in db.TMaddogDBs
                                  where DB.ID == Convert.ToInt32(strMDDBID)
                                  select DB.MaddogDBName;
            if (varMaddogDBName.Count() > 0)
            {
                strMaddogDBName = varMaddogDBName.First();
            }
            return strMaddogDBName;
        }

        public static string GetBranchNamebyID(string strBranchID)
        {
            string strBranchName = null;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varBranchName = from Branch in db.MDDBandBranchDetails
                                where Branch.id == Convert.ToInt32(strBranchID)
                                select Branch.BranchName;
            if (varBranchName.Count() > 0)
            {
                strBranchName = varBranchName.First();
            }

            return strBranchName;
        }

        public static int GetBranchIDbyNameFromScorpionDB(string strBranchName)
        {
            int intBranchID = 0;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varBranchID = from Branch in db.MDDBandBranchDetails
                              where Branch.BranchName == strBranchName
                              select Branch.id;
            if (varBranchID.Count() > 0)
            {
                intBranchID = varBranchID.First();
            }
            return intBranchID;
        }

        public static int GetTestRunTypeIDbyNameFromScorpionDB(string strTestRunTypeName)
        {
            int intTestRunTypeID = 0;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varTestRunTypeID = from _TestRunType in db.TestRunTypes
                                   where _TestRunType.Name == strTestRunTypeName
                                   select _TestRunType.id;
            if (varTestRunTypeID.Count() > 0)
            {
                intTestRunTypeID = varTestRunTypeID.First();
            }
            return intTestRunTypeID;
        }

        public static int GetTestTypeIDbyNameFromScorpionDB(string strTestTypeName)
        {
            int intTestTypeID = 0;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varTestTypeID = from _TestType in db.TestTypes
                                where _TestType.Name == strTestTypeName
                                select _TestType.id;
            if (varTestTypeID.Count() > 0)
            {
                intTestTypeID = varTestTypeID.First();
            }
            return intTestTypeID;
        }

        public static short GetExecutionSystemIDFromScorpionDB(string strMaddogDBName, string strApplicationType = "Scorpion")
        {
            short intExecutionSystemID = 0;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varExecutionSystemID = from ExecutionSystem in db.TExecutionSystems
                                       where ExecutionSystem.ApplicationType == strApplicationType && ExecutionSystem.ExecutionSystemName == strMaddogDBName
                                       select ExecutionSystem.ExecutionSystemID;
            if (varExecutionSystemID.Count() > 0)
            {
                intExecutionSystemID = varExecutionSystemID.First();
            }
            return intExecutionSystemID;
        }

        /// <summary>
        /// Get available product ID from ScorpionDB. To fix foreign key conflict when submit some changes to DB.
        /// </summary>
        /// <returns></returns>
        public static short GetAvailableProductIDFromScorpionDB()
        {
            short intProductID = 0;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varProductID = from _Product in db.TProducts
                               select _Product.ProductID;
            if (varProductID.Count() > 0)
            {
                intProductID = varProductID.First();
            }
            return intProductID;
        }
    }

    public class TestMatrixOwner
    {
        public string TestMatrixName { get; set; }
        public string Owner { get; set; }
    }

    public class PatchInfo
    {
        public int PatchID { get; set; }
        public string KBNumber { get; set; }
        public string BuildNumber { get; set; }
        public string VerificationScript { get; set; }
        public string DDSFilePath { get; set; }
        public string SKUFilePath { get; set; }
        public string ProductName { get; set; }
        public string SPLevel { get; set; }
        public string CreatedBy { get; set; }
    }

    public class PatchFile
    {
        public string PatchLocation { get; set; }
        public string PatchFileName { get; set; }
        public string VerificationFilePath { get; set; }
        public string TargetCPU { get; set; }
        public string TargetLanguage { get; set; }
    }

    public class PatchSKU
    {
        public string Product { get; set; }
        public string SKU { get; set; }
        public string Language { get; set; }
        public string CPU { get; set; }
    }

    public class Run
    {
        public string PatchFileName { get; set; }
        public string ContextFilePath { get; set; }
        public string RunID { get; set; }
        public string RunTitle { get; set; }
        public string ContextBlockName { get; set; }
        public string Status { get; set; }
        public string LogFile { get; set; }//Added since log file link point to the Zip file
    }

    public class TestScenario
    {
        public string MatrixIDTestCaseCode { get; set; }
        public string TestScenarioText { get; set; }
        public string TestCaseCode { get; set; }
    }

    public class PatchInfoEx : PatchInfo
    {
        public string StatusName { get; set; }
        public string ResultName { get; set; }
        public string LastModifyDate { get; set; }
        public int PercentCompleted { get; set; }
    }

    public class PatchFileEx : PatchFile
    {
        public string StatusName { get; set; }
        public string ResultName { get; set; }
        public string LastModifyDate { get; set; }
        public int PercentCompleted { get; set; }
    }

    public class RunEx : Run
    {
        public string ResultName { get; set; }
        public string LastModifyDate { get; set; }
    }

    public class RunReport : RunEx
    {
        public bool IsSAFXRun { get; set; }
    }
}
