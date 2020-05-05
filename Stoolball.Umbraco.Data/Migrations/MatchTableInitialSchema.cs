using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Match)]
    [PrimaryKey(nameof(MatchId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(MatchId))]
        public Guid MatchId { get; set; }

        [Column(nameof(MigratedMatchId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedMatchId { get; set; }

        [Column(nameof(MatchName))]
        public string MatchName { get; set; }

        [Column(nameof(UpdateMatchNameAutomatically))]
        [Constraint(Default = 1)]
        public bool UpdateMatchNameAutomatically { get; set; }

        [Column(nameof(MatchLocationId))]
        [ForeignKey(typeof(MatchLocationTableInitialSchema), Column = nameof(MatchLocationTableInitialSchema.MatchLocationId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? MatchLocationId { get; set; }

        [Column(nameof(StartTime))]
        [Index(IndexTypes.Clustered)]
        public DateTime StartTime { get; set; }

        [Column(nameof(StartTimeIsKnown))]
        [Constraint(Default = 1)]
        public bool StartTimeIsKnown { get; set; }

        [Column(nameof(MatchType))]
        [Index(IndexTypes.NonClustered)]
        public string MatchType { get; set; }

        [Column(nameof(PlayerType))]
        [Index(IndexTypes.NonClustered)]
        public string PlayerType { get; set; }

        [Column(nameof(PlayersPerTeam))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? PlayersPerTeam { get; set; }

        [Column(nameof(TournamentQualificationType))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string TournamentQualificationType { get; set; }

        [Column(nameof(InningsOrderIsKnown))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public bool? InningsOrderIsKnown { get; set; }

        [Column(nameof(OversPerInningsDefault))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OversPerInningsDefault { get; set; }

        [Column(nameof(MatchResultType))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string MatchResultType { get; set; }

        [Column(nameof(TournamentId))]
        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? TournamentId { get; set; }

        [Column(nameof(OrderInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? OrderInTournament { get; set; }

        [Column(nameof(MaximumTeamsInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MaximumTeamsInTournament { get; set; }

        [Column(nameof(SpacesInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? SpacesInTournament { get; set; }

        [Column(nameof(MatchNotes))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string MatchNotes { get; set; }

        [Column(nameof(MatchRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string MatchRoute { get; set; }
    }
}