using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.PlayerIdentity)]
    [PrimaryKey(nameof(PlayerIdentityId), AutoIncrement = true)]
    [ExplicitColumns]
    public class PlayerIdentityTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column(nameof(PlayerIdentityId))]
        public int PlayerIdentityId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        public int TeamId { get; set; }

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
        public int TotalMatches { get; set; }

        [Column(nameof(MissedMatches))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int MissedMatches { get; set; }

        [Column(nameof(Probability))]
        [Index(IndexTypes.NonClustered)]
        [Constraint(Default = 0)]
        public int Probability { get; set; }

        [Column(nameof(PlayerRole))]
        public string PlayerRole { get; set; }

        [Column(nameof(PlayerIdentityRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string PlayerIdentityRoute { get; set; }
    }
}