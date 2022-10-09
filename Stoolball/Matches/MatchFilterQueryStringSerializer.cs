using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class MatchFilterQueryStringSerializer : QueryStringSerializerBase, IMatchFilterQueryStringSerializer
    {
        public string Serialize(MatchFilter? filter)
        {
            return Serialize(filter, null);
        }

        public string Serialize(MatchFilter? filter, MatchFilter? defaultFilter)
        {
            filter = filter ?? new MatchFilter();
            defaultFilter = defaultFilter ?? new MatchFilter();
            ResetSerializer();

            Serialize(filter.Query, "q", defaultFilter.Query);
            Serialize(filter.TeamIds, "team", defaultFilter.TeamIds);
            Serialize(filter.CompetitionIds, "competition", defaultFilter.CompetitionIds);
            Serialize(filter.SeasonIds, "season", defaultFilter.SeasonIds);
            Serialize(filter.MatchTypes, "matchtype", defaultFilter.MatchTypes);
            Serialize(filter.PlayerTypes, "playertype", defaultFilter.PlayerTypes);
            Serialize(filter.MatchResultTypes, "matchresulttype", defaultFilter.MatchResultTypes);
            Serialize(filter.IncludeMatches, "matches", defaultFilter.IncludeMatches);
            Serialize(filter.IncludeTournamentMatches, "tournamentmatches", defaultFilter.IncludeTournamentMatches);
            Serialize(filter.IncludeTournaments, "tournaments", defaultFilter.IncludeTournaments);
            Serialize(filter.FromDate, "from", defaultFilter.FromDate?.Date);
            Serialize(filter.UntilDate, "to", defaultFilter.UntilDate?.Date);
            Serialize(filter.TournamentId, "tournament", defaultFilter.TournamentId);
            Serialize(filter.MatchLocationIds, "location", defaultFilter.MatchLocationIds);
            Serialize(filter.Paging.PageNumber, "page", defaultFilter.Paging.PageNumber);
            Serialize(filter.Paging.PageSize, "pagesize", defaultFilter.Paging.PageSize);

            var serialised = Serializer.ToQueryString();
            return serialised.Length > 1 ? serialised : string.Empty;
        }
    }
}
