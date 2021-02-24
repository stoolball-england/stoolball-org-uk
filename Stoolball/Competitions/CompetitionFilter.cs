using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Competitions
{
    public class CompetitionFilter
    {
        public string Query { get; internal set; }
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}