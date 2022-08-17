using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.OverSet)]
    [PrimaryKey(nameof(OverSetId), AutoIncrement = false)]
    [ExplicitColumns]
    public class OverSetTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(OverSetId))]
        public Guid OverSetId { get; set; }

        [Column(nameof(MatchInningsId))]
        [ForeignKey(typeof(MatchInningsTableInitialSchema), Column = nameof(MatchInningsTableInitialSchema.MatchInningsId))]
        [Index(IndexTypes.Clustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? MatchInningsId { get; set; }

        [Column(nameof(TournamentId))]
        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? TournamentId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? SeasonId { get; set; }

        [Column(nameof(OverSetNumber))]
        public int OverSetNumber { get; set; }

        [Column(nameof(Overs))]
        public int Overs { get; set; }

        [Column(nameof(BallsPerOver))]
        public int BallsPerOver { get; set; }
    }
}