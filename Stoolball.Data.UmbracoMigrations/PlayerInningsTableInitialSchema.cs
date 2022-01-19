using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.PlayerInnings)]
    [PrimaryKey(nameof(PlayerInningsId), AutoIncrement = false)]
    [ExplicitColumns]
    public class PlayerInningsTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(PlayerInningsId))]
        public Guid PlayerInningsId { get; set; }

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.Clustered)]
        public Guid MatchInningsId { get; set; }

        [Column(nameof(BatterPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        public Guid BatterPlayerIdentityId { get; set; }

        [Column(nameof(BattingPosition))]
        public int BattingPosition { get; set; }

        [Column(nameof(DismissalType))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string DismissalType { get; set; }

        [Column(nameof(DismissedByPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballBatting_StoolballPlayerIdentity_DismissedById")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? DismissedByPlayerIdentityId { get; set; }

        [Column(nameof(BowlerPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId), Name = "FK_StoolballBatting_StoolballPlayerIdentity_BowlerId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? BowlerPlayerIdentityId { get; set; }

        [Column(nameof(RunsScored))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsScored { get; set; }

        [Column(nameof(BallsFaced))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BallsFaced { get; set; }

    }
}