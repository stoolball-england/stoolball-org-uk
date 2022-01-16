using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.Over)]
    [PrimaryKey(nameof(OverId), AutoIncrement = false)]
    [ExplicitColumns]
    public class OverTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(OverId))]
        public Guid OverId { get; set; }

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.Clustered)]
        public Guid MatchInningsId { get; set; }

        [Column(nameof(BowlerPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        public Guid BowlerPlayerIdentityId { get; set; }

        [Column(nameof(OverNumber))]
        public int OverNumber { get; set; }

        [Column(nameof(OverSetId))]
        [ForeignKey(typeof(OverSetTableInitialSchema), Column = nameof(OverSetTableInitialSchema.OverSetId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? OverSetId { get; set; }

        [Column(nameof(BallsBowled))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? BallsBowled { get; set; }

        [Column(nameof(NoBalls))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? NoBalls { get; set; }

        [Column(nameof(Wides))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Wides { get; set; }

        [Column(nameof(RunsConceded))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsConceded { get; set; }

    }
}