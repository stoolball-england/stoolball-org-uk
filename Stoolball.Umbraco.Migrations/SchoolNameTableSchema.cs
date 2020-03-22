using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.SchoolName)]
    [PrimaryKey(nameof(SchoolNameId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SchoolNameTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SchoolNameId))]
        public int SchoolNameId { get; set; }

        [Column(nameof(SchoolId))]
        [ForeignKey(typeof(SchoolTableSchema), Column = nameof(SchoolTableSchema.SchoolId))]
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