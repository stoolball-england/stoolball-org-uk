﻿using System;
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
            // Support date filters that were linked from the old website
            if (bool.TryParse(queryString["today"], out var today) && today)
            {
                filter.FromDate = DateTimeOffset.UtcNow.Date;
                filter.UntilDate = DateTimeOffset.UtcNow.Date;
            }
            else
            {
                if (DateTimeOffset.TryParse(queryString["from"], out var from))
                {
                    filter.FromDate = from.Date;
                }
                else
                {
                    filter.FromDate = DateTimeOffset.UtcNow.Date.AddDays(-1);
                }

                if (DateTimeOffset.TryParse(queryString["to"], out var to))
                {
                    filter.UntilDate = to.Date;
                }
                else if (int.TryParse(queryString["days"], out var daysAhead))
                {
                    filter.UntilDate = DateTimeOffset.UtcNow.Date.AddDays(daysAhead);
                }
                else
                {
                    filter.UntilDate = DateTimeOffset.UtcNow.Date.AddDays(365);
                }
            }

            // Ensure the UntilDate filter is inclusive, by advancing from midnight at the start of the day to midnight at the end
            if (filter.UntilDate.HasValue) { filter.UntilDate = filter.UntilDate.Value.AddDays(1); }
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