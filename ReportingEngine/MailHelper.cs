using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Net;
using System.IO;

namespace ReportEngine
{
    public class MailHelper
    {
        public string MailAccount { get; set; }
        public string Password { get; set; }
        public string SmtpServer { get; set; }
        // use ; to seperate each user 
        public string MailTo { get; set; }
        // use ; to seperate each user
        public string MailCC { get; set; }

        public MailHelper(string userName)
        {
            MailAccount = userName;
            Password = string.Empty;
            // Default smtp server
            SmtpServer = "smtphost.redmond.corp.microsoft.com";
        }

        public MailHelper(string userName, string pass, string smtpServer)
        {
            MailAccount = userName;
            Password = pass;
            SmtpServer = smtpServer;
        }

        public void SendMail(string title,string content)
        {
            // to do. uncomment this when we deploy to PROD server 
            sendMail(MailTo, MailCC, title, content, MailAccount, MailAccount, Password, SmtpServer);
        }

        private static bool sendMail(string TO, string CC, string Title, string content, string _Sender, string Account, string Pwd, string Host)
        {
            SmtpClient _smtpClient = new SmtpClient();
            _smtpClient.UseDefaultCredentials = true;
            //_smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            _smtpClient.Host = Host;
            //_smtpClient.Port=Iport;
            //_smtpClient.Credentials = new System.Net.NetworkCredential(Account, Pwd);

            MailMessage _mailMessage = new MailMessage();
            _mailMessage.From = new MailAddress(_Sender);

            if (!string.IsNullOrEmpty(TO))
            {
                //Reciver="v-jingao@microsoft.com;ashk@microsoft.com"
                string[] Recivers = TO.Split(';');
                foreach (string Reciver in Recivers)
                {
                    if (!string.IsNullOrEmpty(Reciver))
                    {
                        _mailMessage.To.Add(new MailAddress(Reciver));
                    }
                }
            }

            if (!string.IsNullOrEmpty(CC))
            {
                //Reciver="v-jingao@microsoft.com;ashk@microsoft.com"
                string[] Recivers = CC.Split(';');
                foreach (string Reciver in Recivers)
                {
                    if (!string.IsNullOrEmpty(Reciver))
                    {
                        _mailMessage.CC.Add(new MailAddress(Reciver));
                    }
                }
            }

            _mailMessage.Subject = Title;
            _mailMessage.Body = content;
            _mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
            _mailMessage.IsBodyHtml = true;
            _mailMessage.Priority = MailPriority.Normal;

            try
            {
                _smtpClient.Send(_mailMessage);
            }
            catch (Exception err)
            {
                throw err;
            }

            return true;
        }

        public string MailSubject
        {
            set
            {
                this.subject = value;
            }
            get
            {
                return this.subject;
            }
        }

        public string MailBody
        {
            set
            {
                this.body = value;
            }
            get
            {
                return this.body;
            }
        }

        private string subject;
        private string body;
    }
}
