using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.Tournament)]
    [PrimaryKey(nameof(TournamentId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TournamentTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TournamentId))]
        public Guid TournamentId { get; set; }

        [Column(nameof(MigratedTournamentId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedTournamentId { get; set; }

        [Column(nameof(TournamentName))]
        public string TournamentName { get; set; }

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

        [Column(nameof(PlayerType))]
        [Index(IndexTypes.NonClustered)]
        public string PlayerType { get; set; }

        [Column(nameof(PlayersPerTeam))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? PlayersPerTeam { get; set; }

        [Column(nameof(QualificationType))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string QualificationType { get; set; }

        [Column(nameof(MaximumTeamsInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MaximumTeamsInTournament { get; set; }

        [Column(nameof(SpacesInTournament))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? SpacesInTournament { get; set; }

        [Column(nameof(TournamentNotes))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string TournamentNotes { get; set; }

        [Column(nameof(TournamentRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string TournamentRoute { get; set; }

        [Column(nameof(MemberKey))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? MemberKey { get; set; }
    }
}