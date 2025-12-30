namespace NetFxSetupLibrary
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Product
    {

        private ProductCommon commonField;

        private ProductSku[] skuField;

        private string nameField;

        private string schemaField;

        /// <remarks/>
        public ProductCommon Common
        {
            get
            {
                return this.commonField;
            }
            set
            {
                this.commonField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Sku")]
        public ProductSku[] Sku
        {
            get
            {
                return this.skuField;
            }
            set
            {
                this.skuField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Schema
        {
            get
            {
                return this.schemaField;
            }
            set
            {
                this.schemaField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductCommon
    {

        private ProductCommonKB[] kBListField;

        private ProductCommonTool[] toolsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("KB", IsNullable = false)]
        public ProductCommonKB[] KBList
        {
            get
            {
                return this.kBListField;
            }
            set
            {
                this.kBListField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Tool", IsNullable = false)]
        public ProductCommonTool[] Tools
        {
            get
            {
                return this.toolsField;
            }
            set
            {
                this.toolsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductCommonKB
    {

        private string nameField;

        private uint valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public uint Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductCommonTool
    {

        private string nameField;

        private string pathField;

        private string descriptionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Path
        {
            get
            {
                return this.pathField;
            }
            set
            {
                this.pathField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
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
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSku
    {

        private ProductSkuPackage[] packageField;

        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Package")]
        public ProductSkuPackage[] Package
        {
            get
            {
                return this.packageField;
            }
            set
            {
                this.packageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackage
    {

        private string pathField;

        private ProductSkuPackageCommands commandsField;

        private ProductSkuPackageLogs logsField;

        private ProductSkuPackageReturnCode[] returnCodesField;

        private ProductSkuPackageVerification[] verificationsField;

        private string nameField;


        /// <remarks/>
        public string Path
        {
            get
            {
                return this.pathField;
            }
            set
            {
                this.pathField = value;
            }
        }

        /// <remarks/>
        public ProductSkuPackageCommands Commands
        {
            get
            {
                return this.commandsField;
            }
            set
            {
                this.commandsField = value;
            }
        }

        /// <remarks/>
        public ProductSkuPackageLogs Logs
        {
            get
            {
                return this.logsField;
            }
            set
            {
                this.logsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ReturnCode", IsNullable = false)]
        public ProductSkuPackageReturnCode[] ReturnCodes
        {
            get
            {
                return this.returnCodesField;
            }
            set
            {
                this.returnCodesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Verification", IsNullable = false)]
        public ProductSkuPackageVerification[] Verifications
        {
            get
            {
                return this.verificationsField;
            }
            set
            {
                this.verificationsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackageCommands
    {

        private string installCommandField;

        private string uninstallCommandField;

        private string repairCommandField;

        private string uninstallCommand_MSUField;

        /// <remarks/>
        public string InstallCommand
        {
            get
            {
                return this.installCommandField;
            }
            set
            {
                this.installCommandField = value;
            }
        }

        /// <remarks/>
        public string UninstallCommand
        {
            get
            {
                return this.uninstallCommandField;
            }
            set
            {
                this.uninstallCommandField = value;
            }
        }

        /// <remarks/>
        public string RepairCommand
        {
            get
            {
                return this.repairCommandField;
            }
            set
            {
                this.repairCommandField = value;
            }
        }

        /// <remarks/>
        public string UninstallCommand_MSU
        {
            get
            {
                return this.uninstallCommand_MSUField;
            }
            set
            {
                this.uninstallCommand_MSUField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackageLogs
    {

        private string installLogField;

        private string uninstallLogField;

        private string repairLogField;

        /// <remarks/>
        public string InstallLog
        {
            get
            {
                return this.installLogField;
            }
            set
            {
                this.installLogField = value;
            }
        }

        /// <remarks/>
        public string UninstallLog
        {
            get
            {
                return this.uninstallLogField;
            }
            set
            {
                this.uninstallLogField = value;
            }
        }

        /// <remarks/>
        public string RepairLog
        {
            get
            {
                return this.repairLogField;
            }
            set
            {
                this.repairLogField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackageReturnCode
    {

        private string nameField;

        private ushort valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public ushort Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackageVerification
    {

        private ProductSkuPackageVerificationVerificationSku[] verificationSKUsField;

        private ProductSkuPackageVerificationVerificationCommand[] verificationCommandsField;

        private string typeField;

        private string aliasField;

        private string dataFilePathField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("VerificationSku", IsNullable = false)]
        public ProductSkuPackageVerificationVerificationSku[] VerificationSKUs
        {
            get
            {
                return this.verificationSKUsField;
            }
            set
            {
                this.verificationSKUsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("VerificationCommand")]
        public ProductSkuPackageVerificationVerificationCommand[] VerificationCommands
        {
            get
            {
                return this.verificationCommandsField;
            }
            set
            {
                this.verificationCommandsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Alias
        {
            get
            {
                return this.aliasField;
            }
            set
            {
                this.aliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DataFilePath
        {
            get
            {
                return this.dataFilePathField;
            }
            set
            {
                this.dataFilePathField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackageVerificationVerificationSku
    {

        private string playloadField;

        private string osField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Playload
        {
            get
            {
                return this.playloadField;
            }
            set
            {
                this.playloadField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string OS
        {
            get
            {
                return this.osField;
            }
            set
            {
                this.osField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ProductSkuPackageVerificationVerificationCommand
    {

        private string operationField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Operation
        {
            get
            {
                return this.operationField;
            }
            set
            {
                this.operationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }
}