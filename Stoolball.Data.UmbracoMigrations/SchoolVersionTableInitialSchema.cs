using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.SchoolVersion)]
    [PrimaryKey(nameof(SchoolVersionId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SchoolVersionTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SchoolVersionId))]
        public Guid SchoolVersionId { get; set; }

        [Column(nameof(SchoolId))]
        [ForeignKey(typeof(SchoolTableInitialSchema), Column = nameof(SchoolTableInitialSchema.SchoolId))]
        [Index(IndexTypes.Clustered)]
        public Guid SchoolId { get; set; }

        [Column(nameof(SchoolName))]
        public string SchoolName { get; set; }

        [Column(nameof(ComparableName))]
        public string ComparableName { get; set; }

        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }
    }
}