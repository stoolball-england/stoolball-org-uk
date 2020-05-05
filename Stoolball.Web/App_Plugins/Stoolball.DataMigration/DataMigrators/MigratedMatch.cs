using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedMatch : Match
    {
        public int MigratedMatchId { get; set; }
        public int? MigratedMatchLocationId { get; set; }
        public List<MigratedMatchInnings> MigratedMatchInnings { get; internal set; } = new List<MigratedMatchInnings>();

        public int? MigratedTournamentId { get; set; }
        public List<MigratedTeamInMatch> MigratedTeams { get; internal set; } = new List<MigratedTeamInMatch>();

        public List<int> MigratedSeasonIds { get; internal set; } = new List<int>();

    }
}