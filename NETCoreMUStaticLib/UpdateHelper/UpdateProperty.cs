using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.UpdateHelper
{
    public class UpdateProperty
    {
        public string ID;
        public string KBArticle;
        public string Type;
        public string UpdateClassification;
        public string MsrcSeverity;
        public string SecurityBulletinID;
        public string EulaID;
        public string TimeToGoLive;
        public string BusinessDate;
        public string RTWDate;
        public bool BrowseOnly;
        public bool IsBeta;
        public bool IsPublic;
        public bool PerUser;
        public bool AutoSelectOnWebSites;
        public bool Site;
        public bool AU;
        public bool SUS;
        public bool Catalog;
        public bool Csa;
        public bool isReadiness;
        public string CveIDs; // ID joined with ';'

        public UpdateProperty()
        {
            Type = "Software";
            UpdateClassification = "cb4e8e34-b2bb-4025-9624-f65d8d9e80fb"; //Update
            MsrcSeverity = String.Empty;
            KBArticle = String.Empty;
            SecurityBulletinID = String.Empty;
            EulaID = String.Empty;
            TimeToGoLive = String.Empty;
            CveIDs = String.Empty;
            BrowseOnly = false;
            IsBeta = false;
            IsPublic = false;
            PerUser = false;
            AutoSelectOnWebSites = false;
            Site = false;
            AU = false;
            SUS = false;
            Catalog = false;
            Csa = false;
            isReadiness = false;
        }
    }
}
