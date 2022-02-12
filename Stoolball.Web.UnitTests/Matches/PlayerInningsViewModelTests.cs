using System;
using System.Linq;
using Stoolball.Testing;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class PlayerInningsViewModelTests : ValidationBaseTest
    {

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-10)]
        public void Valid_RunsScored_passes_validation(int? runs)
        {
            var innings = new PlayerInningsViewModel
            {
                RunsScored = runs
            };

            Assert.DoesNotContain(ValidateModel(innings),
                            v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.RunsScored)) &&
                                 v.ErrorMessage.ToUpperInvariant().Contains("RUNS"));
        }

        [Fact]
        public void Negative_BallsFaced_fails_validation()
        {
            var innings = new PlayerInningsViewModel
            {
                BallsFaced = -1
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.BallsFaced)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("BALLS FACED"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public void Valid_BallsFaced_passes_validation(int? ballsFaced)
        {
            var innings = new PlayerInningsViewModel
            {
                BallsFaced = ballsFaced
            };

            Assert.DoesNotContain(ValidateModel(innings),
                            v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.RunsScored)) &&
                                 v.ErrorMessage.ToUpperInvariant().Contains("BALLS FACED"));
        }
    }
}
