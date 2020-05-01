using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.SchoolName)]
    [PrimaryKey(nameof(SchoolNameId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SchoolNameTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SchoolNameId))]
        public Guid SchoolNameId { get; set; }

        [Column(nameof(SchoolId))]
        [ForeignKey(typeof(SchoolTableInitialSchema), Column = nameof(SchoolTableInitialSchema.SchoolId))]
        [Index(IndexTypes.Clustered)]
        public int SchoolId { get; set; }

        [Column(nameof(SchoolName))]
        public string SchoolName { get; set; }

        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }
    }
}