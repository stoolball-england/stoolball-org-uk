using System;
using System.Web.Mvc;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public static class MatchHtmlHelpers
    {
        private static MvcHtmlString WrapDateInSpan(string fullName)
        {
            var comma = fullName.LastIndexOf(",", StringComparison.OrdinalIgnoreCase);
            return MvcHtmlString.Create(fullName.Substring(0, comma + 2) + "<span class=\"text-nowrap\">" + fullName.Substring(comma + 2) + "</span>");
        }

        public static MvcHtmlString MatchFullName(this HtmlHelper html, Match match, Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            return WrapDateInSpan(match.MatchFullName(dateTimeFormatter));
        }

        public static MvcHtmlString TournamentFullName(this HtmlHelper html, Tournament tournament, Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            return WrapDateInSpan(tournament.TournamentFullName(dateTimeFormatter));
        }

        public static MvcHtmlString TournamentFullNameAndPlayerType(this HtmlHelper html, Tournament tournament, Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            return WrapDateInSpan(tournament.TournamentFullNameAndPlayerType(dateTimeFormatter));
        }
    }
}