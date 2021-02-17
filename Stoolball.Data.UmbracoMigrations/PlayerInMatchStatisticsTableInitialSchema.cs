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
        [Index(IndexTypes.NonClustered)]
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
        [Index(IndexTypes.Clustered)]
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

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.NonClustered)]
        public Guid MatchInningsId { get; set; }

        [Column(nameof(MatchInningsRuns))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchInningsRuns { get; set; }

        [Column(nameof(MatchInningsWickets))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchInningsWickets { get; set; }

        [Column(nameof(InningsOrderInMatch))]
        [Index(IndexTypes.NonClustered)]
        public int InningsOrderInMatch { get; set; }

        [Column(nameof(InningsOrderIsKnown))]
        public bool InningsOrderIsKnown { get; set; }

        [Column(nameof(OverNumberOfFirstOverBowled))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OverNumberOfFirstOverBowled { get; set; }

        [Column(nameof(BallsBowled))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BallsBowled { get; set; }

        [Column(nameof(OversBowled))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal? OversBowled { get; set; }

        [Column(nameof(OversBowledDecimal))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal? OversBowledDecimal { get; set; }

        [Column(nameof(MaidensBowled))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MaidensBowled { get; set; }

        [Column(nameof(RunsConceded))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsConceded { get; set; }

        [Column(nameof(HasRunsConceded))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool HasRunsConceded { get; set; }

        [Column(nameof(Wickets))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Wickets { get; set; }

        [Column(nameof(WicketsWithBowling))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? WicketsWithBowling { get; set; }

        [Column(nameof(PlayerInningsInMatchInnings))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? PlayerInningsInMatchInnings { get; set; }

        [Column(nameof(BattingPosition))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BattingPosition { get; set; }

        [Column(nameof(DismissalType))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string DismissalType { get; set; }

        [Column(nameof(PlayerWasDismissed))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? PlayerWasDismissed { get; set; }

        [Column(nameof(BowledByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_BowledById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? BowledByPlayerIdentityId { get; set; }

        [Column(nameof(BowledByName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string BowledByName { get; set; }

        [Column(nameof(BowledByRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string BowledByRoute { get; set; }

        [Column(nameof(CaughtByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_CaughtById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? CaughtByPlayerIdentityId { get; set; }

        [Column(nameof(CaughtByName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string CaughtByName { get; set; }

        [Column(nameof(CaughtByRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string CaughtByRoute { get; set; }

        [Column(nameof(RunOutByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_RunOutById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? RunOutByPlayerIdentityId { get; set; }

        [Column(nameof(RunOutByName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string RunOutByName { get; set; }

        [Column(nameof(RunOutByRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string RunOutByRoute { get; set; }

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
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Catches { get; set; }

        [Column(nameof(RunOuts))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunOuts { get; set; }

        [Column(nameof(WonMatch))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? WonMatch { get; set; }

        [Column(nameof(PlayerOfTheMatch))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? PlayerOfTheMatch { get; set; }
    }
}