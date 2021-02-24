using System;
using System.Collections.Generic;

namespace Stoolball.Statistics
{
    public class PlayerIdentityFilter
    {
        public string Query { get; set; }
        public List<Guid> TeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> PlayerIdentityIds { get; internal set; } = new List<Guid>();
    }
}