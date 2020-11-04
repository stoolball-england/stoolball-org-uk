
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
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

        [Column(nameof(ClubMark))]
        public bool ClubMark { get; set; }

        [Column(nameof(MemberGroupKey))]
        public Guid MemberGroupKey { get; set; }

        [Column(nameof(MemberGroupName))]
        public string MemberGroupName { get; set; }

        [Column(nameof(ClubRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string ClubRoute { get; set; }
    }
}