using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace THTestLib
{
    public class MetadataFileNode : IEquatable<MetadataFileNode>
    {
        public string AssemblyName { get; set; }
        public string ComponentVersion { get; set; }
        public string FileName { get; set; }
        public string ImportPath { get; set; }
        public string ProcessorArchitecture { get; set; }
        public string FileVersion { get; set; }

        public bool Equals(MetadataFileNode other)
        {
            return this.FileName.Equals(other.FileName, StringComparison.InvariantCultureIgnoreCase) &&
                this.ImportPath.Equals(other.ImportPath, StringComparison.InvariantCultureIgnoreCase) &&
                this.ProcessorArchitecture.Equals(other.ProcessorArchitecture, StringComparison.InvariantCultureIgnoreCase) &&
                this.AssemblyName.Equals(other.AssemblyName);
        }
    }

    public class PackageMetadataReader
    {
        public static List<MetadataFileNode> ReadPackageMetadata(string path, string tfsSku, string ExtractLocation)
        {
            string compVerPrefix = tfsSku[0] < '4' ? "10.0" : "4.0";

            List<MetadataFileNode> allFiles = ReadPackageMetadata(path, ExtractLocation);

            return allFiles.Where(f => f.ComponentVersion.StartsWith(compVerPrefix)).ToList();
        }

        public static List<MetadataFileNode> ReadPackageMetadata(string metadataFilePath, string ExtractLocation)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(metadataFilePath);

            string xpath = "//file[contains(@importPath, 'netfx') or contains(@importPath, 'Microsoft.NET') or contains(@importPath, '\\Framework') or contains(@importPath, '$(build.nttree)\\wwf\\PFiles')]";

            XmlNodeList xmlNodes = xmlDoc.SelectNodes(xpath);
           
            List<MetadataFileNode> result = new List<MetadataFileNode>();
            foreach (XmlNode node in xmlNodes)
            {
                if (node.ParentNode.Attributes["pruned"].Value == "true")
                    continue;
                //skip resource files
                if (node.Attributes["importPath"].Value.Contains("\\loc\\") ||
                    node.Attributes["name"].Value.ToLowerInvariant().Contains(".resources."))
                    continue;
                
                MetadataFileNode fileNode = new MetadataFileNode();
                fileNode.FileName = node.Attributes["name"].Value.ToLowerInvariant();
                fileNode.ImportPath = node.Attributes["importPath"].Value;
                fileNode.ProcessorArchitecture = node.ParentNode.Attributes["processorArchitecture"].Value;

                XmlNode assemblyIdentityNode = node.ParentNode;
                fileNode.ComponentVersion = assemblyIdentityNode.Attributes["version"].Value;
                fileNode.AssemblyName = assemblyIdentityNode.Attributes["name"].Value;

                XmlNode cgdrComponent = assemblyIdentityNode.SelectSingleNode("cgdrComponent");
                //if(cgdrComponent == null)
                //    cgdrComponent = assemblyIdentityNode.SelectSingleNode("lkgComponent");

                if (cgdrComponent != null)
                {
                    string filePath = null;
                    string componentRootDirectory = cgdrComponent.Attributes["componentRootDirectory"].Value;
                    if (componentRootDirectory.StartsWith(@"\\winsehotfix"))
                    {
                        filePath = Path.Combine(ExtractLocation, Path.GetFileName(componentRootDirectory), fileNode.FileName);
                    }
                    else
                    {
                        filePath = System.IO.Path.Combine(componentRootDirectory, fileNode.FileName);
                    }
                    if (File.Exists(filePath))
                    {
                        fileNode.FileVersion = HelperMethods.GetFileVersionString(filePath);
                    }
                }

                result.Add(fileNode);
            }

            return result;
        }
    }
}
