﻿using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.BowlingOver)]
    [PrimaryKey(nameof(BowlingOverId), AutoIncrement = true)]
    [ExplicitColumns]
    public class BowlingOverTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(BowlingOverId))]
        public int BowlingOverId { get; set; }

        [Column(nameof(MatchTeamId))]
        [ForeignKey(typeof(MatchTeamTableInitialSchema), Column = nameof(MatchTeamTableInitialSchema.MatchTeamId))]
        [Index(IndexTypes.Clustered)]
        public int MatchTeamId { get; set; }

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

        [Column(nameof(RunsInOver))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsInOver { get; set; }

    }
}