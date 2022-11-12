using System;
using System.Collections.Generic;
using System.Linq;
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
        public Club? Club { get; set; }
        public Team? Team { get; set; }
        public List<Guid> OppositionTeamIds { get; internal set; } = new List<Guid>();
        public bool SwapTeamAndOppositionFilters { get; set; }
        public Player? Player { get; set; }
        public List<Guid> BowledByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> CaughtByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public List<Guid> RunOutByPlayerIdentityIds { get; internal set; } = new List<Guid>();
        public Season? Season { get; set; }
        public Competition? Competition { get; set; }
        public MatchLocation? MatchLocation { get; set; }
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
        public int? MinimumRunsScored { get; set; }
        public int? MinimumWicketsTaken { get; set; }

        public StatisticsFilter Clone()
        {
            return new StatisticsFilter
            {
                Club = Club,
                Team = Team,
                OppositionTeamIds = new List<Guid>(OppositionTeamIds),
                SwapTeamAndOppositionFilters = SwapTeamAndOppositionFilters,
                Player = Player,
                BowledByPlayerIdentityIds = new List<Guid>(BowledByPlayerIdentityIds),
                CaughtByPlayerIdentityIds = new List<Guid>(CaughtByPlayerIdentityIds),
                RunOutByPlayerIdentityIds = new List<Guid>(RunOutByPlayerIdentityIds),
                Season = Season,
                Competition = Competition,
                MatchLocation = MatchLocation,
                TournamentIds = new List<Guid>(TournamentIds),
                MatchTypes = new List<MatchType>(MatchTypes),
                PlayerTypes = new List<PlayerType>(PlayerTypes),
                DismissalTypes = new List<DismissalType>(DismissalTypes),
                BattingPositions = new List<int>(BattingPositions),
                FromDate = FromDate,
                UntilDate = UntilDate,
                WonMatch = WonMatch,
                BattingFirst = BattingFirst,
                SwapBattingFirstFilter = SwapBattingFirstFilter,
                PlayerOfTheMatch = PlayerOfTheMatch,
                Paging = Paging,
                MaxResultsAllowingExtraResultsIfValuesAreEqual = MaxResultsAllowingExtraResultsIfValuesAreEqual,
                MinimumQualifyingInnings = MinimumQualifyingInnings,
                MinimumRunsScored = MinimumRunsScored,
                MinimumWicketsTaken = MinimumWicketsTaken
            };
        }

        /// <summary>
        /// Creates a new <see cref="PlayerFilter"/> with the criteria in this filter, where applicable
        /// </summary>
        public PlayerFilter ToPlayerFilter()
        {
            var filter = new PlayerFilter();
            if (Club != null && Club.ClubId.HasValue) { filter.ClubIds.Add(Club.ClubId.Value); }
            if (Team != null && Team.TeamId.HasValue) { filter.TeamIds.Add(Team.TeamId.Value); }
            if (Player != null && Player.PlayerId.HasValue) { filter.PlayerIds.Add(Player.PlayerId.Value); }
            if (MatchLocation != null && MatchLocation.MatchLocationId.HasValue) { filter.MatchLocationIds.Add(MatchLocation.MatchLocationId.Value); }
            if (Competition != null && Competition.CompetitionId.HasValue) { filter.CompetitionIds.Add(Competition.CompetitionId.Value); }
            if (Season != null && Season.SeasonId.HasValue) { filter.SeasonIds.Add(Season.SeasonId.Value); }
            return filter;
        }

        /// <summary>
        /// Adds non-null criteria from <c>otherFilter</c> to the current filter. Non-nullable boolean properties are replaced.
        /// </summary>
        /// <returns>This filter</returns>
        public StatisticsFilter Merge(StatisticsFilter otherFilter)
        {
            if (otherFilter is null)
            {
                throw new ArgumentNullException(nameof(otherFilter));
            }

            Club = otherFilter.Club ?? Club;
            Team = otherFilter.Team ?? Team;
            OppositionTeamIds.AddRange(otherFilter.OppositionTeamIds.Where(x => !OppositionTeamIds.Contains(x)));
            SwapTeamAndOppositionFilters = otherFilter.SwapTeamAndOppositionFilters;
            Player = otherFilter.Player ?? Player;
            BowledByPlayerIdentityIds.AddRange(otherFilter.BowledByPlayerIdentityIds.Where(x => !BowledByPlayerIdentityIds.Contains(x)));
            CaughtByPlayerIdentityIds.AddRange(otherFilter.CaughtByPlayerIdentityIds.Where(x => !CaughtByPlayerIdentityIds.Contains(x)));
            RunOutByPlayerIdentityIds.AddRange(otherFilter.RunOutByPlayerIdentityIds.Where(x => !RunOutByPlayerIdentityIds.Contains(x)));
            Season = otherFilter.Season ?? Season;
            Competition = otherFilter.Competition ?? Competition;
            MatchLocation = otherFilter.MatchLocation ?? MatchLocation;
            TournamentIds.AddRange(otherFilter.TournamentIds.Where(x => !TournamentIds.Contains(x)));
            MatchTypes.AddRange(otherFilter.MatchTypes.Where(x => !MatchTypes.Contains(x)));
            PlayerTypes.AddRange(otherFilter.PlayerTypes.Where(x => !PlayerTypes.Contains(x)));
            DismissalTypes.AddRange(otherFilter.DismissalTypes.Where(x => !DismissalTypes.Contains(x)));
            BattingPositions.AddRange(otherFilter.BattingPositions.Where(x => !BattingPositions.Contains(x)));
            FromDate = otherFilter.FromDate ?? FromDate;
            UntilDate = otherFilter.UntilDate ?? UntilDate;
            WonMatch = otherFilter.WonMatch ?? WonMatch;
            BattingFirst = otherFilter.BattingFirst ?? BattingFirst;
            SwapBattingFirstFilter = otherFilter.SwapBattingFirstFilter;
            PlayerOfTheMatch = otherFilter.PlayerOfTheMatch ?? PlayerOfTheMatch;
            Paging.PageUrl = otherFilter.Paging.PageUrl ?? Paging.PageUrl;
            Paging.PageNumber = otherFilter.Paging.PageNumber > 1 ? otherFilter.Paging.PageNumber : Paging.PageNumber;
            Paging.PageSize = otherFilter.Paging.PageSize != int.MaxValue ? otherFilter.Paging.PageSize : Paging.PageSize;
            Paging.Total = otherFilter.Paging.Total > 0 ? otherFilter.Paging.Total : Paging.Total;
            MaxResultsAllowingExtraResultsIfValuesAreEqual = otherFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual ?? MaxResultsAllowingExtraResultsIfValuesAreEqual;
            MinimumQualifyingInnings = otherFilter.MinimumQualifyingInnings ?? MinimumQualifyingInnings;
            MinimumRunsScored = otherFilter.MinimumRunsScored ?? MinimumRunsScored;
            MinimumWicketsTaken = otherFilter.MinimumWicketsTaken ?? MinimumWicketsTaken;
            return this;
        }
    }
}
