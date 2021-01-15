using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.TournamentAward)]
    [PrimaryKey(nameof(TournamentAwardId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TournamentAwardTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TournamentAwardId))]
        public Guid TournamentAwardId { get; set; }

        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(TournamentId))]
        public Guid TournamentId { get; set; }

        [ForeignKey(typeof(AwardTableInitialSchema), Column = nameof(AwardTableInitialSchema.AwardId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(AwardId))]
        public Guid AwardId { get; set; }

        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(PlayerIdentityId))]
        public Guid PlayerIdentityId { get; set; }

        [Column(nameof(Reason))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Reason { get; set; }
    }
}