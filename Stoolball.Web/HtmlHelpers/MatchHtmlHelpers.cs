using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stoolball.Matches;

namespace Stoolball.Web.HtmlHelpers
{
    public static class MatchHtmlHelpers
    {
        private static IHtmlContent WrapDateInSpan(string fullName)
        {
            var comma = fullName.LastIndexOf(",", StringComparison.OrdinalIgnoreCase);
            return new HtmlString(fullName.Substring(0, comma + 2) + "<span class=\"text-nowrap\">" + fullName.Substring(comma + 2) + "</span>");
        }

        public static IHtmlContent MatchFullName(this IHtmlHelper html, Match match, Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            return WrapDateInSpan(match.MatchFullName(dateTimeFormatter));
        }

        public static IHtmlContent TournamentFullName(this IHtmlHelper html, Tournament tournament, Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            return WrapDateInSpan(tournament.TournamentFullName(dateTimeFormatter));
        }

        public static IHtmlContent TournamentFullNameAndPlayerType(this IHtmlHelper html, Tournament tournament, Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            return WrapDateInSpan(tournament.TournamentFullNameAndPlayerType(dateTimeFormatter));
        }
    }
}