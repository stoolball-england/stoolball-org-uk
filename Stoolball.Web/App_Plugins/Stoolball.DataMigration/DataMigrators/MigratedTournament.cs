using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedTournament : Tournament
    {
        public int MigratedTournamentId { get; set; }
        public int? MigratedTournamentLocationId { get; set; }
        public List<MigratedTeamInMatch> MigratedTeams { get; internal set; } = new List<MigratedTeamInMatch>();
        public List<int> MigratedSeasonIds { get; internal set; } = new List<int>();
    }
}