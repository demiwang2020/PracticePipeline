using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Connect2TFS
{
    public static class Connect2TFS
    {
        public static string TFSServerURI { get; private set; }
        private static WorkItemStore TFSStore { get; set; }

        public static void DisconnectFromTFSStore()
        {
            if (TFSStore != null)
            {
                TFSStore = null;
            }
        }

        private static void InitiaizeConnection(string strTFSServerURI)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (TFSStore == null || TFSStore.TeamProjectCollection.Uri.OriginalString != strTFSServerURI)
            {
                TFSServerURI = Uri.EscapeUriString(strTFSServerURI);
                TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(TFSServerURI));
                tpc.Authenticate();
                TFSStore = (WorkItemStore)tpc.GetService(typeof(WorkItemStore));
            }
        }

        public static void InitVSOConnection(string strTFSServerURI, string username, string password, out TfsTeamProjectCollection tfsTeamProjectCollection)
        {
            string serverTrimmed = Uri.EscapeUriString(strTFSServerURI);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            NetworkCredential netCred = new NetworkCredential(username, password);
            BasicAuthCredential basicCred = new BasicAuthCredential(netCred);
            TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred)
            {
                AllowInteractive = false
            };
            TfsTeamProjectCollection TFS = new TfsTeamProjectCollection(new Uri(serverTrimmed), tfsCred);
            TFS.Authenticate();
            TFSStore = (WorkItemStore)TFS.GetService(typeof(WorkItemStore));
            tfsTeamProjectCollection = TFS;
        }

        public static void InitVSOConnection(string strTFSServerURI, string username, string password)
        {
            TfsTeamProjectCollection tfsTeamProjectCollection = null;
            InitVSOConnection(strTFSServerURI, username, password, out tfsTeamProjectCollection);
        }

        public static WorkItemBO GetWorkItemByID(int intWorkItemID, string strTFSServerURI)
        {
            InitiaizeConnection(strTFSServerURI);
            return GetWorkItemBO(intWorkItemID);
        }

        private static WorkItemBO GetWorkItemBO(int intWorkItemID)
        {
            // build query
            StringBuilder wiqlBuilder = new StringBuilder();
            wiqlBuilder.Append("SELECT [System.Id] FROM WorkItems ");
            wiqlBuilder.Append("WHERE [System.Id] = @strWorkItemID");

            Hashtable parameters = new Hashtable();
            parameters.Add("strWorkItemID", intWorkItemID);

            // execute query
            WorkItemCollection workItemCollection;
            try
            {
                workItemCollection = TFSStore.Query(wiqlBuilder.ToString(), parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (workItemCollection.Count == 0)
                return null;
            if (workItemCollection.Count == 1)
                return new WorkItemBO(workItemCollection[0]);
            else
                throw (new Exception("More than one workitem record found"));
        }

        public static WorkItem GetWorkItem(int intWorkItemID, string strTFSServerURI)
        {
            WorkItem wi = null;
            try
            {
                InitiaizeConnection(strTFSServerURI);
                wi = TFSStore.GetWorkItem(intWorkItemID);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while trying to connect to TFS.");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);
            }

            return wi;
        }

        public static void SaveWorkItem(WorkItem workItem, string strTFSServerURI)
        {
            InitiaizeConnection(strTFSServerURI);
            
            ArrayList result = workItem.Validate();
            if (result.Count > 0)
            {
                throw new Exception(string.Format("Error occurred when save work item {0}", workItem.Id));
            }
            else
            {
                workItem.Save();
            }
        }

        public static void SaveWorkItem(int intWorkItemID, Dictionary<string, string> updateInfos, string strTFSServerURI)
        {
            // Check parameter "updateInfo"
            if (updateInfos.Count < 1)
                return;

            WorkItem wi = GetWorkItem(intWorkItemID, strTFSServerURI);

            if (wi == null)
                throw new Exception(string.Format("There is no such work item in TFS which ID is {0}", intWorkItemID));

            foreach (KeyValuePair<string, string> info in updateInfos)
            {
                wi[info.Key] = info.Value;
            }

            ArrayList result = wi.Validate();
            if (result.Count > 0)
            {
                throw new Exception(string.Format("Error occurred when save work item {0}", wi.Id));
            }
            else
            {
                wi.Save();
            }
        }

        public static List<WorkItemBO> QueryWorkItemsInSmokeTest(string strTFSServerURI)
        {
            string queryString = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = 'DevDiv Servicing' AND [System.WorkItemType] = 'Servicing' AND [System.State] = 'Smoke Test'  AND  [Microsoft.DevDiv.Product] = 'Dot Net Framework' ORDER BY [System.Id]";

            InitiaizeConnection(strTFSServerURI);
            WorkItemCollection wis = TFSStore.Query(queryString);

            List<WorkItemBO> workItemBOs = new List<WorkItemBO>();
            foreach (WorkItem wi in wis)
            {
                workItemBOs.Add(new WorkItemBO(wi));
            }

            return workItemBOs;
        }

        public static WorkItemCollection QueryWorkItemsByState(string strTFSServerURI, string state)
        {
            string queryString = String.Format("SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = 'DevDiv Servicing' AND [System.WorkItemType] = 'Servicing' AND [System.State] = '{0}'  AND  [Microsoft.DevDiv.Product] = 'Dot Net Framework' ORDER BY [System.Id]", state);

            InitiaizeConnection(strTFSServerURI);
            return TFSStore.Query(queryString);
        }

        public static List<WorkItemBO> QueryWorkItemsMenuInSmokeTest(string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);

            WorkItemCollection queryResults = TFSStore.Query(@"
                                       Select [id], [KB Article] 
                                       From WorkItems
                                       Where [System.TeamProject] = 'DevDiv Servicing'
                                       AND [System.WorkItemType] = 'Servicing' 
                                       AND [System.State] = 'Smoke Test'  
                                       AND  [Microsoft.DevDiv.Product] = 'Dot Net Framework' 
                                       AND [Microsoft.VSTS.Phoenix.Deliverable] <> 'CBS' 
                                       ORDER BY [System.Id], [Changed Date] Desc");

            var menu = new List<WorkItemBO>();
            foreach (WorkItem item in queryResults)
            {
                menu.Add(new WorkItemBO(Convert.ToInt32(item["Id"]), item["KB Article"].ToString(), item));
            }

            return menu;
        }

        public static WorkItemBO GetWorkItemForSmokeDashboardByID(int workItemID, string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);
            WorkItemBO item = null;

            WorkItemCollection queryResults = TFSStore.Query(@"SELECT [System.Id] FROM WorkItems Where [System.Id] =" + workItemID.ToString());

            if (queryResults.Count == 1)
            {
                item = new WorkItemBO();
                item.SetTFSDataForSmokeTestDashboard(queryResults[0]);
            }
            return item;
        }

        /// <summary>
        /// This is for when code processes work items by WorkItemCollection, but you only need to query one work item
        /// </summary>
        /// <param name="workItemID"></param>
        /// <param name="strTFSServerURI"></param>
        /// <returns></returns>
        public static WorkItemCollection GetWorkItemCollectionByID(int workItemID, string strTFSServerURI, string username, string password)
        {
            // Connect to the work item store
            InitVSOConnection(strTFSServerURI, username, password);

            WorkItemCollection queryResults = TFSStore.Query(@"SELECT [System.Id] FROM WorkItems Where [System.Id] =" + workItemID.ToString());

            return queryResults;
        }

        public static WorkItemBO GetWorkItemByKBNumber(string kbnumber, string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);
            WorkItemBO item = null;

            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Microsoft.VSTS.Dogfood.KBArticle] = '{0}' ORDER BY [System.Id]", kbnumber));

            if (queryResults.Count == 1)
            {
                item = new WorkItemBO();
                item.GetWorkItemTimeStats(queryResults[0]);
            }
            return item;
        }

        public static WorkItem GetWorkItemByKBNumber(int kbnumber, string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);

            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Microsoft.VSTS.Dogfood.KBArticle] = '{0}' ORDER BY [System.Id]", kbnumber));

            if (queryResults.Count >= 1)
            {
                return queryResults[0];
            }
            return null;
        }

        public static WorkItemCollection GetWorkItemsByKBNumber(int kbnumber, string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);

            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Microsoft.VSTS.Dogfood.KBArticle] = '{0}' ORDER BY [System.Id]", kbnumber));

            if (queryResults.Count >= 1)
            {
                return queryResults;
            }
            return null;
        }


        public static int GetWorkitemIDByCPNumber(int cpnumber, string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);

            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Microsoft.DevDiv.Compliance] CONTAINS '{0}' AND [Microsoft.DevDiv.WUParentWorkitem] == 'Yes' ORDER BY [System.Id]", cpnumber));

            if (queryResults.Count == 1)
            {
                return queryResults[0].Id;
            }
            return 0;
        }

        public static string GetWorkItemTitleByKBNumber(string kbnumber, string strTFSServerURI)
        {
            InitiaizeConnection(strTFSServerURI);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Title] " +
                "FROM WorkItems WHERE [Microsoft.VSTS.Dogfood.KBArticle] = '{0}' ORDER BY [System.Id]", kbnumber));
            if (queryResults.Count == 0)
            {
                return string.Empty;
            }
            string titlestring = string.Empty;
            foreach (WorkItem wi in queryResults)
            {
                titlestring += String.Format("{0};", wi.Title);
            }
            return titlestring;
        }

        public static WorkItemCollection GetFixedVSOBugsWithTag(string tag, string strTfsServerURI)
        {
            InitiaizeConnection(strTfsServerURI);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Tags] CONTAINS '{0}' AND [Resolution Bug] = 'Fixed' ORDER BY [System.Id]", tag));
            return queryResults;
        }

        public static WorkItemCollection GetAllVSOBugsWithTag(string tag, string strTfsServerURI)
        {
            InitiaizeConnection(strTfsServerURI);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Tags] CONTAINS '{0}' AND [Created Date] > '12/31/2014' ORDER BY [System.Id]", tag));
            return queryResults;
        }
        public static WorkItemCollection GetAllDOTNETBugsOpenedBy(string openedby, string strTfsServerURI)
        {
            InitiaizeConnection(strTfsServerURI);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Created By] = '{0}' AND [System.WorkItemType] = 'Bug' AND [Area Path] UNDER 'DevDiv\\NET' AND [Created Date] > '12/31/2014' ORDER BY [System.Id]", openedby));
            return queryResults;
        }

        public static List<WorkItem> GetAllUnderPath(string path, string strTfsServerURI, string daterangestart, string daterangeend, bool queryforservicing, string username, string password)
        {
            InitVSOConnection(strTfsServerURI, username, password);

            //Get all active bugs regardless of created date to cover when bugs are reactivated 
            string querystring = String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [Area Path] UNDER '{0}' AND [State] = 'Active' AND [Milestone] = 'Days To Solution' ORDER BY [System.Id]", path);
            Console.WriteLine("Querying active bugs with string {0}", querystring);

            WorkItemCollection activeQueryResults = TFSStore.Query(querystring);

            //Now get all non-active bugs
            querystring = String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [Area Path] UNDER '{0}' AND [State] <> 'Active' AND [Created Date] > '{1}' AND [Created Date] < '{2}' {3} ORDER BY [System.Id]", path, daterangestart, daterangeend, queryforservicing ? "AND [BU Triage] = 'Approved'" : "AND [Milestone] = 'Days To Solution'");
            Console.WriteLine("Querying non active bugs with string {0}", querystring);

            WorkItemCollection nonActiveQueryResults = TFSStore.Query(querystring);

            List<WorkItem> allResults = new List<WorkItem>();
            allResults.AddRange(activeQueryResults.OfType<WorkItem>());
            allResults.AddRange(nonActiveQueryResults.OfType<WorkItem>());

            return allResults;
        }

        public static WorkItemCollection GetAllActiveCSSBugs(string strTfsServerURI, string username, string password, string path)
        {
            InitVSOConnection(strTfsServerURI, username, password);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [Area Path] UNDER '{0}' AND [State] == 'Active' AND [CSS Case ID] != '' AND [Milestone] = 'Days To Solution' ORDER BY [System.Id]", path));
            return queryResults;
        }

        public static WorkItemCollection GetAllActiveCSSBugsNotMarkedAsDTS(string strTfsServerURI, string username, string password, string path)
        {
            InitVSOConnection(strTfsServerURI, username, password);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [Area Path] UNDER '{0}' AND [State] == 'Active' AND [CSS Case ID] != '' AND [Milestone] != 'Days To Solution' ORDER BY [System.Id]", path));
            return queryResults;
        }

        public static WorkItemCollection GetAllActiveDTSBugsWithNoCaseId(string strTfsServerURI, string username, string password, string path)
        {
            InitVSOConnection(strTfsServerURI, username, password);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [Area Path] UNDER '{0}' AND [State] == 'Active' AND [CSS Case ID] == '' AND [Milestone] = 'Days To Solution' ORDER BY [System.Id]", path));
            return queryResults;
        }

        public static TfsTeamProjectCollection GetVSOTeamProjectCollection(string strTfsServerURI, string username, string password)
        {
            TfsTeamProjectCollection tfsTeamProjectCollection = null;
            InitVSOConnection(strTfsServerURI, username, password, out tfsTeamProjectCollection);
            return tfsTeamProjectCollection;
        }


        public static WorkItemCollection GetAllVSOBugsByRelease(string release, string strTfsServerURI)
        {
            InitiaizeConnection(strTfsServerURI);
            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Release] = '{0}' AND [System.WorkItemType] = 'Bug' AND [Created Date] > '12/31/2014' ORDER BY [System.Id]", release));
            return queryResults;
        }

        public static List<WorkItemBOReleaseStatus> GetWorkItemsByTitleSearch(string titlesnippet, string strTFSServerURI)
        {
            // Connect to the work item store
            InitiaizeConnection(strTFSServerURI);

            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [System.Title] Contains '{0}' ORDER BY [System.Id]", titlesnippet));

            var results = new List<WorkItemBOReleaseStatus>();
            foreach (WorkItem item in queryResults)
            {
                results.Add(new WorkItemBOReleaseStatus(item));
            }

            return results;
        }

        public static WorkItemCollection QueryTHWorkItemsInSmokeTest(string strTFSServerURI, string[] osNames)
        {
            string queryString = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = 'DevDiv Servicing' AND [System.WorkItemType] = 'Servicing' AND [System.State] = 'Smoke Test'  AND ( {0} ) ORDER BY [System.Id]";

            string osString = String.Format("[Microsoft.VSTS.Dogfood.Environment] = '{0}'", osNames[0]);
            for (int i = 1; i < osNames.Length; ++i)
            {
                osString = String.Format("{0} OR [Microsoft.VSTS.Dogfood.Environment] = '{1}'", osString, osNames[i]);
            }

            queryString = String.Format(queryString, osString);

            InitiaizeConnection(strTFSServerURI);
            WorkItemCollection wis = TFSStore.Query(queryString);

            return wis;
        }

        public static WorkItemCollection QueryCBSWorkItemsInSmokeTest(string strTFSServerURI)
        {
            string queryString = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = 'DevDiv Servicing' AND [System.WorkItemType] = 'Servicing' AND [System.State] = 'Smoke Test'  AND  [Microsoft.DevDiv.Product] = 'Dot Net Framework' AND [Microsoft.VSTS.Phoenix.Deliverable] = 'CBS' ORDER BY [System.Id]";

            InitiaizeConnection(strTFSServerURI);
            return TFSStore.Query(queryString);
        }

        public static WorkItemCollection QueryWorkItemsInWUTest(string strTFSServerURI)
        {
            string queryString = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = 'DevDiv Servicing' AND [System.WorkItemType] = 'Servicing' AND [System.State] = 'WU Test'  AND  [Microsoft.DevDiv.Product] = 'Dot Net Framework' ORDER BY [System.Id]";

            InitiaizeConnection(strTFSServerURI);
            return TFSStore.Query(queryString);
        }

        /// <summary>
        /// Search revision history of a TFS item to see if value of a specific field is changed after a given time
        /// </summary>
        public static bool WorkItemFieldChanged(string serverURI, int tfsID, string fieldName, DateTime startTime)
        {
            WorkItem wi = GetWorkItem(tfsID, serverURI);
            if (wi == null)
                return false;

            string value = null, previousValue = null;
            bool startTimeReached = false;
            for (int i = 0; i < wi.Revisions.Count; ++i)
            {
                Revision rvs = wi.Revisions[i];

                if (!startTimeReached)
                {
                    DateTime changedDate = Convert.ToDateTime(rvs.Fields["Changed Date"].Value);
                    if (changedDate >= startTime)
                    {
                        startTimeReached = true;
                    }
                    else
                    {
                        previousValue = rvs.Fields[fieldName].Value.ToString();
                        continue;
                    }
                }

                value = rvs.Fields[fieldName].Value.ToString();
                if (!previousValue.Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        public static WorkItemCollection QueryWorkItemByKBNumber(string strTFSServerURI, string kbnumber)
        {
            InitiaizeConnection(strTFSServerURI);

            WorkItemCollection queryResults = TFSStore.Query(String.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] " +
                "FROM WorkItems WHERE [Microsoft.VSTS.Dogfood.KBArticle] = '{0}' ORDER BY [System.Id]", kbnumber));

            return queryResults;
        }

        public static WorkItemCollection GetWorkItemsInSharedQuery(string strTFSServerURI, string teamprojectname, string queryname)
        {
            InitiaizeConnection(strTFSServerURI);
            var teamProject = TFSStore.Projects[teamprojectname];
            var x = teamProject.QueryHierarchy;
            Guid queryIdGUID = FindQuery(x, queryname);
            if (queryIdGUID == Guid.Empty)
            {
                return null;
            }
            var queryDefinition = TFSStore.GetQueryDefinition(queryIdGUID);
            var variables = new Dictionary<string, string>() { { "project", teamprojectname } };
            return TFSStore.Query(queryDefinition.QueryText, variables);
        }

        private static Guid FindQuery(QueryFolder folder, string queryName)
        {
            foreach (var item in folder)
            {
                if (item.Name.Equals(queryName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return item.Id;
                }

                var itemFolder = item as QueryFolder;
                if (itemFolder != null)
                {
                    var result = FindQuery(itemFolder, queryName);
                    if (!result.Equals(Guid.Empty))
                    {
                        return result;
                    }
                }
            }
            return Guid.Empty;
        }
    }
}
