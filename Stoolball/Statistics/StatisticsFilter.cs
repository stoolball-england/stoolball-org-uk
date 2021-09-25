using System;
using System.Collections.Generic;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class StatisticsFilter
    {
        public Club Club { get; set; }
        public Team Team { get; set; }
        public List<Guid> OppositionTeamIds { get; internal set; } = new List<Guid>();
        public bool SwapTeamAndOppositionFilters { get; set; }
        public Player Player { get; set; }
        public List<Guid> BowledByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> CaughtByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> RunOutByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public Season Season { get; set; }
        public Competition Competition { get; set; }
        public MatchLocation MatchLocation { get; set; }
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
        public Paging Paging { get; set; } = new Paging();
        public int? MaxResultsAllowingExtraResultsIfValuesAreEqual { get; set; }
        public int? MinimumQualifyingInnings { get; set; }

        public StatisticsFilter Clone()
        {
            return new StatisticsFilter
            {
                Club = Club,
                Team = Team,
                OppositionTeamIds = OppositionTeamIds,
                SwapTeamAndOppositionFilters = SwapTeamAndOppositionFilters,
                Player = Player,
                BowledByPlayerIdentityIds = BowledByPlayerIdentityIds,
                CaughtByPlayerIdentityIds = CaughtByPlayerIdentityIds,
                RunOutByPlayerIdentityIds = RunOutByPlayerIdentityIds,
                Season = Season,
                Competition = Competition,
                MatchLocation = MatchLocation,
                TournamentIds = TournamentIds,
                MatchTypes = MatchTypes,
                PlayerTypes = PlayerTypes,
                DismissalTypes = DismissalTypes,
                BattingPositions = BattingPositions,
                FromDate = FromDate,
                UntilDate = UntilDate,
                WonMatch = WonMatch,
                BattingFirst = BattingFirst,
                SwapBattingFirstFilter = SwapBattingFirstFilter,
                PlayerOfTheMatch = PlayerOfTheMatch,
                Paging = Paging,
                MaxResultsAllowingExtraResultsIfValuesAreEqual = MaxResultsAllowingExtraResultsIfValuesAreEqual,
                MinimumQualifyingInnings = MinimumQualifyingInnings
            };
        }
    }
}
