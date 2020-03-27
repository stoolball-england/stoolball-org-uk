using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Batting)]
    [PrimaryKey(nameof(BattingId), AutoIncrement = true)]
    [ExplicitColumns]
    public class BattingTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(BattingId))]
        public int BattingId { get; set; }

        [Column(nameof(MatchTeamId))]
        [ForeignKey(typeof(MatchTeamTableInitialSchema), Column = nameof(MatchTeamTableInitialSchema.MatchTeamId))]
        [Index(IndexTypes.Clustered)]
        public int MatchTeamId { get; set; }

        [Column(nameof(PlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        public int PlayerIdentityId { get; set; }

        [Column(nameof(BattingPosition))]
        public int BattingPosition { get; set; }

        [Column(nameof(HowOut))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string HowOut { get; set; }

        [Column(nameof(DismissedById))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballBatting_StoolballPlayerIdentity_DismissedById")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? DismissedById { get; set; }

        [Column(nameof(BowlerId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballBatting_StoolballPlayerIdentity_BowlerId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BowlerId { get; set; }

        [Column(nameof(RunsScored))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsScored { get; set; }

        [Column(nameof(BallsFaced))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BallsFaced { get; set; }

    }
}