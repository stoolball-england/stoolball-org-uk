using System;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.Matches
{
    public class MatchQuery
    {
        public List<int> TeamIds { get; internal set; } = new List<int>();
        public List<int> SeasonIds { get; internal set; } = new List<int>();

        public DateTimeOffset? FromDate { get; set; }
    }
}