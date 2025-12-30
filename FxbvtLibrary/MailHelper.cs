using FxbvtLibrary.GoFxServMailerService;
using MadDogObjects;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static MadDogObjects.Tables;

namespace FxbvtLibrary
{
    public class MailHelper
    {
        public string[] MailTo { get; set; }
        public string[] MailCC { get; set; }
        private GoFxServMailerSoapClient MailClient = new GoFxServMailerSoapClient("GoFxServMailerSoap");
        public MailHelper()
        {
            ServicePointManager.ServerCertificateValidationCallback =
               delegate (object sender, X509Certificate certificate, X509Chain
    chain, SslPolicyErrors sslPolicyErrors)
               {
                   return true;
               };
            //MailTo = ConfigurationManager.AppSettings["MailTo"].Split(';').ToArray();
            MailCC = ConfigurationManager.AppSettings["MailCC"].Split(';').ToArray();
        }

        public void SendMailForFailedTests(Run run, List<FXBVTFailedCase> failedCaseList, string testTeam)
        {
            string prefix = testTeam == "FXBVT" ? $"{GetTeamNameFromRunTitle(run.Title)} - " : string.Empty;
            string subject = $"{prefix}Failed MTP Run {run.ID} {run.Title}";

            #region Generates Mail Body
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            mailBody.AppendLine($"<h4>Run <a href=\"https://aka.ms/mdgoto/?md://OrcasTS/Run/{run.ID}\">{run.ID}</a> still has cases failed after 3 reset as below:</h4>");

            mailBody.AppendLine("	    <table class=\"TableStyle\" >");
            // Table header
            mailBody.AppendLine("            <tr>");
            mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Case&nbsp;ID</strong></td>");
            mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Case&nbsp;Name</strong></td>");
            mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Context&nbsp;ID</strong></td>");
            mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Context&nbsp;Name</strong></td>");
            mailBody.AppendLine("            </tr>");
            foreach (FXBVTFailedCase testCase in failedCaseList)
            {
                mailBody.AppendLine("            <tr>");
                mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", testCase.TestCaseID));
                mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", testCase.TestCaseName));
                mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", testCase.ContextID));
                mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", testCase.ContextName));
                mailBody.AppendLine("            </tr>");
            }
            mailBody.AppendLine("        </table>");

            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            #endregion

            MailClient.SendEmail(GetMailTo(run.Title, testTeam), MailCC, subject, mailBody.ToString());

        }
        public void SendMailAboutAbnormalRun(Run run, int machineID, string reason, string testTeam)
        {

            string subject = string.Empty;

            #region Generates Mail Body
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");
            switch (reason)
            {
                case "LackOfMachines":
                    subject = $"{GetTeamNameFromRunTitle(run.Title)} - Lack of test machine for run {run.ID} {run.Title}";
                    mailBody.AppendLine($"<h4>Run <a href=\"https://aka.ms/mdgoto/?md://OrcasTS/Run/{run.ID}\">{run.ID}</a> didn't get a machine after 2 hours</h4>");
                    break;
                case "MachineH/B":
                    subject = $"{GetTeamNameFromRunTitle(run.Title)} - H/B machine {machineID} detected in run {run.ID} {run.Title}";
                    mailBody.AppendLine($"<h4>Machine <a href=\"https://aka.ms/mdgoto/?md://OrcasTS/Machine/{machineID}\">{machineID}</a> has turned into Hung/Broken state. Please check. Run ID: <a href=\"https://aka.ms/mdgoto/?md://OrcasTS/Run/{run.ID}\">{run.ID}</a></h4>");
                    break;
            }


            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            #endregion

            MailClient.SendEmail(GetMailTo(run.Title, testTeam), MailCC, subject, mailBody.ToString());

        }
        public void SendSummaryMailForFailedTests(string team, Dictionary<Run, List<FXBVTFailedCase>> runFailedCasesPairs)
        {
            if (team == "NCL")
                team = "NCL Functional";
            string subject = $"{team} - Summary for Failed MTP Runs";

            #region Generates Mail Body
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            foreach (KeyValuePair<Run, List<FXBVTFailedCase>> runFailedCases in runFailedCasesPairs)
            {
                mailBody.AppendLine($"<h4>Run <a href=\"https://aka.ms/mdgoto/?md://OrcasTS/Run/{runFailedCases.Key.ID}\">{runFailedCases.Key.ID}</a> {runFailedCases.Key.Title} still has cases failed after 3 reset as below:</h4>");
                mailBody.AppendLine("        <table class=\"TableStyle\" >");
                // Table header
                mailBody.AppendLine("            <tr>");
                mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Case&nbsp;ID</strong></td>");
                mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Case&nbsp;Name</strong></td>");
                mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Context&nbsp;ID</strong></td>");
                mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Context&nbsp;Name</strong></td>");
                mailBody.AppendLine("            </tr>");

                foreach (FXBVTFailedCase fXBVTFailedCase in runFailedCases.Value)
                {
                    mailBody.AppendLine("            <tr>");
                    mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", fXBVTFailedCase.TestCaseID));
                    mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", fXBVTFailedCase.TestCaseName));
                    mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", fXBVTFailedCase.ContextID));
                    mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", fXBVTFailedCase.ContextName));
                    mailBody.AppendLine("            </tr>");
                }
                mailBody.AppendLine("        </table>");
                mailBody.AppendLine("");
            }
            

            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            #endregion

            MailClient.SendEmail(ConfigurationManager.AppSettings[$"MailTo{team}"].Split(';').ToArray(), MailCC, subject, mailBody.ToString());

        }
        private void AddCssToMailBody(StringBuilder mailBody)
        {
            mailBody.AppendLine("	    <style type=\"text/css\">");
            mailBody.AppendLine("            .PassedResultStyle");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                color: green;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .FailedResultStyle");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                color: red;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .NotCompletedResultStyle");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("            color: orange;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .NotStartedResultStyle");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                color: black;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .IDHeaderColumn");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width: 10%;");
            mailBody.AppendLine("                text-align: center;");
            mailBody.AppendLine("                background-color:#6699FF;");
            mailBody.AppendLine("                color: #FFFFFF;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .IDContentColumn");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width: 10%;");
            mailBody.AppendLine("                text-align: center;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .DescriptionHeaderColumn");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width:40%;");
            mailBody.AppendLine("                text-align: center;");
            mailBody.AppendLine("                background-color:#6699FF;");
            mailBody.AppendLine("                color: #FFFFFF;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .DescriptionContentColumn");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width:40%;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .ResultHeaderColumn");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width:50%;");
            mailBody.AppendLine("                text-align: center;");
            mailBody.AppendLine("                background-color:#6699FF;");
            mailBody.AppendLine("                color: #FFFFFF;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .ResultContentColumn");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width:50%;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("            .TableStyle");
            mailBody.AppendLine("            {");
            mailBody.AppendLine("                width:100%;");
            mailBody.AppendLine("                border-collapse:collapse;");
            mailBody.AppendLine("                border: solid #000000;");
            mailBody.AppendLine("                border-width:1px;");
            mailBody.AppendLine("            }");
            mailBody.AppendLine("        </style>");
        }

        private string GetTeamNameFromRunTitle(string runTitle)
        {
            string teamName = string.Empty;
            if (runTitle.StartsWith("Clone"))
                teamName = runTitle.Substring(16, runTitle.IndexOf("NDP") - 10);
            else
                teamName = runTitle.Substring(7, runTitle.IndexOf("NDP") - 10);
            return teamName;
        }

        private string[] GetMailTo(string runTtile, string testTeam)
        {
            if (testTeam == "CLR")
            {
                return ConfigurationManager.AppSettings["MailToCLR"].Split(';').ToArray();
            }
            else if (testTeam == "FXBVT")
            {
                return ConfigurationManager.AppSettings[$"MailTo{GetTeamNameFromRunTitle(runTtile)}"].Split(';').ToArray();
            }
            return ConfigurationManager.AppSettings["MailToAdmin"].Split(';').ToArray();
        }
    }
}