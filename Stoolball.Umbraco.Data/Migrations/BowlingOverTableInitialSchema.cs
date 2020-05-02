using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.BowlingOver)]
    [PrimaryKey(nameof(BowlingOverId), AutoIncrement = false)]
    [ExplicitColumns]
    public class BowlingOverTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(BowlingOverId))]
        public Guid BowlingOverId { get; set; }

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.Clustered)]
        public Guid MatchInningsId { get; set; }

        [Column(nameof(PlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        public int PlayerIdentityId { get; set; }

        [Column(nameof(OverNumber))]
        public int OverNumber { get; set; }

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