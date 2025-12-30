using RMIntegration.MailService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMIntegration
{
    public class MailSvcUtil
    {
        private GoFxServMailerSoapClient MailClient = new GoFxServMailerSoapClient("GoFxServMailerSoap");

        public bool SendEmail(string[] toList, string[] ccList, string subject, string body)
        {
            return MailClient.SendEmail(toList, ccList, subject, body);
        }
    }
}
