using System.Collections.Generic;

namespace Stoolball.Web.Email
{
    public interface IEmailHelper
    {
        bool SendEmail(string toAddress, string subject, string bodyHtml, Dictionary<string, string> tokenValues);
    }
}
