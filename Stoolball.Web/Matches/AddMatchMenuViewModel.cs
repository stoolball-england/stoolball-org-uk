using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    [ExcludeFromCodeCoverage]
    public class AddMatchMenuViewModel
    {
        public List<MatchType> MatchTypes { get; private set; } = new List<MatchType>();
        public string BaseRoute { get; set; }
        public bool EnableTournaments { get; set; }
    }
}