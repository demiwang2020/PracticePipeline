using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NETCoreMUStaticLib.UpdateHelper
{
    public class Update
    {
        public UpdateProperty Properties { get; set; }

        public UpdateRoqData RoqData { get; private set; }

        public bool IsParentUpdate { get; private set; }

        public string ID { get; private set; }

        public List<Update> ChildUpdates { get; private set; }

        private string _publishingXML;
        private XmlDocument _xmlDoc;
        private XmlNamespaceManager _xmlNS;

        public Update(string publishingXML, bool bParent, bool bFromPubsuite)
        {
            IsParentUpdate = bParent;
            _publishingXML = publishingXML;

            _xmlDoc = new XmlDocument();
            _xmlDoc.LoadXml(_publishingXML);

            _xmlNS = new XmlNamespaceManager(_xmlDoc.NameTable);
            _xmlNS.AddNamespace("pub", "http://schemas.microsoft.com/msus/2002/12/Publishing");
            _xmlNS.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            _xmlNS.AddNamespace("cbs", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Cbs");
            _xmlNS.AddNamespace("cmd", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/CommandLineInstallation");

            //Get some basic properties from update
            if (IsParentUpdate && bFromPubsuite)
                GetBasicProperty();

            //Roq Data
            if (bFromPubsuite)
                GetRoqData();
        }

        public string Categories 
        {
            get
            {
                XmlNode node = _xmlDoc.SelectSingleNode("pub:Update/pub:Relationships/pub:Categories", _xmlNS);
                return node.OuterXml;
            }
        }

        public XmlNode Prerequisites
        {
            get
            {
                XmlNode node = _xmlDoc.SelectSingleNode("pub:Update/pub:Relationships/pub:Prerequisites", _xmlNS);
                return node;
            }
        }

        public string Title
        {
            get
            {
                return GetLocalizedEnProperty("pub:Title");
            }
        }

        public string Description
        {
            get
            {
                return GetLocalizedEnProperty("pub:Description");
            }
        }

        public string UninstallNotes
        {
            get
            {
                return GetLocalizedEnProperty("pub:UninstallNotes");
            }
        }

        public string MoreInfoUrl
        {
            get
            {
                return GetLocalizedEnProperty("pub:MoreInfoUrl");
            }
        }

        public string SupportUrl
        {
            get
            {
                return GetLocalizedEnProperty("pub:SupportUrl");
            }
        }

        public string PackagePath
        {
            get
            {
                if (IsParentUpdate)
                    return null;
                
                XmlNodeList nodes = _xmlDoc.SelectNodes("pub:Update/pub:Files/pub:File", _xmlNS);

                if (nodes == null)
                    return null;

                foreach (XmlNode node in nodes)
                {
                    string path = node.Attributes["FileLocation"].Value;
                    if (!String.IsNullOrEmpty(path) && (path.EndsWith(".exe") || path.EndsWith(".msu")))
                        return path;
                }

                return null;
            }
        }

        public XmlNode ApplicabilityRules
        {
            get
            {
                if (IsParentUpdate)
                    throw new Exception("Trying to get applicability rules from parent update");

                XmlNode node = _xmlDoc.SelectSingleNode("pub:Update/pub:ApplicabilityRules", _xmlNS);
                return node;
            }
        }

        public XmlNode IsInstallableRules
        {
            get
            {
                if (IsParentUpdate)
                    throw new Exception("Trying to get applicability rules from parent update");
                
                XmlNode node = _xmlDoc.SelectSingleNode("pub:Update/pub:ApplicabilityRules/pub:IsInstallable", _xmlNS);
                return node;
            }
        }

        public XmlNode IsInstalledRules
        {
            get
            {
                if (IsParentUpdate)
                    throw new Exception("Trying to get applicability rules from parent update");
                
                XmlNode node = _xmlDoc.SelectSingleNode("pub:Update/pub:ApplicabilityRules/pub:IsInstalled", _xmlNS);
                return node;
            }
        }

        public XmlNode HandlerSpecificData
        {
            get
            {
                return _xmlDoc.SelectSingleNode("pub:Update/pub:HandlerSpecificData", _xmlNS);
            }
        }

        public string InstallRebootBehavior
        {
            get
            {
                if (IsParentUpdate)
                    return String.Empty;
                else
                {
                    var node = _xmlDoc.SelectSingleNode("pub:Update/pub:Properties/pub:InstallationBehavior", _xmlNS);
                    if (node != null)
                        return node.Attributes["RebootBehavior"].Value;
                    else
                        return String.Empty;
                }
            }
        }

        public void AddChildUpdate(Update childUpdate)
        {
            if (!this.IsParentUpdate)
                throw new Exception("Trying to add child update to child update");
            if (childUpdate.IsParentUpdate)
                throw new Exception("Trying to add parent update to child update");

            if (ChildUpdates == null)
                ChildUpdates = new List<Update>();

            ChildUpdates.Add(childUpdate);
        }

        /// <summary>
        /// Find out child update that matches with given child update
        /// </summary>
        public Update GetMatchedChildUpdate(Update childUpdateToMatch)
        {
            if (!this.IsParentUpdate)
                throw new Exception("Trying to find child update in child update");
            if (childUpdateToMatch.IsParentUpdate)
                throw new Exception("Trying to find child update matches with a parent update");

            return this.ChildUpdates.Where(p => p.Title.Equals(childUpdateToMatch.Title, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public List<string> GetSupersededUpdates()
        {
            XmlNodeList nodes = _xmlDoc.SelectNodes("pub:Update/pub:Relationships/pub:SupersededUpdates/pub:UpdateIdentity", _xmlNS);

            if (nodes != null && nodes.Count > 0)
            {
                List<string> guids = new List<string>();
                foreach (XmlNode node in nodes)
                {
                    guids.Add(node.Attributes["UpdateID"].Value);
                }

                return guids;
            }
            else
            {
                return null;
            }
        }

        private void GetBasicProperty()
        {
            Properties = new UpdateProperty();

            //ID
            string xpath = "pub:Update/pub:UpdateIdentity";
            XmlNode updateIdentity = _xmlDoc.SelectSingleNode(xpath, _xmlNS);
            Properties.ID = updateIdentity.Attributes["UpdateID"].Value;
            ID = Properties.ID;

            //Properties
            xpath = "pub:Update/pub:Properties";
            XmlNode updateProp = _xmlDoc.SelectSingleNode(xpath, _xmlNS);
            XmlHelper.TryGetBooleanAttribute(updateProp, "IsBeta", ref Properties.IsBeta);
            XmlHelper.TryGetStringAttribute(updateProp, "UpdateType", ref Properties.Type);
            XmlHelper.TryGetBooleanAttribute(updateProp, "BrowseOnly", ref Properties.BrowseOnly);
            XmlHelper.TryGetBooleanAttribute(updateProp, "IsPublic", ref Properties.IsPublic);
            XmlHelper.TryGetBooleanAttribute(updateProp, "PerUser", ref Properties.PerUser);
            XmlHelper.TryGetBooleanAttribute(updateProp, "AutoSelectOnWebSites", ref Properties.AutoSelectOnWebSites);
            XmlHelper.TryGetStringAttribute(updateProp, "MsrcSeverity", ref Properties.MsrcSeverity);
            XmlHelper.TryGetStringAttribute(updateProp, "EulaID", ref Properties.EulaID);
            XmlHelper.TryGetStringAttribute(updateProp, "TimeToGoLive", ref Properties.TimeToGoLive);

            xpath = "pub:SuggestedRecipient";
            XmlNodeList nodes = updateProp.SelectNodes(xpath, _xmlNS);
            foreach(XmlNode node in nodes)
            {
                switch (node.InnerText.ToUpperInvariant())
                {
                    case "SITE":
                        Properties.Site = true;
                        break;

                    case "AU":
                        Properties.AU = true;
                        break;

                    case "SUS":
                        Properties.SUS = true;
                        break;

                    case "CATALOG":
                        Properties.Catalog = true;
                        break;

                    case "CSA":
                        Properties.Csa = true;
                        break;
                }
            }

            //KB
            xpath = "pub:KBArticleID";
            XmlNode singleNode = updateProp.SelectSingleNode(xpath, _xmlNS);
            Properties.KBArticle = singleNode.InnerText;

            //UpdateClassification
            xpath = "pub:UpdateClassification";
            singleNode = updateProp.SelectSingleNode(xpath, _xmlNS);
            Properties.UpdateClassification = singleNode.InnerText;

            //Readiness 
            xpath = "pub:Readiness";
            XmlNodeList ReadinssNodes = updateProp.SelectNodes(xpath, _xmlNS);
            if (ReadinssNodes.Count == 2) {
                Properties.isReadiness = true;
            }
            foreach (XmlNode node in ReadinssNodes)
            {
                switch (node.Attributes["Level"].Value)
                {
                    case "Business":
                        Properties.BusinessDate = node.Attributes["Date"].Value;
                        break;

                    case "RTW":
                        Properties.RTWDate = node.Attributes["Date"].Value;
                        break;
                }
            }

            //SecurityBulletinID
            xpath = "pub:SecurityBulletinID";
            singleNode = updateProp.SelectSingleNode(xpath, _xmlNS);
            if (singleNode != null)
                Properties.SecurityBulletinID = singleNode.InnerText;

            GetCveIDs();
        }

        private string GetLocalizedEnProperty(string propName)
        {
            var node = _xmlDoc.SelectSingleNode("pub:Update/pub:LocalizedPropertiesCollection/pub:LocalizedProperties/pub:Language[text()='en']", _xmlNS);
            return node.ParentNode.SelectSingleNode(propName, _xmlNS).InnerText;
        }

        private void GetRoqData()
        {
            string xpath = "pub:Update/pub:RoqData";
            XmlNode node = _xmlDoc.SelectSingleNode(xpath, _xmlNS);

            if (node != null)
            {
                RoqData = new UpdateRoqData();

                XmlHelper.TryGetBooleanAttribute(node, "IsCritical", ref RoqData.IsCritical);
                XmlHelper.TryGetBooleanAttribute(node, "IsTestOnly", ref RoqData.IsTestOnly);

                XmlNode ownerNode = node.SelectSingleNode("pub:Owner", _xmlNS);
                if(ownerNode != null)
                {
                    XmlHelper.TryGetStringAttribute(ownerNode, "UserID", ref RoqData.Owner);
                }
            }
        }

        private void GetCveIDs()
        {
            if (!IsParentUpdate)
                throw new Exception("Trying to get CveID from child update");

            var nodes = _xmlDoc.SelectNodes("pub:Update/pub:Properties/pub:CveID", _xmlNS);
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    if(String.IsNullOrEmpty(Properties.CveIDs))
                        Properties.CveIDs = node.InnerText;
                    else
                        Properties.CveIDs = Properties.CveIDs + ";" + node.InnerText;
                }
            }
        }
    }
}
