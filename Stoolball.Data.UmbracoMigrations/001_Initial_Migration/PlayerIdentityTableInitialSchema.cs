# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.PlayerIdentity)]
    [PrimaryKey(nameof(PlayerIdentityId), AutoIncrement = false)]
    [ExplicitColumns]
    public class PlayerIdentityTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(PlayerIdentityId))]
        public Guid PlayerIdentityId { get; set; }

        [Column(nameof(PlayerId))]
        [ForeignKey(typeof(PlayerTableInitialSchema), Column = nameof(PlayerTableInitialSchema.PlayerId))]
        public Guid PlayerId { get; set; }

        [Column(nameof(MigratedPlayerIdentityId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedPlayerIdentityId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        public Guid TeamId { get; set; }

        [Column(nameof(PlayerIdentityName))]
        public string PlayerIdentityName { get; set; }

        [Column(nameof(ComparableName))]
        public string ComparableName { get; set; }
    }
}