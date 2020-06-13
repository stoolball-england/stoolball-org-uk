using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.TournamentSeason)]
    [PrimaryKey(nameof(TournamentSeasonId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TournamentSeasonTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TournamentSeasonId))]
        public Guid TournamentSeasonId { get; set; }

        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(TournamentId))]
        public Guid TournamentId { get; set; }

        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public Guid SeasonId { get; set; }
    }
}