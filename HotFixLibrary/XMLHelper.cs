using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;

namespace HotFixLibrary
{
    static class XMLHelper
    {
        public static string GetAttributeValue(XmlDocument xmlDoc, string strTagName, string strNameValue)
        {
            string strValue = string.Empty;

            bool blnFound = false;
            XmlNodeList xmlNdListParameter = xmlDoc.DocumentElement.GetElementsByTagName(strTagName);
            for (int i = 0; i < xmlNdListParameter.Count; i++)
            {
                foreach (XmlAttribute xmlAttParameter in xmlNdListParameter[i].Attributes)
                {
                    if (blnFound == true || xmlAttParameter.Value.Equals(strNameValue))
                    {
                        blnFound = true;
                        if (xmlAttParameter.Name.Equals("Value"))
                        {
                            strValue = xmlAttParameter.Value;
                            break;
                        }
                    }
                }
                if (blnFound == true)
                    break;
            }

            return strValue;
        }
    }
}
