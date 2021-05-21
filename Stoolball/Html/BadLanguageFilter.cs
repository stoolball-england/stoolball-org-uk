using System.Text.RegularExpressions;

namespace Stoolball.Html
{
    public class BadLanguageFilter : IBadLanguageFilter
    {
        public string Filter(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return text; }
            return Regex.Replace(text, "(shit|fuck(ing)?|cunt)", "@*%!", RegexOptions.IgnoreCase);
        }
    }
}
