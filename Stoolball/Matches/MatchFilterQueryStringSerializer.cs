using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class MatchFilterQueryStringSerializer : QueryStringSerializerBase, IMatchFilterSerializer
    {
        public string Serialize(MatchFilter filter)
        {
            filter = filter ?? new MatchFilter();
            ResetSerializer();

            Serialize(filter.Query, "q");
            Serialize(filter.TeamIds, "team");
            Serialize(filter.CompetitionIds, "competition");
            Serialize(filter.SeasonIds, "season");
            Serialize(filter.MatchTypes, "matchtype");
            Serialize(filter.PlayerTypes, "playertype");
            Serialize(filter.MatchResultTypes, "matchresulttype");
            Serialize(filter.IncludeMatches, "matches");
            Serialize(filter.IncludeTournamentMatches, "tournamentmatches");
            Serialize(filter.IncludeTournaments, "tournaments");
            Serialize(filter.FromDate, "from");
            Serialize(filter.UntilDate, "to");
            Serialize(filter.TournamentId, "tournament");
            Serialize(filter.MatchLocationIds, "location");
            Serialize(filter.Paging.PageNumber, "page");
            Serialize(filter.Paging.PageSize, "pagesize");

            return Serializer.ToQueryString();
        }
    }
}
