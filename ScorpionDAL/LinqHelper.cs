using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Configuration;

namespace ScorpionDAL
{
    public static class LinqHelper
    {
        public enum ReportType { None = 0, SAFX = 1, RunTime = 2, WUAutomation = 3 };
        public enum MappingObject { Language = 1, CPU = 2, ProductKey = 3, SKU = 4, ProductFamily = 5, SKUFriendlyName = 6 };

        public static string[] GetTestFramework()
        {
            string strTestFramwork = ScorpionDAL.Properties.Settings.Default.TestFramework;
            //ConfigurationSettings.AppSettings["TestFramework"].ToString();
            return strTestFramwork.Split("|".ToCharArray());
        }

        public static string[] GetPatchVerificationOption()
        {
            string strPatchVerificationOption = ScorpionDAL.Properties.Settings.Default.PatchVerificationOption;
            //ConfigurationSettings.AppSettings["TestFramework"].ToString();
            return strPatchVerificationOption.Split("|".ToCharArray());
        }

        public static string GetMappingObjectName(int? intID, string strSystem, MappingObject enumMappingObject)
        {
            if (intID == null)
                return "";
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            string strMappingObjectName = string.Empty;
            switch (enumMappingObject)
            {
                case MappingObject.Language:
                    var vLanguage = db.TLanguages.SingleOrDefault(l => l.LanguageID == intID);
                    if (strSystem == GetTestFramework()[0])
                    {
                        strMappingObjectName = vLanguage.BriqsLanguage;
                    }
                    break;
                case MappingObject.CPU:
                    var vCPU = db.TCPUs.SingleOrDefault(c => c.CPUID == intID);
                    if (strSystem == GetTestFramework()[0])
                    {
                        strMappingObjectName = vCPU.DecaturCPU.ToUpper();// BriqsCPU;
                    }
                    break;
                case MappingObject.ProductKey:
                    var vProductKey = db.TProducts.SingleOrDefault(pk => pk.ProductID == intID);
                    if (strSystem == GetTestFramework()[0])
                    {
                        strMappingObjectName = vProductKey.BriqsProduct;
                    }
                    break;
                case MappingObject.SKU:
                    var vSKU = db.TSKUs.SingleOrDefault(sku => sku.SKUID == intID);
                    if (strSystem == GetTestFramework()[0])
                    {
                        strMappingObjectName = vSKU.BriqsSKU;
                    }
                    break;
                case MappingObject.ProductFamily:
                    var vSKU1 = db.TSKUs.SingleOrDefault(sku => sku.SKUID == intID);
                    var vProductFamily = db.TProductFamilies.SingleOrDefault(pf => pf.ProductFamilyID == vSKU1.ProductFamilyID);
                    if (strSystem == GetTestFramework()[0])
                    {
                        strMappingObjectName = vProductFamily.ProductFamilyCode;
                    }
                    break;
                case MappingObject.SKUFriendlyName:
                    var vSKU2 = db.TSKUs.SingleOrDefault(sku => sku.SKUID == intID);
                    if (strSystem == GetTestFramework()[0])
                    {
                        strMappingObjectName = vSKU2.SKUFriendlyName;
                    }
                    break;
            }
            return strMappingObjectName;
        }

        public static List<TContextAttribute> GetTestCaseAssociatedContextAttributes(int intTestCaseID, bool blnGetAvailable)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TContextAttribute> lstContextAttribute = new List<TContextAttribute>();

            if (blnGetAvailable == false)
            {
                var varContextAttribute = from context in db.TContextAttributes
                                          join testcontext in db.TTestCaseContextAttributeMappings on context.ContextAttributeID equals testcontext.ContextAttributeID
                                          where testcontext.TestCaseID == intTestCaseID && testcontext.Active == true
                                          select context;

                lstContextAttribute = varContextAttribute.ToList<TContextAttribute>();
            }
            else
            {
                var varContextAttribute = from context in db.TContextAttributes
                                          where !(from testcontext in db.TTestCaseContextAttributeMappings where testcontext.TestCaseID == intTestCaseID && testcontext.Active == true select testcontext.ContextAttributeID).Contains(context.ContextAttributeID)
                                          select context;

                //var varContextAttribute = (from context in db.TContextAttributes 
                //                           join testcontext in db.TTestCaseContextAttributeMappings on context.ContextAttributeID equals testcontext.ContextAttributeID
                //                           where testcontext.TestCaseID == intTestCaseID && context.ContextAttributeID != testcontext.ContextAttributeID
                //                           select context).Distinct();

                //var varContextAttribute = (from context in db.TContextAttributes
                //                          join testcontext in db.TTestCaseContextAttributeMappings on context.ContextAttributeID equals testcontext.ContextAttributeID
                //                          where testcontext.TestCaseID != intTestCaseID
                //                          select context).Distinct();

                lstContextAttribute = varContextAttribute.ToList<TContextAttribute>();
            }

            return lstContextAttribute;
        }

        /// <summary>
        /// Get All Active Context Attributes
        /// </summary>
        /// <returns></returns>
        public static List<TContextAttribute> GetTestCaseAssociatedContextAttributes()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            List<TContextAttribute> lstContextAttribute = new List<TContextAttribute>();

            var varContextAttribute = from context in db.TContextAttributes
                                      where context.Active == true
                                      select context;

            lstContextAttribute = varContextAttribute.ToList<TContextAttribute>();

            return lstContextAttribute;
        }

        // ======================================================================================
        // =====================  The following interface are used by report engine and/or other 
        // ======================================================================================

        public static JobIDReportData GetPTATReportData(long jobID, ReportType type)
        {
            JobIDReportData myJobIDReportData = new JobIDReportData();

            myJobIDReportData.JobID = jobID;
            myJobIDReportData.RunIDList = GetRunIDDataList(jobID, type);

            TJob objTJob;
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                objTJob = dataContext.TJobs.SingleOrDefault(c => c.JobID == jobID && c.Active == true);
            }
            if (objTJob != null)
            {
                myJobIDReportData.TFSWorkItem = objTJob.PID.ToString();
                myJobIDReportData.Creator = objTJob.CreatedBy;
                myJobIDReportData.CreationTime = Convert.ToDateTime(objTJob.CreatedDate);
            }
            else //Error handling - Reporting Engine will take care of empty string.
            {
                myJobIDReportData.TFSWorkItem = string.Empty;
                myJobIDReportData.Creator = string.Empty;
                myJobIDReportData.CreationTime = DateTime.MinValue;
            }

            return myJobIDReportData;
        }

        public static JobIDReportData GetWUAutomationReportData(long jobID)
        {
            JobIDReportData myJobIDReportData = new JobIDReportData();

            myJobIDReportData.JobID = jobID;
            myJobIDReportData.RunIDList = new List<RunIDData>();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                TWUJob job = dataContext.TWUJobs.SingleOrDefault(c => c.ID == jobID && c.Active == true);
                if (job != null)
                {
                    //Get run ID data list
                    var wuruns = from c in dataContext.TWURuns
                                         where c.JobID == jobID
                                         select c;

                    foreach (var run in wuruns)
                    {                                             
                        TWUPatchDetail detail = dataContext.TWUPatchDetails.SingleOrDefault(c => c.ID == run.PatchDetailID);
                        RunIDData myRunIDData = new RunIDData();
                        myRunIDData.RunID = run.MDRunID;
                        myRunIDData.Arch = detail.CPUID;
                        myRunIDData.PatchFile = String.Format("{0}:{1}", detail.Title, detail.GUID);
                        myRunIDData.KBNumber = detail.KB;
                        myJobIDReportData.RunIDList.Add(myRunIDData);
                    }

                    myJobIDReportData.TFSWorkItem = string.Empty;
                    myJobIDReportData.Creator = "vsulab";
                    myJobIDReportData.CreationTime = job.CreateDate;
                }
                else
                {
                    myJobIDReportData.TFSWorkItem = string.Empty;
                    myJobIDReportData.Creator = string.Empty;
                    myJobIDReportData.CreationTime = DateTime.MinValue;
                }
            }

            return myJobIDReportData;
        }

        private static List<RunIDData> GetRunIDDataList(long jobID, ReportType type)
        {
            List<RunIDData> myRunIDList = new List<RunIDData>();

            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                // For 1 job ID, we might get 6 patches. per architecture (x86,amd64,arm) per branch (ldr,gdr).
                if (type == ReportType.RunTime)
                {
                    #region Detect if job kicked off NetfxSetup run
                    var netfxsetupRuns = from c in dataContext.TNetFxSetupRunStatus
                                         where c.JobID == jobID
                                         select c;
                    #endregion

                    if (netfxsetupRuns.Count() == 0) //Beacon runs
                    {
                        List<TTestProdInfo> lstTPatch = dataContext.TTestProdInfos.Where(c => c.JobID == jobID).ToList();

                        foreach (TTestProdInfo patch in lstTPatch)
                        {
                            System.Nullable<int> myArch = patch.TargetArchitecture;
                            // For 1 patch, we will get 1 patch file
                            foreach (TTestProdAttribute patchFile in patch.TTestProdAttributes)
                            {
                                string myPatchFileName = patchFile.TestProdName;

                                // for 1 patch file, we might run it multiple times on different machines
                                // with different configurations (arch, OS type, LP etc)
                                foreach (TRun run in patchFile.TRuns)
                                {
                                    RunIDData myRunIDData = new RunIDData();
                                    myRunIDData.RunID = run.MDRunID;
                                    myRunIDData.Arch = myArch;
                                    myRunIDData.PatchFile = myPatchFileName;
                                    myRunIDData.KBNumber = patch.TestIdentifier;
                                    myRunIDList.Add(myRunIDData);
                                }
                            }
                        }
                    }
                    else
                    {                      
                        foreach (var run in netfxsetupRuns)
                        {

                            TNetFxSetupPatchInfo prodInfo = dataContext.TNetFxSetupPatchInfos.Where(c => c.ID == run.SubmissionID && run.SubmissionType=="Patch").First();

                            RunIDData myRunIDData = new RunIDData();
                            myRunIDData.RunID = run.MDRunID;
                            myRunIDData.Arch = prodInfo.CPUID;
                            myRunIDData.PatchFile = System.IO.Path.GetFileName(prodInfo.PatchLocation);
                            myRunIDData.KBNumber = prodInfo.KBNumber;
                            myRunIDList.Add(myRunIDData);
                        }
                    }
                }
                else if (type == ReportType.SAFX)
                {
                    // Get Patch SAFX data
                    myRunIDList.AddRange(GetSAFXRunIDDataList(dataContext, jobID, true));
                    
                    // Get Product SAFX data, for now it is only HFR
                    myRunIDList.AddRange(GetSAFXRunIDDataList(dataContext, jobID, false));
                }
            }
            return myRunIDList;
        }

        private static List<RunIDData> GetSAFXRunIDDataList(ScorpionDAL.PatchTestDataClassDataContext dataContext, long jobID, bool bCheckPatchSAFX)
        {
            List<RunIDData> listRunIDData = new List<RunIDData>();
            int KBNumberDataID, ArchDataID, PatchPathID;
            if (bCheckPatchSAFX)
            {
                KBNumberDataID = 5;
                ArchDataID = 8;
                PatchPathID = 10;
            }
            else
            {
                KBNumberDataID = 71;
                ArchDataID = 53;
                PatchPathID = 54;
            }

            List<TSAFXProjectSubmittedData> lstTSAFXProjectSubmittedDataOfKBNumber =
                dataContext.TSAFXProjectSubmittedDatas.Where(c => c.JobID == jobID && c.SAFXProjectInputDataID == KBNumberDataID).ToList();
            List<TSAFXProjectSubmittedData> lstTSAFXProjectSubmittedDataOfArch =
                dataContext.TSAFXProjectSubmittedDatas.Where(c => c.JobID == jobID && c.SAFXProjectInputDataID == ArchDataID).ToList();
            List<TSAFXProjectSubmittedData> lstTSAFXProjectSubmittedDataOfPatchPath =
                dataContext.TSAFXProjectSubmittedDatas.Where(c => c.JobID == jobID && c.SAFXProjectInputDataID == PatchPathID).ToList();

            foreach (TSAFXProjectSubmittedData safxObject in lstTSAFXProjectSubmittedDataOfKBNumber)
            {
                RunIDData myRunIDData = new RunIDData();
                myRunIDData.RunID = (int)safxObject.RunID;
                //myRunIDData.Arch = myArch;
                foreach (TSAFXProjectSubmittedData safxObjectArch in lstTSAFXProjectSubmittedDataOfArch)
                {
                    if (safxObject.RunID == safxObjectArch.RunID)
                    {
                        var myarch = dataContext.TCPUs.SingleOrDefault(cpu => cpu.BriqsCPU == safxObjectArch.FieldValue.ToUpper());
                        if (myarch != null)
                            myRunIDData.Arch = myarch.CPUID;
                        break;
                    }
                }

                foreach (TSAFXProjectSubmittedData safxObjectPatchPath in lstTSAFXProjectSubmittedDataOfPatchPath)
                {
                    if (safxObject.RunID == safxObjectPatchPath.RunID)
                    {
                        string strPatchPath = safxObjectPatchPath.FieldValue;
                        myRunIDData.PatchFile = System.IO.Path.GetFileName(strPatchPath);
                        break;
                    }
                }

                myRunIDData.KBNumber = safxObject.FieldValue;
                listRunIDData.Add(myRunIDData);
            }

            return listRunIDData;
        }

        public static IEnumerable<TO> GetOSList(int osTypeId, int maddogDBId)
        {
            using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
            {
                return dataContext.TOs.Where(o => o.OSTypeID == osTypeId && o.Active == true && o.MaddogDBID == maddogDBId).ToList<TO>();
            }
        }

        public class RunIDData
        {
            public int RunID { get; set; }
            public System.Nullable<int> Arch { get; set; }
            public string PatchFile { get; set; }
            public string KBNumber;
        }

        public class JobIDReportData
        {
            public long JobID { get; set; }
            public List<RunIDData> RunIDList { get; set; }
            public string TFSWorkItem { get; set; }
            public DateTime CreationTime { get; set; }
            public string Creator { get; set; }

        }
    }
}
