using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.Competitions
{
    public class CompetitionQuery
    {
        public string Query { get; internal set; }
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}