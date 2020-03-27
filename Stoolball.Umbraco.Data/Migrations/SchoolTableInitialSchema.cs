
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.School)]
    [PrimaryKey(nameof(SchoolId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SchoolTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column(nameof(SchoolId))]
        public int SchoolId { get; set; }

        [Column(nameof(PlaysOutdoors))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? PlaysOutdoors { get; set; }

        [Column(nameof(PlaysIndoors))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? PlaysIndoors { get; set; }

        [Column(nameof(Website))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Website { get; set; }

        [Column(nameof(Twitter))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Twitter { get; set; }

        [Column(nameof(Facebook))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Facebook { get; set; }

        [Column(nameof(Instagram))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Instagram { get; set; }

        [Column(nameof(YouTube))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string YouTube { get; set; }

        [Column(nameof(HowManyPlayers))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? HowManyPlayers { get; set; }

        [Column(nameof(SchoolRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string SchoolRoute { get; set; }
    }
}