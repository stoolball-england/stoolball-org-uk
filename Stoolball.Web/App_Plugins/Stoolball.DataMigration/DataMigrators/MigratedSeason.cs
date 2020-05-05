using Stoolball.Competitions;
using System.Collections.Generic;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedSeason : Season
    {
        public int MigratedSeasonId { get; set; }
        public MigratedCompetition MigratedCompetition { get; set; }

        public List<MigratedTeamInSeason> MigratedTeams { get; internal set; } = new List<MigratedTeamInSeason>();
    }
}