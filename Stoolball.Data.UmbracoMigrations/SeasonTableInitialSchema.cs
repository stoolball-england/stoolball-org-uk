using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.Season)]
    [PrimaryKey(nameof(SeasonId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SeasonTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SeasonId))]
        public Guid SeasonId { get; set; }

        [Column(nameof(MigratedSeasonId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedSeasonId { get; set; }

        [Column(nameof(CompetitionId))]
        [ForeignKey(typeof(CompetitionTableInitialSchema), Column = nameof(CompetitionTableInitialSchema.CompetitionId))]
        [Index(IndexTypes.Clustered)]
        public Guid CompetitionId { get; set; }

        [Column(nameof(FromYear))]
        public int FromYear { get; set; }

        [Column(nameof(UntilYear))]
        public int UntilYear { get; set; }

        [Column(nameof(Introduction))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Introduction { get; set; }

        [Column(nameof(Results))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Results { get; set; }

        [Column(nameof(PlayersPerTeam))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? PlayersPerTeam { get; set; }

        [Column(nameof(EnableTournaments))]
        public bool EnableTournaments { get; set; }

        [Column(nameof(EnableBonusOrPenaltyRuns))]
        public bool EnableBonusOrPenaltyRuns { get; set; }

        [Column(nameof(ResultsTableType))]
        public string ResultsTableType { get; set; }

        [Column(nameof(EnableRunsScored))]
        public bool EnableRunsScored { get; set; }

        [Column(nameof(EnableRunsConceded))]
        public bool EnableRunsConceded { get; set; }

        [Column(nameof(EnableLastPlayerBatsOn))]
        public bool EnableLastPlayerBatsOn { get; set; }

        [Column(nameof(SeasonRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string SeasonRoute { get; set; }
    }
}