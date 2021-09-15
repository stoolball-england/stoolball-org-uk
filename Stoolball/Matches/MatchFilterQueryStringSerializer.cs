using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class MatchFilterQueryStringSerializer : QueryStringSerializerBase, IMatchFilterQueryStringSerializer
    {
        public string Serialize(MatchFilter filter)
        {
            return Serialize(filter, null);
        }

        public string Serialize(MatchFilter filter, MatchFilter defaultFilter)
        {
            filter = filter ?? new MatchFilter();
            defaultFilter = defaultFilter ?? new MatchFilter();
            ResetSerializer();

            Serialize(filter.Query, "q");
            Serialize(filter.TeamIds, "team");
            Serialize(filter.CompetitionIds, "competition");
            Serialize(filter.SeasonIds, "season");
            Serialize(filter.MatchTypes, "matchtype");
            Serialize(filter.PlayerTypes, "playertype");
            Serialize(filter.MatchResultTypes, "matchresulttype");
            Serialize(filter.IncludeMatches, "matches", defaultFilter.IncludeMatches);
            Serialize(filter.IncludeTournamentMatches, "tournamentmatches", defaultFilter.IncludeTournamentMatches);
            Serialize(filter.IncludeTournaments, "tournaments", defaultFilter.IncludeTournaments);
            Serialize(filter.FromDate, "from", defaultFilter.FromDate);
            Serialize(filter.UntilDate, "to", defaultFilter.UntilDate);
            Serialize(filter.TournamentId, "tournament");
            Serialize(filter.MatchLocationIds, "location");
            Serialize(filter.Paging.PageNumber, "page", defaultFilter.Paging.PageNumber);
            Serialize(filter.Paging.PageSize, "pagesize", defaultFilter.Paging.PageSize);

            var serialised = Serializer.ToQueryString();
            return serialised.Length > 1 ? serialised : string.Empty;
        }
    }
}
