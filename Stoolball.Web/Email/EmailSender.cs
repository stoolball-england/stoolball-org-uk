using System.Net.Mail;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration.UmbracoSettings;

namespace Stoolball.Web.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IUmbracoSettingsSection _umbracoSettings;

        public EmailSender(IUmbracoSettingsSection umbracoSettings)
        {
            _umbracoSettings = umbracoSettings;
        }

        public bool SendEmail(string toAddress, string subject, string bodyHtml)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(toAddress);
                    message.From = new MailAddress(_umbracoSettings.Content.NotificationEmailAddress);
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
