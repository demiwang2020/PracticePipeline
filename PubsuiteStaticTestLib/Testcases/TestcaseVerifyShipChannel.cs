using CAIPub;
using CAIPub.dbo;
using Helper;
using PubsuiteStaticTestLib.DbClassContext;
using PubsuiteStaticTestLib.Model;
using PubsuiteStaticTestLib.UpdateHelper;
using RMIntegration.RMService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Testcases
{
    class TestcaseVerifyShipChannel : TestcaseBase
    {
        private Dictionary<string, string> _osNameLookupTable;
        //bool targetppe = false;
        //RTDB rTDB = new RTDB();
        //CaiWorker caiWorker = new CaiWorker();
        //IPublishHelper publishHelper = new PublishHelper();
        public TestcaseVerifyShipChannel(InputData inputData, Update expectUpdate, Update actualUpdate)
          : base(inputData, expectUpdate, actualUpdate, "Publish Channel Verification")
        {
            BuildOSLookupTable();
            
        }
        private void BuildOSLookupTable()
        {
            // map the os names in update title to target os of patch in tfs
            _osNameLookupTable = new Dictionary<string, string>();
            _osNameLookupTable.Add("Windows 10 Version 22H2", "Windows 10 22H2");
            _osNameLookupTable.Add("Windows 10 Version 21H2", "Windows 10 21H2");
            _osNameLookupTable.Add("Windows 10 Version 1809", "Windows 10 1809");
            _osNameLookupTable.Add("Server 2019", "Windows 10 1809");

            _osNameLookupTable.Add("Windows Embedded Standard 7", "Windows 7.0 SP1");
            _osNameLookupTable.Add("Server 2008 R2", "Windows 7.0 SP1");
            _osNameLookupTable.Add("Windows 10 Version 1607", "Windows 10 1607");
            _osNameLookupTable.Add("Server 2016", "Windows 10 1607");
            _osNameLookupTable.Add("Server 2012", "Windows Server 2012");
            _osNameLookupTable.Add("Server 2012 R2", "Windows 8.1");
            _osNameLookupTable.Add("Microsoft server operating system version 21H2", "Windows Server 2022");
            _osNameLookupTable.Add("Microsoft server operating system, version 22H2", "Azure Stack OS 22H2");
            _osNameLookupTable.Add("Microsoft server operating system, version 23H2", "Server OS 23H2 (Zn)");
            _osNameLookupTable.Add("Microsoft server operating system, version 24H2", "Server OS 24H2");
            _osNameLookupTable.Add("Windows 11", "Windows SV 21H2");
            _osNameLookupTable.Add("Windows 11, version 22H2", "Windows 11 22H2");
            _osNameLookupTable.Add("Windows 11, version 23H2", "Windows 11 23H2");
            _osNameLookupTable.Add("Windows 11, version 24H2", "Windows 11 24H2");
            //_osNameLookupTable.Add("Windows Version Next", "Windows 10 21H2");
            //_osNameLookupTable.Add("Windows Server Version Next", "Windows 10 21H2");

        }
        private string GetOS(string title)
        {
            string pattern = @"for";
            string contentBetween = string.Empty;
            if (title.Contains("x64") || title.Contains("ARM64"))
            {
                MatchCollection matches = Regex.Matches(title, pattern);
                if (matches.Count >= 3)
                {
                    int secondForIndex = matches[1].Index + matches[1].Length;
                    int thirdForIndex = matches[2].Index;
                    contentBetween = title.Substring(secondForIndex, thirdForIndex - secondForIndex);
                }
                if(contentBetween.Contains(","))
                {
                    contentBetween = contentBetween.Replace(",Version","");
                }
                
            }
            else
            {

            }
            return contentBetween;
        }
        protected override void RunTest()
        {
            int releaseType = UpdateBuilder.GetUpdateType(_inputData);
           
            if (_inputData.Title.Contains("Security Only")) {
                _result.LogMessage("SO release don't need run this case ");
                return;
            }
            bool flag = false;

            if (_inputData.Title.Contains("Windows 10 Version 1607") || _inputData.Title.Contains("Windows Server 2016") ||
                _inputData.Title.Contains("Windows 11, version 22H2") || _inputData.Title.Contains("Microsoft server operating system, version 23H2") ||
                _inputData.Title.Contains("Windows 11, version 23H2") || _inputData.Title.Contains("Windows 11, version 24H2") ||
                _inputData.Title.Contains("Microsoft server operating system version 24H2")

                )
            {

                flag = true;
            }
            else if (_inputData.Title.Contains("Windows Server 2008 SP2")) {

                flag = false;
            }
            else
            {
                if (_expectedUpdate.Properties.IsCatalogOnly)
                    flag = true;

            }
            if (flag)
            {
                WorkItemHelper tfsObject = new WorkItemHelper(TFSHelper.TFSURI, int.Parse(_inputData.TFSIDs[0].ToString()));
                SettingsAuditWorker worker = new SettingsAuditWorker();
                List<string> list = new List<string>();
                string value = null;
                _result.LogError("Verifying PublishChannels");
                Dictionary<string, string> testResult = worker.AuditMediaFlagForWorkitem(int.Parse(_inputData.TFSIDs[0].ToString()));
                foreach(var item in testResult)
                {
                    list = item.Value.Split(new char[] { ';' }).ToList();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Contains("PASS") || list[i].Contains("FAIL"))
                        {
                            if (list[i].Contains("PASS"))
                                continue;
                            if (list[i].Contains("FAIL"))
                            {
                                _result.Result = false;
                                _result.LogError(list[i - 1]);
                                //pand fail 
                                if (list[i].Contains("PAND"))
                                {
                                    _result.LogError("Pand test fail");
                                    _result.AddFailure("Pand Test", new TestFailure() { ExpectResult = item.Value, ActualResult = item.Key });
                                }
                                else if (list[i].Contains("Marketplace"))
                                {
                                    _result.LogError("Marketplace test fail");
                                    _result.AddFailure("Marketplace Test", new TestFailure() { ExpectResult = item.Value, ActualResult = item.Key });
                                }
                                else if (list[i].Contains("MTP"))
                                {
                                    _result.LogError("MTP test fail");
                                    _result.AddFailure("MTP Test", new TestFailure() { ExpectResult = item.Value, ActualResult = item.Key });
                                }
                                break;
                            }

                        }
                    }
                }
    



                //List<CAIPub.dbo.ReleaseTicket> releasetickets = rTDB.GetReleaseTicketsForWorkItem(int.Parse(_inputData.TFSIDs[0].ToString()));
                //using (var db = new WUSAFXDbContext())
                //{
                //    string os = OSDetector.ParseTargetOSFromUpdateTitle(_inputData.Title);
                //    int osid = db.TOperatingSystems.Where(p => p.Name.Equals(os)).First().ID;

                //    WorkItemHelper tfsObject = new WorkItemHelper(TFSHelper.TFSURI, int.Parse(_inputData.TFSIDs[0].ToString()));

                //    int sku = db.TNetSkus.Where(p => p.SKU == tfsObject.SKU).Single().ID;

                //    string MTPdeliveryRecipients = db.TPublishChannels.Where(c => c.OS.Equals(osid) && c.SKU.Equals(sku)).First().MTPdeliveryRecipients.Trim();
                //    bool Marketplace = db.TPublishChannels.Where(c => c.OS.Equals(osid) && c.SKU.Equals(sku)).First().MTPChannel;

                //    bool PAND = db.TPublishChannels.Where(c => c.OS.Equals(osid) && c.SKU.Equals(sku)).First().MediaFlag;

                //    foreach (CAIPub.dbo.ReleaseTicket rt in releasetickets)
                //    {
                //        if (!rt.Title.Contains(_osNameLookupTable[os].ToString()))
                //        {
                //            continue;
                //        }
                //        publishHelper.SetConfig();
                //        CaiReleaseTicket ticket = publishHelper.GetReleaseTicket(rt.ReleaseTicketId);
                //        if (ticket.PublishingChannels != null)
                //        {
                //            foreach (var channel in ticket.PublishingChannels)
                //            {
                //                switch (channel.Audience.ToString())
                //                {
                //                    case "MTP":
                                       
                //                        _result.LogMessage("Verifying if the bundle contains MTP channel");
                //                        string actualDeliveryRecipients;
                //                        if (channel.DeliveryRecipients.Count == 2)
                //                        {
                //                            actualDeliveryRecipients = channel.DeliveryRecipients[0] + ";" + channel.DeliveryRecipients[1];
                //                            if (actualDeliveryRecipients != MTPdeliveryRecipients)
                //                            {
                //                                _result.LogError("The MTP DeliveryRecipients is not correct");
                //                                _result.AddFailure("MTP DeliveryRecipients", new TestFailure() { ExpectResult = MTPdeliveryRecipients, ActualResult = actualDeliveryRecipients });

                //                                _result.Result = false;
                //                            }
                //                        }
                //                        else if (channel.DeliveryRecipients.Count == 1)
                //                        {

                //                            actualDeliveryRecipients = channel.DeliveryRecipients[0];

                //                            if (String.Compare(actualDeliveryRecipients, MTPdeliveryRecipients, true) != 0)
                //                            {
                //                                _result.LogError("The MTP DeliveryRecipients is not correct");
                //                                _result.AddFailure("MTP DeliveryRecipients", new TestFailure() { ExpectResult = MTPdeliveryRecipients, ActualResult = actualDeliveryRecipients });
 

                //                                _result.Result = false;
                //                            }
                //                        }
                //                        break;
                //                    case "Retail":
                //                        if (releaseType == 4 || releaseType == 8 || releaseType == 7) {
                //                            _result.LogMessage("Non-sec bundles don't have media flag ");
                //                            return;
                //                        }
                //                        _result.LogMessage("Verifying if the bundle contains Marketplace channel");
                //                        if (!channel.DeliveryRecipients.Contains("Marketplace") == Marketplace)
                //                        {
                //                            _result.LogError("The media flag(Marketplace) is not correct");

                //                            _result.AddFailure("The media flag(Marketplace)", new TestFailure() { ExpectResult = Marketplace.ToString(), ActualResult = channel.DeliveryRecipients.ToString() });

                //                            _result.Result = false;
                //                        }
                //                        _result.LogMessage("Verifying if the bundle contains PAND channel");

                //                        if (!channel.DeliveryRecipients.Contains("PAND") == PAND)
                //                        {
                //                            _result.LogError("The media flag(PAND) is not correct");

                //                            _result.AddFailure("The media flag(PAND)", new TestFailure() { ExpectResult = PAND.ToString(), ActualResult = channel.DeliveryRecipients.ToString() });

                //                            _result.Result = false;
                //                        }
                //                        break;


                //                }

                //            }
                //        }
                //    }
                //}
            }

        }
    }

}
