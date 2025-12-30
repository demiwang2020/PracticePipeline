using System;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

using ScorpionDAL;

namespace Connect2TFS
{
    public class WorkItemBO
    {
        public int ServicingID { get; protected set; }
        public string KBNumber { get; protected set; }

        public string Title { get; protected set; }
        public string State { get; protected set; }
        public int Priority { get; protected set; }
        public string Product { get; protected set; }
        public string SKU { get; protected set; }
        public string ProductSPLevel { get; protected set; }
        public string AssignedTo { get; protected set; }

        //Jinfeng added 2011/6/20
        public string ProductLanguage { get; protected set; }
        public string PatchTechnology { get; protected set; }
        public string ProductUnit { get; protected set; }
        public string SecurityUpdateCaseID { get; protected set; }
        public string BulletinNumber { get; protected set; }
        public string Company { get; protected set; }
        public string OSInstalled { get; protected set; }
        public string OSSPLevel { get; protected set; }
        public string OSArchitecture { get; protected set; }

        public string BuildNumber { get; protected set; }

        public string PatchNameX86 { get; protected set; }
        public string PatchLocationX86 { get; protected set; }
        public string MSPNameX86 { get; protected set; }
        public string MSPLocationX86 { get; protected set; }
        public string SmokeLocationX86 { get; protected set; }

        public string PatchNameX64 { get; protected set; }
        public string PatchLocationX64 { get; protected set; }
        public string MSPNameX64 { get; protected set; }
        public string MSPLocationX64 { get; protected set; }
        public string SmokeLocationX64 { get; protected set; }

        public string PatchNameIA64 { get; protected set; }
        public string PatchLocationIA64 { get; protected set; }
        public string MSPNameIA64 { get; protected set; }
        public string MSPLocationIA64 { get; protected set; }
        public string SmokeLocationIA64 { get; protected set; }

        public string PatchNameARM { get; protected set; }
        public string PatchLocationARM { get; protected set; }
        public string SmokeLocationARM { get; protected set; }

        //Jinfeng added 2011/12/13
        public string ReleaseType { get; protected set; }
        public string AreaPath { get; protected set; }
        public string OSLanguage { get; protected set; }
        public string BuildNumberRedbits { get; protected set; }

        //Jinfeng added 2011/12/15
        public string SymbolVerifiedDate { get; protected set; }
        public string ClosedDate { get; protected set; }

        public DateTime SmokeStartDate { get; protected set; }
        public DateTime SmokeEndDate { get; protected set; }
        public string Notes { get; protected set; }
        public bool PatchWasReset { get; protected set; }

        public string Binaries { get; protected set; }

        public string ComponentVersion { get; private set; }
        public string LCUKBArticle { get; private set; }
        public string BaseBuildNumber { get; private set; }

        public string WindowsPackagingId { get; private set; }

        public WorkItemBO(WorkItem objWorkItem)
        {
            if(objWorkItem == null)
                throw new Exception("WorkItem object is null");

            ServicingID = Convert.ToInt32(objWorkItem["Id"]);
            KBNumber = objWorkItem["KB Article"].ToString();
            Title = objWorkItem["Title"].ToString();
            State = objWorkItem["State"].ToString();
            Priority = Convert.ToInt32(objWorkItem["Priority"].ToString());
            Product = objWorkItem["Product"].ToString();
            SKU = objWorkItem["SKU"].ToString();
            ProductSPLevel = objWorkItem["Target"].ToString();
            AssignedTo = objWorkItem["Assigned To"].ToString();

            //Jinfeng added 2011/6/20
            ProductLanguage = objWorkItem["Prod Lang"].ToString();
            PatchTechnology = objWorkItem["Deliverable"].ToString();
            ProductUnit = objWorkItem["DevDiv Group"].ToString();
            SecurityUpdateCaseID = objWorkItem["MSRC ID"].ToString();
            BulletinNumber = objWorkItem["Bulletin Number"].ToString();
            Company = objWorkItem["Company Name"].ToString();
            OSInstalled = objWorkItem["Environment"].ToString();
            OSSPLevel = objWorkItem["Target Architecture"].ToString();
            OSArchitecture = objWorkItem["Processor"].ToString();
            
            BuildNumber = objWorkItem["Build Number"].ToString();

            PatchNameX86 = objWorkItem["Patch Name"].ToString();
            PatchLocationX86 = objWorkItem["KB Published Location"].ToString();
            MSPNameX86 = objWorkItem["MSP Name"].ToString();
            MSPLocationX86 = objWorkItem["MSP Location"].ToString();
            SmokeLocationX86 = objWorkItem["Smoke Location"].ToString();

            PatchNameX64 = objWorkItem["Patch Name X64"].ToString();
            //Jinfeng changed 2011/6/20
            PatchLocationX64 = objWorkItem["Patch Location X64"].ToString();
            MSPNameX64 = objWorkItem["MSP Name X64"].ToString();
            MSPLocationX64 = objWorkItem["MSP Location X64"].ToString();
            SmokeLocationX64 = objWorkItem["Smoke Location X64"].ToString();

            PatchNameIA64 = objWorkItem["Patch Name IA64"].ToString();
            //Jinfeng changed 2011/6/20
            PatchLocationIA64 = objWorkItem["Patch Location IA64"].ToString();
            MSPNameIA64 = objWorkItem["MSP Name IA64"].ToString();
            MSPLocationIA64 = objWorkItem["MSP Location IA64"].ToString();
            SmokeLocationIA64 = objWorkItem["Smoke Location IA64"].ToString();

            //Tongtong add 2014/07/03
            PatchNameARM = objWorkItem["Patch Name ARM"].ToString();
            PatchLocationARM = objWorkItem["Patch Location ARM"].ToString();         
            SmokeLocationARM = objWorkItem["Smoke Location ARM"].ToString();

            //Jinfeng changed 2011/12/13
            ReleaseType = objWorkItem["Release Type"].ToString();
            AreaPath = objWorkItem["Area Path"].ToString();
            OSLanguage = objWorkItem["OSLang"].ToString();
            BuildNumberRedbits = objWorkItem["Build Number Redbits"].ToString();

            bool smokeDateRead = false;
            for (int i = 0; i < objWorkItem.Revisions.Count; i++)
            {
                Revision rvs = objWorkItem.Revisions[objWorkItem.Revisions.Count - 1 - i];
                if (SymbolVerifiedDate == null && ((rvs.Fields["State"].OriginalValue == null || rvs.Fields["State"].OriginalValue.ToString() != "Symbol Verified") && rvs.Fields["State"].Value.ToString() == "Symbol Verified"))
                {
                    SymbolVerifiedDate = rvs.Fields["Changed Date"].Value.ToString().Trim('{', '}');
                }

                if (!smokeDateRead && ((rvs.Fields["State"].OriginalValue == null || rvs.Fields["State"].OriginalValue.ToString() != "Smoke Test") && rvs.Fields["State"].Value.ToString() == "Smoke Test"))
                {
                    SmokeStartDate = Convert.ToDateTime(rvs.Fields["Changed Date"].Value);
                    smokeDateRead = true;
                }

                if (SymbolVerifiedDate != null && smokeDateRead)
                    break;
            }

            if (SymbolVerifiedDate == null || SymbolVerifiedDate == string.Empty)
            {
                for (int i = 0; i < objWorkItem.Revisions.Count; i++)
                {
                    Revision rvs = objWorkItem.Revisions[objWorkItem.Revisions.Count - 1 - i];
                    if ((rvs.Fields["State"].OriginalValue == null || rvs.Fields["State"].OriginalValue.ToString() != "Closed") && rvs.Fields["State"].Value.ToString() == "Closed")
                    {
                        ClosedDate = rvs.Fields["Changed Date"].Value.ToString().Trim('{', '}');
                        break;
                    }
                }
            }

            Notes = objWorkItem["Notes"].ToString();

            Binaries = objWorkItem["Binary Release"].ToString();

            LCUKBArticle = objWorkItem["LCU KB Article"].ToString();
            ComponentVersion = objWorkItem["Windows Component Version"].ToString();
            BaseBuildNumber = objWorkItem["Base Build Number"].ToString();
            WindowsPackagingId = objWorkItem["Windows Packaging ID"].ToString();
        }

        //This is used for private patch
        public WorkItemBO(
            int priServcingID, 
            string priPatchNameX86 = "NA", 
            string priPatchLocationX86 = "NA",
            string priPatchNameX64 = "NA",
            string priPatchLocationX64 = "NA",
            string priPatchNameIA64 = "NA",
            string priPatchLocationIA64 = "NA",
            string priSKU = "4.0",
            string priProductSPLevel = "Unknown",
            string priProductLanguage = "[All Languages]")
        {
            ServicingID = priServcingID;
            PatchNameX86 = priPatchNameX86;
            PatchLocationX86 = priPatchLocationX86;
            PatchNameX64 = priPatchNameX64;
            PatchLocationX64 = priPatchLocationX64;
            PatchNameIA64 = priPatchNameIA64;
            PatchLocationIA64 = priPatchLocationIA64;

            //These proporties are requiered by current implementing of existed callings. Needed by method InsertTable_PIDSetupFile(...)
            SKU = priSKU;
            ProductSPLevel = priProductSPLevel;
            ProductLanguage = priProductLanguage;
        }

        //used for smoke test dashboard
        public WorkItemBO(int servcingID, string kbNumber, WorkItem objWorkItem)
        { 
            ServicingID=servcingID;
            KBNumber = kbNumber;
            OSInstalled = objWorkItem["Environment"].ToString();
            OSSPLevel = objWorkItem["Target Architecture"].ToString();
            SKU = objWorkItem["SKU"].ToString();
            Title = objWorkItem["Title"].ToString();

            bool smokeDateRead = false;

            for (int i = 0; i < objWorkItem.Revisions.Count; i++)
            {
                Revision rvs = objWorkItem.Revisions[objWorkItem.Revisions.Count - 1 - i];

                if (!smokeDateRead && ((rvs.Fields["State"].OriginalValue == null || rvs.Fields["State"].OriginalValue.ToString() != "Smoke Test") && rvs.Fields["State"].Value.ToString() == "Smoke Test"))
                {
                    SmokeStartDate = Convert.ToDateTime(rvs.Fields["Changed Date"].Value);
                    smokeDateRead = true;
                }

                if (smokeDateRead)
                    break;
            }
        }
        public WorkItemBO() { }

        public void SetTFSDataForSmokeTestDashboard(WorkItem objWorkItem)
        {
            if (objWorkItem == null)
                throw new Exception("WorkItem object is null");

            ServicingID = Convert.ToInt32(objWorkItem["Id"]);
            KBNumber = objWorkItem["KB Article"].ToString();
            Title = objWorkItem["Title"].ToString();
            PatchTechnology = objWorkItem["Deliverable"].ToString();
            ReleaseType = objWorkItem["Release Type"].ToString();
            Notes = objWorkItem["Notes"].ToString();
            BuildNumber = objWorkItem["Build Number"].ToString();

            PatchNameX86 = objWorkItem["Patch Name"].ToString();
            PatchLocationX86 = objWorkItem["KB Published Location"].ToString();

            PatchNameX64 = objWorkItem["Patch Name X64"].ToString();
            PatchLocationX64 = objWorkItem["Patch Location X64"].ToString();

            PatchNameIA64 = objWorkItem["Patch Name IA64"].ToString();
            PatchLocationIA64 = objWorkItem["Patch Location IA64"].ToString();

            PatchNameARM = objWorkItem["Patch Name ARM"].ToString();
            PatchLocationARM = objWorkItem["Patch Location ARM"].ToString();
            SmokeLocationARM = objWorkItem["Smoke Location ARM"].ToString();

            WindowsPackagingId = objWorkItem["Windows Packaging ID"].ToString();
            bool smokeDateRead = false;
            for (int i = 0; i < objWorkItem.Revisions.Count; i++)
            {
                Revision rvs = objWorkItem.Revisions[objWorkItem.Revisions.Count - 1 - i];

                if (!smokeDateRead && ((rvs.Fields["State"].OriginalValue == null || rvs.Fields["State"].OriginalValue.ToString() != "Smoke Test") && rvs.Fields["State"].Value.ToString() == "Smoke Test"))
                {
                    SmokeStartDate = Convert.ToDateTime(rvs.Fields["Changed Date"].Value);
                    smokeDateRead = true;
                }

                if (smokeDateRead)
                    break;
            }
        }

        public void GetWorkItemTimeStats(WorkItem objWorkItem)
        {
            if (objWorkItem == null)
                throw new Exception("WorkItem object is null");

            PatchWasReset = false;
            Title = objWorkItem["Title"].ToString();
            bool smokestartdateset = false;
            bool smokeenddateset = false;
            string previousstate = string.Empty;
            string previousdate = string.Empty;
            string currentstate = string.Empty;
            for (int i = 0; i < objWorkItem.Revisions.Count; i++)
            {
                Revision rvs = objWorkItem.Revisions[objWorkItem.Revisions.Count - 1 - i];
                currentstate = rvs.Fields["State"].Value.ToString();
                //These states come listed from latest entry to first entry, so we walk the list from last to first looking for a combo of current and past states to figure out the smoke test times
                if(previousstate == "Functional QA Activities" && currentstate == "Smoke Test")
                {
                    //only set it if it hasn't been set yet.  If a value is already in there, that indicates a reset of the patch
                    if (!smokeenddateset)
                    {
                        SmokeEndDate = Convert.ToDateTime(previousdate);
                        smokeenddateset = true;
                    }
                }
                else if(previousstate == "Smoke Test" && currentstate == "Patch Ready")
                {
                    //only set it if it hasn't been set yet, If a value is already there, that indicates a reset of the patch
                    if (!smokestartdateset)
                    {
                        SmokeStartDate = Convert.ToDateTime(rvs.Fields["Changed Date"].Value);
                        smokestartdateset = true;
                    }
                    else
                    {
                        PatchWasReset = true;
                    }
                }
                previousstate = currentstate;
                previousdate = rvs.Fields["Changed Date"].Value.ToString();
            }

            if (!smokeenddateset || !smokestartdateset)
                throw new Exception(string.Format("Could not set start and end dates for smoke test for patch '{0}'", Title));
        }

        public bool Equals(TTestProdInfo objPatch)
        {
            try
            {
                if (objPatch.TestIdentifier.Equals(KBNumber))
                    if (objPatch.WorkItem.Equals(ServicingID.ToString()))
                        return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        public TTestProdInfo GetTPatch(string strVerificationScript, string strDDSFilePath, string strSKUListPath)
        {
            TTestProdInfo objPatch = new TTestProdInfo();
            //objPatch.KBNumber = KBNumber;
            //objPatch.BuildNumber = BuildNumber;
            //objPatch.VerificationScript = strVerificationScript;
            //objPatch.DDSFilePath = strDDSFilePath;
            //objPatch.SKUFilePath = strSKUListPath;

            //switch (Product)
            //{
            //    case "Dot Net Framework":

            //}

            //PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            //Brand = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "Brand");
            //TProduct objProduct = db.TProducts.SingleOrDefault(product => product.DecaturProduct == Brand);
            //PatchInfo.ProductID = objProduct.ProductID;

            //string strProductName = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "ProductName");
            //PatchInfo.TargetProductName = strProductName;

            //string strBaseLineID = XMLHelper.GetAttributeValue(xmlDocDDSFile, "Parameter", "BaselineId");
            //PatchInfo.BaselineID = strBaseLineID;

            ////string strSPLevel = "SP0";
            ////if (strProductName.Contains("SP1"))
            ////    strSPLevel = "SP1";
            ////if (strProductName.Contains("SP2"))
            ////    strSPLevel = "SP2";
            ////if (strProductName.Contains("SP3"))
            ////    strSPLevel = "SP3";
            ////if (strProductName.Contains("SP4"))
            ////    strSPLevel = "SP4";

            ////SPLevel will be updated later. Look for GetTargetProductSPLevel()
            ////PatchInfo.ProdSPLevel = "SP0"; //"SP2"; // strBaseLineID; // strSPLevel;
            //PatchInfo.ExecutionSystemID = db.TExecutionSystems.Single(exs => exs.ApplicationType == this.appApplicationType.ToString()).ExecutionSystemID;
            ////PatchInfo.TExecutionSystem = (TExecutionSystem) db.TExecutionSystems.Cast<TExecutionSystem>();

            //PatchInfo.CreatedBy = Owner;
            //PatchInfo.CreatedDate = DateTime.Now;
            //PatchInfo.LastModifiedBy = Owner;
            //PatchInfo.LastModifiedDate = PatchInfo.CreatedDate;

            //PatchInfo.PU = PU;
            //PatchInfo.ApplicationType = appApplicationType.ToString();
            //PatchInfo.WorkItem = WorkItem;
            return objPatch;
        }
    }
}
