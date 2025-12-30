using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Connect2TFS;
using System.Data;
using System.Net.Mail;
using System.Net;
using System.IO;
using System.Configuration;
using ScorpionDAL;
using System.Collections;
using Helper;
using RMIntegration;

namespace THTestLib
{
    class MailHelper
    {
        private string[] MailTo;
        private string[] MailCC;
        private string[] ExceptionMailTo;
        private string[] RuntimeMailTo;
        private string[] RuntimeMailCC;

        public string MailContent { get; private set; }

        //private int DuplicateTimes = 3;
        public MailHelper()
        {
            ReadMailReceivers();
        }
        
        private void ReadMailReceivers()
        {
            string mailTo = ConfigurationManager.AppSettings["MailTo"];
            string mailCC = ConfigurationManager.AppSettings["MailCC"];
            string exceptionMailTo = ConfigurationManager.AppSettings["ExceptionMailTo"];
            string runtimeMailTo = ConfigurationManager.AppSettings["RuntimeMailTo"];
            string runtimeMailCC = ConfigurationManager.AppSettings["RuntimeMailCC"];

            if (string.IsNullOrEmpty(mailTo) && string.IsNullOrEmpty(mailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailTo = ExpandMailAddress(mailTo);
            ExceptionMailTo= String.IsNullOrEmpty(exceptionMailTo) ? new string[] { } : ExpandMailAddress(exceptionMailTo);
            MailCC = String.IsNullOrEmpty(mailCC) ? new string[] { } : ExpandMailAddress(mailCC);
            RuntimeMailTo = String.IsNullOrEmpty(runtimeMailTo) ? MailTo : ExpandMailAddress(runtimeMailTo);
            RuntimeMailCC = String.IsNullOrEmpty(runtimeMailCC) ? MailCC : ExpandMailAddress(runtimeMailCC);
        }

        private string[] ExpandMailAddress(string address)
        {
            string [] addresses = address.Split(new char[] { ';' });

            for (int i = 0; i < addresses.Length; ++i)
                addresses[i] = addresses[i] + "@microsoft.com";

            return addresses;
        }

        public void SendStaticTestResultsMail(WorkItemHelper tfsItem, THTestResults thTestResults, List<TTHTestRunInfo> runs)
        {
            string strResult;
            if (thTestResults.Result)
                strResult = thTestResults.HasWarning ? "PASSED with Warnings" : "PASSED";
            else
                strResult = "FAILED";

            string subject = String.Format("{0} NDP {1} patch static test {2} - {3}", Utility.TranslateOSName(tfsItem.OSInstalled, tfsItem.OSSPLevel), tfsItem.SKU, strResult, Utility.ParsePatchGroupFromTFSTitle(tfsItem.Title));

            MailContent = GetStaticTestResultMailBody(tfsItem, thTestResults, runs);

            SendMail(MailTo, MailCC, subject);
        }

        private void SendMail(string[] mailTo, string[] mailCC, string subject)
        {
            MailSvcUtil mailSvc = new MailSvcUtil();
            mailSvc.SendEmail(mailTo, mailCC, subject, MailContent);
        }

        private string GetStaticTestResultMailBody(WorkItemHelper tfsItem, THTestResults thTestResults, List<TTHTestRunInfo> runs)
        {
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            mailBody.AppendLine("    <div class=\"Container\">");

            if (thTestResults.Result)
            {
                if(thTestResults.HasWarning)
                    mailBody.AppendLine("   <h2 class=\"PassedResult\">Static smoke test PASSED <span class=\"FailedResult\">with WARNINGs</span> for below patch</h2>");
                else
                    mailBody.AppendLine("   <h2 class=\"PassedResult\">Static smoke test PASSED for below patch</h2>");
            }
            else
            {
                mailBody.AppendLine("   <h2 class=\"FailedResult\">Static smoke test FAILED for below patch</h2>");
            }

            //Print basic info of this TFS item
            mailBody.AppendLine("   <table class=\"TableBasicInfo\">");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>TFS ID</b></td>");
            mailBody.AppendLine(String.Format("           <td><a href=\"{0}\">{1}</a></td>", "https://vstfdevdiv.corp.microsoft.com/DevDiv/DevDiv%20Servicing/_workitems#_a=edit&id=" + tfsItem.ID, tfsItem.ID));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>KB Article</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.KBNumber));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>SKU</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.SKU));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>Title</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.Title));
            mailBody.AppendLine("       </tr>");

            foreach (Architecture arch in Enum.GetValues(typeof(Architecture)))
            {
                if (!string.IsNullOrEmpty(tfsItem.GetPatchName(arch)))
                {
                    mailBody.AppendLine("       <tr>");
                    mailBody.AppendLine(String.Format("           <td><b>Package Location {0}</b></td>", arch.ToString()));
                    mailBody.AppendLine(String.Format("           <td><a href=\"{0}\">{1}</a></td>", tfsItem.GetPatchFullPath(arch), tfsItem.GetPatchFullPath(arch)));
                    mailBody.AppendLine("       </tr>");
                }
            }

            mailBody.AppendLine("   </table>");
            mailBody.AppendLine("   <br/>");

            int i = 0;

            foreach (DataTable resultTable in thTestResults.ResultDetails)
            {
                //Print table name
                mailBody.AppendLine(String.Format("            <p class=\"TableName\"><span class=\"{0}\">{1}</span></p>", thTestResults.ResultDetailSummaries[i++] ? "PassedResult" : "FailedResult", resultTable.TableName));
                
                //skip empty table
                if (resultTable.Rows.Count == 0)
                {
                    mailBody.AppendLine("        <br/>");
                    continue;
                }

                //int colWidth = 10000 / resultTable.Columns.Count;
                //string colStyle = String.Format("{0}%", (float)colWidth / 100.0);

                mailBody.AppendLine("            <table class=\"TableResults\">");

                //Print table columns
                List<string> htmlAttrs = BuildHtmlElementAttributes(resultTable);
                int attrIndex = 0;

                mailBody.AppendLine("                <tr>");
                foreach (DataColumn col in resultTable.Columns)
                {
                    mailBody.AppendLine(String.Format("                    <td class=\"TableResultsColumnHeader\" {0}>{1}</td>", htmlAttrs[attrIndex++], col.ColumnName));
                }
                mailBody.AppendLine("                </tr>");

                //Print table rows
                foreach (DataRow row in resultTable.Rows)
                {
                    mailBody.AppendLine("                <tr>");
                    for (int j = 0; j < resultTable.Columns.Count; ++j)
                    {
                        if (resultTable.Columns[j].ExtendedProperties.ContainsKey("ResultCol"))
                        {
                            string cssClass = row[j].ToString() == "Pass" ? "PassedResult" : "FailedResult";
                            mailBody.AppendLine(String.Format("                    <td class=\"{0}\" {1}>{2}</td>", cssClass, htmlAttrs[j], row[j].ToString()));
                        }
                        else
                        {
                            mailBody.AppendLine(String.Format("                    <td {0}>{1}</td>", htmlAttrs[j], row[j].ToString()));
                        }
                    }
                    mailBody.AppendLine("                </tr>");
                }

                mailBody.AppendLine("            </table>");
                mailBody.AppendLine("        <br/>");
            }

            //Runtime test details
            if (runs != null && runs.Count > 0)
            {
                mailBody.AppendLine(String.Format("            <p class=\"TableName\">Runtime test is running</p>"));
                GetRuntimeTestResultTable(runs, mailBody);
            }

            mailBody.AppendLine("    </div>");
            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            return mailBody.ToString();
        }

        public void SendExceptionMail(WorkItemHelper tfsItem, Exception ex)
        {
            string subject = String.Format("Patch smoke test threw exception:TFSID-{0} KBNumber-{1} SKU-{2}",
                     tfsItem.ID, tfsItem.KBNumber, tfsItem.SKU);

            MailContent = GetExceptionMailBody(tfsItem, ex);

            SendMail(ExceptionMailTo, MailCC, subject);
        }

        private string GetExceptionMailBody(WorkItemHelper tfsItem, Exception ex)
        {
            StringBuilder mailBody = new StringBuilder();

            mailBody.AppendLine("<html>");
            mailBody.AppendLine("<head>");
            mailBody.AppendLine("</head>");
            // Html body part
            mailBody.AppendLine("<body>");
            mailBody.AppendLine("   <h2 style=\"color:red\">Warning! Exception caught when testing below patch</h2>");

            //Basic job information
            mailBody.AppendLine("   <table border=\"0\">");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>TFS ID</b></td>");
            mailBody.AppendLine(String.Format("           <td><a href=\"{0}\">{1}</td>", "https://vstfdevdiv.corp.microsoft.com/DevDiv/DevDiv%20Servicing/_workitems#_a=edit&id=" + tfsItem.ID, tfsItem.ID));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>KB Article</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.KBNumber));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>SKU</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.SKU));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>Title</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.Title));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("   </table>");

            mailBody.AppendLine("   <br />");

            mailBody.AppendLine("   <h4>Exception Details</h4>");
            mailBody.AppendLine(String.Format("<p>{0}</p>", ex.Message));

            string[] stackTraceStrings = ex.StackTrace.Split(Environment.NewLine.ToCharArray());
            foreach (string s in stackTraceStrings)
            {
                if (String.IsNullOrEmpty(s))
                    continue;

                mailBody.AppendLine(String.Format("   <p>&nbsp;&nbsp;&nbsp;{0}</p>", s));
            }

            mailBody.AppendLine("</body>");

            return mailBody.ToString();
        }

        private void AddCssToMailBody(StringBuilder mailBody)
        {
            mailBody.AppendLine("	    <style type=\"text/css\">");
            mailBody.Append(Utility.MailCSS);
            mailBody.AppendLine("        </style>");
        }

        public void SendRuntimeResultsMail(WorkItemBO tfsItem, TTHTestRecord record, List<TTHTestRunInfo> runs, bool overallResult, bool bForceComplete, int maxElapsedTime)
        {
            string subject = null;

            if (bForceComplete)
            {
                subject = String.Format("{0} NDP {1} patch runtime test did not complete in {2} hours", Utility.TranslateOSName(tfsItem.OSInstalled, tfsItem.OSSPLevel), tfsItem.SKU, maxElapsedTime);
            }
            else
            {
                subject = String.Format("{0} NDP {1} patch runtime test {2} - {3}", Utility.TranslateOSName(tfsItem.OSInstalled, tfsItem.OSSPLevel), tfsItem.SKU, overallResult ? "PASSED" : "FAILED", Utility.ParsePatchGroupFromTFSTitle(tfsItem.Title));
            }

            MailContent = GetRuntimeTestMailBody(tfsItem, record, runs, overallResult, bForceComplete, maxElapsedTime);

            SendMail(RuntimeMailTo, RuntimeMailCC, subject);
        }

        private string GetRuntimeTestMailBody(WorkItemBO tfsItem, TTHTestRecord record, List<TTHTestRunInfo> runs, bool result, bool bForceComplete, int maxElapsedTime)
        {
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            mailBody.AppendLine("    <div class=\"Container\">");

            if (bForceComplete)
            {
                mailBody.AppendLine(String.Format("   <h2>Runtime smoke test did not complete in {0} hours for below patch</h2>", maxElapsedTime));
            }
            else
            {
                if (result)
                {
                    mailBody.AppendLine("   <h2 class=\"PassedResult\">Runtime test PASSED for below patch</h2>");
                }
                else
                {
                    mailBody.AppendLine("   <h2 class=\"FailedResult\">Runtime test FAILED for below patch</h2>");
                }
            }


            //Print basic info of this TFS item
            mailBody.AppendLine("   <table class=\"TableBasicInfo\">");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>TFS ID</b></td>");
            mailBody.AppendLine(String.Format("           <td><a href=\"{0}\">{1}</a></td>", "https://vstfdevdiv.corp.microsoft.com/DevDiv/DevDiv%20Servicing/_workitems#_a=edit&id=" + tfsItem.ServicingID, tfsItem.ServicingID));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>KB Article</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.KBNumber));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>SKU</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.SKU));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>Title</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", tfsItem.Title));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>PackageLocation</b></td>");
            mailBody.AppendLine(String.Format("           <td><a href=\"{0}\">{1}</a></td>", record.X64PatchLocation, record.X64PatchLocation));
            mailBody.AppendLine("       </tr>");

            //Add static test result link
            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td><b>Static Test Results</b></td>");
            mailBody.AppendLine(String.Format("           <td><a href=\"http://dtgpatchtest/patchtest/tools/GetWin10Log.aspx?id={0}\">Click here</a></td>", record.StaticTestLog));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("   </table>");
            mailBody.AppendLine("   <br/>");

            //Print table name
            mailBody.AppendLine(String.Format("            <p class=\"TableName\">Runtime test results</p>"));
            GetRuntimeTestResultTable(runs, mailBody);

            mailBody.AppendLine("    </div>");
            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            return mailBody.ToString();
        }

        private void GetRuntimeTestResultTable(List<TTHTestRunInfo> runs, StringBuilder mailBody)
        {
            mailBody.AppendLine("            <table class=\"TableResults\">");

            bool hasAnalysis = runs.Where(r => (!String.IsNullOrEmpty(r.AutoAnalysis) || !String.IsNullOrEmpty(r.ManualAnalysis))).Count() > 0;
            string[] colWidths = null;
            string[] colNames = null;
            if (!hasAnalysis)
            {
                colWidths = new string[] { "12%", "64%", "12%", "12%" };
                colNames = new string[] { "Run ID", "Title", "Result", "Log" };
            }
            else
            {
                colWidths = new string[] { "12%", "35%", "12%", "12%", "29%" };
                colNames = new string[] { "Run ID", "Title", "Result", "Log", "ResultAnalysis" };
            }

            string runResult;
            int colIndex = 0;

            //Print table columns
            mailBody.AppendLine("                <tr>");
            foreach (string colName in colNames)
            {
                mailBody.AppendLine(String.Format("                    <td class=\"TableResultsColumnHeader\" width=\"{0}\">{1}</td>", colWidths[colIndex++], colName));
            }
            mailBody.AppendLine("                </tr>");

            //Print table rows
            foreach (TTHTestRunInfo r in runs)
            {
                colIndex = 0;
                
                mailBody.AppendLine("                <tr>");

                //Run ID
                mailBody.AppendLine(String.Format("                    <td align=\"left\" width=\"{0}\"><a href=\"http://maddog/goto/MDGoto.Application?md://OrcasTS/Run/{1}\">{2}</a></td>", colWidths[colIndex++], r.MDRunID, r.MDRunID));

                //Run title
                mailBody.AppendLine(String.Format("                    <td width=\"{0}\">{1}</td>", colWidths[colIndex++], r.Title));

                //Run result
                if (r.RunStatusID != (int)Helper.RunStatus.Completed)
                {
                    runResult = ((Helper.RunStatus)r.RunStatusID).ToString();
                }
                else
                {
                    runResult = ((Helper.RunResult)r.RunResultID).ToString();
                }

                if (runResult == "Passed")
                {
                    mailBody.AppendLine(String.Format("                    <td width=\"{0}\" class=\"PassedResult\">Passed</td>", colWidths[colIndex++]));
                }
                else if (!runResult.Equals("Running"))
                {
                    mailBody.AppendLine(String.Format("                    <td width=\"{0}\" class=\"FailedResult\">{1}</td>", colWidths[colIndex++], runResult));
                }
                else
                {
                    mailBody.AppendLine(String.Format("                    <td width=\"{0}\">{1}</td>", colWidths[colIndex++], runResult));
                }

                //View log
                mailBody.AppendLine(String.Format("                    <td width=\"{0}\"><a href=\"file://mdfile3/OrcasTS/Files/Core/Results/Run{1}\">View Log</a></td>", colWidths[colIndex++], r.MDRunID));

                //auto analysis
                if (hasAnalysis)
                {
                    string comments = String.IsNullOrEmpty(r.ManualAnalysis) ? r.AutoAnalysis : r.ManualAnalysis;

                    if (!String.IsNullOrEmpty(comments))
                    {
                        string[] lines = comments.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                        string content = String.Join("<br>", lines);

                        mailBody.AppendLine(String.Format("                    <td width=\"{0}\">{1}</td>", colWidths[colIndex++], content));
                    }
                    else
                    {
                        mailBody.AppendLine(String.Format("                    <td width=\"{0}\"></td>", colWidths[colIndex++]));
                    }
                }

                mailBody.AppendLine("                </tr>");
            }

            mailBody.AppendLine("            </table>");
        }

        private List<string> BuildHtmlElementAttributes(DataTable table)
        {
            List<string> attrList = new List<string>();
            StringBuilder sb = new StringBuilder();

            foreach (DataColumn col in table.Columns)
            {
                foreach (DictionaryEntry de in col.ExtendedProperties)
                {
                    if(IsSupportedHtmlAttribute(de.Key.ToString()))
                        sb.Append(BuildHtmlElementAttribute(de.Key.ToString(), de.Value.ToString()));
                }

                attrList.Add(sb.ToString());
                sb.Clear();
            }

            return attrList;
        }

        private string BuildHtmlElementAttribute(object key, object value)
        {
            return String.Format("{0}=\"{1}\" ", key, value);
        }

        private bool IsSupportedHtmlAttribute(string attrName)
        {
            switch (attrName)
            {
                case "width":
                case "text-align":
                case "style":
                    return true;

                default:
                    return false;
            }
        }
    }
}
