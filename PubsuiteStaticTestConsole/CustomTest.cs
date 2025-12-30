using LoggerLibrary;
using PubsuiteStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestConsole
{
    class CustomTest
    {
#region Compare 2 updates
        //public static void RunTest()
        //{
        //    string expectUpdateID = "c98a81a5-907f-47b7-ade7-fd9449a22e6a";
        //    string actualUpdateID = "16f566f0-91c4-4a07-a6bb-073fe7206634";

        //    PubsuiteStaticTestLib.PubsuiteStaticTestLib.Compare2Updates(expectUpdateID, HandleExpectXmlContent, actualUpdateID, HandleActualXmlContent);
        //}

        //public static string HandleExpectXmlContent(string xmlContent)
        //{
        //    return xmlContent;
        //}

        //public static string HandleActualXmlContent(string xmlContent)
        //{
        //    return xmlContent.Replace(@"\\winsehotfix\hotfixes\Win2003\SP3.Custom\KB4095527\V1.001", @"\\winsehotfix\hotfixes\win2003\sp3.custom\kb4480088\v1.003")
        //                     .Replace("WindowsServer2003-KB4095527-x86-custom", "windowsserver2003-kb4480088-x86-custom")
        //                     .Replace("windowsserver2003-kb4095527-x86-custom", "windowsserver2003-kb4480088-x86-custom")
        //                     .Replace("<bar:FileVersion Path=\"\\Microsoft.NET\\Framework\\v1.1.4322\\system.security.dll\" Comparison=\"GreaterThanOrEqualTo\" Version=\"1.1.4322.2550\" Csidl=\"36\" xmlns:bar=\"http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules\" />", String.Empty)
        //                     .Replace("1.1.4322.2527", "1.1.4322.2554");
        //}
#endregion

        private List<LPAndKB> _allLPAndKBs;

        private string cbsIsinstallableRules = "<pub:IsInstallable><lar:And><bar:RegDword Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SYSTEM\\CurrentControlSet\\Control\\MUI\\UILanguages\\{0}\" Value=\"LCID\" Comparison=\"EqualTo\" Data=\"{1}\" /><cbsar:CbsPackageInstallable /></lar:And></pub:IsInstallable>";
        private string msiIsInstallableRules = "<pub:IsInstallable><lar:Or><lar:And><bar:RegDword Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SYSTEM\\CurrentControlSet\\Control\\MUI\\UILanguages\\{0}\" Value=\"LCID\" Comparison=\"EqualTo\" Data=\"{1}\" /><bar:RegValueExists Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client\" Value=\"Release\" Type=\"REG_DWORD\" /><bar:RegDword Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client\" Value=\"Release\" Comparison=\"LessThanOrEqualTo\" Data=\"528049\" /></lar:And><lar:And><bar:RegDword Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client\\{1}\" Value=\"Install\" Comparison=\"EqualTo\" Data=\"1\" /><lar:Or><bar:RegSzToVersion Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client\\{1}\" Value=\"Version\" Comparison=\"EqualTo\" Data=\"4.0.30319.0\" /><lar:And><bar:RegValueExists Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client\\{1}\" Value=\"Release\" Type=\"REG_DWORD\" /><bar:RegDword Key=\"HKEY_LOCAL_MACHINE\" Subkey=\"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client\\{1}\" Value=\"Release\" Comparison=\"LessThan\" Data=\"528049\" /></lar:And></lar:Or></lar:And></lar:Or></pub:IsInstallable>";

        public CustomTest()
        {
            BuildLPAndKBs();
        }

        #region CBS Test
        //public void RunTest(string guid)
        //{
        //    string logName = "CustomTest" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".log";
        //    StaticLogWriter.createInstance(logName);

        //    bool result = true;

        //    try
        //    {
        //        StaticLogWriter.Instance.logMessage("Retriving update object: " + guid);

        //        Update update = PubsuiteStaticTestLib.PubsuiteStaticTestLib.GetUpdateByID(guid);

        //        StaticLogWriter.Instance.logMessage("Successfully retrived update object: ");

        //        foreach (Update childUpdate in update.ChildUpdates)
        //        {
        //            string title = childUpdate.Title;

        //            StaticLogWriter.Instance.logMessage("\r\n");
        //            StaticLogWriter.Instance.logMessage("Verifying " + title + "...");

        //            string[] temp = title.Split(new char[] { '-' });
        //            int kb = 0;

        //            foreach (string s in temp)
        //            {
        //                if (s.StartsWith("kb"))
        //                {
        //                    kb = Convert.ToInt32(s.Substring(2));
        //                    break;
        //                }
        //            }

        //            if (kb == 0)
        //            {
        //                StaticLogWriter.Instance.logError("Failed to locate KB number");
        //                result = false;
        //            }
        //            else
        //            {
        //                StaticLogWriter.Instance.logMessage("KB = " + kb.ToString());

        //                LPAndKB lpInfo = _allLPAndKBs.Where(p => p.Srv2K12KB == kb || p.BlueKB == kb || p.Win10AKB == kb || p.Win10BKB == kb).SingleOrDefault();
        //                if (lpInfo == null)
        //                {
        //                    StaticLogWriter.Instance.logError("Failed to find matched LP info");
        //                    result = false;
        //                }
        //                else
        //                {
        //                    StaticLogWriter.Instance.logMessage("Found matched LP info: " + lpInfo.Language);

        //                    string expectedRules = String.Format(cbsIsinstallableRules, lpInfo.LgAndRg, lpInfo.LCID);
        //                    string actualRules = childUpdate.IsInstallableRules.OuterXml
        //                        .Replace(" xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\"", String.Empty)
        //                        .Replace(" xmlns:lar=\"http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules\"", String.Empty)
        //                        .Replace(" xmlns:bar=\"http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules\"", String.Empty)
        //                        .Replace(" xmlns:cbsar=\"http://schemas.microsoft.com/msus/2002/12/CbsApplicabilityRules\"", String.Empty);

        //                    if(!expectedRules.Equals(actualRules))
        //                    {
        //                        StaticLogWriter.Instance.logError("Mismatch found");
        //                        StaticLogWriter.Instance.logMessage("Expect: ");
        //                        StaticLogWriter.Instance.logMessage(expectedRules);
        //                        StaticLogWriter.Instance.logMessage("Actual: ");
        //                        StaticLogWriter.Instance.logMessage(actualRules);
        //                        result = false;
        //                    }
        //                    else
        //                    {
        //                        StaticLogWriter.Instance.logMessage("Verification PASS");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        StaticLogWriter.Instance.logError("Exception caught: " + ex.Message);
        //        StaticLogWriter.Instance.logMessage(ex.StackTrace);
        //    }

        //    StaticLogWriter.Instance.logMessage("\r\n");
        //    StaticLogWriter.Instance.logMessage("Overall Result --> " + (result ? "Pass" : "Fail"));

        //    StaticLogWriter.Instance.close();
        //}
        #endregion

#region MSI Test
        public void RunTest(string guid)
        {
            string logName = "CustomTest" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".log";
            StaticLogWriter.createInstance(logName);

            bool result = true;

            try
            {
                StaticLogWriter.Instance.logMessage("Retriving update object: " + guid);

                Update update = PubsuiteStaticTestLib.PubsuiteStaticTestLib.GetUpdateByID(guid);

                StaticLogWriter.Instance.logMessage("Successfully retrived update object: ");

                foreach (Update childUpdate in update.ChildUpdates)
                {
                    string title = childUpdate.Title;

                    StaticLogWriter.Instance.logMessage("\r\n");
                    StaticLogWriter.Instance.logMessage("Verifying " + title + "...");

                    string[] temp = title.Split(new char[] { '-' });
                    string language = temp.Last().ToUpperInvariant();

                    StaticLogWriter.Instance.logMessage("Language = " + language);

                    LPAndKB lpInfo = _allLPAndKBs.Where(p => p.Language == language).SingleOrDefault();
                    if (lpInfo == null)
                    {
                        StaticLogWriter.Instance.logError("Failed to find matched LP info");
                        result = false;
                    }
                    else
                    {
                        StaticLogWriter.Instance.logMessage("Found matched LP info: " + lpInfo.Language);

                        string expectedRules = String.Format(msiIsInstallableRules, lpInfo.LgAndRg, lpInfo.LCID);
                        string actualRules = childUpdate.IsInstallableRules.OuterXml
                            .Replace(" xmlns:pub=\"http://schemas.microsoft.com/msus/2002/12/Publishing\"", String.Empty)
                            .Replace(" xmlns:lar=\"http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules\"", String.Empty)
                            .Replace(" xmlns:bar=\"http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules\"", String.Empty)
                            .Replace(" xmlns:cbsar=\"http://schemas.microsoft.com/msus/2002/12/CbsApplicabilityRules\"", String.Empty);

                        if (!expectedRules.Equals(actualRules))
                        {
                            StaticLogWriter.Instance.logError("Mismatch found");
                            StaticLogWriter.Instance.logMessage("Expect: ");
                            StaticLogWriter.Instance.logMessage(expectedRules);
                            StaticLogWriter.Instance.logMessage("Actual: ");
                            StaticLogWriter.Instance.logMessage(actualRules);
                            result = false;
                        }
                        else
                        {
                            StaticLogWriter.Instance.logMessage("Verification PASS");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StaticLogWriter.Instance.logError("Exception caught: " + ex.Message);
                StaticLogWriter.Instance.logMessage(ex.StackTrace);
            }

            StaticLogWriter.Instance.logMessage("\r\n");
            StaticLogWriter.Instance.logMessage("Overall Result --> " + (result ? "Pass" : "Fail"));

            StaticLogWriter.Instance.close();
        }
#endregion

        private void BuildLPAndKBs()
        {
            int srv2k12base = 4486081;
            int bluebase = 4486105;
            int win10abase = 4486129;
            int win10bbase = 4486153;

            int i = 1;
            _allLPAndKBs = new List<LPAndKB>();

            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "ARA",
                LgAndRg = "ar-SA",
                LCID = 1025,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;

            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "CHS",
                LgAndRg = "zh-CN",
                LCID = 2052,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "CHT",
                LgAndRg = "zh-TW",
                LCID = 1028,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "CSY",
                LgAndRg = "cs-CZ",
                LCID = 1029,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "DAN",
                LgAndRg = "da-DK",
                LCID = 1030,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "DEU",
                LgAndRg = "de-DE",
                LCID = 1031,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "ELL",
                LgAndRg = "el-GR",
                LCID = 1032,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "ESN",
                LgAndRg = "es-ES",
                LCID = 3082,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "FIN",
                LgAndRg = "fi-FI",
                LCID = 1035,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "FRA",
                LgAndRg = "fr-FR",
                LCID = 1036,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "HEB",
                LgAndRg = "he-IL",
                LCID = 1037,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "HUN",
                LgAndRg = "hu-HU",
                LCID = 1038,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "ITA",
                LgAndRg = "it-IT",
                LCID = 1040,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "JPN",
                LgAndRg = "ja-JP",
                LCID = 1041,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "KOR",
                LgAndRg = "ko-KR",
                LCID = 1042,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "NLD",
                LgAndRg = "nl-NL",
                LCID = 1043,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "NOR",
                LgAndRg = "nb-NO",
                LCID = 1044,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "PLK",
                LgAndRg = "pl-PL",
                LCID = 1045,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "PTB",
                LgAndRg = "pt-BR",
                LCID = 1046,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "PTG",
                LgAndRg = "pt-PT",
                LCID = 2070,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "RUS",
                LgAndRg = "ru-RU",
                LCID = 1049,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "SVE",
                LgAndRg = "sv-SE",
                LCID = 1053,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });

            ++i;
            _allLPAndKBs.Add(new LPAndKB()
            {
                Language = "TRK",
                LgAndRg = "tr-TR",
                LCID = 1055,
                Srv2K12KB = srv2k12base + i,
                BlueKB = bluebase + i,
                Win10AKB = win10abase + i,
                Win10BKB = win10bbase + i
            });
        }
    }

    class LPAndKB
    {
        public string Language;
        public string LgAndRg;
        public int LCID;

        public int Srv2K12KB;
        public int BlueKB;
        public int Win10AKB;
        public int Win10BKB;
    }
}
