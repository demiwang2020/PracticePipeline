using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using ScorpionDAL;
using System.Globalization;

namespace ReportEngine
{
    public enum MailType
    {
        Text,
        HTML
    }

    public struct MailContentData
    {
        public int[,] Breakdowndata;
        public List<string> PatchList;
        public List<string> TestList;
        public List<FailTestCase> FailTestCaseList;
    }

    class MailContent
    {
        private MailType contentType;
        private Dictionary<string, string> Parameters;
        private string content;
        public TestResult result { get; set; }
        public LinqHelper.ReportType reportType { get; set; }
        MailContentData mailContentData;
        private int totalPass;
        private int totalFail;

        public MailContent()
        {
            this.contentType = MailType.Text;
            this.Parameters = new Dictionary<string,string>();
            this.content = string.Empty;
            this.result = TestResult.Success;
            this.totalPass = 0;
            this.totalFail = 0;
        }

        public MailContent(MailType type, Dictionary<string, string> paramlist, TestResult result, LinqHelper.ReportType runType, MailContentData myMailContentData)
        {
            this.contentType = type;
            this.Parameters = paramlist;
            this.content = string.Empty;
            this.result = result;
            this.totalPass = 0;
            this.totalFail = 0;
            this.reportType = runType;
            this.mailContentData = myMailContentData;

            this.Parameters.Add(@"SUMMARYTABLE", GenerateBreakDownSummaryTable());

            if (result == TestResult.Success)
            {
                this.Parameters.Add(@"SUMMARY", string.Format("({0}/{1})", this.totalPass, this.totalPass + this.totalFail));
            }
            else if (result == TestResult.Fail)
            {
                this.Parameters.Add(@"SUMMARY", string.Format("({0}/{1})", this.totalFail, this.totalPass + this.totalFail));
            }

            if (result == TestResult.Fail)
            {
                this.Parameters.Add(@"ERRORTABLES", GenerateErrorListSummaryTable());
            }
        }

        public string Generate()
        {
            string templateFile = string.Empty;
            if (this.contentType == MailType.Text)
            {
                switch (this.result)
                {
                    case TestResult.Success:
                        templateFile = (reportType == LinqHelper.ReportType.SAFX) ? "email_success_safx.txt" : "email_success_runtime.txt";
                        break;
                    case TestResult.Fail:
                        templateFile = (reportType == LinqHelper.ReportType.SAFX) ? "email_fail_safx.txt" : "email_fail_runtime.txt";
                        break;
                }
            }
            else if (this.contentType == MailType.HTML)
            {
                switch (this.result)
                {
                    case TestResult.Success:
                        templateFile = (reportType == LinqHelper.ReportType.SAFX) ? "email_success_safx.html" : "email_success_runtime.html";
                        break;
                    case TestResult.Fail:
                        templateFile = (reportType == LinqHelper.ReportType.SAFX) ? "email_fail_safx.html" : "email_fail_runtime.html";
                        break;
                }
            }
            templateFile = Path.Combine(System.Configuration.ConfigurationManager.AppSettings["MailTemplateLocation"], templateFile);
            this.content = (this.contentType == MailType.Text) ?GenerateText(templateFile) : GenerateHtml(templateFile);
            return this.content;
        }

        public string GetContent()
        {
            return this.content;
        }
        // Save the email content to a file under subdirectory 'data'
        //
        public void Save()
        {
            // save the content to a file. This can be used for testing purpose
            string ext = (contentType == MailType.HTML) ? ".html" : ".txt";
            DateTime now = DateTime.Now;
            string fileName = string.Empty;
            if (this.Parameters != null && this.Parameters.ContainsKey("KBNUMBER"))
            {
                fileName = "report_" + this.Parameters["KBNUMBER"] + "_" + now.Year + "_" + now.Month + "_" + now.Day + "_" + now.Hour + "_" + now.Minute + "_" + now.Second + ext;
            }
            else
            {
                fileName = "report_" + RandomString(6) + "_" + now.Year + "_" + now.Month + "_" + now.Day + "_" + now.Hour + "_" + now.Minute + "_" + now.Second + ext;
            }
            string filePath = @"\\vsufile\Workspace\Current\SetupTest\Mail\Prod";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            fileName = Path.Combine(filePath, fileName);
            File.WriteAllText(fileName, this.content);
        }


        // This methood will replace a variable defined [%VAR%] with its actual value. The following rules are assumed. 
        //     1) the variable name can be 26 letters (lowercase or uppercase) and 10 digits (0 ~ 9)
        //     2) it always starts with [% and ends with %]
        //     3) if a variable is not found in the dictionary, it will be replaced with string.empty
        //  For example,  Parameters[KB]=123456
        //                [%KB%] will be replaced with 123456
        private string GenerateText(string templateFile)
        {
            string readText = File.ReadAllText(templateFile);
            while (true)
            {
                int start = readText.IndexOf("[%");
                int end = readText.IndexOf("%]");
                if (start < 0 || end < 0 || (end - start <= 2))
                {
                    break;
                }
                string varName = readText.Substring(start + 2, end - start - 2);
                if (!IsAlphaNum(varName))
                {
                    break;
                }

                if (this.Parameters.ContainsKey(varName))
                {
                    readText = readText.Replace("[%" + varName + "%]", this.Parameters[varName]);
                }
                else
                {
                    readText = readText.Replace("[%" + varName + "%]", string.Empty);
                }
            }
            return readText;
        }

        private string GenerateHtml(string templateFile)
        {
            return GenerateText(templateFile);
        }


        /**************************************************************************************************
        <table border="1" cellspacing="0" cellpadding="0">
          <tr>
            <td width="103" valign="top"><p><strong>Breakdown:</strong> </p></td>
            <td width="270" valign="top"><p align="center"><strong>Test Case</strong> </p></td>
            <td width="120" colspan="2" valign="top"><p align="center">KB9999013-x86.exe  </p></td>
            <td width="117" colspan="2" valign="top"><p align="center">KB9999013-x64.exe</p></td>
            <td width="141" colspan="2" valign="top"><p align="center">KB9999013-ia64.exe</p></td>
          </tr>
          <tr>
            <td width="103" valign="top"><p>&nbsp;</p></td>
            <td width="270" valign="top"><p>&nbsp;</p></td>
            <td width="60" valign="top"><p align="center">PASS</p></td>
            <td width="60" valign="top"><p align="center">FAIL</p></td>
            <td width="58" valign="top"><p align="center">PASS</p></td>
            <td width="58" valign="top"><p align="center">FAIL</p></td>
            <td width="71" valign="top"><p align="center">PASS</p></td>
            <td width="71" valign="top"><p align="center">FAIL</p></td>
          </tr>
          <tr>
            <td width="103" valign="top"><p>&nbsp;</p></td>
            <td width="270" valign="top"><p>[SAFX      Test Case Name 1]</p></td>
            <td width="60" valign="top"><p align="center">1</p></td>
            <td width="60" valign="top"><p align="center">0</p></td>
            <td width="58" valign="top"><p align="center">1</p></td>
            <td width="58" valign="top"><p align="center">-</p></td>
            <td width="71" valign="top"><p align="center">1</p></td>
            <td width="71" valign="top"><p align="center">-</p></td>
          </tr>
          <tr>
            <td width="103" valign="top"><p>&nbsp;</p></td>
            <td width="270" valign="top"><p>[SAFX      Test Case Name 2]</p></td>
            <td width="60" valign="top"><p align="center">1</p></td>
            <td width="60" valign="top"><p align="center">0</p></td>
            <td width="58" valign="top"><p align="center">0 </p></td>
            <td width="58" valign="top"><p align="center">1 </p></td>
            <td width="71" valign="top"><p align="center">1</p></td>
            <td width="71" valign="top"><p align="center">-</p></td>
          </tr>
          <tr>
            <td width="103" valign="top"><p><strong>Total</strong> </p></td>
            <td width="270" valign="top"><p align="center"><strong>3</strong></p></td>
            <td width="60" valign="top"><p align="center">3</p></td>
            <td width="60" valign="top"><p align="center">0</p></td>
            <td width="58" valign="top"><p align="center">2 </p></td>
            <td width="58" valign="top"><p align="center">1</p></td>
            <td width="71" valign="top"><p align="center">3 </p></td>
            <td width="71" valign="top"><p align="center">0</p></td>
          </tr>
          <tr>
            <td width="103" valign="top"><p><strong>PASS RATE</strong></p></td>
            <td width="270" valign="top"><p align="center"><strong>&nbsp;</strong></p></td>
            <td width="120" colspan="2" valign="top"><p align="center">100%</p></td>
            <td width="117" colspan="2" valign="top"><p align="center">66.7%</p></td>
            <td width="141" colspan="2" valign="top"><p align="center">100%</p></td>
          </tr>
      </table>
         **************************************************************************************************/
        private string GenerateBreakDownSummaryTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"        <table border=""1"" cellspacing=""0"" cellpadding=""0"">");

            // append patch name to the top row
            sb.AppendLine(@"          <tr>");
            sb.AppendLine(@"            <td width=""103"" valign=""top""><p><strong>Breakdown:</strong> </p></td>");
            sb.AppendLine(@"            <td width=""270"" valign=""top""><p align=""center""><strong>Test Case</strong> </p></td>");
            foreach (string patchName in mailContentData.PatchList)
            {
                sb.AppendFormat("{0}{1}{2}\r\n",@"            <td width=""120"" colspan=""2"" valign=""top""><p align=""center"">",patchName,@"  </p></td>");
            }
            sb.AppendLine(@"          </tr>");
            
            // Append table header row
            sb.AppendLine(@"          <tr>");
            sb.AppendLine(@"            <td width=""103"" valign=""top""><p>&nbsp;</p></td>");
            sb.AppendLine(@"            <td width=""270"" valign=""top""><p>&nbsp;</p></td>");
            for (int i = 1; i <= mailContentData.PatchList.Count; i++)
            {
                sb.AppendLine(@"            <td width=""60"" valign=""top""><p align=""center"">PASS</p></td>");
                sb.AppendLine(@"            <td width=""60"" valign=""top""><p align=""center"">FAIL</p></td>");
            }
            sb.AppendLine(@"          </tr>");

            // Append each test cast result
            int row = 0;
            int col = 0;
            int totalPatches = mailContentData.PatchList.Count;
            int[] passCounter = new int[totalPatches];
            int[] failCounter = new int[totalPatches];
            foreach (string test in mailContentData.TestList)
            {
                col = 0;
                sb.AppendLine(@"          <tr>");
                sb.AppendLine(@"            <td width=""103"" valign=""top""><p>&nbsp;</p></td>");
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""270"" valign=""top""><p>[", test, @"]</p></td>");
                foreach (string patch in mailContentData.PatchList)
                {
                    passCounter[col/2] += mailContentData.Breakdowndata[row, col];
                    failCounter[col/2] += mailContentData.Breakdowndata[row, col+1];
                    this.totalPass += mailContentData.Breakdowndata[row, col];
                    this.totalFail += mailContentData.Breakdowndata[row, col+1];
                    sb.AppendFormat("{0}{1}{2}\r\n", @"<td width=""60"" valign=""top""><p align=""center"">", mailContentData.Breakdowndata[row, col], @"</p></td>");

                    if (mailContentData.Breakdowndata[row, col + 1] > 0)
                    {
                        //Fix for task#824193, Make the fail counts more prominent. Using Bold and red background.
                        sb.AppendFormat("{0}{1}{2}\r\n", @"<td width=""60"" valign=""top"" style=""background-color: red;""><p align=""center""> <font> <strong>", mailContentData.Breakdowndata[row, col + 1], @"</strong></font></p></td>");
                    }
                    else
                    {
                        sb.AppendFormat("{0}{1}{2}\r\n", @"<td width=""60"" valign=""top""><p align=""center"">", mailContentData.Breakdowndata[row, col + 1], @"</p></td>");
                    }
                    col += 2;
                }
                sb.AppendLine(@"          </tr>");
                row++;
            }

            // append line summary for each patch

            // append percentage line summary
            sb.AppendLine(@"          <tr>");
            sb.AppendLine(@"            <td width=""103"" valign=""top""><p><strong>PASS RATE</strong></p></td>");
            sb.AppendLine(@"            <td width=""270"" valign=""top""><p align=""center""><strong>&nbsp;</strong></p></td>");
            for (int i = 0; i < totalPatches; i++)
            {
                string percentage = (decimal.Divide(passCounter[i], passCounter[i] + failCounter[i])).ToString("#0.##%", CultureInfo.InvariantCulture);
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""120"" colspan=""2"" valign=""top""><p align=""center"">", percentage, @"</p></td>");
            }
            
            sb.AppendLine(@"          </tr>");
            // end of table
            sb.AppendLine(@"      </table>");

            return sb.ToString();
        }


        /**************************************************************************************************
<table>
  <tr><td colspan="2" valign="top"><p><strong>All failures must be analyzed within ONE business day</strong><strong> </strong></p></tr>
  <tr>
   <td colspan="2" valign="top"><p><strong>All failures must be analyzed within ONE business day</strong><strong> </strong></p>
        <table border="1" cellspacing="0" cellpadding="0">
          <tr>
            <td width="968" colspan="4" valign="top" bgcolor="#FF0000"><p><strong>1 of 1</strong><strong> -</strong><strong> </strong><strong>Test Case </strong><a href="http://maddog/goto/MDGoto.Application?md://OrcasTS/Testcase/2158706;BranchID=536;"><strong>2158706</strong> </a><strong> : Test Case Name</strong><strong> </strong></p></td>
          </tr>
          <tr><td width="968" colspan="4" valign="top"><p>&lt;Placeholder      for error message&gt;</p></td></tr>
          <tr>
            <td width="109" valign="top"><p><strong>Maddog Result:</strong><strong> </strong></p></td>
            <td width="157" valign="top"><p> <a href="http://maddog/goto/MDGoto.Application?md://OrcasTS/Query/Result%2fRunID%3b%3d%3b1865231%3bAND%3bResultTypeID%3b%3d%3b2%3bAND%3b">Query </a> (for example) </p></td>
            <td width="82" valign="top"><p><strong>Start Time: </strong> </p></td>
            <td width="620" valign="top"><p>MM/DD/YYYY&nbsp;      HH:MM:SS TT </p></td>
          </tr>
          <tr>
            <td width="109" valign="top"><p><strong>Maddog RunID:</strong> </p></td>
            <td width="157" valign="top"><p>1742251 (for      example) </p></td>
            <td width="82" valign="top"><p><strong>Finish&nbsp;Time:</strong> </p></td>
            <td width="620" valign="top"><p>MM/DD/YYYY&nbsp;      HH:MM:SS TT </p></td>
          </tr>
          <tr>
            <td width="109" valign="top"><p><strong>Maddog Logs:</strong> </p></td>
            <td width="157" valign="top"><p> <a href="file:///\\mdfile3\OrcasTS\Files\Core\Results\Run1742251\2057916_512663_Logs.zip">Logs.zip </a> (for example) </p></td>
            <td width="82" valign="top"><p><strong>Duration:</strong> </p></td>
            <td width="620" valign="top"><p>HH:MM:SS </p></td>
          </tr>
      </table></td>
  </tr>
 <table>
        **************************************************************************************************/
        private string GenerateErrorListSummaryTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<table>");

            // Append each error record
            int total = mailContentData.FailTestCaseList.Count;
            int index = 0;
            foreach (FailTestCase test in mailContentData.FailTestCaseList)
            {
                index++;
                sb.AppendLine(@"  <tr>");
                sb.AppendLine(@"   <td colspan=""2"" valign=""top"">");
                sb.AppendLine(@"        <table border=""1"" cellspacing=""0"" cellpadding=""0"">");
                sb.AppendLine(@"          <tr>");
                // http://maddog/goto/MDGoto.Application?md://OrcasTS/Testcase/2158705;BranchID=536
                string testCaseLink = @"http://maddog/goto/MDGoto.Application?md://OrcasTS/Testcase/" + test.testID + ";BranchID=536";
                sb.AppendFormat("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\r\n", @"            <td width=""968"" colspan=""4"" valign=""top"" bgcolor=""#FF5050""><p><strong>",index,@" of ",total,@"</strong><strong> -</strong><strong> </strong><strong>Test Case </strong><a href=""",testCaseLink,@"""><strong>",test.testID,@"</strong> </a><strong> : ",test.Name,@"</strong><strong> </strong></p></td>");
                sb.AppendLine(@"          </tr>");

                // sb.AppendLine(@"          <tr><td width=""968"" colspan=""4"" valign=""top""><p>&lt;Placeholder      for error message&gt;</p></td></tr>");
                //  http://maddog/goto/MDGoto.Application?md://OrcasTS/Query/Result%2fRunID%3b%3d%3b1865231%3bAND%3bResultTypeID%3b%3d%3b2%3bAND%3b 
                sb.AppendLine(@"          <tr>");
                sb.AppendLine(@"            <td width=""109"" valign=""top""><p><strong>Maddog Result:</strong><strong> </strong></p></td>");
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""157"" valign=""top""><p> <a href=""http://maddog/goto/MDGoto.Application?md://OrcasTS/Query/Result%2fRunID%3b%3d%3b", test.runID, @"%3bAND%3bResultTypeID%3b%3d%3b2%3bAND%3b"">Query </a></p></td>");
                sb.AppendLine(@"            <td width=""82"" valign=""top""><p><strong>Start Time: </strong> </p></td>");
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""620"" valign=""top""><p>",test.startTime, @" </p></td>");
                sb.AppendLine(@"          </tr>");

                sb.AppendLine(@"          <tr>");
                sb.AppendLine(@"            <td width=""109"" valign=""top""><p><strong>Maddog RunID:</strong> </p></td>");
                // http://maddog/goto/MDGoto.Application?md://OrcasTS/Run/1930897 
                // <a href="http://maddog/goto/MDGoto.Application?md://OrcasTS/Run/1930897">1930897</a>
                // sb.AppendFormat("{0}{1}{2}{3}{4}\r\n", @"            <td width=""157"" valign=""top""><p><a href=""http://maddog/goto/MDGoto.Application?md://OrcasTS/Run/",test.runID,@""">",test.runID, @"</a></p></td>");
                sb.AppendFormat("{0}{1}{2}{3}{4}\r\n", @"            <td width=""157"" valign=""top""><p><a href=""",test.MaddogURL, @""">", test.runID, @"</a></p></td>");
                sb.AppendLine(@"            <td width=""82"" valign=""top""><p><strong>Finish&nbsp;Time:</strong> </p></td>");
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""620"" valign=""top""><p>",test.finishTime,@" </p></td>");
                sb.AppendLine(@"          </tr>");

                TimeSpan diff = test.finishTime - test.startTime;
                string duration = string.Empty;
                if (diff.Days == 0)
                {
                    duration = string.Format("{0}:{1}:{2}", diff.Hours, diff.Minutes, diff.Seconds);
                }
                else
                {
                    duration = string.Format("{0}.{1}:{2}:{3}", diff.Days,diff.Hours, diff.Minutes, diff.Seconds);
                }

                sb.AppendLine(@"          <tr>");
                sb.AppendLine(@"            <td width=""109"" valign=""top""><p><strong>Maddog Logs:</strong> </p></td>");
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""157"" valign=""top""><p> <a href=""",test.Log, @""">Logs</a></p></td>");
                sb.AppendLine(@"            <td width=""82"" valign=""top""><p><strong>Duration:</strong> </p></td>");
                sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""620"" valign=""top""><p>", duration, @" </p></td>");
                sb.AppendLine(@"          </tr>");

                // The following two lines are only needed for RunTime report
                if (this.reportType == LinqHelper.ReportType.RunTime)
                {
                    // MachineNameAndID example: "CPXVM267:1054488"
                    string[] split = test.MachineNameAndID.Split(':');
                    string machineID = string.Empty;
                    string machineName = string.Empty;
                    if (split.Length >= 2)
                    {
                        machineName = split[0];
                        machineID = split[1];
                    }
                    if (string.IsNullOrEmpty(machineID))
                    {
                        machineID = "invalidID";
                    }
                    if (string.IsNullOrEmpty(machineName))
                    {
                        machineName = "invalidName";
                    }
                    sb.AppendLine(@"          <tr>");
                    sb.AppendLine(@"            <td width=""109"" valign=""top""><p><strong>Repro Config:</strong> </p></td>");
                    sb.AppendFormat("{0}{1}{2}\r\n", @"            <td width=""620"" valign=""top""><p>", test.reproConfig, @" </p></td>");
                    sb.AppendLine(@"            <td width=""82"" valign=""top""><p><strong>Repro Machine:</strong> </p></td>");
                    sb.AppendFormat("{0}{1}{2}{3}{4}\r\n", @"            <td width=""157"" valign=""top""><p><a href=""http://maddog/goto/MDGoto.Application?md://OrcasTS/Machine/",machineID, @""">", machineName, @"</a></p></td>");
                    sb.AppendLine(@"          </tr>");

                    // add line for Login info and Note
                    sb.AppendLine(@"          <tr>");
                    sb.AppendLine(@"            <td width=""109"" valign=""top""><p><strong>Login Info:</strong> </p></td>");
                    sb.AppendFormat("{0}\r\n", @"            <td width=""157"" valign=""top""><p><a href=""file:\\vsufile\secure$\currentpassword.txt"">User Name and Password</a></p></td>");
                    sb.AppendLine(@"            <td width=""82"" valign=""top""><p><strong>Note:</strong> </p></td>");
                    sb.AppendFormat("{0}\r\n", @"            <td width=""157"" valign=""top""><p> To gain read access to this file, <a href=""http://devdiv/sites/netfx/Servicing/test/NetFX Servicing Test Wiki/How to gain access to currentpassword.txt where all lab test accounts info is saved.aspx"">open a one-time Techease ticket </a></p></td>");
                    sb.AppendLine(@"          </tr>");
                }

                sb.AppendLine(@"      </table></td>");
                sb.AppendLine(@"  </tr>");
            }

            sb.AppendLine(@" <table>");

            return sb.ToString();
        }

        private string RandomString(int size) 
        { 
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private bool IsAlphaNum(string str)
        {
            Regex r = new Regex("^[a-zA-Z0-9]*$");
            if (r.IsMatch(str))
            {
                return true;
            }
            return false;
        }

    }
}
