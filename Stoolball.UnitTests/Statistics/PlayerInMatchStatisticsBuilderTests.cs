using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class PlayerInMatchStatisticsBuilderTests : IClassFixture<MatchFixture>
    {
        private readonly Stoolball.Matches.Match _match;
        public List<PlayerIdentity> PlayerIdentities { get; } = new List<PlayerIdentity>();

        public PlayerInMatchStatisticsBuilderTests(MatchFixture matchFixture)
        {
            if (matchFixture is null || matchFixture.Match is null)
            {
                throw new System.ArgumentNullException(nameof(matchFixture));
            }

            _match = matchFixture.Match;
            PlayerIdentities.AddRange(new PlayerIdentityFinder().PlayerIdentitiesInMatch(_match));
        }
    }
}
