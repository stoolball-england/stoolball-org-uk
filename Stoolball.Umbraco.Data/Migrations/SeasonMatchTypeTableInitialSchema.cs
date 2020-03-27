using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.SeasonMatchType)]
    [PrimaryKey(nameof(SeasonMatchTypeId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonMatchTypeTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonMatchTypeId))]
        public int SeasonMatchTypeId { get; set; }

        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public int SeasonId { get; set; }

        [Column(nameof(MatchType))]
        public string MatchType { get; set; }

    }
}