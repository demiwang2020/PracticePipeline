using System.Collections.Generic;
namespace NetFxSetupLibrary
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class InputData
    {

        private List<InputDataItem> dataField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Data")]
        public List<InputDataItem> Data
        {
            get
            {
                return this.dataField;
            }
            set
            {
                this.dataField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class InputDataItem
    {

        private string fieldNameField;

        private string fieldValueField;

        private string fieldTypeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FieldName
        {
            get
            {
                return this.fieldNameField;
            }
            set
            {
                this.fieldNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FieldValue
        {
            get
            {
                return this.fieldValueField;
            }
            set
            {
                this.fieldValueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FieldType
        {
            get
            {
                return this.fieldTypeField;
            }
            set
            {
                this.fieldTypeField = value;
            }
        }
    }


    public class VerificationFilePath
    {
        public string RegistryFilePath { get; set; }
        public Dictionary<string,string> VersionFilePath { get; set; }

        public string LCUInstallScriptPath { get; set; }
    }
}
