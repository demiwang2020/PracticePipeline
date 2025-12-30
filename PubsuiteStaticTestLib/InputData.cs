using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib
{
    public class InputData
    {
        public List<int> TFSIDs;
        public string KB;
        public string Title;
        public string UpdateID;
        public string SupersededKB;
        public string PublishingXmlContent;
        public string ShipChannels;
        public Boolean IsCatalogOnly;

        //AUClassification: Important; Recommended; Optional
        //ReleaseType:SecurityUpdate,MonthlyRollup,SecurityOnly,Preview,CSA,CatalogOnly
        public Dictionary<string, string> OtherProperties;
    }
}
