using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Testing.MatchDataProviders
{
    internal abstract class BaseMatchDataProvider
    {
        /// <summary>
        /// Providers run in ascending order. Providers with the same order run in the order they were registered.
        /// Use a lower value for a provider whose matches other providers need to already exist.
        /// </summary>
        internal virtual int Order => 0;

        internal abstract IEnumerable<Match> CreateMatches(TestData readOnlyTestData);
    }
}