namespace Microsoft.UpdateServices.Publishing.Schemas
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing", IsNullable = false)]
    public partial class Update
    {

        private UpdateIdentity updateIdentityField;

        private UpdateProperties propertiesField;

        private UpdateLocalizedPropertiesCollection localizedPropertiesCollectionField;

        private UpdateRelationships relationshipsField;

        private UpdateFile[] filesField;

        private UpdateRoqData roqDataField;

        /// <remarks/>
        public UpdateIdentity UpdateIdentity
        {
            get
            {
                return this.updateIdentityField;
            }
            set
            {
                this.updateIdentityField = value;
            }
        }

        /// <remarks/>
        public UpdateProperties Properties
        {
            get
            {
                return this.propertiesField;
            }
            set
            {
                this.propertiesField = value;
            }
        }

        /// <remarks/>
        public UpdateLocalizedPropertiesCollection LocalizedPropertiesCollection
        {
            get
            {
                return this.localizedPropertiesCollectionField;
            }
            set
            {
                this.localizedPropertiesCollectionField = value;
            }
        }

        /// <remarks/>
        public UpdateRelationships Relationships
        {
            get
            {
                return this.relationshipsField;
            }
            set
            {
                this.relationshipsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("File", IsNullable = false)]
        public UpdateFile[] Files
        {
            get
            {
                return this.filesField;
            }
            set
            {
                this.filesField = value;
            }
        }

        /// <remarks/>
        public UpdateRoqData RoqData
        {
            get
            {
                return this.roqDataField;
            }
            set
            {
                this.roqDataField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateIdentity
    {

        private string updateIDField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UpdateID
        {
            get
            {
                return this.updateIDField;
            }
            set
            {
                this.updateIDField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateProperties
    {

        private string supportUrlField;

        private string[] suggestedRecipientField;

        private string securityBulletinIDField;

        private uint kBArticleIDField;

        private string updateClassificationField;

        private string revisionNotesField;

        private string defaultPropertiesLanguageField;

        private string updateTypeField;

        private bool explicitlyDeployableField;

        private bool autoSelectOnWebSitesField;

        private string timeToGoLiveField;

        private bool perUserField;

        private bool isPublicField;

        private bool isBetaField;

        private string friendlyNameField;

        /// <remarks/>
        public string SupportUrl
        {
            get
            {
                return this.supportUrlField;
            }
            set
            {
                this.supportUrlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("SuggestedRecipient")]
        public string[] SuggestedRecipient
        {
            get
            {
                return this.suggestedRecipientField;
            }
            set
            {
                this.suggestedRecipientField = value;
            }
        }

        /// <remarks/>
        public string SecurityBulletinID
        {
            get
            {
                return this.securityBulletinIDField;
            }
            set
            {
                this.securityBulletinIDField = value;
            }
        }

        /// <remarks/>
        public uint KBArticleID
        {
            get
            {
                return this.kBArticleIDField;
            }
            set
            {
                this.kBArticleIDField = value;
            }
        }

        /// <remarks/>
        public string UpdateClassification
        {
            get
            {
                return this.updateClassificationField;
            }
            set
            {
                this.updateClassificationField = value;
            }
        }

        /// <remarks/>
        public string RevisionNotes
        {
            get
            {
                return this.revisionNotesField;
            }
            set
            {
                this.revisionNotesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DefaultPropertiesLanguage
        {
            get
            {
                return this.defaultPropertiesLanguageField;
            }
            set
            {
                this.defaultPropertiesLanguageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UpdateType
        {
            get
            {
                return this.updateTypeField;
            }
            set
            {
                this.updateTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool ExplicitlyDeployable
        {
            get
            {
                return this.explicitlyDeployableField;
            }
            set
            {
                this.explicitlyDeployableField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool AutoSelectOnWebSites
        {
            get
            {
                return this.autoSelectOnWebSitesField;
            }
            set
            {
                this.autoSelectOnWebSitesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string TimeToGoLive
        {
            get
            {
                return this.timeToGoLiveField;
            }
            set
            {
                this.timeToGoLiveField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool PerUser
        {
            get
            {
                return this.perUserField;
            }
            set
            {
                this.perUserField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool IsPublic
        {
            get
            {
                return this.isPublicField;
            }
            set
            {
                this.isPublicField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool IsBeta
        {
            get
            {
                return this.isBetaField;
            }
            set
            {
                this.isBetaField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FriendlyName
        {
            get
            {
                return this.friendlyNameField;
            }
            set
            {
                this.friendlyNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateLocalizedPropertiesCollection
    {

        private UpdateLocalizedPropertiesCollectionLocalizedProperties localizedPropertiesField;

        /// <remarks/>
        public UpdateLocalizedPropertiesCollectionLocalizedProperties LocalizedProperties
        {
            get
            {
                return this.localizedPropertiesField;
            }
            set
            {
                this.localizedPropertiesField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateLocalizedPropertiesCollectionLocalizedProperties
    {

        private string languageField;

        private string titleField;

        private string descriptionField;

        private string uninstallNotesField;

        private string moreInfoUrlField;

        private string supportUrlField;

        /// <remarks/>
        public string Language
        {
            get
            {
                return this.languageField;
            }
            set
            {
                this.languageField = value;
            }
        }

        /// <remarks/>
        public string Title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
            }
        }

        /// <remarks/>
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public string UninstallNotes
        {
            get
            {
                return this.uninstallNotesField;
            }
            set
            {
                this.uninstallNotesField = value;
            }
        }

        /// <remarks/>
        public string MoreInfoUrl
        {
            get
            {
                return this.moreInfoUrlField;
            }
            set
            {
                this.moreInfoUrlField = value;
            }
        }

        /// <remarks/>
        public string SupportUrl
        {
            get
            {
                return this.supportUrlField;
            }
            set
            {
                this.supportUrlField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRelationships
    {

        private UpdateIdentity[] supersededUpdatesField;

        private UpdateRelationshipsPrerequisites prerequisitesField;

        private UpdateIdentity[] categoriesField;

        private UpdateRelationshipsBundledUpdates bundledUpdatesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("UpdateIdentity", IsNullable = false)]
        public UpdateIdentity[] SupersededUpdates
        {
            get
            {
                return this.supersededUpdatesField;
            }
            set
            {
                this.supersededUpdatesField = value;
            }
        }

        /// <remarks/>
        public UpdateRelationshipsPrerequisites Prerequisites
        {
            get
            {
                return this.prerequisitesField;
            }
            set
            {
                this.prerequisitesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("UpdateIdentity", IsNullable = false)]
        public UpdateIdentity[] Categories
        {
            get
            {
                return this.categoriesField;
            }
            set
            {
                this.categoriesField = value;
            }
        }

        /// <remarks/>
        public UpdateRelationshipsBundledUpdates BundledUpdates
        {
            get
            {
                return this.bundledUpdatesField;
            }
            set
            {
                this.bundledUpdatesField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRelationshipsPrerequisites
    {

        private object[] itemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("AtLeastOne", typeof(UpdateIdentityAtLeastOne))]
        [System.Xml.Serialization.XmlElementAttribute("UpdateIdentity", typeof(UpdateIdentity))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateIdentityAtLeastOne
    {

        private UpdateIdentity[] updateIdentityField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("UpdateIdentity")]
        public UpdateIdentity[] UpdateIdentity
        {
            get
            {
                return this.updateIdentityField;
            }
            set
            {
                this.updateIdentityField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRelationshipsPrerequisitesAtLeastOneUpdateIdentity
    {

        private string updateIDField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UpdateID
        {
            get
            {
                return this.updateIDField;
            }
            set
            {
                this.updateIDField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRelationshipsBundledUpdates
    {

        private object[] itemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("AtLeastOne", typeof(UpdateIdentityAtLeastOne))]
        [System.Xml.Serialization.XmlElementAttribute("UpdateIdentity", typeof(UpdateIdentity))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateFile
    {

        private string fileLocationField;

        private string fileNameField;

        private string patchingTypeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FileLocation
        {
            get
            {
                return this.fileLocationField;
            }
            set
            {
                this.fileLocationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FileName
        {
            get
            {
                return this.fileNameField;
            }
            set
            {
                this.fileNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string PatchingType
        {
            get
            {
                return this.patchingTypeField;
            }
            set
            {
                this.patchingTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRoqData
    {

        private UpdateRoqDataOwner ownerField;

        private UpdateRoqDataNotification notificationField;

        private bool isCriticalField;

        private bool isTestOnlyField;

        /// <remarks/>
        public UpdateRoqDataOwner Owner
        {
            get
            {
                return this.ownerField;
            }
            set
            {
                this.ownerField = value;
            }
        }

        /// <remarks/>
        public UpdateRoqDataNotification Notification
        {
            get
            {
                return this.notificationField;
            }
            set
            {
                this.notificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool IsCritical
        {
            get
            {
                return this.isCriticalField;
            }
            set
            {
                this.isCriticalField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool IsTestOnly
        {
            get
            {
                return this.isTestOnlyField;
            }
            set
            {
                this.isTestOnlyField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRoqDataOwner
    {

        private string userIDField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UserID
        {
            get
            {
                return this.userIDField;
            }
            set
            {
                this.userIDField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/msus/2002/12/Publishing")]
    public partial class UpdateRoqDataNotification
    {

        private string eventField;

        private string userIDField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Event
        {
            get
            {
                return this.eventField;
            }
            set
            {
                this.eventField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UserID
        {
            get
            {
                return this.userIDField;
            }
            set
            {
                this.userIDField = value;
            }
        }
    }



}