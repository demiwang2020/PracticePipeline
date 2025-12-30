using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WUTestManagerLib
{
    [Serializable]
    public class ExcelData
    {
        public string TFSID { get; set; }
        public string KB { get; set; }
        public string ProductLayer { get; set; }
        public string Title { get; set; }
        public string GUID { get; set; }
        public string SSKBs { get; set; }
        public string ShipChannels { get; set; }
        public bool IsCatalogOnly { get; set; }
        public string OtherProperties { get; set; }
    }
}
