# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.ClubVersion)]
    [PrimaryKey(nameof(ClubVersionId), AutoIncrement = false)]
    [ExplicitColumns]
    public class ClubVersionTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(ClubVersionId))]
        public Guid ClubVersionId { get; set; }

        [Column(nameof(ClubId))]
        [ForeignKey(typeof(ClubTableInitialSchema), Column = nameof(ClubTableInitialSchema.ClubId))]
        [Index(IndexTypes.Clustered)]
        public Guid ClubId { get; set; }

        [Column(nameof(ClubName))]
        public string ClubName { get; set; }

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