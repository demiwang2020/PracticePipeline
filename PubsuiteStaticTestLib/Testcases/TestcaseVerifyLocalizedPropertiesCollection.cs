using Helper;
using PubsuiteStaticTestLib.DbClassContext;
using PubsuiteStaticTestLib.Model;
using PubsuiteStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Testcases
{
    /// <summary>
    /// This case verifes Localized Properties for all languages
    /// 1. Title
    ///     a. Year and Month
    ///     b. OS Names
    ///     c. Arch
    ///     d. .NET SKU names
    ///     e. KB article
    ///     f. no additional spaces
    /// 2. Description
    /// 3. UninstallNotes
    /// 4. MoreInfoUrl
    /// 5. SupportUrl
    /// </summary>

    class TestcaseVerifyLocalizedPropertiesCollection : TestcaseBase
    {
        private string _supportUrl;
        private string _moreInfoUrl;
        private bool _isSecurityRelease;

        private string _releaseYearMonth;
        private List<string> _targetNetSkus;
        private List<string> _targetOSes;
        private Architecture _targetArch;
        private string _kbArticle;

        private List<string> _lstSpecialServerOS;
        private List<string> _lstSpecialServerLanguages;
        private List<string> _lstIA64Languages;

        public TestcaseVerifyLocalizedPropertiesCollection(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Verify Localized Properties for all languages")
        {
            _supportUrl = actualUpdate.SupportUrl;
            _moreInfoUrl = actualUpdate.MoreInfoUrl;

            AnalyzeEnTitle(actualUpdate.Title);
            BuildExceptionLists();
        }

        protected override void RunTest()
        {
            Dictionary<string, UpdateLocalizedProperties> actualProperties = _actualUpdate.GetLocalizedPropertiesCollection();

            using (var db = new WUSAFXDbContext())
            {
                var dbLocalizedProperties = PickExpectedLanguages(db.TLocalizedProperties);

                // verify loc node count matches
                _result.LogMessage("Verifying Localized Properties node count...");
                if (dbLocalizedProperties.Count() != actualProperties.Count)
                {
                    GenerateFailResult("Localized Properties node count verification failed",
                        "Localized Properties node count",
                        dbLocalizedProperties.Count().ToString(),
                        actualProperties.Count.ToString());
                }
                else
                {
                    _result.LogMessage("Update carries expected node count: " + actualProperties.Count.ToString());
                }

                // verify loc properties of each language
                foreach (TLocalizedProperty dbRec in dbLocalizedProperties)
                {
                    // Skip en language because there are other 2 cases specially for en
                    // TestcaseVerifyEnLocalizedPropertiesCollection and TestcaseVerifyTitleFormat
                    if (dbRec.Name == "en")
                        continue;

                    _result.LogMessage("Verifying localized properties of " + dbRec.Name);

                    if (actualProperties.ContainsKey(dbRec.Name))
                    {
                        UpdateLocalizedProperties props = actualProperties[dbRec.Name];

                        string locTitle = PreprocessTitle(props.Title);

                        //1.a Year and Month
                        if (!locTitle.Contains(_releaseYearMonth) && 
                            !KnownFailureInReleaseMonth(dbRec.Name, _releaseYearMonth, locTitle))
                        {
                            // actual result: just print whole title for easier failure analysis
                            GenerateFailResult("Title year and month verification failed", "Title year and month of " + dbRec.Name, _releaseYearMonth, props.Title);
                        }

                        //1.b Target .NET SKU
                        string missingSku = null;
                        foreach (var sku in _targetNetSkus)
                        {
                            if (!locTitle.Contains(sku))
                            {
                                missingSku = missingSku == null ? sku : missingSku + ", " + sku;
                            }
                        }

                        if (!String.IsNullOrEmpty(missingSku) && 
                            !KnownFailureInNETSKU(dbRec.Name, missingSku, locTitle))
                        {
                            GenerateFailResult("Title .NET SKU name verification failed", "Title .NET SKU names of " + dbRec.Name, String.Join(", ", _targetNetSkus), "Missing " + missingSku);
                        }

                        // 1.c Arch
                        Architecture actualArch = (Architecture)ArchDetector.ParseArchFromLocalizedTitle(locTitle);
                        if (_targetArch != actualArch)
                        {
                            GenerateFailResult("Title Architecture verification failed", "Title Architecture of " + dbRec.Name, _targetArch.ToString(), actualArch.ToString());
                        }

                        // 1.d Target OS
                        string missingOS = null;
                        foreach (var os in _targetOSes)
                        {
                            if (!locTitle.Contains(os))
                            {
                                missingOS = missingOS == null ? os : missingOS + ", " + os;
                            }
                        }

                        if (!String.IsNullOrEmpty(missingOS))
                        {
                            GenerateFailResult("Title OS verification failed", "Title OS of " + dbRec.Name, String.Join(", ", _targetOSes), "Missing " + missingOS);
                        }

                        // 1.e KB Article
                        if (!locTitle.Contains(_kbArticle) &&
                            !KnownFailureInKBArticle(dbRec.Name, locTitle))
                        {
                            GenerateFailResult("Title KB Article verification failed", "Title KB Article of " + dbRec.Name, _kbArticle, props.Title);
                        }

                        // 1.f Verify title doesn't have additional spaces
                        if (CommonHelper.TooManyAdditionalSpaces(locTitle))
                        {
                            GenerateFailResult("Title spaces verification failed", "Title spaces of " + dbRec.Name, "No unexpected spaces", props.Title);
                        }

                        // 2. Description
                        string expectDesc = _isSecurityRelease ? dbRec.SecDescription : dbRec.NonSecDescription;
                        if (!CompareDescritpion(dbRec.Name, expectDesc, props.Description))
                        {
                            GenerateFailResult("Description verification failed", "Description of " + dbRec.Name, expectDesc, props.Description);
                        }

                        // 3. UninstallNotes
                        if (!dbRec.UninstallNotes.Equals(props.UninstallNotes))
                        {
                            GenerateFailResult("UninstallNotes verification failed", "UninstallNotes of " + dbRec.Name, dbRec.UninstallNotes, props.UninstallNotes);
                        }

                        // 4. SupportUrl
                        if (!_supportUrl.Equals(props.SupportUrl))
                        {
                            GenerateFailResult("SupportUrl verification failed", "SupportUrl of " + dbRec.Name, _supportUrl, props.SupportUrl);
                        }

                        // 5. MoreinfoUrl
                        if (!_moreInfoUrl.Equals(props.MoreInfoUrl))
                        {
                            GenerateFailResult("MoreInfoUrl verification failed", "MoreInfoUrl of " + dbRec.Name, _moreInfoUrl, props.MoreInfoUrl);
                        }
                    }
                    else
                    {
                        GenerateFailResult("Not found localized properties of language " + dbRec.Name, dbRec.Name + " localized properties", "Exist", "Missing");
                    }
                }
            }
        }

        private void AnalyzeEnTitle(string enTitle)
        {
            // release year and month
            var matches = Regex.Matches(enTitle, @"^\d{4}-\d{2}");
            if (matches != null && matches.Count > 0)
                _releaseYearMonth = matches[0].Value;
            else
                _releaseYearMonth = String.Empty;

            // .NET SKU
            int startIndex = enTitle.IndexOf(" .NET Framework ");
            int endIndex = _actualUpdate.Title.IndexOf(" on ", startIndex);
            if (endIndex < 0)
                endIndex = _actualUpdate.Title.IndexOf(" for ", startIndex);
            string skus = _actualUpdate.Title.Substring(startIndex + 16, endIndex - startIndex - 16);

            _targetNetSkus = skus.Split(new string[] { ", ", " and " }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Target OS
            _targetOSes = OSDetector.ParseAllTargetOSFromUpdateTitle(enTitle);

            // Target Arch
            _targetArch = (Architecture)ArchDetector.ParseArchFromUpdateTitle(enTitle);

            // KB Article
            _kbArticle = String.Format("(KB{0})", _actualUpdate.Properties.KBArticle);

            _isSecurityRelease = _expectedUpdate.Properties.UpdateClassification.Equals("0FA1201D-4330-4FA8-8AE9-B877473B6441", StringComparison.InvariantCultureIgnoreCase);
        }

        private void BuildExceptionLists()
        {
            _lstSpecialServerOS = OSDetector.GetServerNamesWithSpecialLocalization();

            // some servers have 18 languages 
            _lstSpecialServerLanguages = new List<string>()
            {
                "en",
                "cs",
                "de",
                "es",
                "fr",
                "hu",
                "it",
                "ja",
                "ko",
                "nl",
                "pl",
                "pt-br",
                "pt",
                "ru",
                "sv",
                "tr",
                "zh-cn",
                "zh-tw"
            };

            // IA64 has 4 languages
            _lstIA64Languages = new List<string>()
            {
                "en",
                "de",
                "fr",
                "ja"
            };
        }

        private List<TLocalizedProperty> PickExpectedLanguages(DbSet<TLocalizedProperty> localizedProperties)
        {
            // IA64, 4 languages
            if (_targetArch == Architecture.IA64)
            {
                return localizedProperties.Where(p => _lstIA64Languages.Contains(p.Name)).ToList();
            }
            else
            {
                string os = OSDetector.ParseTargetOSFromUpdateTitle(_actualUpdate.Title);
                if (_lstSpecialServerOS.Contains(os)) // 18 languages server OS
                {
                    return localizedProperties.Where(p => _lstSpecialServerLanguages.Contains(p.Name)).ToList();
                }
                else
                {
                    return localizedProperties.ToList();
                }
            }
        }

        private string GetReleaseMonthInThaiBuddhistEra()
        {
            string ttgl = _actualUpdate.Properties.TimeToGoLive;
            DateTime dtTTGL = CommonHelper.TTGLString2DateTime(ttgl);

            // Creates an instance of the ThaiBuddhistCalendar.
            ThaiBuddhistCalendar myCal = new ThaiBuddhistCalendar();

            return String.Format("{0}-{1:00}", myCal.GetYear(dtTTGL), myCal.GetMonth(dtTTGL));
        }

        private bool KnownFailureInReleaseMonth(string language, string expectVal, string actualVal)
        {
            bool result = false;

            if (language == "th") //Thailand uses different calendar
            {
                string thaiReleaseMonth = GetReleaseMonthInThaiBuddhistEra();

                result = actualVal.Contains(thaiReleaseMonth);
            }

            return result;
        }

        private bool KnownFailureInNETSKU(string language, string missingSKU, string title)
        {
            bool result = false;

            // 2020-05 saugos ir kokybės nauj. paket., skirtas „.NET Framework“ 3.5.1, 4.5.2, 4.6–4.6.2, 4.7–4.7.2, 4.8, veikiančioms „Windows Embedded Standard 7“ x64 (KB4556399)
            if (language == "lt")
            {
                if (missingSKU == "4.6.1, 4.7.1" &&
                    title.Contains("4.6-4.6.2") && 
                    title.Contains("4.7-4.7.2"))
                    result = true;
            }

            return result;
        }

        private bool KnownFailureInKBArticle(string language, string title)
        {
            bool result = false;

            if (language == "lt")
            {
                string kb = String.Format("(„KB{0}“)", _actualUpdate.Properties.KBArticle);

                result = title.Contains(kb);
            }
            else if (language == "zh-cn")
            {
                string kb = String.Format("（KB{0}）", _actualUpdate.Properties.KBArticle);

                result = title.Contains(kb);
            }

            return result;
        }

        private bool CompareDescritpion(string language, string expectDesc, string actualDesc)
        {
            expectDesc = expectDesc.Trim();
            actualDesc = actualDesc.Trim();

            bool result = String.CompareOrdinal(expectDesc, actualDesc) == 0;

            if (!result)
            {
                //1. he
                if (language == "he")
                {
                    expectDesc = expectDesc + ".";
                    result = String.CompareOrdinal(expectDesc, actualDesc) == 0;
                }
            }

            return result;
        }

        private string PreprocessTitle(string title)
        {
            return title.Replace('–', '-').Replace((char)0xA0, (char)0x20);
        }
    }
}
