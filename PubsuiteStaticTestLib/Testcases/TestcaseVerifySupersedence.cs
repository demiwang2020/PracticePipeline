using Helper;
using KeyVaultManagementLib;
using PubsuiteStaticTestLib.DbClassContext;
using PubsuiteStaticTestLib.UpdateHelper;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
namespace PubsuiteStaticTestLib.Testcases
{
    class TestcaseVerifySupersedence : TestcaseBase
    {
        public TestcaseVerifySupersedence(InputData inputData, Update expectUpdate, Update actualUpdate)
           : base(inputData, expectUpdate, actualUpdate, "Verify superseded updates")
        {
        }

        public static string GetManagedId()
        {
            if (Regex.Match(Environment.GetEnvironmentVariable("COMPUTERNAME"), "DotNetPatchTest", RegexOptions.IgnoreCase).Success)
            {
                return ConfigurationManager.AppSettings["gofxservinfra01ManagedId"];
            }
            return string.Empty;
        }
        protected override void RunTest()
        {
            _result.LogMessage("Getting Superseded updates...");

            List<string> ssGuids = _actualUpdate.GetSupersededUpdates();
            int actualSSCount = 0;
            if (ssGuids != null && ssGuids.Count > 0)
            {
                _result.LogMessage(ssGuids.Count.ToString() + " superseded updates found: ");
                _result.LogMessage(String.Join(", ", ssGuids));
                actualSSCount = ssGuids.Count;
            }
            else
            {
                _result.LogMessage("0 superseded updates found");
            }

            _result.LogMessage("Verifying Superseded updates...");

            List<string> expectedSSKBs = null;
            int expectSSCount = 0;
            if (!String.IsNullOrEmpty(base._inputData.SupersededKB))
            {
                var ssKb = base._inputData.SupersededKB.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                expectedSSKBs = new List<string>();
                foreach (var s in ssKb)
                {
                    expectedSSKBs.Add(s.Trim());
                }
                //Regex regex = new Regex(@"KB(\w+)");
                //MatchCollection sskb = regex.Matches(base._inputData.SupersededKB);
                //foreach (Match match in sskb)
                //{
                //    expectedSSKBs.Add(match.ToString().Substring(2, 7).Trim());
                //}
                //expectSSCount = expectedSSKBs.Count;
            }

            if (actualSSCount != expectSSCount && expectSSCount > actualSSCount)
            {
                _result.LogError("Expect SS KB count doesn't match with actual");
                _result.AddFailure("SS KB count", new TestFailure() { ExpectResult = expectSSCount.ToString(), ActualResult = actualSSCount.ToString() });

                _result.Result = false;
            }
            else if (actualSSCount > 0)
            {
                List<Update> ssUpdates = new List<Update>();

                foreach (var s in ssGuids)
                {              
                    ssUpdates.Add(UpdateBuilder.QueryUpdateFromGUID(s));
                }

                //If expired
                //PubUtilManager.PubUtilClient _pubUtilClient = new PubUtilManager.PubUtilClient(ConfigurationManager.AppSettings["ServiceAccountName"],
                //KeyVaultAccess.GetGoFXKVSecret("VsulabServiceAccountPassword", GetManagedId()),
                //ConfigurationManager.AppSettings["PubsuiteName"]);
                //if (_pubUtilClient.CheckIfGuidIsExpired(_actualUpdate.ID))
                //{
                //    _result.LogError("Check if Guid Is Expired" + _actualUpdate.ID);
                //    _result.AddFailure("Guid is " + _actualUpdate.ID, new TestFailure() { ExpectResult = "Guid is not expired", ActualResult = "Guid is expired" });
                //    _result.Result = false;
                //}              

                //5. Compare actual SS
                if (_inputData.Title.Contains("Version Next"))
                {
                    CompareSSForWIP(ssUpdates);
                }
                else if (_inputData.Title.Contains("Preview"))
                {
                    CompareSSForD(ssUpdates);
                }
                else
                {
                    CompareSSForB(ssUpdates);
                }

            }

            if (_result.Result)
            {
                _result.LogMessage("Supersedence verification PASSED");
            }
            else
            {
                _result.LogMessage("Supersedence verification FAILED");
            }

        }



        private void CompareSSForD(List<Update> ssUpdate)
        {
            int osid = 0;
            foreach (var item in ssUpdate)
            {
                VerifySSForD(item, item.Title.Contains("Preview"));
            }
            //using (var db = new WUSAFXDbContext())
            //{
            //    string os = OSDetector.ParseTargetOSFromUpdateTitle(_inputData.Title);
            //    osid = db.TOperatingSystems.Where(p => p.Name.Equals(os)).First().ID;
            //}
            //if (!(osid >= 1043 || osid == 1037))
            //{
            //    if(ssUpdate.Count != 2)
            //    {
            //        _result.LogError(" Verify Actual SS Count for D");
            //        _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "2", ActualResult = ssUpdate.Count.ToString() });
            //        _result.Result = false;
            //    }
            //}

        }
        private void CompareSSForWIP(List<Update> ssUpdates)
        {
            foreach (var item in ssUpdates)
            {
                verifySSForWIP(item);
            }
            if(ssUpdates.Count!=2)
            {
                _result.LogError(" Verify Actual SS Count for D");
                _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "1", ActualResult = ssUpdates.Count.ToString() });
                _result.Result = false;
            }
        }
        private void CompareSSForB(List<Update> ssUpdates)
        {
            List<string> ActualDes = new List<string>();
            if (_inputData.ShipChannels.Contains("SiteAUSUSCatalog") && ssUpdates.Count >= 3)
            {

                foreach (var item in ssUpdates)
                {
                    VerifySSDetail(item, CombineDestination(item));
                    ActualDes.Add(CombineDestination(item));
                }
                if (!(ActualDes.Contains("Site") && ActualDes.Contains("SUSCatalog") && ActualDes.Contains("SiteAUSUSCatalog")))
                {
                    _result.LogError(" Verify SS count");
                    _result.AddFailure("Actual SS count less than Except", new TestFailure() { ExpectResult = "SiteAUSUSCatalog,SUSCatalog,Site", ActualResult = "Search" + _inputData.UpdateID + "in pubsuits" });
                    _result.Result = false;
                }
            }
            else if (_inputData.ShipChannels.Contains("SUSCatalog") && ssUpdates.Count >= 1)
            {
                foreach (var item in ssUpdates)
                {
                    VerifySSDetail(item, CombineDestination(item));
                    ActualDes.Add(CombineDestination(item));
                }
                if (!ActualDes.Contains("SUSCatalog"))
                {
                    _result.LogError(" Verify SS count");
                    _result.AddFailure("Actual SS count less than Except", new TestFailure() { ExpectResult = "SUSCatalog", ActualResult = "Search" + _inputData.UpdateID + "in pubsuits" });
                    _result.Result = false;
                }
            }
            else if (_inputData.ShipChannels.Equals("Site") && ssUpdates.Count >= 2)
            {
                foreach (var item in ssUpdates)
                {
                    VerifySSDetail(item, CombineDestination(item));
                    ActualDes.Add(CombineDestination(item));
                }
                if (!ActualDes.Contains("Site"))
                {
                    _result.LogError(" Verify SS count");
                    _result.AddFailure("Actual SS count less than Except", new TestFailure() { ExpectResult = "N-2 Wu bundles", ActualResult = "Search" + _inputData.UpdateID + "in pubsuits" });
                    _result.Result = false;
                }
            }
            else
            {
                _result.LogError(" Verify SS count");
                _result.AddFailure("Actual SS count less than Except", new TestFailure() { ExpectResult = "View detail in Onenote", ActualResult = "Search" + _inputData.UpdateID + "in pubsuits" });
                _result.Result = false;
            }

        }
        private void VerifySSForD(Update SSUpdate, bool IsD)
        {
            int osid = 0;
            string time = _inputData.Title.Substring(0, 7);
            List<string> month = GetLatestTwoMonth(time," ");
            

            using (var db = new WUSAFXDbContext())
            {
                string os = OSDetector.ParseTargetOSFromUpdateTitle(_inputData.Title);
                osid = db.TOperatingSystems.Where(p => p.Name.Equals(os)).First().ID;
            }
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                if (!(osid >= 1043 || osid == 1037))
                {
                    if (!IsD)
                    {
                        var BBundle = dbContext.TPubsuiteStaticTest.Where(p => p.Title == SSUpdate.Title && (p.Comments == "Site" || p.Comments == "SiteAuSusCatalog")).Distinct().OrderByDescending(p => p.UpdateID).FirstOrDefault();
                        if (!(BBundle.Title.Substring(0, 7) == month[0] && BBundle.UpdateGUID == SSUpdate.ID))
                        {
                            _result.LogError(" Verify Actual SS Destionations for D");
                            _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Contain latest B ", ActualResult = "Search " + SSUpdate.ID + " in pubsuits" });
                            _result.Result = false;
                        }
                    }
                    else
                    {
                        month = GetLatestTwoMonth(time,"Preview");
                        var Guid = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) == month[0]).Select(p => p.UpdateGUID).Distinct().ToList();
                        if (!Guid.Contains(SSUpdate.ID) && CombineDestination(SSUpdate) != "Site")
                        {
                            _result.LogError(" Verify Actual SS Destionations for D");
                            _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Contain latest D ", ActualResult = "Search " + SSUpdate.ID + " in pubsuits" });
                            _result.Result = false;
                        }
                    }
                }
                else 
                {
                    if (CombineDestination(SSUpdate) != "Site")
                    {
                        var Info = dbContext.TPubsuiteStaticTest.Where(p => p.UpdateGUID == SSUpdate.ID).OrderByDescending(p => p.UpdateID).Distinct().FirstOrDefault();
                        if (!month.Contains(Info.Title.Substring(0, 7)))
                        {
                            _result.LogError(" Verify Actual SS Destionations for D");
                            _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Contain last D ", ActualResult = "Search " + SSUpdate.ID + "in pubsuits" });
                            _result.Result = false;
                        }

                    }
                    

                }
            }
        }
        private void verifySSForWIP(Update SSUpdate)
        {
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                string time = _inputData.Title.Substring(0, 7);
                List<string> month = GetLatestTwoMonth(time, "Version Next");
                var Guid = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Contains("Version Next") && p.Title.Substring(0, 7) == month[0]).Select(p => p.UpdateGUID).Distinct().ToList();
                var Guid1 = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Contains("Version Next") && p.Title.Substring(0, 7) == month[1]).Select(p => p.UpdateGUID).Distinct().ToList();
                if (!(Guid.Contains(SSUpdate.ID) || Guid1.Contains(SSUpdate.ID)))
                {
                    _result.LogError(" Verify Actual SS Destionations for WIP");
                    _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Search " + SSUpdate.ID + "in pubsuits", ActualResult = "Search " + SSUpdate.ID + "in pubsuits" });
                    _result.Result = false;
                }
            }
        }

        private void VerifySSDetail(Update SSUpdate, string ShipChannel)
        {
            string time = _inputData.Title.Substring(0, 7);  //2024-07
            List<string> month = new List<string>();
            string OsName = GetOS(_inputData.Title);
            if (!_inputData.ShipChannels.Equals("Site"))
            {
                month = GetMonthForB(time, ShipChannel);
                using (var dbContext = new PatchTestDataClassDataContext())
                {
                    var Title = dbContext.TPubsuiteStaticTest.Where(p => p.UpdateGUID == SSUpdate.ID && p.Comments == ShipChannel).OrderByDescending(p => p.UpdateID).Select(p => p.Title).Distinct().FirstOrDefault();
                    if (!(month[0] == Title.Substring(0, 7) && OsName == GetOS(SSUpdate.Title)))
                    {
                        _result.LogError(" Verify Actual SS Destionations");
                        _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Search " + SSUpdate.ID + " in pubsuits", ActualResult = "Search " + SSUpdate.ID + "in pubsuits" });
                        _result.Result = false;
                    }
                }
            }
            else
            {
                month = GetLatestTwoMonth(time, OsName);
                using (var dbContext = new PatchTestDataClassDataContext())
                {
                    var Title = dbContext.TPubsuiteStaticTest.Where(p => p.UpdateGUID == SSUpdate.ID && p.Comments == ShipChannel).OrderByDescending(p => p.UpdateID).Select(p => p.Title).Distinct().FirstOrDefault();
                    if(Title != null)
                    {
                        if (!(month.Contains(Title.Substring(0, 7)) && OsName == GetOS(SSUpdate.Title)))
                        {
                            _result.LogError(" Verify Actual SS Destionations");
                            _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Can not find " + SSUpdate.ID + " in database", ActualResult = "Search " + SSUpdate.ID + "in pubsuits" });
                            _result.Result = false;
                        }
                    }
                    else
                    {
                        _result.LogError(" Verify Actual SS Destionations");
                        _result.AddFailure("Actual SS Destinations ", new TestFailure() { ExpectResult = "Search " + SSUpdate.ID + "in pubsuits", ActualResult = "Search " + SSUpdate.ID + "in pubsuits" });
                        _result.Result = false;
                    }
                    
                }
            }



        }

        private string CombineDestination(Update update)
        {
            string des = string.Empty;
            des += update.Properties.Site ? "Site" : "";
            des += update.Properties.AU ? "AU" : "";
            des += update.Properties.SUS ? "SUS" : "";
            des += update.Properties.Catalog ? "Catalog" : "";
            return des;
        }
        private List<string> GetMonthForB(string time, string KeyWord)
        {
            List<string> month = new List<string>();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                var updateID = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) == time).OrderByDescending(p => p.UpdateID).Select(p => p.UpdateID).Distinct().FirstOrDefault();
                while (month.Count < 2)
                {
                    var title = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) != time && p.Comments == KeyWord && p.UpdateID < updateID).OrderByDescending(p => p.UpdateID).Select(p => p.Title).FirstOrDefault();
                    var titlec = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) != time && p.Title.Substring(0, 7) != title.Substring(0, 7) && p.Comments == KeyWord && p.UpdateID < updateID).OrderByDescending(p => p.UpdateID).Select(p => p.Title).FirstOrDefault();
                    month.Add(title.Substring(0, 7));
                    month.Add(titlec.Substring(0, 7));
                }
            }
            return month;
        }
        private List<string> GetLatestTwoMonth(string time,string KeyWord)
        {
            List<string> month = new List<string>();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                var updateID = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) == time).OrderByDescending(p => p.UpdateID).Select(p => p.UpdateID).Distinct().FirstOrDefault();
                while (month.Count < 2)
                {
                    var title = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) != time && p.Title.Contains(KeyWord) && p.UpdateID < updateID && p.Comments != null).OrderByDescending(p => p.UpdateID).Select(p => p.Title).FirstOrDefault();
                    var titlec = dbContext.TPubsuiteStaticTest.Where(p => p.Title.Substring(0, 7) != time && p.Title.Contains(KeyWord) && p.Title.Substring(0, 7) != title.Substring(0, 7) && p.UpdateID < updateID && p.Comments != null).OrderByDescending(p => p.UpdateID).Select(p => p.Title).FirstOrDefault();
                    month.Add(title.Substring(0, 7));
                    month.Add(titlec.Substring(0, 7));
                }
            }
            return month;
        }
        private string GetOS(string title)
        {
            string pattern = @"for";
            MatchCollection matches = Regex.Matches(title, pattern);
            if (matches.Count >= 3)
            {
                int secondForIndex = matches[1].Index + matches[1].Length;
                int thirdForIndex = matches[2].Index;
                string contentBetween = title.Substring(secondForIndex, thirdForIndex - secondForIndex);
                return contentBetween;
            }
            else
            {
                int secondForIndex = matches[1].Index + matches[1].Length;
                int thirdForIndex = title.IndexOf('(');
                string contentBetween = title.Substring(secondForIndex, thirdForIndex - secondForIndex);
                return contentBetween;

            }
            return null;
        }
        private void VerifySSDestination(List<Update> ssUpdates, string dest, bool expect)
        {
            foreach (var update in ssUpdates)
            {
                if ((dest.Contains("AU") || dest.Contains("WU")) && _actualUpdate.Properties.AU != expect)
                {
                    GenerateSSDestinationFailure(update, "AU", expect);
                }

                if (dest.Contains("Catalog") && _actualUpdate.Properties.Catalog != expect)
                {
                    GenerateSSDestinationFailure(update, "Catalog", expect);
                }

                if (dest.Contains("Site") && _actualUpdate.Properties.Site != expect)
                {
                    GenerateSSDestinationFailure(update, "Site", expect);
                }

                if (dest.Contains("SUS") && update.Properties.SUS != expect)
                {
                    GenerateSSDestinationFailure(update, "SUS", expect);
                }

                if (dest.Contains("Csa") && update.Properties.Csa != expect)
                {
                    GenerateSSDestinationFailure(update, "Csa", expect);
                }
            }
        }

        private void GenerateSSDestinationFailure(Update update, string channel, bool expect)
        {
            base.GenerateFailResult(String.Format("{0} of SS update {1} is not expected", channel, update.ID),
                                    String.Format("{0} of {1}", channel, update.ID),
                                    expect.ToString(),
                                    (!expect).ToString());
        }
    }
}
