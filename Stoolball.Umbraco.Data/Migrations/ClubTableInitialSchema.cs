
using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Club)]
    [PrimaryKey(nameof(ClubId), AutoIncrement = false)]
    [ExplicitColumns]
    public class ClubTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column(nameof(ClubId))]
        public Guid ClubId { get; set; }

        [Column(nameof(MigratedClubId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedClubId { get; set; }

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

        [Column(nameof(ClubMark))]
        public bool ClubMark { get; set; }

        [Column(nameof(MemberGroupId))]
        public int MemberGroupId { get; set; }

        [Column(nameof(MemberGroupName))]
        public string MemberGroupName { get; set; }

        [Column(nameof(ClubRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string ClubRoute { get; set; }
    }
}