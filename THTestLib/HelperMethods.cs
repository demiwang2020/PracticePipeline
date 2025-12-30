using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THTestLib.GoFxService;

namespace THTestLib
{
    class HelperMethods
    {
        public static string SaveTestLog(string log, string namePrefix = null)
        {
            string logBase = System.Configuration.ConfigurationManager.AppSettings["LogStorePath"];

            string name = DateTime.Now.Ticks.ToString();
            if (!String.IsNullOrEmpty(namePrefix))
            {
                name = String.Format("{0}_{1}.html", namePrefix, name);
            }

            string logPath = System.IO.Path.Combine(logBase, name);

            using (System.IO.TextWriter textWriter = new System.IO.StreamWriter(logPath, false))
            {
                textWriter.Write(log);
                textWriter.Close();
            }
            if (namePrefix != null) {

                string[] prefix = namePrefix.Split(new char[] { '_' });

                AddAttachmentToWI(int.Parse(prefix[0]), logPath);
            }
            return logPath;
        }
        public static void AddAttachmentToWI(int id, string filePath)
        {
            GoFxService.GoFxService client = new GoFxService.GoFxService();
            client.UseDefaultCredentials = true;

            client.AddAttachmentToWI(id, TFSProject.DevDivServicing, filePath, null);

        }
        //public static void UploadFileToWI(int tfsid, string filePath)
        //{

        //    string fileName = Path.GetFileName(filePath);
        //    UploadFiles upload = new UploadFiles();
        //    string TFUrl = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
        //    upload.ConnectWithDefaultCreds(TFUrl);
        //    //if (!upload.CheckIfTheFileAlreadyUploaded(tfsid, fileName))
        //    //{

        //    //    upload.AddAttachment(tfsid, filePath);
        //    //}

        //}
        public static void RobocopyFolder(string sourceDir, string destDir)
        {
            string toolPath = "Robocopy.exe";
            string args = string.Format(" \"{0}\" \"{1}\" /R:3 /E", sourceDir, destDir);

            Helper.Utility.ExecuteCommandSync(toolPath, args, -1);
        }

        public static void RobocopyFile(string filePath, string destDir)
        {
            string toolPath = "Robocopy.exe";
            string args = string.Format(" \"{0}\" \"{1}\" {2} /R:3", System.IO.Path.GetDirectoryName(filePath), destDir, System.IO.Path.GetFileName(filePath));

            Helper.Utility.ExecuteCommandSync(toolPath, args, -1);
        }

        /// <summary>
        /// Create a datatable with given name and column names
        /// </summary>
        public static DataTable CreateDataTable(string tableName, string[] columnNames, string[] properties = null)
        {
            DataTable table = new DataTable();
            table.TableName = tableName;

            foreach (string c in columnNames)
            {
                table.Columns.Add(new DataColumn(c)); 
            }

            SetTableColExtendedProperties(table, properties);

            return table;
        }

        public static void SetTableColExtendedProperties(DataTable table, string[] properties)
        {
            if (properties != null && properties.Length == table.Columns.Count)
            {
                for (int i = 0; i < properties.Length; ++i)
                {
                    if (!String.IsNullOrEmpty(properties[i]))
                    {
                        string[] prop_pairs = properties[i].Split(new char[] { '#' });
                        foreach (string s in prop_pairs)
                        {
                            string[] prop_pair = s.Split(new char[] { '=' });

                            table.Columns[i].ExtendedProperties.Add(prop_pair[0], prop_pair[1]);
                        }
                    }
                }
            }
        }

        public static string GetFileVersionString(string filePath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                if (!String.IsNullOrEmpty(versionInfo.ProductVersion))
                {
                    return String.Format("{0}.{1}.{2}.{3}", versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
                }
            }
            catch
            {
            }

            return String.Empty;
        }

        /// <summary>
        /// Detect .NET SKU from file version
        /// </summary>
        /// <returns>Sku name</returns>
        public static string GetBinarySKUFromFileVersion(THTestObject testObj, string fileVersion)
        {
            string sku = Utility.DetectSKUFromVersion(fileVersion);

            if (String.IsNullOrEmpty(sku))
            {
                //sku = testObj.TFSItem.SKU.Split(new char[] { '/' }).Last().Substring(0, 3);
                if (fileVersion.StartsWith("14.6."))
                {
                    sku = "4.6.2";
                }
                if (fileVersion.StartsWith("14.7.")) {
                    sku = "4.7.2";
                }
                if (fileVersion.StartsWith("14.8.9"))
                {
                    sku = "4.8.1";
                }
                if (fileVersion.StartsWith("14.8.4"))
                {
                    sku = "4.8";
                }
                if (fileVersion.StartsWith("12."))
                {
                    sku = "4.5.2";
                }
                if (fileVersion.StartsWith("14.29"))
                {
                    sku = "4.0";
                }
            }

            if ((sku == "4.8" && testObj.TFSItem.SKU == "4.8.1")|| fileVersion.StartsWith("4.8.9") || fileVersion.StartsWith("14.8.9"))
            {
                sku = "4.8.1";
            }
            if (sku == "4.7" && testObj.TFSItem.SKU == "4.7.2")
            {
                sku = "4.7.2";
            }
            if (sku == "4.6" && testObj.TFSItem.SKU == "4.6.2")
            {
                sku = "4.6.2";
            }

            return sku;
        }

        public static string GetBinarySKUFromFileVersionForUpgradePackage(string Sku, string fileVersion)
        {
            string sku = Utility.DetectSKUFromVersion(fileVersion);

            if (String.IsNullOrEmpty(sku))
            {
                //sku = testObj.TFSItem.SKU.Split(new char[] { '/' }).Last().Substring(0, 3);
                if (fileVersion.StartsWith("14.6."))
                {
                    sku = "4.6.2";
                }
                if (fileVersion.StartsWith("14.7."))
                {
                    sku = "4.7.2";
                }
                if (fileVersion.StartsWith("14.8.9"))
                {
                    sku = "4.8.1";
                }
                if (fileVersion.StartsWith("14.8.4"))
                {
                    sku = "4.8";
                }
            }

            if ((sku == "4.8" && Sku == "4.8.1") || fileVersion.StartsWith("4.8.9") || fileVersion.StartsWith("14.8.9"))
            {
                sku = "4.8.1";
            }
            if (sku == "4.7" && Sku == "4.7.2")
            {
                sku = "4.7.2";
            }
            if (sku == "4.6" && Sku == "4.6.2")
            {
                sku = "4.6.2";
            }

            return sku;
        }

        /// <summary>
        /// Detect SKU from component version
        /// </summary>
        /// <returns>Sku name</returns>
        public static string GetBinarySKUFromComponentVersion(THTestObject testObj, string fileName, string componentVersion,string destinationPath)
        {
            char sep = '4';
            bool bGreaterThanOrEqual = true;
            if (componentVersion.StartsWith("10.0"))
            {
                bGreaterThanOrEqual = false;
            }
            else if (componentVersion.StartsWith("6.")) //2.0/3.0/3.5 on Win7/8/8.1
            {
                bGreaterThanOrEqual = false;
            }
            
            foreach (KeyValuePair<string, Dictionary<string, string>> kvSKU in testObj.ExpectedBinariesVersions)
            {
                if ((bGreaterThanOrEqual && kvSKU.Key[0] >= sep ||
                    !bGreaterThanOrEqual && kvSKU.Key[0] < sep)
                    && kvSKU.Value.ContainsKey(fileName.ToLowerInvariant()))
                    return kvSKU.Key;
            }
            string[] pathComponent = destinationPath.Split('\\');
            foreach (string item in pathComponent)
            {
                if (item.StartsWith("v"))
                {
                    var sku=item.Substring(1, 3);
                    if (sku == "4.0") {
                        return testObj.TFSItem.SKU;
                    }
                    return sku;
                }

            }
            //if (bGreaterThanOrEqual && testObj.TFSItem.SKU[0] >= sep)
            //{
            //    return testObj.TFSItem.SKU;
            //}
            //else if (!bGreaterThanOrEqual && testObj.TFSItem.SKU[0] < sep)
            //{
            //    return testObj.TFSItem.SKU;
            //}

            return bGreaterThanOrEqual ? "4.X" : "2.0";
        }

        public static string GetBinarySKUFromComponentVersionForUpgradePackage( string fileName, string componentVersion, string destinationPath, Dictionary<string, Dictionary<string, string>> ExpectedBinaries, WorkItem wi,string Sku)
        {
            char sep = '4';
            bool bGreaterThanOrEqual = true;
            if (componentVersion.StartsWith("10.0"))
            {
                bGreaterThanOrEqual = false;
            }
            else if (componentVersion.StartsWith("6.")) //2.0/3.0/3.5 on Win7/8/8.1
            {
                bGreaterThanOrEqual = false;
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> kvSKU in ExpectedBinaries)
            {
                if ((bGreaterThanOrEqual && kvSKU.Key[0] >= sep ||
                    !bGreaterThanOrEqual && kvSKU.Key[0] < sep)
                    && kvSKU.Value.ContainsKey(fileName.ToLowerInvariant()))
                    return kvSKU.Key;
            }
            string[] pathComponent = destinationPath.Split('\\');
            foreach (string item in pathComponent)
            {
                if (item.StartsWith("v"))
                {
                    var sku = item.Substring(1, 3);
                    if (sku == "4.0")
                    {
                        return Sku;
                    }
                    return sku;
                }

            }
            //if (bGreaterThanOrEqual && testObj.TFSItem.SKU[0] >= sep)
            //{
            //    return testObj.TFSItem.SKU;
            //}
            //else if (!bGreaterThanOrEqual && testObj.TFSItem.SKU[0] < sep)
            //{
            //    return testObj.TFSItem.SKU;
            //}

            return bGreaterThanOrEqual ? "4.X" : "2.0";
        }
        public static bool LCUTestFailureIgnorable(MetadataFileNode nodeInLCU, MetadataFileNode nodeInCurPatch)
        {
            //#1 skip NetFx-AspNet-NonWow64-Shared
            if ((nodeInLCU != null) &&
                (nodeInLCU.AssemblyName == "NetFx-AspNet-NonWow64-Shared") &&
                (nodeInCurPatch != null))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Compare two versions in string
        /// </summary>
        public static int VersionCompare(string strVersion1, string strVersion2)
        {
            Version version1 = new Version(strVersion1);
            Version version2 = new Version(strVersion2);
            return version1.CompareTo(version2);
        }

        /// <summary>
        /// Verify if file version result can be warning
        /// When testing 4.X, if file version is 4.X, then fail, else warning
        /// When testing 2.0, if file version is 2.0 then fail, else warning
        /// </summary>
        /// <param name="resultTable"></param>
        /// <param name="versionColName"></param>
        /// <param name="sku"></param>
        /// <returns>True: result is warning; False: result is failing</returns>
        public static bool IsVersionResultWarning(DataTable resultTable, string versionColName, string sku)
        {
            if (sku.StartsWith("4.5"))
            {
                sku = "4.0";
            }
            else if (sku.StartsWith("4.6"))
            {
                sku = "4.6";
            }
            else if (sku.StartsWith("4.7"))
            {
                sku = "4.7";
            }
            else if (sku.StartsWith("4.8"))
            {
                sku = "4.8";
            }

            foreach (DataRow row in resultTable.Rows)
            {
                if(row[versionColName].ToString().StartsWith(sku))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
