using System.Collections.Generic;

namespace Stoolball.Email
{
    public interface IEmailFormatter
    {
        (string subject, string body) FormatEmailContent(string subject, string bodyHtml, Dictionary<string, string> tokenValues);
    }
}