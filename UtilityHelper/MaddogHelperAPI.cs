using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DefinitionInterpreter;
using System.Data;
// New MadDog APIs (Orcas and above)
//
using MDO = MadDogObjects;
using MDOF = MadDogObjects.Forms;
using MadDogObjects.RunManagement;

namespace Helper
{
    /// <summary>
    /// MaddogHelperAPI holds every possible API that need to deal with maddog and its data bases. As part of this design, we choose to use singleton pattern to avoid multiple instances of this calss 
    /// and also to ensure that one user at any point of time will be dealing with one database/branch of the maddog to avoid any conflicts.
    /// </summary>
    public class MaddogHelperAPI
    {
        private static MaddogHelperAPI MDObjectInstance = null;
        private static object syncRoot = new Object();
        private string Owner;
        private string MDServerName;
        private string MDDBName;
        private string MDBranchName;
        private string AppName;

        /// <summary>
        /// private constructor for singleton.
        /// </summary>
        private MaddogHelperAPI(string owner = @"GlobalConstants.MDUSER")
        {
            if (owner == "GlobalConstants.MDUSER")
                owner = GlobalConstants.MDUSER;

            MDServerName = MDConstants.MDSERVERNAMEORCAS;
            MDDBName = MDConstants.MDDBNAME.OrcasTS.ToString();
            MDBranchName = MDConstants.PUCLRBRANCH;
            AppName = GlobalConstants.APPNAME.ToString();
            Owner = owner;
            ConnectMDObject(owner);
        }
        /// <summary>
        /// Private constructor with initialization. I have not seen a use of it so far but just for the sake of completenes.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="MDservername"></param>
        /// <param name="MDdbname"></param>
        /// <param name="MDbranchname"></param>
        /// <param name="Appname"></param>
        private MaddogHelperAPI(string owner, string MDservername, string MDdbname, string MDbranchname, string Appname)
        {
            Owner = owner;
            MDServerName = MDservername;
            MDDBName = MDdbname;
            MDBranchName = MDbranchname;
            AppName = Appname;
            ConnectMDObject();
        }
        /// <summary>
        /// function to initialize MD object.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="MDservername"></param>
        /// <param name="MDdbnameame"></param>
        /// <param name="MDbranchname"></param>
        /// <param name="Appname"></param>
        /// <returns></returns>
        public bool ConnectMDObject(string MDbranchname, string MDdbnameame, string MDservername = @"MDSQL3", string owner = @"GlobalConstants.MDUSER", string Appname = @"Scorpion")
        {
            if (owner == "GlobalConstants.MDUSER")
                owner = GlobalConstants.MDUSER;

            if (AppName != null && Owner != null && MDServerName != null && MDDBName != null)
            {
                MDO.Utilities.Security.AppName = AppName.ToString();
                MDO.Utilities.Security.AppOwner = Owner.ToString();
                MDO.Utilities.Security.SetDB(MDServerName, MDDBName);
                MDO.Branch.CurrentBranch = new MDO.Branch(MDBranchName);
                MDO.Branch.CurrentBranch.SetNoLocalEnlistment();
                return true;
            }
            return false;
        }
        /// <summary>
        /// COnnecting with default values
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="MDservername"></param>
        /// <param name="MDdbnameame"></param>
        /// <param name="MDbranchname"></param>
        /// <param name="Appname"></param>
        /// <returns></returns>
        public bool ConnectMDObject(string owner)
        {
            if (Owner != null)
            {
                MDO.Utilities.Security.AppName = AppName.ToString();
                MDO.Utilities.Security.AppOwner = Owner.ToString();
                MDO.Utilities.Security.SetDB(MDServerName, MDDBName);
                MDO.Branch.CurrentBranch = new MDO.Branch(MDBranchName);
                MDO.Branch.CurrentBranch.SetNoLocalEnlistment();
                return true;
            }
            return false;
        }
        /// <summary>
        /// function to initialize MD object from member variables if they were initialized as part of constructor (if at all)
        /// </summary>
        /// <returns></returns>
        public bool ConnectMDObject()
        {
            if (AppName != null && Owner != null && MDServerName != null && MDDBName != null)
            {
                MDO.Utilities.Security.AppName = AppName.ToString();
                MDO.Utilities.Security.AppOwner = Owner.ToString();
                MDO.Utilities.Security.SetDB(MDServerName, MDDBName);
                MDO.Branch.CurrentBranch = new MDO.Branch(MDBranchName);
                MDO.Branch.CurrentBranch.SetNoLocalEnlistment();
                return true;
            }
            return false;

        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="MDservername"></param>
        /// <param name="MDdbnameame"></param>
        /// <param name="MDbranchname"></param>
        /// <param name="Appname"></param>
        /// <returns></returns>
        public static MaddogHelperAPI Instance(string MDbranchname, string MDdbnameame, string MDservername = @"MDSQL3.corp.microsoft.com", string owner = @"GlobalConstants.MDUSER", string Appname = @"Scorpion")
        {

            if (owner == "GlobalConstants.MDUSER")
                owner = GlobalConstants.MDUSER;


            // If the instance is null then create one and init the Queue
            if (MDObjectInstance == null)
            {
                lock (syncRoot)
                {
                    if (MDObjectInstance == null)
                    {
                        MDObjectInstance = new MaddogHelperAPI(owner, MDservername, MDdbnameame, MDbranchname, Appname);

                    }
                }
            }
            return MDObjectInstance;

        }
        /// <summary>
        /// Singleton instance. defaulting with OrcasDB/Puclr  branch.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="MDservername"></param>
        /// <param name="MDdbnameame"></param>
        /// <param name="MDbranchname"></param>
        /// <param name="Appname"></param>
        /// <returns></returns>
        public static MaddogHelperAPI Instance(string owner = @"GlobalConstants.MDUSER")
        {
            {
                // If the instance is null then create one and init the Queue
                if (MDObjectInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (MDObjectInstance == null)
                        {
                            MDObjectInstance = new MaddogHelperAPI(owner);
                        }
                    }
                }
                return MDObjectInstance;
            }
        }
        /// <summary>
        /// Disposing MD object that was initialized
        /// </summary>
        public void Dispose()
        {
            if (MDObjectInstance != null)
            {
                MDObjectInstance = null;
            }
        }
        /// <summary>
        /// Gives run status at the moment.
        /// </summary>
        /// <param name="RunID"></param>
        /// <param name="RunStatus"></param>
        /// <returns></returns>
        public bool GetRunStatus(int RunID, out string RunStatus)
        {
            ConnectMDObject();
            MDO.Run objMDRun = new MDO.Run(RunID);
            try
            {
                RunStatus = objMDRun.RunState.ToString();
                return true;
            }
            catch (Exception)
            {
                RunStatus = "";
                return false;
            }
        }

        public MDO.Run GetRunByID(int RunID)
        {
            ConnectMDObject();
            MDO.Run objMDRun = new MDO.Run(RunID);
            return objMDRun;
        }

        /// <summary>
        /// Returns a hash table that contains test casee name and IDs of a test case query
        /// </summary>
        /// <param name="TestCaseQueryID"></param>
        /// <returns></returns>
        public bool GetTestCasesIDs(int TestCaseQueryID, out Hashtable TestCaseList)
        {
            ConnectMDObject();
            TestCaseList = new Hashtable();
            try
            {
                MDO.QueryObject TCQuery = new MDO.QueryObject(TestCaseQueryID);

                //First coloumn is always test case IDs. if this assumption chnages, it might fail. I can not list coloumn name as it might introduce unnecessary string constants in the code and might expose future chnage errors.

                object[] objID = TCQuery.GetAllValuesForColumn(TCQuery.DisplayFields[0], true);


                for (int i = 0; i < objID.Length; i++)
                {
                    MDO.Testcase tc = new MDO.Testcase(int.Parse(objID[i].ToString()));

                    TestCaseList.Add(int.Parse(objID[i].ToString()), tc.Name);
                    tc = null;
                }

            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }
        /// <summary>
        /// Returns a hash table that contains context name and IDs of a context query
        /// </summary>
        /// <param name="ContextQueryID"></param>
        /// <returns></returns>
        public bool GetContextIDs(int ContextQueryID, out Hashtable ContextList)
        {
            ConnectMDObject();
            ContextList = new Hashtable();
            try
            {
                MDO.QueryObject ContextQuery = new MDO.QueryObject(ContextQueryID);

                //First coloumn is always test case IDs. if this assumption chnages, it might fail. I can not list coloumn name as it might introduce unnecessary string constants in the code and might expose future chnage errors.

                object[] objID = ContextQuery.GetAllValuesForColumn(ContextQuery.DisplayFields[0], true);


                for (int i = 0; i < objID.Length; i++)
                {
                    string ContextName;
                    if (GetContextName(int.Parse(objID[i].ToString()), out ContextName))
                    {
                        ContextList.Add(int.Parse(objID[i].ToString()), ContextName);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }
        /// <summary>
        /// Returns hash table with context name and ID that were included in test case contexts section
        /// </summary>
        /// <param name="TestCaseSpecific"></param>
        /// <returns></returns>
        public bool GetTestCaseContextIDs(MDO.Testcase TestCaseSpecific, out Hashtable ContextIDList)
        {
            ContextIDList = new Hashtable();
            try
            {
                object[] objID = TestCaseSpecific.Contexts.GetAllValuesForColumn(TestCaseSpecific.Contexts.Clauses[0].Field, true);

                for (int i = 0; i < objID.Length; i++)
                {
                    string ContextName;
                    if (GetContextName(int.Parse(objID[i].ToString()), out ContextName))
                    {
                        ContextIDList.Add(int.Parse(objID[i].ToString()), ContextName);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Return a run object of given ID. Default is creating a new emty run.
        /// </summary>
        /// <param name="RunID"></param>
        /// <returns></returns>
        public MDO.Run GetRunObject(int RunID = -1)
        {
            ConnectMDObject();
            MDO.Run runObject;
            if (RunID != -1)
                runObject = new MDO.Run(RunID);
            else
                runObject = new MDO.Run();
            return runObject;
        }
        /// <summary>
        /// Return a OS object of given ID. Default is creating a new emty OS object.
        /// </summary>
        /// <param name="OSID"></param>
        /// <returns></returns>
        public MDO.OS GetOSObject(int OSID = -1)
        {
            ConnectMDObject();
            try
            {

                MDO.OS OSObject;
                if (OSID != -1)
                    OSObject = new MDO.OS(OSID);
                else
                    OSObject = new MDO.OS();
                return OSObject;
            }
            catch (Exception)
            {
                return null;
            }



        }
        /// <summary>
        /// Sets an OS object on a given run ID. OS object will be defaulted for RTM SP level
        /// </summary>
        /// <param name="runID"></param>
        /// <param name="OSObj"></param>
        /// <returns></returns>
        public bool SetOSObjectOnRun(int runID, MDO.OS OSObj)
        {
            ConnectMDObject();
            int ID;
            if (runID == -1 || OSObj == null)
                return false;
            try
            {

                MDO.Run run = new MDO.Run(runID);
                run.OS = OSObj;
                // Image must be set when OS was set. so setting with the default RTM of OS
                run.OSImage = GetImageObject(OSObj.ID, out ID); ;
                run.Save();
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }
        /// <summary>
        /// Return OS image object of a given OSID and its SP.  RTM level by default
        /// </summary>
        /// <param name="OSID"></param>
        /// <param name="OSImageID"></param>
        /// <param name="OSImage"></param>
        /// <returns></returns>
        public MDO.OSImage GetImageObject(int OSID, out int OSImageID, string OSImage = "RTM")
        {
            ConnectMDObject();
            try
            {

                MDO.QueryObject ImageQuery = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.OSImages);
                ImageQuery.QueryAdd(MDO.Tables.OSImageTable.OSIDFIELD, MDO.QueryConstants.EQUALTO, OSID, "AND");
                ImageQuery.QueryAdd(MDO.Tables.OSImageTable.NAMEFIELD, MDO.QueryConstants.EQUALTO, OSImage, "AND");
                object[] obj = ImageQuery.GetAllValuesForColumn("OSImageID", true);
                if (obj.Length > 1)
                {
                    // if there are multiple image IDs exists?????? I still want to go ahead with first in the query. lets see if this makes any noise.
                }
                OSImageID = int.Parse(obj[0].ToString());
                MDO.OSImage Image = new MDO.OSImage(OSImageID);
                return Image;
            }
            catch (Exception)
            {
                OSImageID = 0;
                return null;
            }

        }
        /// <summary>
        /// Returnan OSimage object of a given image ID
        /// </summary>
        /// <param name="OSImageID"></param>
        /// <returns></returns>
        public MDO.OSImage GetImageObject(int OSImageID)
        {
            ConnectMDObject();
            try
            {
                MDO.OSImage Image = new MDO.OSImage(OSImageID);
                return Image;
            }
            catch (Exception)
            {
                return null;
            }

        }
        /// <summary>
        /// Sets OSimage object of a given run ID.
        /// </summary>
        /// <param name="runID"></param>
        /// <param name="Image"></param>
        /// <returns></returns>
        public bool SetImageObjectOnRun(int runID, MDO.OSImage Image)
        {
            ConnectMDObject();
            if (runID == -1 || Image == null)
                return false;
            try
            {
                MDO.Run run = new MDO.Run(runID);
                run.OSImage = Image;
                run.Save();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Gets run object of the template
        /// </summary>
        /// <param name="TemplateID"></param>
        /// <returns></returns>
        public MDO.Run GetRunTemplateObject(int TemplateID)
        {
            try
            {
                return GetRunObject(TemplateID);
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// Retreives context query froma given run ID.
        /// </summary>
        /// <param name="MDRunID"></param>
        /// <param name="ContextQueryName"></param>
        /// <param name="ContextQueryID"></param>
        /// <returns></returns>
        public bool GetContextQueryForRun(int MDRunID, out string ContextQueryName, out int ContextQueryID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RunObject = new MDO.Run(MDRunID);
                ContextQueryID = RunObject.ContextQuery.ID;
                ContextQueryName = RunObject.ContextQuery.Name;
                return true;
            }
            catch (Exception)
            {
                ContextQueryID = 0;
                ContextQueryName = "Can't Get Context";
                return false;
            }

        }
        /// <summary>
        /// Sets context query for a given run(ID)
        /// </summary>
        /// <param name="MDRunID"></param>
        /// <param name="ContextQueryID"></param>
        /// <returns></returns>
        public bool SetContextQueryForRun(int MDRunID, int ContextQueryID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RunObject = new MDO.Run(MDRunID);
                RunObject.ContextQuery = new MDO.QueryObject(ContextQueryID);
                RunObject.Save();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        /// <summary>
        /// Gets machine query for a given runID
        /// </summary>
        /// <param name="MDRunID"></param>
        /// <param name="MachineQueryName"></param>
        /// <param name="MachineQueryID"></param>
        /// <returns></returns>
        public bool GetMachineQueryForRun(int MDRunID, out string MachineQueryName, out int MachineQueryID)
        {
            ConnectMDObject();
            MDO.Run RunObject = new MDO.Run(MDRunID);

            try
            {
                MachineQueryID = RunObject.MachineQuery.ID;
                MachineQueryName = RunObject.MachineQuery.Name;
                return true;
            }
            catch (Exception)
            {
                MachineQueryID = 0;
                MachineQueryName = "Can't Get Context";
                return false;
            }

        }
        /// <summary>
        /// Sets machine query for a given runID
        /// </summary>
        /// <param name="MDRunID"></param>
        /// <param name="MachineQueryID"></param>
        /// <returns></returns>
        public bool SetMachineQueryForRun(int MDRunID, int MachineQueryID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RunObject = new MDO.Run(MDRunID);
                RunObject.MachineQuery = new MDO.QueryObject(MachineQueryID);
                RunObject.Save();

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        /// <summary>
        /// Gets test query for a given runID
        /// </summary>
        /// <param name="MDRunID"></param>
        /// <param name="TestQueryName"></param>
        /// <param name="TestQueryID"></param>
        /// <returns></returns>
        public bool GetTestQueryForRun(int MDRunID, out string TestQueryName, out int TestQueryID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RunObject = new MDO.Run(MDRunID);
                TestQueryID = RunObject.TestcaseQuery.ID;
                TestQueryName = RunObject.TestcaseQuery.Name;
                return true;
            }
            catch (Exception)
            {
                TestQueryID = 0;
                TestQueryName = "Can't Get Context";
                return false;
            }

        }
        /// <summary>
        /// Sets test query for a given runID
        /// </summary>
        /// <param name="MDRunID"></param>
        /// <param name="TestQueryID"></param>
        /// <returns></returns>
        public bool SetTestQueryForRun(int MDRunID, int TestQueryID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RunObject = new MDO.Run(MDRunID);
                RunObject.TestcaseQuery = new MDO.QueryObject(TestQueryID);
                RunObject.Save();

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        /// <summary>
        /// Gets context name for a given context ID
        /// </summary>
        /// <param name="ContextID"></param>
        /// <param name="ContextName"></param>
        /// <returns></returns>
        public bool GetContextName(int ContextID, out string ContextName)
        {
            ConnectMDObject();
            try
            {
                MDO.Context c = new MDO.Context(ContextID);
                ContextName = c.Name;
                return true;
            }
            catch (Exception)
            {
                ContextName = "ErrorInGettingContext";
                return false;
            }


        }
        /// <summary>
        /// Clones witha given run ID
        /// </summary>
        /// <param name="RunID"></param>
        /// <returns></returns>
        public int CloneRun(int RunID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run TemplaterunObject = new MDO.Run(RunID);

                MDO.Run ResultRunObject = TemplaterunObject.Clone();
                ResultRunObject.Save();
                return ResultRunObject.ID;
            }
            catch (Exception)
            {
                return -1;
            }

        }

        /// <summary>
        /// Start Run
        /// </summary>
        /// <param name="RunID"></param>
        /// <returns></returns>
        public bool StartRun(int RunID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RuntoStart = new MDO.Run(RunID);
                MDO.Run.RunHelpers.StartRun(RuntoStart);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        /// <summary>
        /// Stop Run
        /// </summary>
        /// <param name="RunID"></param>
        /// <returns></returns>
        public bool StopRun(int RunID)
        {
            ConnectMDObject();
            try
            {
                MDO.Run RuntoStart = new MDO.Run(RunID);
                MDO.Run.RunHelpers.StopRun(RuntoStart);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        /// <summary>
        /// Returns List of selections of the installs/command that were added to run in install sequence section
        /// </summary>
        /// <param name="RunID"></param>
        /// <param name="PrerequisitesInstallSequence"></param>
        /// <returns></returns>
        public bool GetPrerequisites(int RunID, out List<Selection> PrerequisitesInstallSequence)
        {
            //sample content 
            /* Selection.Name = "Visual Studio Team Suite Dev10";
             Selection.Tokens = new Dictionary<string, DefinitionInterpreter.Token>();
             Selection.Tokens.Add("Action", new DefinitionInterpreter.Token("Action", "Install"));
             Selection.Add("SetupPath", new DefinitionInterpreter.Token("SetupPath", @"\\Path\to\setup\bits"));*/
            ConnectMDObject();
            PrerequisitesInstallSequence = new List<Selection>();
            try
            {
                MDO.Run RunObject = new MDO.Run(RunID);

                foreach (Selection s in RunObject.InstallSelections.InputSequence)
                {
                    PrerequisitesInstallSequence.Add(s);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retreive token from install sequence for a given input Key.
        /// </summary>
        /// <param name="PrerequisitesInstallSequence"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool GetTokenValue(List<Selection> PrerequisitesInstallSequence, string key, out string value)
        {

            try
            {
                if (PrerequisitesInstallSequence.Count > 0)
                {
                    foreach (Selection s in PrerequisitesInstallSequence)
                    {
                        bool found = false;
                        foreach (KeyValuePair<string, DefinitionInterpreter.Token> d in s.Tokens)
                        {
                            if (found)
                            {
                                value = d.Value.Value.ToString();
                                return true;
                            }
                            if (d.Value.Value.ToString() == key)
                            {
                                found = true;
                                continue;
                            }
                        }

                    }
                }

            }
            catch (Exception)
            {
                value = null;
                return false;
            }
            value = null;
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="RunID"></param>
        /// <param name="PrerequisitesInstallSequence"></param>
        /// <returns></returns>
        public bool SetPrerequisites(int RunID, List<Selection> PrerequisitesInstallSequence)
        {
            ConnectMDObject();
            if (PrerequisitesInstallSequence.Count() == 0)
                return false;
            try
            {
                MDO.Run RunObject = new MDO.Run(RunID);
                foreach (Selection s in PrerequisitesInstallSequence)
                {
                    RunObject.InstallSelections.InputSequence.Add(s);

                }
                RunObject.InstallSelections.UpdateInputSequenceXml();
                RunObject.GenerateInstallationSequence();
                if (hasWarningsOrErrors(RunObject))
                {
                    throw new InvalidOperationException("Warnings or errors encountered when generating install sequence");
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        /// <summary>
        /// Add an install step in the maddog run - strin command should exactly match with name, what was definied in the "add packages" list
        /// </summary>
        /// <param name="RunID"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool AddInstallSequenceToRun(int RunID, string command)
        {
            ConnectMDObject();
            DefinitionManager.CurrentDefinitionManager = new DefinitionManager(@"\\mdfile3\OrcasTS\Files\Core\TeamData\UniversalInstaller\v3\Definitions");
            try
            {
                MDO.Run RunObject = new MDO.Run(RunID);
                Selection CommandSel = new Selection(command);
                if (CommandSel.Tokens.Keys.Contains("UseOfficialBuild"))
                    CommandSel.SetToken("UseOfficialBuild", "True"); //For servicing this is always TRUE
                RunObject.InstallSelections.InputSequence.Add(CommandSel);
                RunObject.InstallSelections.UpdateInputSequenceXml();
                RunObject.GenerateInstallationSequence();

                RunObject.Save();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        /// <summary>
        /// Verifies warnings or error in the install sequence that we mentioned 
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        private bool hasWarningsOrErrors(MDO.Run run)
        {
            ConnectMDObject();
            if (run.InstallSelections.Warnings.Any())
            {
                return true;
            }
            if (run.InstallSelections.Errors.Any())
            {
                return true;
            }
            if (run.InstallSelections.InputSequence.Warnings.Any())
            {
                return true;
            }
            if (run.InstallSelections.InputSequence.Errors.Any())
            {
                return true;
            }
            if (run.InstallSelections.OutputSequence.Warnings.Any())
            {
                return true;
            }
            if (run.InstallSelections.OutputSequence.Errors.Any())
            {
                return true;
            }
            if (run.InstallSelections.OutputSequence.Any(x => x.Warnings.Any() || x.Errors.Any()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// According to OS name, get all the matches OS infomation from maddog DB
        /// </summary>
        /// <param name="strOSName"></param>
        /// <param name="dtOSDetail"></param>
        /// <returns></returns>
        public bool GetMatchedOSDetails(string strOSName, out DataTable dtOSDetail)
        {
            //string[] matchNeededItems = strOSName.Split(' ');
            ConnectMDObject();

            MDO.QueryObject ImageQuery = new MDO.QueryObject(Convert.ToInt32(MTPConstants.MDFORMATQUERYID.ToString()));

            ImageQuery.QueryAdd(MDO.Tables.OSImageTable.OSNAMEFIELD, MDO.QueryConstants.CONTAINS, strOSName, "AND");

            dtOSDetail = new DataTable();

            System.Data.DataSet ds = ImageQuery.GetDataSet();
            dtOSDetail = ds.Tables[0];

            return true;
        }

        /// <summary>
        /// Get run template by owner name and template name keywords
        /// </summary>
        /// <param name="owner">template owner</param>
        /// <param name="nameKeyWords">keywords in template name </param>
        /// <returns>Data table</returns>
        public DataTable GetRunTemplates(string owner, string nameKeyWords)
        {
            ConnectMDObject();

            // Create a query for the test cases we're interested in.
            MDO.QueryObject q = new MDO.QueryObject(MDO.QueryConstants.BaseObjectTypes.RMRunTemplates);
            q.QueryAdd(MDO.Tables.RMRunTemplateTable.OWNERNAMEFIELD, "=", owner, "AND");
            q.QueryAdd(MDO.Tables.RMRunTemplateTable.NAMEFIELD, "CONTAINS", nameKeyWords);

            // Create a string array to define the fields we want to get from the server.
            string[] saFields = { "RunTemplateID", "RunTemplateName" };

            // Set the display fields we want, then call MD query object
            // to get a dataset containing our query results.
            q.DisplayFields = saFields;
            DataSet ds = q.GetDataSet();
            return ds.Tables["table"];
        }

        /// <summary>
        /// Get run template by template id
        /// </summary>
        /// <param name="templateID">template id</param>
        /// <returns>RMRunTemplate</returns>
        public RMRunTemplate GetRMRunTemplate(int templateID)
        {
            return new RMRunTemplate(templateID);
        }

        /// <summary>
        /// Set RMRunTemplate Installation XML
        /// </summary>
        /// <param name="templateID">template id</param>
        /// <param name="xmlString">xmlString</param>
        public void SetRMRunTemplateInstallationXML(int templateID,string xmlString)
        {
            RMRunTemplate vsUltimate = new RMRunTemplate(templateID);
            vsUltimate.Data.StaticFields.Selections = xmlString;
        }

    }
}
