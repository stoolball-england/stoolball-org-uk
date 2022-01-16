using System.Collections.Generic;

namespace Stoolball.Web.Email
{
    public interface IEmailFormatter
    {
        (string subject, string body) FormatEmailContent(string subject, string bodyHtml, Dictionary<string, string> tokenValues);
    }
}