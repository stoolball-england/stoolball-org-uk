using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
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

        [Column(nameof(AwardSet))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? AwardSet { get; set; }

        [Column(nameof(EquivalentAwardSet))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? EquivalentAwardSet { get; set; }
    }
}