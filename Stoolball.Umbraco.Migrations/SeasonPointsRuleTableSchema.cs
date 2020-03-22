using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.SeasonPointsRule)]
    [PrimaryKey(nameof(SeasonPointsRuleId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonPointsRuleTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonPointsRuleId))]
        public int SeasonPointsRuleId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableSchema), Column = nameof(SeasonTableSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        public int SeasonId { get; set; }

        [Column(nameof(MatchResultType))]
        public string MatchResultType { get; set; }

        [Column(nameof(HomePoints))]
        public int HomePoints { get; set; }

        [Column(nameof(AwayPoints))]
        public int AwayPoints { get; set; }
    }
}