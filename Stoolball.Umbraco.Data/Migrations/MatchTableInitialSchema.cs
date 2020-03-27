using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Match)]
    [PrimaryKey(nameof(MatchId), AutoIncrement = true)]
    [ExplicitColumns]
    public class MatchTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }

        [Column(nameof(MatchTitle))]
        public string MatchTitle { get; set; }

        [Column(nameof(IsCustomTitle))]
        public bool IsCustomTitle { get; set; }

        [Column(nameof(MatchLocationId))]
        [ForeignKey(typeof(MatchLocationTableInitialSchema), Column = nameof(MatchLocationTableInitialSchema.MatchLocationId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MatchLocationId { get; set; }

        [Column(nameof(MatchStartTime))]
        [Index(IndexTypes.Clustered)]
        public DateTime MatchStartTime { get; set; }

        [Column(nameof(MatchStartTimeIsKnown))]
        [Constraint(Default = 1)]
        public bool MatchStartTimeIsKnown { get; set; }

        [Column(nameof(MatchType))]
        [Index(IndexTypes.NonClustered)]
        public string MatchType { get; set; }

        [Column(nameof(PlayerType))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerType { get; set; }

        [Column(nameof(PlayersPerTeam))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int PlayersPerTeam { get; set; }

        [Column(nameof(MatchQualificationType))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchQualificationType { get; set; }

        [Column(nameof(HomeTeamWonToss))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? HomeTeamWonToss { get; set; }

        [Column(nameof(InningsOrderIsKnown))]
        public bool InningsOrderIsKnown { get; set; }

        [Column(nameof(MatchResultType))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchResultType { get; set; }

        [Column(nameof(TournamentId))]
        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TournamentId { get; set; }

        [Column(nameof(OrderInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OrderInTournament { get; set; }

        [Column(nameof(MaxTeamsInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MaxTeamsInTournament { get; set; }

        [Column(nameof(TournamentSpaces))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TournamentSpaces { get; set; }

        [Column(nameof(MatchNotes))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string MatchNotes { get; set; }

        [Column(nameof(MatchRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string MatchRoute { get; set; }
    }
}