using System;
using System.Collections.Specialized;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class MatchesRssQueryStringParser : IMatchesRssQueryStringParser
    {
        public MatchFilter ParseFilterFromQueryString(NameValueCollection queryString)
        {
            if (queryString is null)
            {
                throw new ArgumentNullException(nameof(queryString));
            }

            var filter = new MatchFilter();

            ParseDateFilter(queryString, filter);
            ParsePlayerTypeFilter(queryString, filter);
            ParseMatchTypeFilter(queryString, filter);
            ParseMatchResultTypeFilter(queryString, filter);

            return filter;
        }

        private static void ParseMatchResultTypeFilter(NameValueCollection queryString, MatchFilter filter)
        {
            if (queryString["format"] == "tweet")
            {
                filter.MatchResultTypes.Add(null);
            }
        }

        private static void ParseDateFilter(NameValueCollection queryString, MatchFilter filter)
        {
            // Dates should be assumed to be in the UK time zone since that's where matches are expected to be
            var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.UkTimeZone());
            var ukToday = new DateTimeOffset(DateTimeOffset.UtcNow.Date, ukTimeZone.GetUtcOffset(DateTimeOffset.UtcNow.Date));

            // Support date filters that were linked from the old website
            if (bool.TryParse(queryString["today"], out var today) && today)
            {
                filter.FromDate = ukToday;
                filter.UntilDate = ukToday;
            }
            else
            {
                if (DateTime.TryParse(queryString["from"], out var from))
                {
                    var ukFrom = new DateTimeOffset(from.Date, ukTimeZone.GetUtcOffset(from.Date));
                    filter.FromDate = ukFrom > ukToday ? ukFrom : ukToday;
                }
                else
                {
                    filter.FromDate = ukToday;
                }

                if (DateTime.TryParse(queryString["to"], out var to))
                {
                    filter.UntilDate = new DateTimeOffset(to.Date, ukTimeZone.GetUtcOffset(to.Date));
                }
                else if (int.TryParse(queryString["days"], out var daysAhead))
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

        private static void ParsePlayerTypeFilter(NameValueCollection queryString, MatchFilter filter)
        {
            // Support player type ID filters that were linked from the old website
            if (int.TryParse(queryString["player"], out var playerTypeId) && playerTypeId > 0 && playerTypeId < 3)
            {
                var playerTypeFilter = (PlayerType)(playerTypeId - 1);
                filter.PlayerTypes.Add(playerTypeFilter);
            }
        }

        private static void ParseMatchTypeFilter(NameValueCollection queryString, MatchFilter filter)
        {
            filter.IncludeMatches = true;
            filter.IncludeTournaments = false;
            filter.IncludeTournamentMatches = false;

            if (int.TryParse(queryString["type"], out var matchTypeId) && (matchTypeId < 6))
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