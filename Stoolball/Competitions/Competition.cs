using System;
using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class Competition
    {
        public int? CompetitionId { get; set; }

        public string CompetitionName { get; set; }

        public List<Season> Seasons { get; private set; } = new List<Season>();

        public string Introduction { get; set; }

        public string PublicContactDetails { get; set; }

        public string PrivateContactDetails { get; set; }

        public string Website { get; set; }

        public string Twitter { get; set; }

        public string Facebook { get; set; }

        public string Instagram { get; set; }

        public string YouTube { get; set; }

        public DateTimeOffset FromDate { get; set; }

        public DateTimeOffset? UntilDate { get; set; }

        public PlayerType PlayerType { get; set; }

        public int PlayersPerTeam { get; set; } = 11;

        public int Overs { get; set; } = 16;

        public string CompetitionRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/competition/{CompetitionId}"); }
        }
    }
}
