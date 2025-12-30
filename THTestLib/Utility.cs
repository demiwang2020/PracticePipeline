using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib
{
    static class Utility
    {
        static Utility()
        {
            _skuDBInfo = new Dictionary<string, Tuple<int, string>>();

            //downlevel SKU could be hardcoded
            _skuDBInfo.Add("2.0", new Tuple<int, string>(1, "SP2"));
            _skuDBInfo.Add("3.0", new Tuple<int, string>(2, "SP2"));
            _skuDBInfo.Add("3.5", new Tuple<int, string>(3, "SP1"));

            //Get high level SKU name from config
            CollectHighLevelSKUs();

            // Read OS info stored in OSInfos.txt
            ReadOSInfoConfigs();
        }
        public static List<string> x86OnlyBinaries
        {
            get
            {
                if (_lstX86OnlyFiles == null)
                {
                    _lstX86OnlyFiles = GetExceptionListOnDiffArch("x86", "amd64");
                }

                return _lstX86OnlyFiles;
            }
        }

        public static List<string> x64OnlyBinaries
        {
            get
            {
                if (_lstX64OnlyFiles == null)
                {
                    _lstX64OnlyFiles = GetExceptionListOnDiffArch("amd64", "x86");
                }

                return _lstX64OnlyFiles;
            }
        }
        public static List<string> arm64OnlyBinaries
        {
            get
            {
                if (_lstArm64OnlyFiles == null)
                {
                    _lstArm64OnlyFiles = GetExceptionListOnDiffArch("arm64", "x86");
                }

                return _lstArm64OnlyFiles;
            }
        }

        public static List<string> MsilDestinationExceptions
        {
            get
            {
                if (_lstMsilDestinationExceptions == null)
                {
                    _lstMsilDestinationExceptions = GetMsilDestinationExceptions();
                }

                return _lstMsilDestinationExceptions;
            }
        }

        public static string MailCSS
        {
            get
            {
                if(String.IsNullOrEmpty(_strMailCSS))
                {
                    _strMailCSS = GetMailCSS();
                }

                return _strMailCSS;
            }
        }

        public static int GetDBProductIDFromSKU(string sku)
        {
            return _skuDBInfo.ContainsKey(sku) ? _skuDBInfo[sku].Item1 : 0;
        }

        public static string GetDBProductSPLevel(string sku)
        {
            return _skuDBInfo.ContainsKey(sku) ? _skuDBInfo[sku].Item2 : String.Empty;
        }

        public static string DetectSKUFromVersion(string fileVersion)
        {
            var result = _skuDBInfo.Keys.Where(a => fileVersion.StartsWith(a));

            return result.FirstOrDefault();
        }

        //public static string HighLevelSKU
        //{
        //    get
        //    {
        //        return _skuDBInfo.Keys.Last();
        //    }
        //}

        public static bool IsDownlevelSKU(string skuName)
        {
            return skuName[0] < '4';
        }

        public static bool IsHighlevelSKU(string skuName)
        {
            return skuName[0] > '3';
        }

        private static List<string> _lstX86OnlyFiles;
        private static List<string> _lstX64OnlyFiles;
        private static List<string> _lstArm64OnlyFiles;
        private static List<string> _lstMsilDestinationExceptions;
        private static string _strMailCSS;
        private static Dictionary<string, Tuple<int, string>> _skuDBInfo;
        private static List<Tuple<string, string, int, string>> _lstOSInfos; // OS Name, sp level, weight, displayed name
        
        /// <summary>
        /// Get exception file names.
        /// Exception files are files with only one arch in x64/ia64 patches. e.g. There is only x86 xpsviewer.exe in x64 patches.
        /// </summary>
        private static List<string> GetExceptionListOnDiffArch(string supportedArch, string unsupportedArch)
        {
            List<string> fileList = new List<string>();
            string line = String.Empty;

            string exceptionFilePath = System.Configuration.ConfigurationManager.AppSettings["Verify86PayloadSameAs64_ExceptionFilesLocation"];
            if (File.Exists(exceptionFilePath))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(exceptionFilePath))
                    {
                        while (line != null)
                        {
                            if (-1 != sr.Peek())
                            {
                                line = sr.ReadLine();
                                if (!line.StartsWith("#") && line.Contains(supportedArch) && !line.Contains(unsupportedArch))
                                    fileList.Add(line.Split(':')[0].Trim().ToLowerInvariant());
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    fileList.Clear();
                }
            }

            return fileList;
        }


        /// <summary>
        /// Get MSIL destination exceptions. 
        /// (Some MSIL files may miss destination path in manifest files, which is acceptable)
        /// </summary>
        /// <returns></returns>
        private static List<string> GetMsilDestinationExceptions()
        {
            string exceptionFilePath = System.Configuration.ConfigurationManager.AppSettings["MSILDestinationExceptions"];
            try
            {
                using (StreamReader sr = new StreamReader(exceptionFilePath))
                {
                    string content = sr.ReadToEnd();

                    return content.ToLowerInvariant().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
            catch
            {
            }

            return null;
        }

        private static string GetMailCSS()
        {
            string mailCSSPath = System.Configuration.ConfigurationManager.AppSettings["MailCSS"];
            try
            {
                using (StreamReader sr = new StreamReader(mailCSSPath))
                {
                    return sr.ReadToEnd();
                }
            }
            catch
            {
            }

            return null;
        }

        private static void CollectHighLevelSKUs()
        {
            using (PatchTestDataClassDataContext dataContext = new PatchTestDataClassDataContext())
            {
                var skusInDB = from r in dataContext.TTHTestConfigs
                               select new { Name = r.SKUName, ID = r.SKUID };

                foreach (var sku in skusInDB)
                {
                    if (!_skuDBInfo.ContainsKey(sku.Name))
                    {
                        _skuDBInfo.Add(sku.Name, new Tuple<int, string>((int)sku.ID, "RTM"));
                    }
                }
            }
        }

        private static void ReadOSInfoConfigs()
        {
            string osInfoConfigs = System.Configuration.ConfigurationManager.AppSettings["OSInfoConfigs"];
            _lstOSInfos = new List<Tuple<string, string, int, string>>();

            try
            {
                using (StreamReader sr = new StreamReader(osInfoConfigs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] temp = line.Split(new char[] { '\t' });
                        if (temp.Length > 2)
                        {
                            _lstOSInfos.Add(Tuple.Create<string, string, int, string>(temp[0], temp[1], Convert.ToInt32(temp[2]), temp.Length > 3 ? temp[3] : null));
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static Tuple<string, string, int, string> FindOSInfo(string osName, string osSPLevel)
        {
            var result = _lstOSInfos.Where(p => p.Item1 == osName && p.Item2 == osSPLevel).FirstOrDefault();

            // every OS should be supported
            if (result == null)
                throw new Exception(String.Format("{0} {1} is not found in OSInfos.txt", osName, osSPLevel));

            return result;
        }

        public static string TranslateOSName(string osName, string osSPLevel)
        {
            var result = FindOSInfo(osName, osSPLevel);

            if (!String.IsNullOrEmpty(result.Item4))
            {
                return result.Item4;
            }
            else if (result.Item2 == "RTM")
            {
                return osName;
            }
            else
            {
                return String.Format("{0} {1}", osName, osSPLevel);
            }
        }

        public static string ParsePatchGroupFromTFSTitle(string title)
        {
            int index = title.IndexOf("NDP");
            if (index < 0)
                index = title.IndexOf("NPD");
            if (index < 0)
                return title;

            return title.Substring(0, index).TrimEnd(new char[] { ' ', '-' });
        }

        public static int CompareOS(string os1, string splevel1, string os2, string splevel2)
        {
            var osInfo1 = FindOSInfo(os1, splevel1);
            var osInfo2 = FindOSInfo(os2, splevel2);

            return osInfo1.Item3 - osInfo2.Item3;
        }
    }
}
