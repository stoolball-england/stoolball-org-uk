using System;
using System.Text.RegularExpressions;

namespace Stoolball.Matches
{
    public class MatchInningsUrlParser : IMatchInningsUrlParser
    {
        public int? ParseInningsOrderInMatchFromUrl(Uri url)
        {
            if (url == null) return null;

            var match = Regex.Match(url.ToString(), "/innings/(?<InningsOrderInMatch>[0-9]+)/", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            if (int.TryParse(match.Groups["InningsOrderInMatch"].Value, out var innings) && innings > 0)
            {
                return innings;
            }
            else return null;
        }
    }
}