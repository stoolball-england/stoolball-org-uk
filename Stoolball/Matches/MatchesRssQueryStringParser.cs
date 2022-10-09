using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class MatchesRssQueryStringParser : IMatchesRssQueryStringParser
    {
        public MatchFilter ParseFilterFromQueryString(string? queryString)
        {
            var filter = new MatchFilter();

            var query = QueryHelpers.ParseQuery(queryString);

            ParseDateFilter(query, filter);
            ParsePlayerTypeFilter(query, filter);
            ParseMatchTypeFilter(query, filter);
            ParseMatchResultTypeFilter(query, filter);

            return filter;
        }

        private static void ParseMatchResultTypeFilter(Dictionary<string, StringValues> queryString, MatchFilter filter)
        {
            if (queryString.ContainsKey("format") && queryString["format"] == "tweet")
            {
                filter.MatchResultTypes.Add(null);
            }
        }

        private static void ParseDateFilter(Dictionary<string, StringValues> queryString, MatchFilter filter)
        {
            // Dates should be assumed to be in the UK time zone since that's where matches are expected to be
            var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.UkTimeZone());
            var ukToday = new DateTimeOffset(DateTimeOffset.UtcNow.Date, ukTimeZone.GetUtcOffset(DateTimeOffset.UtcNow.Date));

            // Support date filters that were linked from the old website
            if (queryString.ContainsKey("today") && bool.TryParse(queryString["today"], out var today) && today)
            {
                filter.FromDate = ukToday;
                filter.UntilDate = ukToday;
            }
            else
            {
                if (queryString.ContainsKey("from") && DateTime.TryParse(queryString["from"], out var from))
                {
                    var ukFrom = new DateTimeOffset(from.Date, ukTimeZone.GetUtcOffset(from.Date));
                    filter.FromDate = ukFrom > ukToday ? ukFrom : ukToday;
                }
                else
                {
                    filter.FromDate = ukToday;
                }

                if (queryString.ContainsKey("to") && DateTime.TryParse(queryString["to"], out var to))
                {
                    filter.UntilDate = new DateTimeOffset(to.Date, ukTimeZone.GetUtcOffset(to.Date));
                }
                else if (queryString.ContainsKey("days") && int.TryParse(queryString["days"], out var daysAhead))
                {
                    filter.UntilDate = ukToday.AddDays(daysAhead);
                }
                else
                {
                    filter.UntilDate = ukToday.AddDays(365);
                }
            }

            // Ensure the UntilDate filter is inclusive, by advancing from midnight at the start of the day to midnight at the end
            if (filter.UntilDate.HasValue) { filter.UntilDate = filter.UntilDate.Value.AddDays(1).AddSeconds(-1); }
        }

        private static void ParsePlayerTypeFilter(Dictionary<string, StringValues> queryString, MatchFilter filter)
        {
            // Support player type ID filters that were linked from the old website
            if (queryString.ContainsKey("player") && int.TryParse(queryString["player"], out var playerTypeId) && playerTypeId > 0 && playerTypeId < 3)
            {
                var playerTypeFilter = (PlayerType)(playerTypeId - 1);
                filter.PlayerTypes.Add(playerTypeFilter);
            }
        }

        private static void ParseMatchTypeFilter(Dictionary<string, StringValues> queryString, MatchFilter filter)
        {
            filter.IncludeMatches = true;
            filter.IncludeTournaments = false;
            filter.IncludeTournamentMatches = false;

            if (queryString.ContainsKey("type") && int.TryParse(queryString["type"], out var matchTypeId) && (matchTypeId < 6))
            {
                if (matchTypeId == 1)
                {
                    filter.IncludeMatches = false;
                    filter.IncludeTournaments = true;
                    filter.IncludeTournamentMatches = false;
                }
                else
                {
                    var matchTypeFilter = (MatchType)(matchTypeId > 2 ? matchTypeId - 2 : 0);
                    filter.MatchTypes.Add(matchTypeFilter);
                }
            }
        }
    }
}