using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.FallOfWicket)]
    [PrimaryKey(nameof(FallOfWicketId), AutoIncrement = false)]
    [ExplicitColumns]
    public class FallOfWicketTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(FallOfWicketId))]
        public Guid FallOfWicketId { get; set; }

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.Clustered)]
        public Guid MatchInningsId { get; set; }

        [Column(nameof(BatterPlayerIdentityId))]
        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        public Guid BatterPlayerIdentityId { get; set; }

        [Column(nameof(BattingPosition))]
        public int BattingPosition { get; set; }

        [Column(nameof(Runs))]
        public int Runs { get; set; }

        [Column(nameof(Wickets))]
        public int Wickets { get; set; }
    }
}