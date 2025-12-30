using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Connect2TFS;

namespace UploadAndDownloadFilesToWI
{
    public class UploadFiles
    {
        static readonly string TFUrl = "https://vstfdevdiv.corp.microsoft.com/DevDiv"; //for tfs
        //static readonly string TFUrl = "https://dev.azure.com/<your_org>/"; // for devops azure 
        static readonly string UserAccount = "";
        static readonly string UserPassword = "";
        static readonly string UserPAT = "<your pat>";


        static WorkItemTrackingHttpClient WitClient;
        static BuildHttpClient BuildClient;
        static ProjectHttpClient ProjectClient;
        static GitHttpClient GitClient;
        static TfvcHttpClient TfvsClient;
        static TestManagementHttpClient TestManagementClient;

        //static void Main(string[] args)
        //{
        //    int wiId = 1260280; // the work item id to upload and download attachmets

        //    string filePath = @"C:\Users\v-wxiaox\Desktop\1260280_Static_638476474731621910.html"; //
        //    string fileName = Path.GetFileName(filePath);                                                                                  //the file path that will be uploaded
        //    string destinationFolder = @"F:\Log"; //the existing folder for all attachments from the work item with wiId
        //    ConnectWithDefaultCreds(TFUrl);
        //    if (!CheckIfTheFileAlreadyUploaded(wiId, fileName))
        //    {

        //        AddAttachment(wiId, filePath);
        //    }
        //    if (!File.Exists(Path.Combine(destinationFolder, fileName)))
        //    {
        //        DownloadAttachments(wiId, @"F:\Log", fileName);

        //    }
        //    Console.WriteLine();

        //}

        public void AddAttachment(int WiID, string FilePath)
        {
            AttachmentReference att;
            string[] filePathSplit = FilePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            using (FileStream attStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                att = WitClient.CreateAttachmentAsync(attStream).Result; // upload the file
            List<object> references = new List<object>(); //list with references

            references.Add(new
            {
                rel = RelConstants.AttachmentRefStr,
                url = att.Url + "?fileName=" + Path.GetFileName(FilePath),
                attributes = new { comment = "Comments for the file " + filePathSplit[filePathSplit.Length - 1] }
            });
            CheckIfTheFileAlreadyUploaded(WiID,Path.GetFileName(FilePath));
            AddWorkItemRelations(WiID, references);
        }
        public void CheckIfTheFileAlreadyUploaded(int WIId, string fileName)
        {

            WorkItem workItem = WitClient.GetWorkItemAsync(WIId, expand: WorkItemExpand.Relations).Result;

            foreach (var rf in workItem.Relations)
            {
                if (rf.Rel == RelConstants.AttachmentRefStr)
                {
                    if (fileName.Equals(rf.Attributes["name"]))
                    {

                        DeleteWorkItemRelations(WIId, fileName);
                    }


                }
            }
        }

        /// <summary>
        /// Download all atachments from a work item
        /// </summary>
        /// <param name="WIId"></param>
        /// <param name="DestFolder"></param>
        public void DownloadAttachmentByFilename(int WIId, string DestFolder, string fileName)
        {
            WorkItem workItem = WitClient.GetWorkItemAsync(WIId, expand: WorkItemExpand.Relations).Result;

            foreach (var rf in workItem.Relations)
            {
                if (rf.Rel == RelConstants.AttachmentRefStr)
                {
                    if (fileName.Equals(rf.Attributes["name"]))
                    {

                        string[] urlSplit = rf.Url.ToString().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                        using (Stream attStream = WitClient.GetAttachmentContentAsync(new Guid(urlSplit[urlSplit.Length - 1])).Result) // get an attachment stream
                        using (FileStream destFile = new FileStream(DestFolder + "\\" + rf.Attributes["name"], FileMode.Create, FileAccess.Write)) // create new file

                            attStream.CopyTo(destFile); //copy content to the file
                    }

                }
            }
        }
        public void DownloadAllAttachments(int WIId, string DestFolder)
        {
            WorkItem workItem = WitClient.GetWorkItemAsync(WIId, expand: WorkItemExpand.Relations).Result;

            foreach (var rf in workItem.Relations)
            {
                if (rf.Rel == RelConstants.AttachmentRefStr)
                {
                    string[] urlSplit = rf.Url.ToString().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    using (Stream attStream = WitClient.GetAttachmentContentAsync(new Guid(urlSplit[urlSplit.Length - 1])).Result) // get an attachment stream
                    using (FileStream destFile = File.Create(DestFolder + "\\" + rf.Attributes["name"])) // create new file
                        //destFile.Name.Replace(destFile.Name, Path.Combine(DestFolder, rf.Attributes["name"].ToString()));
                        attStream.CopyTo(destFile); //copy content to the file

                }
            }
        }

        /// <summary>
        /// Add Relations
        /// </summary>
        /// <param name="WIId"></param>
        /// <param name="References"></param>
        /// <returns></returns>
        public WorkItem AddWorkItemRelations(int WIId, List<object> References)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            foreach (object rf in References)
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = rf
                });

            return WitClient.UpdateWorkItemAsync(patchDocument, WIId).Result; // return updated work item
        }
        static void DeleteWorkItemRelations(int WIId, string fileName)
        {
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem wi = Connect2TFS.Connect2TFS.GetWorkItem(WIId, TFUrl);
            for (int i = wi.Attachments.Count - 1; i >= 0; i--)
            {
                if (wi.Attachments[i].Name == fileName)
                {
                    // Remove the attachment
                    wi.Attachments.RemoveAt(i);
                }
            }

            // Save the changes
            wi.Save();
        }

        #region create new connections
        public void InitClients(VssConnection Connection)
        {
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            BuildClient = Connection.GetClient<BuildHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            GitClient = Connection.GetClient<GitHttpClient>();
            TfvsClient = Connection.GetClient<TfvcHttpClient>();
            TestManagementClient = Connection.GetClient<TestManagementHttpClient>();
        }

        public void ConnectWithDefaultCreds(string ServiceURL)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            VssConnection connection = new VssConnection(new Uri(ServiceURL), new VssCredentials());
            InitClients(connection);
        }

        public void ConnectWithCustomCreds(string ServiceURL, string User, string Password)
        {
            VssConnection connection = new VssConnection(new Uri(ServiceURL), new WindowsCredential(new NetworkCredential(User, Password)));
            InitClients(connection);
        }

        public void ConnectWithPAT(string ServiceURL, string PAT)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            VssConnection connection = new VssConnection(new Uri(ServiceURL), new VssBasicCredential(string.Empty, PAT));
            InitClients(connection);
        }
        #endregion
        
        class RelConstants
        {
            public const string AttachmentRefStr = "AttachedFile";
        }
    }
}

