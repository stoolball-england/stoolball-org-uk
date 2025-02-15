﻿# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.MatchTeam)]
    [PrimaryKey(nameof(MatchTeamId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchTeamTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(MatchTeamId))]
        public Guid MatchTeamId { get; set; }

        [Column(nameof(MigratedMatchTeamId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedMatchTeamId { get; set; }

        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(TeamId))]
        public Guid TeamId { get; set; }

        [Column(nameof(PlayingAsTeamName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayingAsTeamName { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchId))]
        public Guid MatchId { get; set; }

        [Column(nameof(TeamRole))]
        public string TeamRole { get; set; }

        [Column(nameof(WonToss))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? WonToss { get; set; }

        [Column(nameof(WinnerOfMatchId))]
        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId), Name = "FK_StoolballMatch_StoolballMatchTeam_WinnerOfMatchId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? WinnerOfMatchId { get; set; }
    }
}