using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ScorpionDAL;
using System.Data;
//For MadDog APIs
//
//
// New MadDog APIs (Orcas and above)
//
using MDO = MadDogObjects;
using MDOF = MadDogObjects.Forms;

using LoggerLibrary;

using System.Collections;

namespace HotFixLibrary
{
    public class Maintenance
    {
        public static void ConnectToMadDog(string strUser)
        {
            try
            {
                MDO.Utilities.Security.AppName = "Setup";
                MDO.Utilities.Security.AppOwner = strUser;
                MDO.Utilities.Security.SetDB("MDSQL3", "OrcasTS");
                //MDO.Branch.CurrentBranch = new MDO.Branch(536);

                //MDL.Utilities.Security.AppName = appApplicatonType.ToString();
                //MDL.Utilities.Security.AppOwner = strOwner;
                //MDL.Utilities.Security.SetDB(strExecutionSystemDatabaseName, strExecutionSystemName);
            }
            catch (Exception ex)
            {
                Logger.Instance.AddLogMessage(ex.Message, LogHelper.LogLevel.ERROR, null, ex);
                throw (ex);
            }
        }

        public static DataTable GetAllAvailableImages(string strUser, int intLabID, bool blnLoad)
        {
            ConnectToMadDog(strUser);
            
            //MDO.Testcase objMDLTestCase;
            //objMDLTestCase = new MDL.Testcase(intTestCaseID);
            //strTestCaseName = ((MaddogObjects.Legacy.NamedObject)(objMDLTestCase)).Name;
            //strTestCaseDescription = objMDLTestCase.Description;
            //return objMDLTestCase.Contexts.GetDataSet();

            DataTable dtLabIDs = new DataTable();
            dtLabIDs.Columns.Add("LabID", typeof(int));

            dtLabIDs.Rows.Add(intLabID);

            //MDO.OSImage mdOSImage = new MDO.OSImage("Windows 7 SP1 build", 2303);
            MDO.OSImage mdOSImage = new MDO.OSImage(13206);
            //MDO.Run run1 = new MDO.Run();

            MDO.QueryObject qo =  new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.OS);
            qo.QueryAdd(MDO.Tables.OSTable.ACTIVEFIELD, "=", 1);
            qo.DisplayFields = new string[] {MDO.Tables.OSTable.IDFIELD};
            
            DataTable dtOSIDs = qo.GetDataSet().Tables[0];

            //Get a list of all our images
            DataTable dt = MDO.OS.GetAllAvailableImages(dtLabIDs, dtOSIDs);

            List<int> lstInt = new List<int>();
            foreach(DataRow drwRow in dt.Rows)
            {
                if(!(lstInt.Contains(Convert.ToInt32(drwRow["OSID"]))))
                    lstInt.Add(Convert.ToInt32(drwRow["OSID"]));
            }

                //string strOsImageTypeIDs = arrObj.CopyTo(

            if (blnLoad)
            {

                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                List<TMaddogOSImage> lstMaddogOSImage = new List<TMaddogOSImage>();
                TMaddogOSImage objMaddogOSImage = new TMaddogOSImage();

                foreach (DataRow drwRow in dt.Rows)
                {
                    MDO.OSImage mdOSImageCheck = new MDO.OSImage(Convert.ToInt32(drwRow["OSImageID"]));
                    MDO.QueryObject q = mdOSImageCheck.OSImageValues;
                    object[] arrObj = q.GetAllValuesForColumn("OSImageTypeID", true);

                    string strOsImageTypeIDs = string.Empty;
                    foreach (object obj in arrObj)
                        strOsImageTypeIDs += obj.ToString() + ";";

                    objMaddogOSImage = new TMaddogOSImage();

                    objMaddogOSImage.OSImageID = Convert.ToInt32(drwRow["OSImageID"]);
                    objMaddogOSImage.OSImageName = drwRow["OSImageName"].ToString();
                    objMaddogOSImage.IsAvailable = Convert.ToBoolean(drwRow["IsAvailable"]);
                    objMaddogOSImage.LabName = drwRow["LabName"].ToString();
                    objMaddogOSImage.OSID = Convert.ToInt32(drwRow["OSID"]);
                    objMaddogOSImage.OSName = drwRow["OSName"].ToString();
                    objMaddogOSImage.OSLanguageLocale = drwRow["OSLanguageLocale"].ToString();
                    objMaddogOSImage.PlatformSpecific = drwRow["PlatformSpecific"].ToString();
                    objMaddogOSImage.OsImageTypeIDs = strOsImageTypeIDs;

                    PopulateTOSSpecificDetails(objMaddogOSImage);

                    lstMaddogOSImage.Add(objMaddogOSImage);

                    //        //db.TOSStagings.InsertOnSubmit(objOSStaging);
                }

                db.TMaddogOSImages.InsertAllOnSubmit(lstMaddogOSImage);
                db.SubmitChanges();

            }

            return dt;

            /*
                Insert into TOSImage
                select TM.OSID, TM.OSSPLevel, TM.OSImage, 'Redmond\ashk', '6/22/2010', null, null, 1, TM.OSImageID, TM.OSImageName, TM.LabName, 2
                from TMaddogOSImage TM where TM.OSID  IN (select distinct MDOSID from TOS) 
                and TM.Active = 1 and  TM.OSSPLevel is not null
             */
        }

        private static void PopulateTOSSpecificDetails(TMaddogOSImage objMaddogOSImage)
        {
            objMaddogOSImage.OSSPLevel = null;
            objMaddogOSImage.Active = true;

            ///////////////////////////////////////////////////////////////////////////////////////////////

            if (objMaddogOSImage.OSImageName.Contains("RTM"))
                objMaddogOSImage.OSSPLevel = "RTM";
            if (objMaddogOSImage.OSImageName.Contains("SP1"))
                objMaddogOSImage.OSSPLevel = "SP1";
            if (objMaddogOSImage.OSImageName.Contains("SP2"))
                objMaddogOSImage.OSSPLevel = "SP2";
            if (objMaddogOSImage.OSImageName.Contains("SP3"))
                objMaddogOSImage.OSSPLevel = "SP3";
            if (objMaddogOSImage.OSImageName.Contains("SP4"))
                objMaddogOSImage.OSSPLevel = "SP4";

            if (objMaddogOSImage.OSImageName.Contains("Windows XP Media Center 2003"))
                objMaddogOSImage.OSSPLevel = "2003";
            if (objMaddogOSImage.OSImageName.Contains("Windows XP Media Center 2004"))
                objMaddogOSImage.OSSPLevel = "2004";
            if (objMaddogOSImage.OSImageName.Contains("Windows XP Media Center 2005"))
                objMaddogOSImage.OSSPLevel = "2005";

            if (objMaddogOSImage.OSImageName.Contains("RTM/SP1"))
                objMaddogOSImage.OSSPLevel = "RTM/SP1";
            
            if (objMaddogOSImage.OSImageName.Contains("escrow"))
                objMaddogOSImage.OSSPLevel = null;

            ///////////////////////////////////////////////////////////////////////////////////////////////

            if(objMaddogOSImage.OSSPLevel == null)
                objMaddogOSImage.Active = false;

            if(objMaddogOSImage.OSImageName.Contains("May cause problems if not used correctly"))
                objMaddogOSImage.Active = false;

            ///////////////////////////////////////////////////////////////////////////////////////////////

            objMaddogOSImage.OSImage = null;
            if(objMaddogOSImage.Active == true)
                objMaddogOSImage.OSImage = objMaddogOSImage.OSID.ToString() + "-" + objMaddogOSImage.OSSPLevel;

            ///////////////////////////////////////////////////////////////////////////////////////////////
            

            objMaddogOSImage.OSVersion = null;
            objMaddogOSImage.OSDetails = objMaddogOSImage.OSName + " " + objMaddogOSImage.OSLanguageLocale + " " + objMaddogOSImage.PlatformSpecific;
            objMaddogOSImage.OSCPUID = null;
            objMaddogOSImage.OSLanguageID = null;
            objMaddogOSImage.OSTypeID = null;

            ///////////////////////////////////////////////////////////////////////////////////////////////

            return;
        }


        public static void CreateRun(string strUser)
        {
            ConnectToMadDog(strUser);
            MDO.Run run1 = new MDO.Run();
            run1.Title = "asd";
            MDO.Run runTemplate = new MDO.Run(928983);
            run1 = runTemplate.Clone();
            run1.Title = "Automatic Run";

            run1.OS = new MDO.OS(371);
            run1.OSImage = new MDO.OSImage(503);
            run1.Save();
        }

        public static int CreateContext(string strUser, int intContextStartNumber, int intContextEndNumber)
        {
            ConnectToMadDog(strUser);

            /*
            //MDO.Testcase objMDLTestCase;
            //objMDLTestCase = new MDL.Testcase(intTestCaseID);
            //strTestCaseName = ((MaddogObjects.Legacy.NamedObject)(objMDLTestCase)).Name;
            //strTestCaseDescription = objMDLTestCase.Description;
            //return objMDLTestCase.Contexts.GetDataSet();
            
            //new MDO.Context(@"E:\dd\DTG_1\src\QA\md\wd\DDSE\Servicing\SampleContext\DDCPX0018.ctx.tcproj");
            MDO.Context conContext = new MDO.Context(510755);
            MDO.Context newContext = conContext.Clone();

            newContext.Save();
            ////conContext.Branch = new MDO.Branch(536);
            ////conContext.DepotPath = "$(BRANCH_PATH)\QA\md\wd\DDSE\Servicing\SampleContext\DDCPX0019.ctx.tcproj";
            //conContext.Name = "DDCPX: 0019";
            //conContext.ContextType = new MDO.ContextType(1);
            //    //E:\dd\DTG_1\src\QA\md\wd\DDSE\Servicing\SampleContext\DDCPX0018.ctx.tcproj
            //conContext.Save();
            */
            MDO.Branch mdbranch = new MDO.Branch(536);
            //MDO.Branch.CurrentBranch = mdbranch; 
            for (int intContext = intContextStartNumber; intContext < intContextEndNumber; intContext++)
            {
                MDO.Branch.CurrentBranch.LocalClientData = "ashutosh-win7_ashk_DTG_1";
                MDO.Context orcasContext = new MDO.Context();
                string strContextName = "DDCPX: ST" + String.Format("{0:0000}", intContext);
                orcasContext.Name = strContextName;
                orcasContext.ContextType = new MDO.ContextType(1);
                //orcasContext.Requirements = qo;
                //orcasContext.Tiers = orcasTiers;
                //orcasContext.Attributes = qo;


                // set the depot path for the Context, using the whidbey name field. 
                // Need to do this last due to the semi-recursive nature of this fcn. 
                string filteredName = MDO.SerializationUtilities.GetFilteredObjectName(strContextName);
                //MDO.SerializationUtilities.BranchLocation
                //MDO.Utilities.SourceProvider.CurrentSourceProvider. 

                string orcasPath = MDO.SerializationUtilities.LOCAL_ROOT /*@"Dev10\pu\DTG"*/ /*MDO.SerializationUtilities.BranchLocation */ + @"\qa\md\" + @"wd\DDSE\Servicing\Context\Setup\GDR" + @"\" + filteredName;
                //orcasPath = CheckPath(orcasPath); // make sure we're not overwriting something
                orcasContext.SetDepotPath(orcasPath);
                
                // use Persist instead of Save, so we can use pending changes. 
                orcasContext.Persist();
            }
            return 1;
            


            //return;
        }

    }
}
