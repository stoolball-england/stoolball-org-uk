using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.SeasonPointsAdjustment)]
    [PrimaryKey(nameof(SeasonPointsAdjustmentId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SeasonPointsAdjustmentTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SeasonPointsAdjustmentId))]
        public Guid SeasonPointsAdjustmentId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        public Guid SeasonId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        public Guid TeamId { get; set; }

        [Column(nameof(Points))]
        public int Points { get; set; }

        [Column(nameof(Reason))]
        public string Reason { get; set; }
    }
}