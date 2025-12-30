using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WUTestManagerLib
{
    class TFSCache
    {
        /// <summary>
        /// The longest time of keeping TFS data, in seconds
        /// </summary>
        public int TimeOut { get; set; }

        private Dictionary<int, TFSCacheItem> _tfsDict;

        public TFSCache()
        {
            TimeOut = 3600 * 24; //Default is one day
            _tfsDict = new Dictionary<int, TFSCacheItem>();
        }

        public Connect2TFS.WorkItemBO QueryTFSWorkItem(int tfsID)
        {
            if(_tfsDict.ContainsKey(tfsID))
            {
                TFSCacheItem item = _tfsDict[tfsID];
                if((int)(DateTime.Now - item.RetriveTime).TotalSeconds < TimeOut)
                {
                    return item.TFSWorkItemObject;
                }
            }

            Connect2TFS.WorkItemBO workitem = Connect2TFS.Connect2TFS.GetWorkItemByID(tfsID, WUTestManagerLib.TFSServerURI);
            if (workitem != null)
            {
                TFSCacheItem item = new TFSCacheItem(workitem);
                _tfsDict[tfsID] = item;
            }

            return workitem;
        }
    }

    class TFSCacheItem
    {
        public DateTime RetriveTime {get; private set;}
        public Connect2TFS.WorkItemBO TFSWorkItemObject { get; private set; }

        public TFSCacheItem(Connect2TFS.WorkItemBO tfsItem)
        {
            TFSWorkItemObject = tfsItem;
            RetriveTime = DateTime.Now;
        }
    }
}
