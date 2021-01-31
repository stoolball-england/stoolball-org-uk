using System.Collections.Generic;
using Stoolball.Competitions;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedSeason : Season
    {
        public int MigratedSeasonId { get; set; }
        public MigratedCompetition MigratedCompetition { get; set; }

        public List<MigratedTeamInSeason> MigratedTeams { get; internal set; } = new List<MigratedTeamInSeason>();
        public List<MigratedPointsAdjustment> MigratedPointsAdjustments { get; internal set; } = new List<MigratedPointsAdjustment>();

        public int? Overs { get; set; }
    }
}