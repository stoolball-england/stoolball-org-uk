using System;
using Stoolball.Awards;
using Stoolball.Statistics;

namespace Stoolball.Matches
{
    public class MatchAward
    {
        public Guid? AwardedToId { get; set; }
        public Award Award { get; set; }
        public PlayerIdentity PlayerIdentity { get; set; }
        public string Reason { get; set; }
    }
}
