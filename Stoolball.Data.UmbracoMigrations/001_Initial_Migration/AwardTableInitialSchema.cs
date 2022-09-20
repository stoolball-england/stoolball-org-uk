# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.Award)]
    [PrimaryKey(nameof(AwardId), AutoIncrement = false)]
    [ExplicitColumns]
    public class AwardTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column(nameof(AwardId))]
        public Guid AwardId { get; set; }

        [Column(nameof(AwardName))]
        public string AwardName { get; set; }

        [Column(nameof(AwardScope))]
        [Index(IndexTypes.NonClustered)]
        public string AwardScope { get; set; }
    }
}