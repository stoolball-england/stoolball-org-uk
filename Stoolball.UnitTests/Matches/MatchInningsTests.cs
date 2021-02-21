using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchInningsTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 2)]
        [InlineData(5, 3)]
        [InlineData(6, 3)]
        public void MatchInningsPair_is_correct(int inningsOrderInMatch, int inningsPair)
        {
            var innings = new MatchInnings { InningsOrderInMatch = inningsOrderInMatch };

            var result = innings.InningsPair();

            Assert.Equal(inningsPair, result);
        }
    }
}
