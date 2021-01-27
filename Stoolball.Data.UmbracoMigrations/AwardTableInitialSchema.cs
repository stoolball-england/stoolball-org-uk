using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.Award)]
    [PrimaryKey(nameof(AwardId), AutoIncrement = false)]
    [ExplicitColumns]
    public class AwardTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column(nameof(AwardId))]
        public Guid AwardId { get; set; }

        [Column(nameof(AwardName))]
        public string AwardName { get; set; }

        [Column(nameof(AwardForScope))]
        [Index(IndexTypes.NonClustered)]
        public string AwardForScope { get; set; }

        [Column(nameof(AwardByScope))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string AwardByScope { get; set; }
    }
}