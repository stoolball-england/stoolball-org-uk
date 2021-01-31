using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedTournament : Tournament
    {
        public int MigratedTournamentId { get; set; }
        public int? MigratedTournamentLocationId { get; set; }
        public List<MigratedTeamInMatch> MigratedTeams { get; internal set; } = new List<MigratedTeamInMatch>();
        public List<int> MigratedSeasonIds { get; internal set; } = new List<int>();
        public int? OversPerInningsDefault { get; set; }
    }
}