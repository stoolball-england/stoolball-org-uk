using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Stoolball.Statistics
{
    public class PlayerNameFormatter : IPlayerNameFormatter
    {
        public string CapitaliseName(string playerIdentityName)
        {
            if (playerIdentityName is null)
            {
                throw new ArgumentNullException(nameof(playerIdentityName));
            }

            var segments = Regex.Replace(playerIdentityName, @"\s", " ").Split(' ');
            for (var i = 0; i < segments.Length; i++)
            {
                segments[i] = CapitaliseNameSegment(segments[i]);
            }
            segments = string.Join(" ", segments).Split('-');
            for (var i = 0; i < segments.Length; i++)
            {
                segments[i] = CapitaliseNameSegment(segments[i]);
            }
            return string.Join("-", segments);
        }

        private static string CapitaliseNameSegment(string segment)
        {
            return segment.Length > 1 && Array.IndexOf(new[] { "de", "la", "di", "da", "della", "van", "von" }, segment) == -1
              ? segment.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + segment.Substring(1)
              : segment;
        }
    }
}
