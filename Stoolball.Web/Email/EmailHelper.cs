using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Persistence;
using Umbraco.Web;
using Umbraco.Core;
using System.Net.Mail;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace Stoolball.Web.Email
{
    public class EmailHelper : IEmailHelper
    {
        public bool SendEmail(string toAddress, string subject, string bodyHtml, Dictionary<string, string> tokenValues)
        {
            // Replace tokens in text
            var revisedSubject = ReplaceTokens(subject != null ? subject : string.Empty, tokenValues);
            var revisedBody = ReplaceTokens(bodyHtml != null ? bodyHtml : string.Empty, tokenValues);

            // Replace those texts into the email template
            revisedBody = File.ReadAllText(HostingEnvironment.MapPath("~/Email/EmailTemplate.html"))
                .Replace("{{SUBJECT}}", revisedSubject)
                .Replace("{{BODY}}", revisedBody);

            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(toAddress);
                    message.From = new MailAddress("alerts@stoolball.org.uk", "Stoolball England");
                    message.Subject = revisedSubject;

                    message.IsBodyHtml = true;
                    message.Body = revisedBody;
                    using (var smtp = new SmtpClient())
                    {
                        smtp.Send(message);
                    }
                    return true;
                }
            }
            catch (SmtpException e)
            {
                Umbraco.Core.Composing.Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Error trying to send email: " + e);
                return false;
            }
        }

        private static string ReplaceTokens(string textIn, Dictionary<string, string> tokenValues)
        {
            string textOut = textIn;

            if (tokenValues != null)
            {
                foreach (var token in tokenValues)
                {
                    textOut = textOut.Replace(string.Concat("{{", token.Key.ToUpperInvariant(), "}}"), token.Value);
                }
            }

            return textOut;
        }
    }
}
