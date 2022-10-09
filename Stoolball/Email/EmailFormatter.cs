using System.Collections.Generic;
using System.IO;

namespace Stoolball.Email
{
    public class EmailFormatter : IEmailFormatter
    {
        public (string subject, string body) FormatEmailContent(string subject, string bodyHtml, Dictionary<string, string> tokenValues)
        {
            // Replace tokens in text
            var revisedSubject = ReplaceTokens(subject != null ? subject : string.Empty, tokenValues);
            var revisedBody = ReplaceTokens(bodyHtml != null ? bodyHtml : string.Empty, tokenValues);

            // Replace those texts into the email template
            var resourceName = "Stoolball.Email.EmailTemplate.html";
            using (var stream = typeof(EmailFormatter).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) { throw new FileNotFoundException($"Embedded resource not found: {resourceName}"); }
                using (var reader = new StreamReader(stream))
                {
                    revisedBody = reader.ReadToEnd()
                        .Replace("{{SUBJECT}}", revisedSubject)
                        .Replace("{{BODY}}", revisedBody);
                }
            }

            return (revisedSubject, revisedBody);
        }

        private static string ReplaceTokens(string textIn, Dictionary<string, string> tokenValues)
        {
            var textOut = textIn;

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
