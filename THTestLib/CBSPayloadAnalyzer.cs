using Helper;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Test.DevDiv.SAFX.CommonLibraries.CBSAnalyzerLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib
{
    class CBSPayloadAnalyzer
    {
        /// <summary>
        /// Collect all .NET binaries info into a DataTable
        /// </summary>
        /// <returns>A DateTable that stores payload information</returns>
        public static DataTable GetPatchDotNetBinaries(THTestObject testObj, string extractLocation, Architecture arch)
        {
            CBSManifestAnalyzer analyzer = new CBSManifestAnalyzer();
            analyzer.ExtractionPath = extractLocation;
            analyzer.RunAnalyze();

            DataTable table = HelperMethods.CreateDataTable(String.Empty,
                new string[] { "FileName", "DestinationName", "ComponentVersion", "Version", "ProcessorArchitecture", "SKU", "DestPath", "ExtractPath", "InExpectFileList", "Size", "LastModifiedDate", "Links" });

            foreach (CBSManifest manifest in analyzer.Assemblies)
            {
                
                if (!CBSContentHelper.IsDotNetManifest(manifest))
                    continue;

                if (manifest.Files != null && manifest.Files.Count > 0)
                {
                    
                    foreach (FileItem item in manifest.Files)
                    {
                        
                        string name = item.Name.ToLower();
                        if (name == "vbc.exe")
                        {

                            Console.WriteLine();
                        }
                        //skip resource files
                        if (name.Contains("resources.") || name.Equals("vbc7ui.dll", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        //Try to detect which SKU the file belongs, first from file version, then from component version
                        string fileVersion = HelperMethods.GetFileVersionString(Path.Combine(extractLocation, manifest.Name, item.Name));
                        string sku = String.Empty;
                        if (!String.IsNullOrEmpty(fileVersion))
                        {
                            sku = HelperMethods.GetBinarySKUFromFileVersion(testObj, fileVersion);
                        }
                        if (String.IsNullOrEmpty(sku)) 
                        {
                            sku = HelperMethods.GetBinarySKUFromComponentVersion(testObj, item.Name, manifest.Identity.Version, item.DestinationPath);                           
                        }
                        if (!String.IsNullOrEmpty(item.DestinationPath))
                        {
                            if ((sku.Equals("2.0") && item.DestinationPath.Contains("v3.5")) || (sku.Equals("3.5") && item.DestinationPath.Contains("v2.0")))
                            {
                                continue;
                            }
                        }
                        
                        if((sku.Equals("2.0") && fileVersion.StartsWith("9.0")) || (sku.Equals("3.5") || sku.Equals("3.0")) && fileVersion.StartsWith("8.0"))
                        {
                            continue;
                        }
                        //skip the files that version is in different version (for example, if sku is 3.0, the version should be start with 3.0 )
                        //if (sku == "3.0" && !fileVersion.StartsWith(sku))
                        //    continue;
                        //if (sku == "2.0" && !fileVersion.StartsWith(sku))
                        //    continue;
                        //string otherSKU = " ";
                        //if (testObj.TFSItem.SKU.StartsWith("2") || testObj.TFSItem.SKU.StartsWith("3"))
                        //{
                        //    otherSKU = "4";
                        //}
                        //else
                        //{
                        //    otherSKU = "2";
                        //}

                        //if (sku!=testObj.TFSItem.SKU && !sku.StartsWith(otherSKU))
                        //    if (sku != testObj.TFSItem.SKU && !sku.StartsWith("3"))
                        //        continue;
                        //if (IsBinaryInExpectFiles(item.Name.ToLowerInvariant(), sku))
                        {
                            DataRow row = table.NewRow();
                            row["FileName"] = name;
                            row["DestinationName"] = String.IsNullOrEmpty(item.DestinationName) ? name : item.DestinationName.ToLower();
                            row["Version"] = fileVersion;
                            row["ComponentVersion"] = manifest.Identity.Version;
                            row["ProcessorArchitecture"] = manifest.Identity.ProcessorAchitecture;
                            row["SKU"] = sku;
                            row["DestPath"] = item.DestinationPath;
                            row["ExtractPath"] = Path.Combine(extractLocation, manifest.Name, item.Name);
                            row["InExpectFileList"] = IsBinaryInExpectFiles(testObj, row["DestinationName"].ToString(), sku) ? "1" : "0";
                            row["Links"] = item.Links;
                            //when file directory length bigger than 255, needs to add '\\?\' before path
                            if (row["ExtractPath"].ToString().Length > 255 && !row["ExtractPath"].ToString().StartsWith(@"\\"))
                                row["ExtractPath"] = @"\\?\" + row["ExtractPath"].ToString();
                            FileInfo fi = new FileInfo(row["ExtractPath"].ToString());
                            row["Size"] = fi.Length;
                            row["LastModifiedDate"] = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                            table.Rows.Add(row);
                        }
                    }
                }
            }

            return table;
        }

        public static DataTable GetPatchDotNetBinariesForUpgradePackage(THTestObject testObj,String Sku, string extractLocation, Architecture arch, Dictionary<string, Dictionary<string, string>> ExpectedBinaries, WorkItem workItem)
        {
            CBSManifestAnalyzer analyzer = new CBSManifestAnalyzer();
            analyzer.ExtractionPath = extractLocation;
            analyzer.RunAnalyze();

            DataTable table = HelperMethods.CreateDataTable(String.Empty,
                new string[] { "FileName", "DestinationName", "ComponentVersion", "Version", "ProcessorArchitecture", "SKU", "DestPath", "ExtractPath", "InExpectFileList", "Size", "LastModifiedDate", "Links" });

            foreach (CBSManifest manifest in analyzer.Assemblies)
            {

                if (!CBSContentHelper.IsDotNetManifest(manifest))
                    continue;

                if (manifest.Files != null && manifest.Files.Count > 0)
                {

                    foreach (FileItem item in manifest.Files)
                    {

                        string name = item.Name.ToLower();
                        if (name == "vbc.exe")
                        {

                            Console.WriteLine();
                        }
                        //skip resource files
                        if (name.Contains("resources.") || name.Equals("vbc7ui.dll", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        //Try to detect which SKU the file belongs, first from file version, then from component version
                        string fileVersion = HelperMethods.GetFileVersionString(Path.Combine(extractLocation, manifest.Name, item.Name));
                        string sku = String.Empty;
                        if (!String.IsNullOrEmpty(fileVersion))
                        {
                            sku = HelperMethods.GetBinarySKUFromFileVersionForUpgradePackage(Sku, fileVersion);
                        }
                        if (String.IsNullOrEmpty(sku))
                        {
                            sku = HelperMethods.GetBinarySKUFromComponentVersionForUpgradePackage( item.Name, manifest.Identity.Version, item.DestinationPath,ExpectedBinaries, workItem,Sku);
                        }
                        if (!String.IsNullOrEmpty(item.DestinationPath))
                        {
                            if ((sku.Equals("2.0") && item.DestinationPath.Contains("v3.5")) || (sku.Equals("3.5") && item.DestinationPath.Contains("v2.0")))
                            {
                                continue;
                            }
                        }

                        if ((sku.Equals("2.0") && fileVersion.StartsWith("9.0")) || (sku.Equals("3.5") || sku.Equals("3.0")) && fileVersion.StartsWith("8.0"))
                        {
                            continue;
                        }
                        //skip the files that version is in different version (for example, if sku is 3.0, the version should be start with 3.0 )
                        //if (sku == "3.0" && !fileVersion.StartsWith(sku))
                        //    continue;
                        //if (sku == "2.0" && !fileVersion.StartsWith(sku))
                        //    continue;
                        //string otherSKU = " ";
                        //if (testObj.TFSItem.SKU.StartsWith("2") || testObj.TFSItem.SKU.StartsWith("3"))
                        //{
                        //    otherSKU = "4";
                        //}
                        //else
                        //{
                        //    otherSKU = "2";
                        //}

                        //if (sku!=testObj.TFSItem.SKU && !sku.StartsWith(otherSKU))
                        //    if (sku != testObj.TFSItem.SKU && !sku.StartsWith("3"))
                        //        continue;
                        //if (IsBinaryInExpectFiles(item.Name.ToLowerInvariant(), sku))
                        {
                            DataRow row = table.NewRow();
                            row["FileName"] = name;
                            row["DestinationName"] = String.IsNullOrEmpty(item.DestinationName) ? name : item.DestinationName.ToLower();
                            row["Version"] = fileVersion;
                            row["ComponentVersion"] = manifest.Identity.Version;
                            row["ProcessorArchitecture"] = manifest.Identity.ProcessorAchitecture;
                            row["SKU"] = sku;
                            row["DestPath"] = item.DestinationPath;
                            row["ExtractPath"] = Path.Combine(extractLocation, manifest.Name, item.Name);
                            row["InExpectFileList"] = IsBinaryInExpectFilesForUpgradePackage(workItem, row["DestinationName"].ToString(), sku,ExpectedBinaries) ? "1" : "0";
                            row["Links"] = item.Links;
                            //when file directory length bigger than 255, needs to add '\\?\' before path
                            if (row["ExtractPath"].ToString().Length > 255 && !row["ExtractPath"].ToString().StartsWith(@"\\"))
                                row["ExtractPath"] = @"\\?\" + row["ExtractPath"].ToString();
                            FileInfo fi = new FileInfo(row["ExtractPath"].ToString());
                            row["Size"] = fi.Length;
                            row["LastModifiedDate"] = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                            table.Rows.Add(row);
                        }
                    }
                }
            }

            return table;
        }

        private static bool IsBinaryInExpectFiles(THTestObject testObj, string fileName, string sku)
        {
            if (testObj.ExpectedBinariesVersions.ContainsKey(sku) && testObj.ExpectedBinariesVersions[sku].ContainsKey(fileName))
            {
                return true;
            }
            
            try
            {
                if (testObj.ExpectedBinariesVersions[sku].ContainsKey("presentationcore.dll") && fileName == "presentationfontcache.exe.config")
                {

                    return true;
                }
            }
            catch (KeyNotFoundException e)
            {
            }
            return false;
        }

        private static bool IsBinaryInExpectFilesForUpgradePackage( WorkItem wi, string fileName, string sku, Dictionary<string, Dictionary<string, string>> ExpectedBinaries)
        {
            if (ExpectedBinaries.ContainsKey(sku) && ExpectedBinaries[sku].ContainsKey(fileName))
            {
                return true;
            }

            try
            {
                if (ExpectedBinaries[sku].ContainsKey("presentationcore.dll") && fileName == "presentationfontcache.exe.config")
                {

                    return true;
                }
            }
            catch (KeyNotFoundException e)
            {
            }
            return false;
        }
    }
}
