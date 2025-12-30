using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Net;
using System.IO;
using ScorpionDAL;
using HotFixLibrary;

namespace WorkerProcess
{
    class MailHelper
    {
        public string MailAccount { get; set; }
        public string SmtpServer { get; set; }
        // use ; to seperate each user 
        public string MailTo { get; set; }
        // use ; to seperate each user
        public string MailCC { get; set; }

        private int DuplicateTimes = 3;

        public MailHelper(string userName)
        {
            MailAccount = userName + "@microsoft.com";
            // Default smtp server
            SmtpServer = "smtphost.redmond.corp.microsoft.com";
        }

        //KickOff Report

        public MailHelper(string userName, string smtpServer)
        {
            MailAccount = userName + "@microsoft.com";
            SmtpServer = smtpServer;
        }

        public void SendMailAboutAbnormalRun(int runID, int interval)
        {
            /*
             *Subject: 
             *      Report of Abnormal Runs [6/25/2012 14:52]
             *Mail Body: 
             *      Run Statistics Overview:
             *      The different between Created Date and Last Modified Date is more than 24 hours
             *      RunIDs:
             *      1862333
             *      1862334
             *      1862335
             * 
             */

            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
                throw new Exception("To list and CC list cannot be empty at same time");

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }

            mail.Subject = string.Format("Abnormal Run - RunID: {0}", runID);
            mail.IsBodyHtml = true;
            mail.Body = GetAbnormalRunMailBody(runID, interval);
            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            server.Send(mail);

        }

        public void SendMailAboutAbnormalRun(TJob objTJob, List<TRun> lstRuns, int interval)
        {
            #region Prepares Mail Account&TOs&CCs
            if (MailTo.Equals(string.Empty) && MailCC.Equals(string.Empty))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }
            #endregion

            mail.Subject = String.Format("Abnormal Runs of Job [{0}] - PID - [{1}] - Status - [{2}], Result - [{3}], PercentCompleted - [{4}%]",
                  objTJob.JobID, objTJob.PID, HotFixLibrary.HotFixUtility.GetStatusName(objTJob.StatusID), HotFixLibrary.HotFixUtility.GetResultName(objTJob.ResultID), objTJob.PercentCompleted);
            mail.IsBodyHtml = true;

            /*
            *Subject: 
            *      Report of Abnormal Runs [6/25/2012 14:52]
            *Mail Body: 
            *      The different between Created Date and Last Modified Date is more than 24 hours
            *      Run as Below:
            *      RunID        Interval Hours
            *      1862333      26
            *      1862334      24
            *      1862335      22
            * 
            */

            #region Generates Mail Body
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            //for each Run if the diff between Created Date and Last Modified Date is more than 24 hours
            // (keep it configurable), send an email with Run ID. 

            mailBody.AppendLine(string.Format("	    The different between Created Date and Last Modified Date is more than {0} hours for this run.", interval));
            mailBody.AppendLine("<h4>RunIDs as below:</h4>");
            mailBody.AppendLine("Run&nbsp ID&nbsp; Actual Interval Hours</br>");
            foreach (var run in lstRuns)
            {
                mailBody.AppendLine(String.Format("{0}&nbsp; {1}</br>", run.MDRunID, Math.Round((run.LastModifiedDate - run.CreatedDate).TotalHours)));
            }

            //mailBody.AppendLine("        </table>");
            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            mail.Body = mailBody.ToString();
            mail.From = new MailAddress(MailAccount);
            #endregion

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            server.Send(mail);

        }

        public void SendMailAboutAbnormalRun(TJob objTJob, List<TNetFxSetupRunStatus> lstRuns, int interval)
        {
            #region Prepares Mail Account&TOs&CCs
            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }
            #endregion

            mail.Subject = String.Format("Abnormal Runs of Job [{0}] - PID - [{1}] - Status - [{2}], Result - [{3}], PercentCompleted - [{4}%]",
                  objTJob.JobID, objTJob.PID, HotFixLibrary.HotFixUtility.GetStatusName(objTJob.StatusID), HotFixLibrary.HotFixUtility.GetResultName(objTJob.ResultID), objTJob.PercentCompleted);
            mail.IsBodyHtml = true;

            /*
            *Subject: 
            *      Report of Abnormal Runs [6/25/2012 14:52]
            *Mail Body: 
            *      The different between Created Date and Last Modified Date is more than 24 hours
            *      Run as Below:
            *      RunID        Interval Hours
            *      1862333      26
            *      1862334      24
            *      1862335      22
            * 
            */

            #region Generates Mail Body
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            //for each Run if the diff between Created Date and Last Modified Date is more than 24 hours
            // (keep it configurable), send an email with Run ID. 

            mailBody.AppendLine(string.Format("	    The different between Created Date and Last Modified Date is more than {0} hours for this run.", interval));
            mailBody.AppendLine("<h4>RunIDs as below:</h4>");
            mailBody.AppendLine("Run&nbsp ID&nbsp;&nbsp;&nbsp; Actual Interval Hours&nbsp;&nbsp;Run Title</br>");
            foreach (var run in lstRuns)
            {
                mailBody.AppendLine(String.Format("<a href='http://maddog/goto/MDGoto.Application?md://OrcasTS/Run/{0}'>{0}</a>&nbsp;&nbsp; {1}&nbsp;&nbsp; {2}</br>",
                    run.MDRunID, Math.Round((run.LastModifiedDate - run.CreatedDate).TotalHours), run.RunTitle));
            }

            //mailBody.AppendLine("        </table>");
            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            mail.Body = mailBody.ToString();
            #endregion

            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            try
            {
                server.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            #region Del Since it may send 3 times in special case which is unknow
            //int sendingCount = 0;
            ////why also sending if failed, since server could make sending failed, so duplicate some times lead to sucess.
            //while (sendingCount++ < DuplicateTimes)
            //{
            //    try
            //    {
            //        mail.From = new MailAddress(MailAccount);

            //        SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            //        server.UseDefaultCredentials = true;

            //        server.Send(mail);
            //        break;
            //    }
            //    catch (SmtpException ex)
            //    {
            //        if (sendingCount >= DuplicateTimes)
            //        {
            //            throw ex;
            //        }
            //    }
            //}
            #endregion
        }

        public void SendMailAboutAbnormalWURun(TWUJob objTJob, List<TWURun> lstRuns, int interval)
        {
            #region Prepares Mail Account&TOs&CCs
            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!MailTo.Equals(string.Empty))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!MailCC.Equals(string.Empty))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }
            #endregion

            mail.Subject = String.Format("Abnormal Runs of WU Job [{0}] {1} - Status - [{2}], Result - [{3}], PercentCompleted - [{4}%]",
                  objTJob.ID, objTJob.JobDescription, HotFixLibrary.HotFixUtility.GetStatusName(objTJob.StatusID), HotFixLibrary.HotFixUtility.GetResultName(objTJob.ResultID), objTJob.PercentCompleted);
            mail.IsBodyHtml = true;

            /*
            *Subject: 
            *      Report of Abnormal Runs [6/25/2012 14:52]
            *Mail Body: 
            *      The different between Created Date and Last Modified Date is more than 24 hours
            *      Run as Below:
            *      RunID        Interval Hours
            *      1862333      26
            *      1862334      24
            *      1862335      22
            * 
            */

            #region Generates Mail Body
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            //for each Run if the diff between Created Date and Last Modified Date is more than 24 hours
            // (keep it configurable), send an email with Run ID. 

            mailBody.AppendLine(string.Format("	    The different between Created Date and Last Modified Date is more than {0} hours for this run.", interval));
            mailBody.AppendLine("<h4>RunIDs as below:</h4>");
            mailBody.AppendLine("Run&nbsp ID&nbsp;&nbsp;&nbsp; Actual Interval Hours&nbsp;&nbsp;Run Title</br>");
            foreach (var run in lstRuns)
            {
                mailBody.AppendLine(String.Format("<a href='http://maddog/goto/MDGoto.Application?md://OrcasTS/Run/{0}'>{0}</a>&nbsp;&nbsp; {1}&nbsp;&nbsp; {2}</br>",
                    run.MDRunID, interval, run.Title));
            }

            //mailBody.AppendLine("        </table>");
            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            mail.Body = mailBody.ToString();
            #endregion

            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            try
            {
                server.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string GetAbnormalRunMailBody(int runID, int interval)
        {
            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            //for each Run if the diff between Created Date and Last Modified Date is more than 24 hours
            // (keep it configurable), send an email with Run ID. 

            mailBody.AppendLine(string.Format("	    The different between Created Date and Last Modified Date is more than {0} hours for this run.", interval));

            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            return mailBody.ToString();
        }

        public void SendMailAfterKickingOff(TJob objTJob, List<TTestProdInfo> lstTPatch, List<TTestProdAttribute> lstTPatchFile, List<TRun> lstTRun)
        {
            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }

            mail.Subject = String.Format("Job [{0}] - PID - [{1}] - Status - [{2}], Result - [{3}], PercentCompleted - [{4}%]",
                    objTJob.JobID, objTJob.PID, HotFixLibrary.HotFixUtility.GetStatusName(objTJob.StatusID), HotFixLibrary.HotFixUtility.GetResultName(objTJob.ResultID), objTJob.PercentCompleted);
            mail.IsBodyHtml = true;
            mail.Body = GetAfterKickingOffMailBody(lstTPatch, lstTPatchFile, lstTRun);

            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            server.Send(mail);
        }

        public void SendMailAfterKickingOff(TJob objTJob, List<RunReport> lstRunReport, string fileList = "")
        {
            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }

            mail.Subject = String.Format("Job [{0}] - PID - [{1}] - Status - [{2}], Result - [{3}], PercentCompleted - [{4}%]",
                    objTJob.JobID, objTJob.PID, HotFixLibrary.HotFixUtility.GetStatusName(objTJob.StatusID), HotFixLibrary.HotFixUtility.GetResultName(objTJob.ResultID), objTJob.PercentCompleted);
            mail.IsBodyHtml = true;
            mail.Body = GetAfterKickingOffMailBody(lstRunReport, fileList);

            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            server.Send(mail);
        }

        private string GetAfterKickingOffMailBody(List<RunReport> lstRunReport, string fileList)
        {
            /*
             * Subject:
             *      Job[1] - PID[953526] - Status[Running] - Result[Unkown] - PercentCompleted[0%] - [6/25/2012 14:25]
             *
             *      Run Details			
             *      RunID	  RunTitle      Status	    
             *      1871172	                Completed	
             *      1871173	                Completed	
             *      1871174	                Completed	
             * 
             * The one(s) in Red back color is(are) Failed.				
             * The one(s) in Green back color is(are) Passed.				
             * The one(s) in Yellow back color is(are) failed which status based on sub items.				
             */

            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            if (fileList != "")
            {
                mailBody.AppendLine("	    <h4>Binaries Details:</h4>");
                mailBody.AppendLine(fileList);
            }

            var SetupRuns = lstRunReport.Where(c => c.IsSAFXRun == false);
            if (SetupRuns.Count() > 0)
            {
                mailBody.AppendLine("</br>");
                mailBody.AppendLine("	    <h4>Setup Automated Run Details:</h4>");

                mailBody.AppendLine("	    <table class=\"TableStyle\" >");
                // Table header
                mailBody.AppendLine("            <tr>");
                mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Run&nbsp; ID</strong></td>");
                mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Run&nbsp; Title</strong></td>");
                mailBody.AppendLine("                <td class=\"ResultHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Status</strong></td>");
                mailBody.AppendLine("            </tr>");

                foreach (RunReport run in SetupRuns)
                {
                    mailBody.AppendLine("            <tr>");
                    mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", run.RunID));
                    mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", run.RunTitle));
                    mailBody.AppendLine("                <td class=\"ResultContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", run.Status));
                    mailBody.AppendLine("            </tr>");
                }
                mailBody.AppendLine("        </table>");
            }

            var SAFXRuns = lstRunReport.Where(c => c.IsSAFXRun == true);
            if (SAFXRuns.Count() > 0)
            {
                mailBody.AppendLine("</br>");
                mailBody.AppendLine("	    <h4>SAFX Automated Run Details:</h4>");
                mailBody.AppendLine("	    <table class=\"TableStyle\" >");
                // Table header
                mailBody.AppendLine("            <tr>");
                mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Run&nbsp; ID</strong></td>");
                mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Run&nbsp; Title</strong></td>");
                mailBody.AppendLine("                <td class=\"ResultHeaderColumn\">");
                mailBody.AppendLine("                    <strong>Status</strong></td>");
                mailBody.AppendLine("            </tr>");

                foreach (RunReport run in SAFXRuns)
                {
                    mailBody.AppendLine("            <tr>");
                    mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", run.RunID));
                    mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", run.RunTitle));
                    mailBody.AppendLine("                <td class=\"ResultContentColumn\">");
                    mailBody.AppendLine(String.Format("                    {0}</td>", run.Status));
                    mailBody.AppendLine("            </tr>");
                }

                mailBody.AppendLine("        </table>");
            }
            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            return mailBody.ToString();
        }

        private string GetAfterKickingOffMailBody(List<TTestProdInfo> lstTPatch, List<TTestProdAttribute> lstTPatchFile, List<TRun> lstTRun)
        {
            /*
             * Subject:
             *      Job[1] - PID[953526] - Status[Running] - Result[Unkown] - PercentCompleted[0%] - [6/25/2012 14:25]
             *
             *      Run Details			
             *      RunID	  RunTitle      Status	    
             *      1871172	                Completed	
             *      1871173	                Completed	
             *      1871174	                Completed	
             * 
             * The one(s) in Red back color is(are) Failed.				
             * The one(s) in Green back color is(are) Passed.				
             * The one(s) in Yellow back color is(are) failed which status based on sub items.				
             */

            StringBuilder mailBody = new StringBuilder();
            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            mailBody.AppendLine("	    <h4>Run Details:</h4>");

            mailBody.AppendLine("	    <table class=\"TableStyle\" >");
            // Table header
            mailBody.AppendLine("            <tr>");
            mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Run&nbsp; ID</strong></td>");
            mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Run&nbsp; Title</strong></td>");
            mailBody.AppendLine("                <td class=\"ResultHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Status</strong></td>");
            mailBody.AppendLine("            </tr>");
            foreach (TTestProdInfo patch in lstTPatch)
            {
                foreach (TTestProdAttribute patchFile in lstTPatchFile.Where(p => p.TTestProdInfoID == patch.TTestProdInfoID).ToList())
                {
                    var runs = lstTRun.Where(c => c.TTestProdAttributesID == patchFile.TestProdAttributesID);
                    foreach (var run in runs)
                    {
                        mailBody.AppendLine("            <tr>");
                        mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                        mailBody.AppendLine(String.Format("                    {0}</td>", run.MDRunID));
                        mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                        mailBody.AppendLine(String.Format("                    {0}</td>", patch.WorkItem));
                        mailBody.AppendLine("                <td class=\"ResultContentColumn\">");
                        mailBody.AppendLine(String.Format("                    {0}</td>", HotFixLibrary.HotFixUtility.GetStatusName((int?)run.RunStatusID)));
                        mailBody.AppendLine("            </tr>");
                    }
                }
            }

            mailBody.AppendLine("        </table>");

            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            return mailBody.ToString();
        }

        public void SendWUKickingOffMail(TWUJob tWUJob, List<RunEx> lstRunReport)
        {
            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }

            mail.Subject = String.Format("WU Job [{0}] {1} - Status - [{2}], Result - [{3}], PercentCompleted - [{4}%]",
                    tWUJob.ID, tWUJob.JobDescription, HotFixLibrary.HotFixUtility.GetStatusName(tWUJob.StatusID), HotFixLibrary.HotFixUtility.GetResultName(tWUJob.ResultID), tWUJob.PercentCompleted);
            mail.IsBodyHtml = true;
            mail.Body = GetWUKickingOffMailBody(lstRunReport);

            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            server.Send(mail);
        }

        private string GetWUKickingOffMailBody(List<RunEx> lstRunReport)
        {
            StringBuilder mailBody = new StringBuilder();

            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("	<head>");
            AddCssToMailBody(mailBody);
            mailBody.AppendLine("	</head>");
            // Html body part
            mailBody.AppendLine("	<body>");

            mailBody.AppendLine("</br>");
            mailBody.AppendLine("	    <h4>WU Automated Run Details:</h4>");

            mailBody.AppendLine("	    <table class=\"TableStyle\" >");
            // Table header
            mailBody.AppendLine("            <tr>");
            mailBody.AppendLine("                <td class=\"IDHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Run&nbsp; ID</strong></td>");
            mailBody.AppendLine("                <td class=\"DescriptionHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Run&nbsp; Title</strong></td>");
            mailBody.AppendLine("                <td class=\"ResultHeaderColumn\">");
            mailBody.AppendLine("                    <strong>Status</strong></td>");
            mailBody.AppendLine("            </tr>");

            foreach (RunEx run in lstRunReport)
            {
                mailBody.AppendLine("            <tr>");
                mailBody.AppendLine("                <td class=\"IDContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", run.RunID));
                mailBody.AppendLine("                <td class=\"DescriptionContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", run.RunTitle));
                mailBody.AppendLine("                <td class=\"ResultContentColumn\">");
                mailBody.AppendLine(String.Format("                    {0}</td>", run.Status));
                mailBody.AppendLine("            </tr>");
            }
            mailBody.AppendLine("        </table>");

            mailBody.AppendLine("	</body>");
            mailBody.AppendLine("</html>");

            return mailBody.ToString();
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

        public void SendKickOffExceptionMail(TJob job, Exception ex)
        {
            if (string.IsNullOrEmpty(MailTo) && string.IsNullOrEmpty(MailCC))
            {
                throw new Exception("To list and CC list cannot be empty at same time");
            }

            MailMessage mail = new MailMessage();

            if (!string.IsNullOrEmpty(MailTo))
            {
                // Add To list 
                foreach (string person in MailTo.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.To.Add(person + "@microsoft.com");
                }
            }

            if (!string.IsNullOrEmpty(MailCC))
            {
                // Add CC list
                foreach (string person in MailCC.Split(';'))
                {
                    // so far, we only support sending mail to person who in microsoft
                    mail.CC.Add(person + "@microsoft.com");
                }
            }

            mail.Subject = String.Format("Failed to Kick-off Runs for Job-{0} PID-{1} Title-{2}",
                    job.JobID, job.PID, job.JobDescription);
            mail.IsBodyHtml = true;
            mail.Body = GetExceptionMailBody(job, ex);

            mail.From = new MailAddress(MailAccount);

            SmtpClient server = new System.Net.Mail.SmtpClient(SmtpServer);
            server.UseDefaultCredentials = true;

            server.Send(mail);
        }

        private string GetExceptionMailBody(TJob job, Exception ex)
        {
            StringBuilder mailBody = new StringBuilder();

            // Html head part
            mailBody.AppendLine("<html>");
            mailBody.AppendLine("<head>");
            mailBody.AppendLine("</head>");
            // Html body part
            mailBody.AppendLine("<body>");

            mailBody.AppendLine("   <br />");
            mailBody.AppendLine("   <h2 style=\"color:red\">Warning!! Failed to kick off test runs for below Job</h2>");

            //Basic job information
            mailBody.AppendLine("   <table border=\"0\">");
            
            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>Job ID</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", job.JobID));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>TFS ID</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", job.PID));
            mailBody.AppendLine("       </tr>");

            mailBody.AppendLine("       <tr>");
            mailBody.AppendLine("           <td style=\"width:75px\"><b>TFS Title</b></td>");
            mailBody.AppendLine(String.Format("           <td>{0}</td>", job.JobDescription));
            mailBody.AppendLine("       </tr>");
            
            mailBody.AppendLine("   </table>");

            mailBody.AppendLine("   <br />");

            mailBody.AppendLine("   <h4>Exception Details</h4>");
            mailBody.AppendLine(String.Format(  "<p>{0}</p>", ex.Message));

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
    }
}