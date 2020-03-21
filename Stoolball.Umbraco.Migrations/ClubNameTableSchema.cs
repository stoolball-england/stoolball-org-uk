using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.ClubName)]
    [PrimaryKey(nameof(ClubNameId), AutoIncrement = true)]
    [ExplicitColumns]
    public class ClubNameTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(ClubNameId))]
        public int ClubNameId { get; set; }

        [ForeignKey(typeof(ClubTableSchema), Column = nameof(ClubTableSchema.ClubId))]
        [Index(IndexTypes.Clustered)]
        public int ClubId { get; set; }

        [Column(nameof(ClubName))]
        public string ClubName { get; set; }

        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }
    }
}