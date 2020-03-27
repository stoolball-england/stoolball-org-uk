using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.SeasonPointsAdjustment)]
    [PrimaryKey(nameof(SeasonPointsAdjustmentId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonPointsAdjustmentTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonPointsAdjustmentId))]
        public int SeasonPointsAdjustmentId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        public int SeasonId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        public int TeamId { get; set; }

        [Column(nameof(Points))]
        public int Points { get; set; }

        [Column(nameof(Reason))]
        public string Reason { get; set; }
    }
}