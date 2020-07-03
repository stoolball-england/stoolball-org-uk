using Stoolball.Awards;
using Stoolball.Teams;
using System;

namespace Stoolball.Matches
{
    public class MatchAward
    {
        public Guid? MatchAwardId { get; set; }
        public Award Award { get; set; }
        public PlayerIdentity PlayerIdentity { get; set; }
    }
}
