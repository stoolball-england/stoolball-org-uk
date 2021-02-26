using System;
using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class StatisticsFilter
    {
        public List<Guid> TeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> OppositionTeamIds { get; internal set; } = new List<Guid>();
        public bool SwapTeamAndOppositionFilters { get; set; }
        public List<string> PlayerRoutes { get; internal set; } = new List<string>();
        public List<Guid> BowledByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> CaughtByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> RunOutByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> SeasonIds { get; internal set; } = new List<Guid>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public List<Guid> MatchLocationIds { get; internal set; } = new List<Guid>();
        public List<Guid> TournamentIds { get; internal set; } = new List<Guid>();
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public List<PlayerType> PlayerTypes { get; internal set; } = new List<PlayerType>();
        public List<DismissalType> DismissalTypes { get; internal set; } = new List<DismissalType>();
        public List<int> BattingPositions { get; internal set; } = new List<int>();
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? UntilDate { get; set; }
        public bool? WonMatch { get; set; }
        public bool? BattingFirst { get; set; }
        public bool SwapBattingFirstFilter { get; set; }
        public bool? PlayerOfTheMatch { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? MaxResultsAllowingExtraResultsIfValuesAreEqual { get; set; }
    }
}
