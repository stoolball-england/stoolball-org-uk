using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Testing.MatchDataProviders
{
    internal abstract class BaseMatchDataProvider
    {
        internal abstract IEnumerable<Match> CreateMatches(TestData readOnlyTestData);
    }
}