using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.SeasonPointsAdjustment)]
    [PrimaryKey(nameof(SeasonPointsAdjustmentId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonPointsAdjustmentTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonPointsAdjustmentId))]
        public int SeasonPointsAdjustmentId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableSchema), Column = nameof(SeasonTableSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        public int SeasonId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        public int TeamId { get; set; }

        [Column(nameof(Points))]
        public int Points { get; set; }

        [Column(nameof(Reason))]
        public string Reason { get; set; }
    }
}