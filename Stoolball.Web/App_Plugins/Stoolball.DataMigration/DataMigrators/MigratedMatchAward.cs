using System;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedMatchAward
    {
        public Guid? MatchId { get; set; }
        public Guid? AwardId { get; set; }
        public Guid? MatchAwardId { get; internal set; }
        public int MigratedMatchId { get; set; }
        public int? PlayerOfTheMatchId { get; set; }
        public int? PlayerOfTheMatchHomeId { get; set; }
        public int? PlayerOfTheMatchAwayId { get; set; }
        public Guid? PlayerIdentityId { get; internal set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/award/{MatchAwardId}"); }
        }
    }
}