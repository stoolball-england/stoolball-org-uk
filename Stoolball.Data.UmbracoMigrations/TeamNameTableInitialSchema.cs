﻿using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.TeamName)]
    [PrimaryKey(nameof(TeamNameId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TeamNameTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TeamNameId))]
        public Guid TeamNameId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        public Guid TeamId { get; set; }

        [Column(nameof(TeamName))]
        public string TeamName { get; set; }

        [Column(nameof(TeamComparableName))]
        public string TeamComparableName { get; set; }

        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }
    }
}