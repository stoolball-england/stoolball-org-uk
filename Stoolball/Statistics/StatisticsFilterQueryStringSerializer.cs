using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Stoolball.Statistics
{
    public class StatisticsFilterQueryStringSerializer : IStatisticsFilterSerializer
    {
        public string Serialize(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var serializer = new NameValueCollection();

            Serialize(filter.Club?.ClubId, serializer, "club");
            Serialize(filter.Team?.TeamId, serializer, "team");
            Serialize(filter.OppositionTeamIds, serializer, "opposition");
            Serialize(filter.SwapTeamAndOppositionFilters, serializer, "swapteam");
            Serialize(filter.Player?.PlayerId, serializer, "player");
            Serialize(filter.BowledByPlayerIdentityIds, serializer, "bowler");
            Serialize(filter.CaughtByPlayerIdentityIds, serializer, "catcher");
            Serialize(filter.RunOutByPlayerIdentityIds, serializer, "fielder");
            Serialize(filter.Season?.SeasonId, serializer, "season");
            Serialize(filter.Competition?.CompetitionId, serializer, "competition");
            Serialize(filter.MatchLocation?.MatchLocationId, serializer, "location");
            Serialize(filter.TournamentIds, serializer, "tournament");
            Serialize(filter.MatchTypes, serializer, "matchtype");
            Serialize(filter.PlayerTypes, serializer, "playertype");
            Serialize(filter.DismissalTypes, serializer, "dismissaltype");
            Serialize(filter.BattingPositions, serializer, "battingposition");
            Serialize(filter.FromDate, serializer, "from");
            Serialize(filter.UntilDate, serializer, "to");
            Serialize(filter.WonMatch, serializer, "won");
            Serialize(filter.BattingFirst, serializer, "batfirst");
            Serialize(filter.SwapBattingFirstFilter, serializer, "swapbatfirst");
            Serialize(filter.PlayerOfTheMatch, serializer, "playerofmatch");
            Serialize(filter.Paging.PageNumber, serializer, "page");
            Serialize(filter.Paging.PageSize, serializer, "pagesize");
            Serialize(filter.MaxResultsAllowingExtraResultsIfValuesAreEqual, serializer, "max");

            return serializer.ToQueryString();
        }

        private static void Serialize(int value, NameValueCollection serializer, string key)
        {
            serializer.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Serialize(int? value, NameValueCollection serializer, string key)
        {
            if (value.HasValue) { serializer.Add(key, value.Value.ToString(CultureInfo.InvariantCulture)); }
        }

        private static void Serialize(DateTimeOffset? value, NameValueCollection serializer, string key)
        {
            if (value.HasValue) { serializer.Add(key, value.Value.ToString("yyyy-M-d", CultureInfo.InvariantCulture)); }
        }

        private static void Serialize(bool value, NameValueCollection serializer, string key)
        {
            if (value) { serializer.Add(key, value ? "1" : "0"); }
        }

        private static void Serialize(bool? value, NameValueCollection serializer, string key)
        {
            if (value.HasValue) { serializer.Add(key, value.Value ? "1" : "0"); }
        }

        private static void Serialize(Guid? value, NameValueCollection serializer, string key)
        {
            if (value.HasValue) { serializer.Add(key, value.Value.ToString()); }
        }

        private static void Serialize<T>(List<T> value, NameValueCollection serializer, string key)
        {
            var sortedList = value.Select(x => x.ToString()).OrderBy(x => x);
            foreach (var item in sortedList)
            {
                serializer.Add(key, item);
            }
        }
    }
}
