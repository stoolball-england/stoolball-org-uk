using System.Collections.Generic;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class OversHelperTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 1)]
        [InlineData(6, 2)]
        [InlineData(8, 2)]
        [InlineData(9, 3)]
        [InlineData(10, 3)]
        public void OverSetForOver_gets_the_right_over(int overNumber, int overSetNumber)
        {
            var sets = new List<OverSet> {
                new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 },
                new OverSet { OverSetNumber = 2, Overs = 3, BallsPerOver = 10 },
                new OverSet { OverSetNumber = 3, Overs = 2, BallsPerOver = 12 }
            };

            var result = new OversHelper().OverSetForOver(sets, overNumber);

            Assert.Equal(overSetNumber, result.OverSetNumber);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(6)]
        public void OverSetForOver_returns_null_if_over_not_found(int overNumber)
        {
            var sets = new List<OverSet> { new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 } };

            var result = new OversHelper().OverSetForOver(sets, overNumber);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(0.3, 3)]
        [InlineData(1, 8)]
        [InlineData(3, 24)]
        [InlineData(4.5, 37)]
        public void OversToBallsBowled_is_correct_for_8_ball_overs(decimal overs, int expectedBallsBowled)
        {
            var result = new OversHelper().OversToBallsBowled(overs);

            Assert.Equal(expectedBallsBowled, result);
        }
    }
}
