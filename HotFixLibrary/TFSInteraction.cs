using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Connect2TFS;

namespace HotFixLibrary
{
    //Not yet started using it.
    class TFSInteraction
    {
        public static bool VerifyTFSPatchDetails(Patch objPatch, string strTFSServerURI)
        {
            WorkItemBO objWorkItemBO = Connect2TFS.Connect2TFS.GetWorkItemByID(Convert.ToInt32(objPatch.PatchInfo.WorkItem), strTFSServerURI);
            return objWorkItemBO.Equals(objPatch);
        }
    }
}
