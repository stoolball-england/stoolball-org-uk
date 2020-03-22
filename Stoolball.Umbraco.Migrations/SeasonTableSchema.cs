using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.Season)]
    [PrimaryKey(nameof(SeasonId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonId))]
        public int SeasonId { get; set; }

        [Column(nameof(SeasonName))]
        public string SeasonName { get; set; }

        [Column(nameof(CompetitionId))]
        [ForeignKey(typeof(CompetitionTableSchema), Column = nameof(CompetitionTableSchema.CompetitionId))]
        [Index(IndexTypes.Clustered)]
        public int CompetitionId { get; set; }

        [Column(nameof(IsLatestSeason))]
        public bool IsLatestSeason { get; set; }

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