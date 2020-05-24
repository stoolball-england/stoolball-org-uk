using System;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.MatchLocations
{
    public class MatchLocationQuery
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeMatchLocationIds { get; internal set; } = new List<Guid>();
    }
}