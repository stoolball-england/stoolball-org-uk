using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.MatchInnings)]
    [PrimaryKey(nameof(MatchInningsId), AutoIncrement = true)]
    [ExplicitColumns]
    public class MatchInningsTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(MatchInningsId))]
        public int MatchInningsId { get; set; }

        [ForeignKey(typeof(MatchTableSchema), Column = nameof(MatchTableSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }

        [Column(nameof(InningsOrderInMatch))]
        public int InningsOrderInMatch { get; set; }

        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Column(nameof(TeamId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TeamId { get; set; }

        [Column(nameof(Overs))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Overs { get; set; }

        [Column(nameof(Runs))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Runs { get; set; }

        [Column(nameof(Wickets))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? Wickets { get; set; }
    }
}