using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Team)]
    [PrimaryKey(nameof(TeamId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TeamTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TeamId))]
        public Guid TeamId { get; set; }

        [Column(nameof(MigratedTeamId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedTeamId { get; set; }

        [Column(nameof(ClubId))]
        [ForeignKey(typeof(ClubTableInitialSchema), Column = nameof(ClubTableInitialSchema.ClubId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? ClubId { get; set; }

        [Column(nameof(SchoolId))]
        [ForeignKey(typeof(SchoolTableInitialSchema), Column = nameof(SchoolTableInitialSchema.SchoolId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? SchoolId { get; set; }

        [Column(nameof(TeamType))]
        [Index(IndexTypes.NonClustered)]
        public string TeamType { get; set; }

        [Column(nameof(PlayerType))]
        [Index(IndexTypes.NonClustered)]
        public string PlayerType { get; set; }

        [Column(nameof(Introduction))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Introduction { get; set; }

        [Column(nameof(AgeRangeLower))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? AgeRangeLower { get; set; }

        [Column(nameof(AgeRangeUpper))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? AgeRangeUpper { get; set; }

        [Column(nameof(FromYear))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? FromYear { get; set; }

        [Column(nameof(UntilYear))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? UntilYear { get; set; }

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

        [Column(nameof(PublicContactDetails))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string PublicContactDetails { get; set; }

        [Column(nameof(PrivateContactDetails))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string PrivateContactDetails { get; set; }

        [Column(nameof(PlayingTimes))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string PlayingTimes { get; set; }

        [Column(nameof(Cost))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Cost { get; set; }

        [Column(nameof(MemberGroupId))]
        public int MemberGroupId { get; set; }

        [Column(nameof(MemberGroupName))]
        public string MemberGroupName { get; set; }

        [Column(nameof(TeamRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string TeamRoute { get; set; }
    }
}