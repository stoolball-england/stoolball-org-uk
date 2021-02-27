using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.PlayerInMatchStatistics)]
    [PrimaryKey(nameof(PlayerInMatchStatisticsId), AutoIncrement = false)]
    [ExplicitColumns]
    public class PlayerInMatchStatisticsTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(PlayerInMatchStatisticsId))]
        public Guid PlayerInMatchStatisticsId { get; set; }

        [Column(nameof(PlayerId))]
        [ForeignKey(typeof(PlayerTableInitialSchema), Column = nameof(PlayerTableInitialSchema.PlayerId))]
        [Index(IndexTypes.Clustered)]
        public Guid PlayerId { get; set; }

        [Column(nameof(PlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        public Guid PlayerIdentityId { get; set; }

        [Column(nameof(PlayerIdentityName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerIdentityName { get; set; }

        [Column(nameof(PlayerRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerRoute { get; set; }

        [Column(nameof(MatchId))]
        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.NonClustered)]
        public Guid MatchId { get; set; }

        [Column(nameof(MatchStartTime))]
        [Index(IndexTypes.NonClustered)]
        public DateTime MatchStartTime { get; set; }

        [Column(nameof(MatchType))]
        [Index(IndexTypes.NonClustered)]
        public string MatchType { get; set; }

        [Column(nameof(MatchPlayerType))]
        [Index(IndexTypes.NonClustered)]
        public string MatchPlayerType { get; set; }

        [Column(nameof(MatchName))]
        public string MatchName { get; set; }

        [Column(nameof(MatchRoute))]
        public string MatchRoute { get; set; }

        [Column(nameof(TournamentId))]
        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? TournamentId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? SeasonId { get; set; }

        [Column(nameof(CompetitionId))]
        [ForeignKey(typeof(CompetitionTableInitialSchema), Column = nameof(CompetitionTableInitialSchema.CompetitionId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? CompetitionId { get; set; }

        [Column(nameof(MatchTeamId))]
        [ForeignKey(typeof(MatchTeamTableInitialSchema), Column = nameof(MatchTeamTableInitialSchema.MatchTeamId))]
        [Index(IndexTypes.NonClustered)]
        public Guid MatchTeamId { get; set; }

        [Column(nameof(ClubId))]
        [ForeignKey(typeof(ClubTableInitialSchema), Column = nameof(ClubTableInitialSchema.ClubId))]
        [Index(IndexTypes.NonClustered)]
        public Guid ClubId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        public Guid TeamId { get; set; }

        [Column(nameof(TeamName))]
        public string TeamName { get; set; }

        [Column(nameof(TeamRoute))]
        public string TeamRoute { get; set; }

        [Column(nameof(OppositionTeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballTeam_OppositionTeamId")]
        [Index(IndexTypes.NonClustered)]
        public Guid? OppositionTeamId { get; set; }

        [Column(nameof(OppositionTeamName))]
        public string OppositionTeamName { get; set; }

        [Column(nameof(OppositionTeamRoute))]
        public string OppositionTeamRoute { get; set; }

        [Column(nameof(MatchLocationId))]
        [ForeignKey(typeof(MatchLocationTableInitialSchema), Column = nameof(MatchLocationTableInitialSchema.MatchLocationId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? MatchLocationId { get; set; }

        [Column(nameof(MatchInningsRuns))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchInningsRuns { get; set; }

        [Column(nameof(MatchInningsWickets))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchInningsWickets { get; set; }

        [Column(nameof(OppositionMatchInningsRuns))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OppositionMatchInningsRuns { get; set; }

        [Column(nameof(OppositionMatchInningsWickets))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OppositionMatchInningsWickets { get; set; }

        [Column(nameof(MatchInningsPair))]
        [Index(IndexTypes.NonClustered)]
        public int MatchInningsPair { get; set; }

        [Column(nameof(WonToss))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? WonToss { get; set; }

        [Column(nameof(BattedFirst))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? BattedFirst { get; set; }

        [Column(nameof(OverNumberOfFirstOverBowled))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OverNumberOfFirstOverBowled { get; set; }

        [Column(nameof(BallsBowled))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BallsBowled { get; set; }

        [Column(nameof(Overs))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal? Overs { get; set; }

        [Column(nameof(Maidens))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Maidens { get; set; }

        [Column(nameof(RunsConceded))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsConceded { get; set; }

        [Column(nameof(HasRunsConceded))]
        public bool HasRunsConceded { get; set; }

        [Column(nameof(Wickets))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Wickets { get; set; }

        [Column(nameof(WicketsWithBowling))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? WicketsWithBowling { get; set; }

        [Column(nameof(PlayerInningsNumber))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? PlayerInningsNumber { get; set; }

        [Column(nameof(PlayerInningsId))]
        [ForeignKey(typeof(PlayerInningsTableInitialSchema), Column = nameof(PlayerInningsTableInitialSchema.PlayerInningsId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? PlayerInningsId { get; set; }

        [Column(nameof(BattingPosition))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BattingPosition { get; set; }

        [Column(nameof(DismissalType))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? DismissalType { get; set; }

        [Column(nameof(PlayerWasDismissed))]
        [Index(IndexTypes.NonClustered)]
        public bool PlayerWasDismissed { get; set; }

        [Column(nameof(BowledByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_BowledById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? BowledByPlayerIdentityId { get; set; }

        [Column(nameof(BowledByPlayerIdentityName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string BowledByPlayerIdentityName { get; set; }

        [Column(nameof(BowledByPlayerRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string BowledByPlayerRoute { get; set; }

        [Column(nameof(CaughtByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_CaughtById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? CaughtByPlayerIdentityId { get; set; }

        [Column(nameof(CaughtByPlayerIdentityName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string CaughtByPlayerIdentityName { get; set; }

        [Column(nameof(CaughtByPlayerRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string CaughtByPlayerRoute { get; set; }

        [Column(nameof(RunOutByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_RunOutById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? RunOutByPlayerIdentityId { get; set; }

        [Column(nameof(RunOutByPlayerIdentityName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string RunOutByPlayerIdentityName { get; set; }

        [Column(nameof(RunOutByPlayerRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string RunOutByPlayerRoute { get; set; }

        [Column(nameof(RunsScored))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsScored { get; set; }

        [Column(nameof(BallsFaced))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BallsFaced { get; set; }

        [Column(nameof(Catches))]
        [Index(IndexTypes.NonClustered)]
        public int Catches { get; set; }

        [Column(nameof(RunOuts))]
        [Index(IndexTypes.NonClustered)]
        public int RunOuts { get; set; }

        [Column(nameof(WonMatch))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? WonMatch { get; set; }

        [Column(nameof(PlayerOfTheMatch))]
        [Index(IndexTypes.NonClustered)]
        public bool PlayerOfTheMatch { get; set; }
    }
}