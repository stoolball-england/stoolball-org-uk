using System.Collections.Generic;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class OverSetTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 1)]
        [InlineData(6, 2)]
        [InlineData(8, 2)]
        [InlineData(9, 3)]
        [InlineData(10, 3)]
        public void ForOver_gets_the_right_over(int overNumber, int overSetNumber)
        {
            var sets = new List<OverSet> {
                new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 },
                new OverSet { OverSetNumber = 2, Overs = 3, BallsPerOver = 10 },
                new OverSet { OverSetNumber = 3, Overs = 2, BallsPerOver = 12 }
            };

            var result = OverSet.ForOver(sets, overNumber);

            Assert.Equal(overSetNumber, result.OverSetNumber);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(6)]
        public void ForOver_returns_null_if_over_not_found(int overNumber)
        {
            var sets = new List<OverSet> { new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 } };

            var result = OverSet.ForOver(sets, overNumber);

            Assert.Null(result);
        }
    }
}
