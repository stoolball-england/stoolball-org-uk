
using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.School)]
    [PrimaryKey(nameof(SchoolId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SchoolTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column(nameof(SchoolId))]
        public Guid SchoolId { get; set; }

        [Column(nameof(MigratedSchoolId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedSchoolId { get; set; }

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

        [Column(nameof(SchoolRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string SchoolRoute { get; set; }
    }
}