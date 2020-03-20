using System.Net.Mail;
using Umbraco.Core.Composing;

namespace Stoolball.Web.Email
{
    public class EmailSender : IEmailSender
    {
        public bool SendEmail(string toAddress, string subject, string bodyHtml)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(toAddress);
                    message.From = new MailAddress("alerts@stoolball.org.uk", "Stoolball England");
                    message.Subject = subject;

                    message.IsBodyHtml = true;
                    message.Body = bodyHtml;
                    using (var smtp = new SmtpClient())
                    {
                        smtp.Send(message);
                    }
                    return true;
                }
            }
            catch (SmtpException e)
            {
                Current.Logger.Info(GetType(), "Error trying to send email: " + e);
                return false;
            }
        }
    }
}
