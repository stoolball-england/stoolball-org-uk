using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Season)]
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

        [Column(nameof(StartYear))]
        public int StartYear { get; set; }

        [Column(nameof(EndYear))]
        public int EndYear { get; set; }

        [Column(nameof(Introduction))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Introduction { get; set; }

        [Column(nameof(Results))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Results { get; set; }

        [Column(nameof(ShowTable))]
        public bool ShowTable { get; set; }

        [Column(nameof(ShowRunsScored))]
        public bool ShowRunsScored { get; set; }

        [Column(nameof(ShowRunsConceded))]
        public bool ShowRunsConceded { get; set; }

        [Column(nameof(SeasonRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string SeasonRoute { get; set; }
    }
}