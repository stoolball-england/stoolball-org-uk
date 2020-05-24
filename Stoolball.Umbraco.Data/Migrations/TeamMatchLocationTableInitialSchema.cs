using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.TeamMatchLocation)]
    [PrimaryKey(nameof(TeamMatchLocationId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TeamMatchLocationTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TeamMatchLocationId))]
        public Guid TeamMatchLocationId { get; set; }

        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(TeamId))]
        public Guid TeamId { get; set; }

        [ForeignKey(typeof(MatchLocationTableInitialSchema), Column = nameof(MatchLocationTableInitialSchema.MatchLocationId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchLocationId))]
        public Guid MatchLocationId { get; set; }
    }
}