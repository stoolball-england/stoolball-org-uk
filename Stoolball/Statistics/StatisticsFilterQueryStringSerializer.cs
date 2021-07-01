namespace Stoolball.Statistics
{
    public class StatisticsFilterQueryStringSerializer : QueryStringSerializerBase, IStatisticsFilterSerializer
    {
        public string Serialize(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            ResetSerializer();

            Serialize(filter.Club?.ClubId, "club");
            Serialize(filter.Team?.TeamId, "team");
            Serialize(filter.OppositionTeamIds, "opposition");
            Serialize(filter.SwapTeamAndOppositionFilters, "swapteam");
            Serialize(filter.Player?.PlayerId, "player");
            Serialize(filter.BowledByPlayerIdentityIds, "bowler");
            Serialize(filter.CaughtByPlayerIdentityIds, "catcher");
            Serialize(filter.RunOutByPlayerIdentityIds, "fielder");
            Serialize(filter.Season?.SeasonId, "season");
            Serialize(filter.Competition?.CompetitionId, "competition");
            Serialize(filter.MatchLocation?.MatchLocationId, "location");
            Serialize(filter.TournamentIds, "tournament");
            Serialize(filter.MatchTypes, "matchtype");
            Serialize(filter.PlayerTypes, "playertype");
            Serialize(filter.DismissalTypes, "dismissaltype");
            Serialize(filter.BattingPositions, "battingposition");
            Serialize(filter.FromDate, "from");
            Serialize(filter.UntilDate, "to");
            Serialize(filter.WonMatch, "won");
            Serialize(filter.BattingFirst, "batfirst");
            Serialize(filter.SwapBattingFirstFilter, "swapbatfirst");
            Serialize(filter.PlayerOfTheMatch, "playerofmatch");
            Serialize(filter.Paging.PageNumber, "page");
            Serialize(filter.Paging.PageSize, "pagesize");
            Serialize(filter.MaxResultsAllowingExtraResultsIfValuesAreEqual, "max");
            Serialize(filter.MinimumQualifyingInnings, "min");

            return Serializer.ToQueryString();
        }
    }
}
