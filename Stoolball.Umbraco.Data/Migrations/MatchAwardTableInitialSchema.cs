using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.MatchAward)]
    [PrimaryKey(nameof(MatchAwardId), AutoIncrement = true)]
    [ExplicitColumns]
    public class MatchAwardTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(MatchAwardId))]
        public int MatchAwardId { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }

        [ForeignKey(typeof(MatchAwardTypeTableInitialSchema), Column = nameof(MatchAwardTypeTableInitialSchema.MatchAwardTypeId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchAwardTypeId))]
        public int MatchAwardTypeId { get; set; }

        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(PlayerIdentityId))]
        public int PlayerIdentityId { get; set; }
    }
}