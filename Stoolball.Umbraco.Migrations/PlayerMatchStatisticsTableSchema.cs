using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.PlayerMatchStatistics)]
    [PrimaryKey(nameof(PlayerMatchStatisticsId), AutoIncrement = true)]
    [ExplicitColumns]
    public class PlayerMatchStatisticsTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(PlayerMatchStatisticsId))]
        public int PlayerMatchStatisticsId { get; set; }

        [Column(nameof(PlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableSchema), Column = nameof(PlayerIdentityTableSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        public int PlayerIdentityId { get; set; }

        [Column(nameof(PlayerRole))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerRole { get; set; }

        [Column(nameof(PlayerIdentityName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerIdentityName { get; set; }

        [Column(nameof(PlayerIdentityRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerIdentityRoute { get; set; }

        [Column(nameof(MatchId))]
        [ForeignKey(typeof(MatchTableSchema), Column = nameof(MatchTableSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        public int MatchId { get; set; }

        [Column(nameof(TournamentId))]
        [ForeignKey(typeof(MatchTableSchema), Column = nameof(MatchTableSchema.MatchId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballMatch_TournamentId")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TournamentId { get; set; }

        [Column(nameof(MatchStartTime))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? MatchStartTime { get; set; }

        [Column(nameof(MatchType))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchType { get; set; }

        [Column(nameof(MatchPlayerType))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchPlayerType { get; set; }

        [Column(nameof(MatchTitle))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchTitle { get; set; }

        [Column(nameof(MatchRoute))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchRoute { get; set; }

        [Column(nameof(MatchTeamId))]
        [ForeignKey(typeof(MatchTeamTableSchema), Column = nameof(MatchTeamTableSchema.MatchTeamId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchTeamId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TeamId { get; set; }

        [Column(nameof(TeamName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string TeamName { get; set; }

        [Column(nameof(OppositionTeamId))]
        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballTeam_OppositionTeamId")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OppositionTeamId { get; set; }

        [Column(nameof(OppositionTeamName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string OppositionTeamName { get; set; }

        [Column(nameof(MatchLocationId))]
        [ForeignKey(typeof(MatchLocationTableSchema), Column = nameof(MatchLocationTableSchema.MatchLocationId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchLocationId { get; set; }

        [Column(nameof(InningsOrderInMatch))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? InningsOrderInMatch { get; set; }

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
        public bool? HasRunsConceded { get; set; }

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

        [Column(nameof(HowOut))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string HowOut { get; set; }

        [Column(nameof(PlayerWasDismissed))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? PlayerWasDismissed { get; set; }

        [Column(nameof(BowledById))]
        [ForeignKey(typeof(PlayerIdentityTableSchema), Column = nameof(PlayerIdentityTableSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_BowledById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BowledById { get; set; }

        [Column(nameof(CaughtById))]
        [ForeignKey(typeof(PlayerIdentityTableSchema), Column = nameof(PlayerIdentityTableSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_CaughtById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? CaughtById { get; set; }

        [Column(nameof(RunOutById))]
        [ForeignKey(typeof(PlayerIdentityTableSchema), Column = nameof(PlayerIdentityTableSchema.PlayerIdentityId), Name = "FK_StoolballStatisticsPlayerMatch_StoolballPlayerIdentity_RunOutById")]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunOutById { get; set; }

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