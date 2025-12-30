using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace Helper
{
    public class XMLHelper
    {
        public static void XmlSerializeToFile(object o, string filePath, Type type)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath is null or empty");
            }

            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                XmlSerializeInternal(file, o, type);
            }
        }

        private static void XmlSerializeInternal(FileStream file, object o, Type type)
        {
            XmlSerializer SerializerObj = new XmlSerializer(type);
            SerializerObj.Serialize(file, o);
        }

        public static T XmlDeserialize<T>(string xmlString, Encoding encoding)
        {
            if (string.IsNullOrEmpty(xmlString))
                throw new ArgumentNullException("xmlString is not set");
            if (encoding == null)
                throw new ArgumentNullException("encoding is not set");

            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(encoding.GetBytes(xmlString)))
            {
                using (StreamReader sr = new StreamReader(ms, encoding))
                {
                    return (T)mySerializer.Deserialize(sr);
                }
            }
        }

        public static T XmlDeserializeFromFile<T>(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path is null or empty");
            if (encoding == null)
                throw new ArgumentNullException("encoding is not set");

            string xml = File.ReadAllText(path, encoding);
            return XmlDeserialize<T>(xml, encoding);
        }       
    }
}
