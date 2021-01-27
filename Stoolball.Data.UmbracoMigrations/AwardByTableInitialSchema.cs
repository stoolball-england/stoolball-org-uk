using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.AwardBy)]
    [PrimaryKey(nameof(AwardById), AutoIncrement = false)]
    [ExplicitColumns]
    public class AwardByTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(AwardById))]
        public Guid AwardById { get; set; }

        [ForeignKey(typeof(AwardTableInitialSchema), Column = nameof(AwardTableInitialSchema.AwardId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(AwardId))]
        public Guid AwardId { get; set; }

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
    }
}