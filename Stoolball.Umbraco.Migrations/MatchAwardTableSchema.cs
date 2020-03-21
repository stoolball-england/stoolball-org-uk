using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.MatchAward)]
    [PrimaryKey(nameof(MatchAwardId), AutoIncrement = true)]
    [ExplicitColumns]
    public class MatchAwardTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(MatchAwardId))]
        public int MatchAwardId { get; set; }

        [ForeignKey(typeof(MatchTableSchema), Column = nameof(MatchTableSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }

        [ForeignKey(typeof(MatchAwardTypeTableSchema), Column = nameof(MatchAwardTypeTableSchema.MatchAwardTypeId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchAwardTypeId))]
        public int MatchAwardTypeId { get; set; }

        [ForeignKey(typeof(PlayerIdentityTableSchema), Column = nameof(PlayerIdentityTableSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(PlayerIdentityId))]
        public int PlayerIdentityId { get; set; }
    }
}