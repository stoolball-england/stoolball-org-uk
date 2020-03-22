using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.SeasonMatch)]
    [PrimaryKey(nameof(SeasonMatchId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonMatchTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonMatchId))]
        public int SeasonMatchId { get; set; }

        [ForeignKey(typeof(SeasonTableSchema), Column = nameof(SeasonTableSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public int SeasonId { get; set; }

        [ForeignKey(typeof(MatchTableSchema), Column = nameof(MatchTableSchema.MatchId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }
    }
}