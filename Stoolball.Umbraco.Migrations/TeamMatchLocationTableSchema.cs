﻿using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.TeamMatchLocation)]
    [PrimaryKey(nameof(TeamMatchLocationId), AutoIncrement = true)]
    [ExplicitColumns]
    public class TeamMatchLocationTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(TeamMatchLocationId))]
        public int TeamMatchLocationId { get; set; }

        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(TeamId))]
        public int TeamId { get; set; }

        [ForeignKey(typeof(MatchLocationTableSchema), Column = nameof(MatchLocationTableSchema.MatchLocationId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchLocationId))]

        public int MatchLocationId { get; set; }
        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }
    }
}