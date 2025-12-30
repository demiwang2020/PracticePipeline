using System;
using Microsoft.TeamFoundation.WorkItemTracking.Client; // provides Project, WorkItem, WorkItemType

namespace Connect2TFS
{
    public class WorkItemBOReleaseStatus : WorkItemBO
    {
        public WorkItemBOReleaseStatus(WorkItem objWorkItem)
        {
            if (objWorkItem == null)
                throw new Exception("WorkItem object is null");
            
            ServicingID = Convert.ToInt32(objWorkItem["Id"]);
            State = objWorkItem["State"].ToString();
            SKU = objWorkItem["SKU"].ToString();
            AssignedTo = objWorkItem["Assigned To"].ToString();
            OSInstalled = objWorkItem["Environment"].ToString();
            Notes = objWorkItem["Notes"].ToString();
        }
    }
}
