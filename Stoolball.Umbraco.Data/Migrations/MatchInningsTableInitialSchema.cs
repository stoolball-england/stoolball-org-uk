using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.MatchInnings)]
    [PrimaryKey(nameof(MatchInningsId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchInningsTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(MatchInningsId))]
        public Guid MatchInningsId { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(MatchId))]
        public Guid MatchId { get; set; }

        [Column(nameof(InningsOrderInMatch))]
        public int InningsOrderInMatch { get; set; }

        [ForeignKey(typeof(MatchTeamTableInitialSchema), Column = nameof(MatchTeamTableInitialSchema.MatchTeamId), Name = "FK_StoolballMatchInnings_StoolballMatchTeam_BattingMatchTeamId")]
        [Column(nameof(BattingMatchTeamId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? BattingMatchTeamId { get; set; }

        [ForeignKey(typeof(MatchTeamTableInitialSchema), Column = nameof(MatchTeamTableInitialSchema.MatchTeamId), Name = "FK_StoolballMatchInnings_StoolballMatchTeam_BowlingMatchTeamId")]
        [Column(nameof(BowlingMatchTeamId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? BowlingMatchTeamId { get; set; }

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