using Stoolball.Matches;
using System;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.Matches
{
    public class MatchQuery
    {
        public List<int> TeamIds { get; internal set; } = new List<int>();
        public List<int> SeasonIds { get; internal set; } = new List<int>();
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();

        public DateTimeOffset? FromDate { get; set; }
    }
}