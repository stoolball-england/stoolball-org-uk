using System;
using Stoolball.Awards;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class MatchAward
    {
        public Guid? MatchAwardId { get; set; }
        public Award Award { get; set; }
        public PlayerIdentity PlayerIdentity { get; set; }
        public string Reason { get; set; }
    }
}
