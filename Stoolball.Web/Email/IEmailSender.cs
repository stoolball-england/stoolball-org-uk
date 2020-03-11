using System.Collections.Generic;

namespace Stoolball.Web.Email
{
    public interface IEmailSender
    {
        bool SendEmail(string toAddress, string subject, string bodyHtml);
    }
}
