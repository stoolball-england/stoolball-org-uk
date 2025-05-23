﻿# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.TournamentTeam)]
    [PrimaryKey(nameof(TournamentTeamId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TournamentTeamTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TournamentTeamId))]
        public Guid TournamentTeamId { get; set; }

        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(TeamId))]
        public Guid TeamId { get; set; }

        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(TournamentId))]
        public Guid TournamentId { get; set; }

        [Column(nameof(TeamRole))]
        public string TeamRole { get; set; }

        [Column(nameof(PlayingAsTeamName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayingAsTeamName { get; set; }
    }
}