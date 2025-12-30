using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NETCoreMUStaticLib
{
    public class XmlHelper
    {
        public static void TryGetBooleanAttribute(XmlNode node, string attrName, ref bool value)
        {
            if (node.Attributes[attrName] != null)
            {
                bool b;
                if (Boolean.TryParse(node.Attributes[attrName].Value, out b))
                {
                    value = b;
                }
            }
        }

        public static void TryGetStringAttribute(XmlNode node, string attrName, ref string value)
        {
            if (node.Attributes[attrName] != null)
            {
                value = node.Attributes[attrName].Value;
            }
        }

        /// <summary>
        /// A simple method to compare XML nodes
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        public static bool CompareNodes(XmlNode node1, XmlNode node2, bool ignoreCaseForValues = false)
        {
            if (node1 == null || node2 == null)
                throw new Exception("Trying to compare empty XML nodes");

            if (node1.OuterXml == node2.OuterXml)
                return true;

            //1. compare attributes
            if (CountNonNameSpaceAttr(node1) != CountNonNameSpaceAttr(node2))
                return false;

            if (node1.Attributes.Count > 0)
            {
                foreach (XmlAttribute attr in node1.Attributes)
                {
                    // skip namespace declare
                    if (IsNameSpaceAttrName(attr.Name))
                        continue;
                    
                    //attr is missing
                    if (node2.Attributes[attr.Name] == null)
                        return false;

                    //attr value is different
                    //if (!node2.Attributes[attr.Name].Value.Equals(attr.Value))
                    //    return false;
                    if (String.Compare(node2.Attributes[attr.Name].Value, attr.Value, ignoreCaseForValues) != 0)
                        return false;
                }
            }

            //2. compare inner text
            bool bInnerText1 = String.IsNullOrEmpty(node1.InnerText);
            bool bInnerText2 = String.IsNullOrEmpty(node2.InnerText);
            if (bInnerText1 != bInnerText2)
                return false;

            if(!bInnerText1)
            {
                if (String.Compare(node1.InnerText, node2.InnerText, ignoreCaseForValues) != 0)
                    return false;
            }

            //3. Compare child nodes
            if (CountNormalChildNodes(node1) != CountNormalChildNodes(node2))
                return false;

            foreach (XmlNode childNode1 in node1.ChildNodes)
            {
                if (childNode1.NodeType == XmlNodeType.Comment)
                    continue;

                bool bFind = false;
                foreach (XmlNode childNode2 in node2.ChildNodes)
                {
                    bFind = CompareNodes(childNode1, childNode2, ignoreCaseForValues);
                    if (bFind)
                        break;
                }

                if (!bFind)
                    return false;
            }

            return true;
        }

        private static int CountNonNameSpaceAttr(XmlNode node)
        { 
            int count = node.Attributes.Count;

            foreach (XmlAttribute attr in node.Attributes)
            {
                if (IsNameSpaceAttrName(attr.Name))
                    --count;
            }

            return count;
        }

        private static bool IsNameSpaceAttrName(string name)
        {
            return name.StartsWith("xmlns:");
        }

        private static int CountNormalChildNodes(XmlNode parentNode)
        {
            int count = 0;
            foreach (XmlNode node in parentNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Comment)
                    ++count;
            }

            return count;
        }
    }
}
