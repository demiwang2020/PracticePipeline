using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using System.Configuration;
using Helper;
using System.Collections.Concurrent;
using System.Threading;

namespace WUTestManagerLib
{
    public class WUTestManagerLib
    {
        private readonly static string parameterFileRootPath = ConfigurationManager.AppSettings["WUParameterFileRootPath"].ToString();
        private readonly static string VVPATH = ConfigurationManager.AppSettings["VVPATH"].ToString();

        public static string TFSServerURI 
        {
            get { return "https://vstfdevdiv.corp.microsoft.com/DevDiv/"; }
        }

        public static List<ExcelData> ReadExcel(string file)
        {
            List<ExcelData> dataList = new List<ExcelData>();
            try
            {
                var excelWorkbook = new XLWorkbook(file);
                var excelWorksheet = excelWorkbook.Worksheet("Sheet1");

                //A1,A1 = ID column
                //B1,B1 = KB column
                //C1,C1 = Product Layer column
                //D1,D1 = Title column
                //E1,E1 = RTW Bundle GUID column
                //F1,F1 = Superseded KB column
                //G1,G1 = Other properties column
                int row = 2;
                string previousid = string.Empty;
                string previousproductlayer = string.Empty;
                while (true)
                {
                    string cellblock = String.Format("{0}{1}", "B", row);
                    string KB = GetStringValueFromCell(excelWorksheet, cellblock);
                    //We use the presense of a KB value to keep reading through the spreadsheet
                    if (String.IsNullOrEmpty(KB))
                        break;

                    cellblock = String.Format("{0}{1}", "A", row);
                    string ID = GetStringValueFromCell(excelWorksheet, cellblock);
                    //If the ID value is null, use the previously read value
                    if (String.IsNullOrEmpty(ID))
                        ID = previousid;
                    else
                        previousid = ID;

                    cellblock = String.Format("{0}{1}", "C", row);
                    string productlayer = GetStringValueFromCell(excelWorksheet, cellblock);
                    //If the product layer value is null, use the previously read value
                    if (String.IsNullOrEmpty(productlayer))
                        productlayer = previousproductlayer;
                    else
                        previousproductlayer = productlayer;

                    cellblock = String.Format("{0}{1}", "D", row);
                    string title = GetStringValueFromCell(excelWorksheet, cellblock);

                    cellblock = String.Format("{0}{1}", "E", row);
                    string guid = GetStringValueFromCell(excelWorksheet, cellblock);

                    cellblock = String.Format("{0}{1}", "F", row);
                    string ss = GetStringValueFromCell(excelWorksheet, cellblock);

                    cellblock = String.Format("{0}{1}", "I", row);
                    string sp = GetStringValueFromCell(excelWorksheet, cellblock);

                    cellblock = String.Format("{0}{1}", "H", row);
                    Boolean isCatalogOnly = GetValueFromCell(excelWorksheet, cellblock);

                    cellblock = String.Format("{0}{1}", "G", row);
                    string otherProperties = GetStringValueFromCell(excelWorksheet, cellblock);

                    dataList.Add(new ExcelData() { TFSID = ID, KB = KB, ProductLayer = productlayer, ShipChannels = sp, SSKBs = ss, Title = title, GUID = guid, IsCatalogOnly=isCatalogOnly,OtherProperties = otherProperties });
                    row++;
                }

                ReleaseObject(excelWorkbook);
                ReleaseObject(excelWorksheet);

                return dataList;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + "\r\n" + e.StackTrace);
            }
        }

        public static Dictionary<string, KBGroup> DataAggregator(List<ExcelData> data)
        {
            Dictionary<string, KBGroup> kbgroupsdic = new Dictionary<string, KBGroup>();
            try
            {
                foreach (var item in data)
                {
                    if (!kbgroupsdic.ContainsKey(item.KB))
                    {
                        KBGroup group = new KBGroup(item.KB);
                        kbgroupsdic.Add(item.KB, group);
                    }
                    kbgroupsdic[item.KB].GroupKBs.Add(new KBToTest(item.TFSID, item.KB, item.ProductLayer, item.Title, item.GUID, item.SSKBs, item.OtherProperties));
                }
                return kbgroupsdic;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + "\r\n" + e.StackTrace);
            }
        }

        private static string GetStringValueFromCell(IXLWorksheet worksheet, string cellblock)
        {
            object value = worksheet.Cell(cellblock).Value;
            if (value != null)
                return value.ToString().Trim();
            return null;
        }
        //private static bool GetValueFromCell(IXLWorksheet worksheet, string cellblock)
        //{
        //    var value = worksheet.Cell(cellblock).Value;
        //    return Convert.ToBoolean(value);
        //    //return (bool)value;
        //}
        private static bool GetValueFromCell(IXLWorksheet worksheet, string cellblock) {
            var cell = worksheet.Cell(cellblock);
            if (cell == null || cell.IsEmpty())
                return false;

            try {
                // Prefer strongly-typed access provided by ClosedXML
                if (cell.DataType == XLDataType.Boolean)
                    return cell.GetBoolean();

                if (cell.DataType == XLDataType.Number) {
                    // Treat any non-zero number as true
                    return cell.GetDouble() != 0;
                }

                // Strings: accept common truthy/falsey values
                var text = cell.GetString()?.Trim();
                if (String.IsNullOrEmpty(text))
                    return false;

                if (Boolean.TryParse(text, out bool parsedBool))
                    return parsedBool;

                if (Int32.TryParse(text, out int parsedInt))
                    return parsedInt != 0;

                var lower = text.ToLowerInvariant();
                if (lower == "yes" || lower == "y" || lower == "true" || lower == "1")
                    return true;
                if (lower == "no" || lower == "n" || lower == "false" || lower == "0")
                    return false;

                // If cell contains a date or other type, do not throw — return false (or change to desired default)
                return false;
            }
            catch {
                // Defensive: if any unexpected conversion error occurs, return false.
                return false;
            }
        }

        private static void ReleaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception e)
            {
                obj = null;
            }
            finally
            {
                GC.Collect();
            }
        }

        public static string CreateBasicIURFile(KBToTest kbtotest, SubKBInfo subkb, string parameterFilePath)
        {
            try
            {
                string path = Path.GetDirectoryName(parameterFilePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                
                if (File.Exists(parameterFilePath))
                    File.Delete(parameterFilePath);

                using (StreamWriter writer = new StreamWriter(parameterFilePath))
                {
                    writer.WriteLine("KB1={0}", subkb.KB);
                    writer.WriteLine("Title1={0}", kbtotest.Title);
                    writer.WriteLine("KB1GUID={0}", kbtotest.GUID);
                    writer.WriteLine("VersionVerificationPath={0}", VVPATH);
                    writer.WriteLine("VersionInstallParas={0}", subkb.VerificationFilePath);

                    if (!String.IsNullOrEmpty(subkb.PatchPath))
                    {
                        writer.WriteLine("KB1Path={0}", subkb.PatchPath);
                    }

                    WriteUpdateProperties(writer, kbtotest.OtherUpdateProperties);
                }

                return parameterFilePath;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not create BasicIUR text file: {0}\r\n{1}", e.Message, e.StackTrace));
            }
        }

        public static string CreateInstallAllFile(KBToTest kbtotest, SubKBInfo subkb, string parameterFilePath)
        {
            string file = CreateBasicIURFile(kbtotest, subkb, parameterFilePath);
            using (StreamWriter sw = File.AppendText(file))
            {
                sw.WriteLine(string.Format("TargetProduct={0}", subkb.ProductLayer));
            }

            return file;
        }

        public static string CreateInstallAllFile(KBToTest kbtotest, List<SubKBInfo> downlevelKBs, SubKBInfo highlevelKB, string parameterFilePath)
        {
            string file = CreateBasicIURCrossSKUFile(kbtotest, downlevelKBs, highlevelKB, parameterFilePath);

            string content;
            using (StreamReader sr = File.OpenText(file))
            {
                content = sr.ReadToEnd();
            }

            string newContent = content.Replace("KBDownlevel", "KB1").Replace("UpdateGUID", "KB1GUID");

            using (StreamWriter sw = File.CreateText(file))
            {
                sw.Write(newContent);
                sw.WriteLine(string.Format("TargetProduct={0}", highlevelKB.ProductLayer));
            }

            return file;
        }

        public static string CreateLiveBasicIURFile(KBToTest kbtotest, SubKBInfo subkb, string parameterFilePath)
        {
            try
            {
                string path = Path.GetDirectoryName(parameterFilePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                
                if (File.Exists(parameterFilePath))
                    File.Delete(parameterFilePath);

                using (StreamWriter writer = new StreamWriter(parameterFilePath))
                {
                    //writer.WriteLine("WaitForFile={0}", Path.Combine(parameterFileRootPath, kbtotest.KB, kbtotest.ARCH.ToString(), kbtotest.KB + "go.txt"));
                    writer.WriteLine("WaitForFile={0}", Path.Combine(parameterFileRootPath, kbtotest.KB, String.Format("{0}go_{1}.txt", kbtotest.KB, kbtotest.ARCH.ToString())));
                    writer.WriteLine("KB1={0}", subkb.KB);
                    writer.WriteLine("Title1={0}", kbtotest.Title);
                    writer.WriteLine("KB1GUID={0}", kbtotest.GUID);
                    writer.WriteLine("VersionVerificationPath={0}", VVPATH);
                    writer.WriteLine("VersionInstallParas={0}", subkb.VerificationFilePath);

                    if (!String.IsNullOrEmpty(subkb.PatchPath))
                    {
                        writer.WriteLine("KB1Path={0}", subkb.PatchPath);
                    }

                    WriteUpdateProperties(writer, kbtotest.OtherUpdateProperties);
                }

                return parameterFilePath;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not create BasicIUR text file: {0}\r\n{1}", e.Message, e.StackTrace));
            }
        }

        //public static string CreateSSFile(KBToTest kbtotest, SubKBInfo subkb, string parameterFilePath)
        //{
        //    // Search in SS KB list to see if there is valid ss update for this sub KB
        //    string childSSKBString = string.Empty;
        //    string kbandguidlist = string.Empty;
        //    string ssguidlist = string.Empty;
        //    bool firsstvalue = true;
        //    string sku = subkb.ProductLayer.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
        //    foreach (SSUpdate sskb in kbtotest.SSUpdates)
        //    {
        //        foreach (SSChildUpdate ssChild in sskb.ChildUpdates)
        //        {
        //            if (ssChild.TargetDotNetSKU.Contains(sku))
        //            {
        //                if (firsstvalue)
        //                {
        //                    childSSKBString = ssChild.KBNumber;
        //                    ssguidlist = sskb.ID.ToString();
        //                    kbandguidlist = String.Format("{0};{1}", sskb.KBNumber, sskb.ID);
        //                    firsstvalue = false;
        //                }
        //                else
        //                {
        //                    childSSKBString += "," + ssChild.KBNumber;
        //                    ssguidlist += "," + sskb.ID.ToString();
        //                    kbandguidlist += String.Format(",{0};{1}", sskb.KBNumber, sskb.ID);
        //                }
        //            }
        //        }
        //    }

        //    if (String.IsNullOrEmpty(childSSKBString))
        //        return null;
            
        //    try
        //    {
        //        string path = Path.GetDirectoryName(parameterFilePath);
        //        if (!Directory.Exists(path))
        //            Directory.CreateDirectory(path);

        //        if (File.Exists(parameterFilePath))
        //            File.Delete(parameterFilePath);

        //        using (StreamWriter writer = new StreamWriter(parameterFilePath))
        //        {
        //            // Target update info
        //            writer.WriteLine("ParentTargetKB={0}", kbtotest.KB);
        //            writer.WriteLine("UpdateID={0}", kbtotest.GUID);
        //            writer.WriteLine("ChildTargetKB={0}", subkb.KB);

        //            // SS updates info
        //            writer.WriteLine("ChildSSKB={0}", childSSKBString);
        //            writer.WriteLine("KBSGUIDS={0}", ssguidlist);
        //            writer.WriteLine("SSKBAndGuidList={0}", kbandguidlist);

        //            //Extra KBs to remove, for now, this list is empty as publishing API does not support well 
        //            //for querying superseding KBs for an update which has mutiple superseding updates
        //            writer.WriteLine("KBsToRemove=");
        //            writer.WriteLine("ExtraKBGuidsToHide=");

        //            WriteUpdateProperties(writer, kbtotest.OtherUpdateProperties);
        //        }

        //        return parameterFilePath;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(String.Format("Could not create SS text file: {0}\r\n{1}", e.Message, e.StackTrace));
        //    }
        //}

        //public static string CreateSSFile(KBToTest kbtotest, string parameterFilePath)
        //{
        //    if (File.Exists(parameterFilePath))
        //        File.Delete(parameterFilePath);

        //    using (StreamWriter writer = new StreamWriter(parameterFilePath))
        //    {
        //        writer.WriteLine("KB1={0}", kbtotest.KB);
        //        writer.WriteLine("KB1GUID={0}", kbtotest.GUID);
        //        string kbsstring = string.Empty;
        //        string kbandguidlist = string.Empty;
        //        bool firsstvalue = true;
        //        foreach (SSKB sskb in kbtotest.SSKBs)
        //        {
        //            if (firsstvalue)
        //            {
        //                kbsstring += sskb.KBNumber;
        //                kbandguidlist += String.Format("{0};{1}", sskb.KBNumber, sskb.GUID);
        //                firsstvalue = false;
        //            }
        //            else
        //            {
        //                kbsstring += "," + sskb.KBNumber;
        //                kbandguidlist += String.Format(",{0};{1}", sskb.KBNumber, sskb.GUID);
        //            }
        //        }
        //        writer.WriteLine("KBS={0}", kbsstring);
        //        writer.WriteLine("KBAndGuidList={0}", kbandguidlist);
        //        string kbstoremove = string.Empty;
        //        string extrakbstohide = string.Empty;
        //        firsstvalue = true;
        //        foreach (SSKB sskb in kbtotest.ExtraKBsToHide)
        //        {
        //            if (firsstvalue)
        //            {
        //                kbstoremove += sskb.KBNumber;
        //                extrakbstohide += String.Format("{0}", sskb.GUID);
        //                firsstvalue = false;
        //            }
        //            else
        //            {
        //                kbstoremove += "," + sskb.KBNumber;
        //                extrakbstohide += String.Format(",{0}", sskb.GUID);
        //            }
        //        }
        //        writer.WriteLine("KBsToRemove={0}", kbstoremove);
        //        writer.WriteLine("ExtraKBGuidsToHide={0}", extrakbstohide);

        //        WriteUpdateProperties(writer, kbtotest.OtherUpdateProperties);
        //    }

        //    return parameterFilePath;
        //}

        public static string CreateNegativeMissingChildSKUFile(KBToTest kbtotest, SubKBInfo subkb, List<string> otherKBs, string parameterFilePath)
        {
            if(otherKBs == null || otherKBs.Count == 0)
                return null;
            
            try
            {
                string path = Path.GetDirectoryName(parameterFilePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                
                if (File.Exists(parameterFilePath))
                    File.Delete(parameterFilePath);

                using (StreamWriter writer = new StreamWriter(parameterFilePath))
                {
                    writer.WriteLine("KB1={0}", subkb.KB);
                    writer.WriteLine("Title1={0}", kbtotest.Title);
                    writer.WriteLine("KB1GUID={0}", kbtotest.GUID);
                    writer.WriteLine("VersionVerificationPath={0}", VVPATH);
                    writer.WriteLine("VersionInstallParas={0}", subkb.VerificationFilePath);
                    writer.WriteLine("ExpectedNotInstalledKBs={0}", String.Join(",", otherKBs));

                    if (!String.IsNullOrEmpty(subkb.PatchPath))
                    {
                        writer.WriteLine("KB1Path={0}", subkb.PatchPath);
                    }

                    WriteUpdateProperties(writer, kbtotest.OtherUpdateProperties);
                }

                return parameterFilePath;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not create Negative-MissingChildSKU text file: {0}\r\n{1}", e.Message, e.StackTrace));
            }
        }


        public static string CreateBasicIURCrossSKUFile(KBToTest kbtotest, List<SubKBInfo> downlevelKBs, SubKBInfo highlevelKB, string parameterFilePath)
        {
            if ((downlevelKBs == null || downlevelKBs.Count == 0) || String.IsNullOrEmpty(highlevelKB.KB))
                return null;

            try
            {
                string path = Path.GetDirectoryName(parameterFilePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                if (File.Exists(parameterFilePath))
                    File.Delete(parameterFilePath);

                using (StreamWriter writer = new StreamWriter(parameterFilePath))
                {
                    writer.WriteLine("KBDownlevel={0}", String.Join(",", downlevelKBs.Select(a=>a.KB).ToList()).Substring(0, 7));
                    writer.WriteLine("KBHighlevel={0}", highlevelKB.KB);
                    writer.WriteLine("UpdateTitle={0}", kbtotest.Title);
                    writer.WriteLine("UpdateGUID={0}", kbtotest.GUID);
                    writer.WriteLine("VersionVerificationPath={0}", VVPATH);
                    
                    //Generate a version verification file that carries all patches
                    string vvFile = Path.Combine(Path.GetDirectoryName(parameterFilePath), "CombinedFileVersions.txt");
                    if(File.Exists(vvFile))
                    {
                        File.Delete(vvFile);
                    }
                    foreach (SubKBInfo subKb in downlevelKBs)
                    {
                        CombineVersionVerificationFile(vvFile, subKb.VerificationFilePath);
                    }
                    CombineVersionVerificationFile(vvFile, highlevelKB.VerificationFilePath);

                    writer.WriteLine("VersionInstallParas={0}", vvFile);

                    if (!String.IsNullOrEmpty(highlevelKB.PatchPath))
                    {
                        writer.WriteLine("KBHighlevelPath={0}", highlevelKB.PatchPath);
                    }

                    string downlevelKBPath = null;
                    foreach(SubKBInfo downlevelKB in downlevelKBs)
                    {
                        if (!String.IsNullOrEmpty(downlevelKB.PatchPath))
                        {
                            if (String.IsNullOrEmpty(downlevelKBPath))
                                downlevelKBPath = downlevelKB.PatchPath;
                            else
                                downlevelKBPath = String.Format("{0},{1}", downlevelKBPath, downlevelKB.PatchPath);
                        }
                    }
                    if(!String.IsNullOrEmpty(downlevelKBPath))
                    {
                        writer.WriteLine("KBDownlevelPath={0}", downlevelKBPath);
                    }

                    WriteUpdateProperties(writer, kbtotest.OtherUpdateProperties);
                }

                return parameterFilePath;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not create BasicIURCrossSKU text file: {0}\r\n{1}", e.Message, e.StackTrace));
            }
        }

        public static string CreateBasicInstallMRSOFile(KBToTest kbtotest, SubKBInfo subkb, string parameterFilePath)
        {
            if (kbtotest.OtherUpdateProperties.ContainsKey("SO"))
            {
                return CreateBasicIURFile(kbtotest, subkb, parameterFilePath);
            }

            return null;
        }

        //private static void CombineVersionVerificationFile(string vvFile, string sourceVVFile) {
        //    using (var fs = new FileStream(vvFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        //    using (var writer = new StreamWriter(fs))
        //    using (var reader = new StreamReader(sourceVVFile)) {
        //        writer.WriteLine();
        //        writer.Write(reader.ReadToEnd());
        //    }
        //}
        // <snip top of file stays unchanged>
    private static readonly ConcurrentDictionary<string, object> _fileLocks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    private static void CombineVersionVerificationFile(string vvFile, string sourceVVFile) {
        // Use a process-level per-path lock to serialize writes from this process.
        // Also add a short retry loop to tolerate transient locks (other processes/AV).
        var fileLock = _fileLocks.GetOrAdd(vvFile, _ => new object());
        bool lockTaken = false;
        try {
            Monitor.TryEnter(fileLock, 30000, ref lockTaken); // wait up to 30s for process-local lock
            if (!lockTaken)
                throw new IOException($"Timed out waiting to acquire lock for '{vvFile}'.");

            const int maxAttempts = 5;
            int attempt = 0;
            while (true) {
                attempt++;
                try {
                    // Read source file with shared read to avoid conflicts if it's opened elsewhere.
                    string sourceContent;
                    using (var srcStream = new FileStream(sourceVVFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(srcStream)) {
                        sourceContent = reader.ReadToEnd();
                    }

                    // Ensure directory exists before writing
                    var dir = Path.GetDirectoryName(vvFile);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    // Append to target with a FileShare that allows readers; if another writer has exclusive lock, IOException will be thrown.
                    using (var fs = new FileStream(vvFile, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fs)) {
                        // Ensure a newline separation only when file is not empty
                        if (fs.Length > 0)
                            writer.WriteLine();
                        writer.Write(sourceContent);
                        writer.Flush();
                    }

                    break; // success
                }
                catch (IOException) when (attempt < maxAttempts) {
                    // Short exponential backoff
                    Thread.Sleep(200 * attempt);
                    continue;
                }
            }
        }
        finally {
            if (lockTaken)
                Monitor.Exit(fileLock);

            // Optionally remove the lock object to prevent growth of the dictionary
            _fileLocks.TryRemove(vvFile, out _);
        }
    }

    private static void WriteUpdateProperties(StreamWriter writer, Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0)
                return;

            foreach (KeyValuePair<string, string> kv in properties)
            {
                writer.WriteLine("{0}={1}", kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Detect whether a given dot net product is downlevel sku
        /// Downlevel SKU (2.0/3.0/3.5)
        /// Highlevel SKU (4.0/4.5/4.5.X/4.6/4.6.X and above)
        /// </summary>
        /// <param name="productLayer">Product name in format like 4.5.2 RTM MSI, 2.0 SP2 CBS</param>
        /// <returns></returns>
        public static bool IsDownlevelDotNetSKU(string productLayer)
        {
            return productLayer[0] < '4';
        }
    }
}
