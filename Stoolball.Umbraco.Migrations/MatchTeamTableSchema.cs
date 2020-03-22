using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.MatchTeam)]
    [PrimaryKey(nameof(MatchTeamId), AutoIncrement = true)]
    [ExplicitColumns]
    public class MatchTeamTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(MatchTeamId))]
        public int MatchTeamId { get; set; }

        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(TeamId))]
        public int TeamId { get; set; }

        [ForeignKey(typeof(MatchTableSchema), Column = nameof(MatchTableSchema.MatchId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }

        [Column(nameof(TeamRole))]
        public string TeamRole { get; set; }
    }
}