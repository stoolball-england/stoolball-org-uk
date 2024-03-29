﻿namespace Stoolball.Statistics
{
    public class StatisticsFilterQueryStringSerializer : QueryStringSerializerBase, IStatisticsFilterQueryStringSerializer
    {
        public string Serialize(StatisticsFilter? filter)
        {
            return Serialize(filter, null);
        }

        private string? RemovePrefix(string? text, string prefix)
        {
            if (text == null) { return text; }
            if (!text.StartsWith(prefix)) { return text; }
            return text.Substring(prefix.Length);
        }

        public string Serialize(StatisticsFilter? filter, StatisticsFilter? defaultFilter)
        {
            filter = filter ?? new StatisticsFilter();
            defaultFilter = defaultFilter ?? new StatisticsFilter();
            ResetSerializer();

            Serialize(filter.Club?.ClubId, "club", defaultFilter.Club?.ClubId);
            Serialize(RemovePrefix(filter.Team?.TeamRoute, "/teams/"), "team", RemovePrefix(defaultFilter.Team?.TeamRoute, "/teams/"));
            Serialize(filter.OppositionTeamIds, "opposition", defaultFilter.OppositionTeamIds);
            Serialize(filter.SwapTeamAndOppositionFilters, "swapteam", defaultFilter.SwapTeamAndOppositionFilters);
            Serialize(filter.Player?.PlayerId, "player", defaultFilter.Player?.PlayerId);
            Serialize(filter.BowledByPlayerIdentityIds, "bowler", defaultFilter.BowledByPlayerIdentityIds);
            Serialize(filter.CaughtByPlayerIdentityIds, "catcher", defaultFilter.CaughtByPlayerIdentityIds);
            Serialize(filter.RunOutByPlayerIdentityIds, "fielder", defaultFilter.RunOutByPlayerIdentityIds);
            Serialize(filter.Season?.SeasonId, "season", defaultFilter.Season?.SeasonId);
            Serialize(filter.Competition?.CompetitionId, "competition", defaultFilter.Competition?.CompetitionId);
            Serialize(filter.MatchLocation?.MatchLocationId, "location", defaultFilter.MatchLocation?.MatchLocationId);
            Serialize(filter.TournamentIds, "tournament", defaultFilter.TournamentIds);
            Serialize(filter.MatchTypes, "matchtype", defaultFilter.MatchTypes);
            Serialize(filter.PlayerTypes, "playertype", defaultFilter.PlayerTypes);
            Serialize(filter.DismissalTypes, "dismissaltype", defaultFilter.DismissalTypes);
            Serialize(filter.BattingPositions, "battingposition", defaultFilter.BattingPositions);
            Serialize(filter.FromDate, "from", defaultFilter.FromDate);
            Serialize(filter.UntilDate, "to", defaultFilter.UntilDate);
            Serialize(filter.WonMatch, "won", defaultFilter.WonMatch);
            Serialize(filter.BattingFirst, "batfirst", defaultFilter.BattingFirst);
            Serialize(filter.SwapBattingFirstFilter, "swapbatfirst", defaultFilter.SwapBattingFirstFilter);
            Serialize(filter.PlayerOfTheMatch, "playerofmatch", defaultFilter.PlayerOfTheMatch);
            Serialize(filter.Paging.PageNumber, "page", defaultFilter.Paging.PageNumber);
            Serialize(filter.Paging.PageSize, "pagesize", defaultFilter.Paging.PageSize);
            Serialize(filter.MaxResultsAllowingExtraResultsIfValuesAreEqual, "max", defaultFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual);
            Serialize(filter.MinimumQualifyingInnings, "min", defaultFilter.MinimumQualifyingInnings);
            Serialize(filter.MinimumRunsScored, "minruns", defaultFilter.MinimumRunsScored);
            Serialize(filter.MinimumWicketsTaken, "minwickets", defaultFilter.MinimumWicketsTaken);

            var serialised = Serializer.ToQueryString();
            return serialised.Length > 1 ? serialised : string.Empty;
        }
    }
}
