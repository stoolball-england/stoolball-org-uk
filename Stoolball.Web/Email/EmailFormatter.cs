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
using System.Runtime.CompilerServices;

namespace Stoolball.Web.Email
{
    public class EmailFormatter : IEmailFormatter
    {
        public (string subject, string body) FormatEmailContent(string subject, string bodyHtml, Dictionary<string, string> tokenValues)
        {
            // Replace tokens in text
            var revisedSubject = ReplaceTokens(subject != null ? subject : string.Empty, tokenValues);
            var revisedBody = ReplaceTokens(bodyHtml != null ? bodyHtml : string.Empty, tokenValues);

            // Replace those texts into the email template
            revisedBody = File.ReadAllText(HostingEnvironment.MapPath("~/Email/EmailTemplate.html"))
                .Replace("{{SUBJECT}}", revisedSubject)
                .Replace("{{BODY}}", revisedBody);

            return (revisedSubject, revisedBody);
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
