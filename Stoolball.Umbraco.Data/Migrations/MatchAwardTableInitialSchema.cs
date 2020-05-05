using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.MatchAward)]
    [PrimaryKey(nameof(MatchAwardId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchAwardTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(MatchAwardId))]
        public Guid MatchAwardId { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(MatchId))]
        public Guid MatchId { get; set; }

        [ForeignKey(typeof(MatchAwardTypeTableInitialSchema), Column = nameof(MatchAwardTypeTableInitialSchema.MatchAwardTypeId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchAwardTypeId))]
        public Guid MatchAwardTypeId { get; set; }

        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(PlayerIdentityId))]
        public Guid PlayerIdentityId { get; set; }
    }
}