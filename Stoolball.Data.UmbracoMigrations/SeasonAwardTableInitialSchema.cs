using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.SeasonAward)]
    [PrimaryKey(nameof(SeasonAwardId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SeasonAwardTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SeasonAwardId))]
        public Guid SeasonAwardId { get; set; }

        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public Guid SeasonId { get; set; }

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