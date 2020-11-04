using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.PlayerIdentity)]
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

        [Column(nameof(PlayerIdentityComparableName))]
        public string PlayerIdentityComparableName { get; set; }

        [Column(nameof(FirstPlayed))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? FirstPlayed { get; set; }

        [Column(nameof(LastPlayed))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? LastPlayed { get; set; }

        [Column(nameof(TotalMatches))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TotalMatches { get; set; }

        [Column(nameof(MissedMatches))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MissedMatches { get; set; }

        [Column(nameof(Probability))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Probability { get; set; }
    }
}