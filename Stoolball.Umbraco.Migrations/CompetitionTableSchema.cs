using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Migrations
{
    [TableName(Constants.Tables.Competition)]
    [PrimaryKey(nameof(CompetitionId), AutoIncrement = true)]
    [ExplicitColumns]
    public class CompetitionTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column(nameof(CompetitionId))]
        public int CompetitionId { get; set; }

        [Column(nameof(CompetitionName))]
        public string CompetitionName { get; set; }

        [Column(nameof(Introduction))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Introduction { get; set; }

        [Column(nameof(PublicContactDetails))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string PublicContactDetails { get; set; }

        [Column(nameof(PrivateContactDetails))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string PrivateContactDetails { get; set; }

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

        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }

        [Column(nameof(PlayerType))]
        [Index(IndexTypes.NonClustered)]
        public string PlayerType { get; set; }

        [Column(nameof(PlayersPerTeam))]
        public int PlayersPerTeam { get; set; }

        [Column(nameof(Overs))]
        public int Overs { get; set; }

        [Column(nameof(CompetitionRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string CompetitionRoute { get; set; }
    }
}