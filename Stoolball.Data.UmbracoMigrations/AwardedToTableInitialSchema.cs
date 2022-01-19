using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.AwardedTo)]
    [PrimaryKey(nameof(AwardedToId), AutoIncrement = false)]
    [ExplicitColumns]
    public class AwardedToTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(AwardedToId))]
        public Guid AwardedToId { get; set; }

        [ForeignKey(typeof(AwardTableInitialSchema), Column = nameof(AwardTableInitialSchema.AwardId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(AwardId))]
        public Guid AwardId { get; set; }

        [ForeignKey(typeof(PlayerIdentityTableInitialSchema), Column = nameof(PlayerIdentityTableInitialSchema.PlayerIdentityId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(PlayerIdentityId))]
        public Guid PlayerIdentityId { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(MatchId))]
        public Guid? MatchId { get; set; }

        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(TournamentId))]
        public Guid? TournamentId { get; set; }

        [ForeignKey(typeof(ClubTableInitialSchema), Column = nameof(ClubTableInitialSchema.ClubId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(ClubId))]
        public Guid? ClubId { get; set; }

        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(TeamId))]
        public Guid? TeamId { get; set; }

        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(SeasonId))]
        public Guid? SeasonId { get; set; }

        [Column(nameof(Reason))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Reason { get; set; }
    }
}