using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Navigation;

namespace Stoolball.Competitions
{
    public class CompetitionFilter
    {
        public string Query { get; set; }
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public Paging Paging { get; set; } = new Paging();
    }
}