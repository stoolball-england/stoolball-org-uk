using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Testing.PlayerDataProviders
{
    internal abstract class BasePlayerDataProvider
    {
        internal abstract IEnumerable<Player> CreatePlayers(TestData readOnlyTestData);
    }
}