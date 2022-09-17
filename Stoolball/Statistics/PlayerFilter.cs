using System;
using System.Collections.Generic;

namespace Stoolball.Statistics
{
    public class PlayerFilter
    {
        public string Query { get; set; }
        public List<Guid> ClubIds { get; internal set; } = new();
        public List<Guid> TeamIds { get; internal set; } = new();
        public List<Guid> PlayerIds { get; internal set; } = new();
        public List<Guid> PlayerIdentityIds { get; internal set; } = new();
        public List<Guid> MatchLocationIds { get; internal set; } = new();
        public List<Guid> CompetitionIds { get; internal set; } = new();
        public List<Guid> SeasonIds { get; internal set; } = new();
    }
}