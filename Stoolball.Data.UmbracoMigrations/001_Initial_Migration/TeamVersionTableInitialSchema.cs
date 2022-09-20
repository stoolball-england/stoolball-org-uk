# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.TeamVersion)]
    [PrimaryKey(nameof(TeamVersionId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TeamVersionTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TeamVersionId))]
        public Guid TeamVersionId { get; set; }

        [Column(nameof(TeamId))]
        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        public Guid TeamId { get; set; }

        [Column(nameof(TeamName))]
        public string TeamName { get; set; }

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