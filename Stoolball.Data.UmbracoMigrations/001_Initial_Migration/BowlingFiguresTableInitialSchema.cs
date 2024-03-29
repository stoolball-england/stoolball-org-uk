﻿using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.BowlingFigures)]
    [PrimaryKey(nameof(BowlingFiguresId), AutoIncrement = false)]
    [ExplicitColumns]
    public class BowlingFiguresTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(BowlingFiguresId))]
        public Guid BowlingFiguresId { get; set; }

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.Clustered)]
        public Guid MatchInningsId { get; set; }

        [Column(nameof(BowlingOrder))]
        public int BowlingOrder { get; set; }

        [Column(nameof(BowlerPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        public Guid BowlerPlayerIdentityId { get; set; }

        [Column(nameof(Overs))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public double? Overs { get; set; }

        [Column(nameof(Maidens))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Maidens { get; set; }

        [Column(nameof(RunsConceded))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? RunsConceded { get; set; }

        [Column(nameof(Wickets))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Wickets { get; set; }

        [Column(nameof(IsFromOversBowled))]
        public bool IsFromOversBowled { get; set; }
    }
}