using System;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchEqualityComparerTests
    {
        [Fact]
        public void Compares_MatchId_if_present()
        {
            var MatchA = new Match { MatchId = Guid.NewGuid(), MatchRoute = "/matches/match-a" };
            var MatchB = new Match { MatchId = MatchA.MatchId, MatchRoute = "/matches/match-b" };
            var comparer = new MatchEqualityComparer();

            var resultA = comparer.Equals(MatchA, MatchB);

            MatchB.MatchId = Guid.NewGuid();
            var resultB = comparer.Equals(MatchA, MatchB);

            Assert.True(resultA);
            Assert.False(resultB);
        }

        [Fact]
        public void Compares_MatchRoute_if_no_MatchId()
        {
            var MatchA = new Match { MatchId = null, MatchRoute = "/matches/match-a" };
            var MatchB = new Match { MatchId = null, MatchRoute = MatchA.MatchRoute };
            var comparer = new MatchEqualityComparer();

            var resultA = comparer.Equals(MatchA, MatchB);

            MatchB.MatchRoute = "/matches/match-b";
            var resultB = comparer.Equals(MatchA, MatchB);

            Assert.True(resultA);
            Assert.False(resultB);
        }

        [Fact]
        public void Returns_false_if_no_MatchId_or_MatchRoute()
        {
            var MatchA = new Match();
            var MatchB = new Match();
            var comparer = new MatchEqualityComparer();

            var result = comparer.Equals(MatchA, MatchB);

            Assert.False(result);
        }
    }
}
