using System;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Testing;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches.Models
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

        [Theory]
        [InlineData(DismissalType.BodyBeforeWicket)]
        [InlineData(DismissalType.Bowled)]
        [InlineData(DismissalType.Caught)]
        [InlineData(DismissalType.CaughtAndBowled)]
        [InlineData(DismissalType.HitTheBallTwice)]
        [InlineData(DismissalType.NotOut)]
        [InlineData(DismissalType.Retired)]
        [InlineData(DismissalType.RetiredHurt)]
        [InlineData(DismissalType.RunOut)]
        [InlineData(DismissalType.TimedOut)]
        public void Dismissal_type_requires_batter(DismissalType dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                DismissalType = dismissalType
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.Batter)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("PLEASE NAME THE BATTER"));
        }

        [Theory]
        [InlineData(DismissalType.DidNotBat)]
        [InlineData(null)]
        public void Dismissal_type_does_not_require_batter(DismissalType? dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                DismissalType = dismissalType
            };

            Assert.DoesNotContain(ValidateModel(innings),
                            v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.Batter)) &&
                                 v.ErrorMessage.ToUpperInvariant().Contains("PLEASE NAME THE BATTER"));
        }

        [Fact]
        public void DismissedBy_requires_batter()
        {
            var innings = new PlayerInningsViewModel
            {
                DismissedBy = "John Smith"
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.Batter)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("PLEASE NAME THE BATTER"));
        }

        [Fact]
        public void BowledBy_requires_batter()
        {
            var innings = new PlayerInningsViewModel
            {
                Bowler = "John Smith"
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.Batter)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("PLEASE NAME THE BATTER"));
        }

        [Fact]
        public void Runs_scored_requires_batter()
        {
            var innings = new PlayerInningsViewModel
            {
                RunsScored = 10
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.Batter)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("PLEASE NAME THE BATTER"));
        }

        [Fact]
        public void Balls_faced_requires_batter()
        {
            var innings = new PlayerInningsViewModel
            {
                BallsFaced = 10
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.Batter)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("PLEASE NAME THE BATTER"));
        }

        [Theory]
        [InlineData(DismissalType.BodyBeforeWicket)]
        [InlineData(DismissalType.Bowled)]
        [InlineData(DismissalType.CaughtAndBowled)]
        [InlineData(DismissalType.DidNotBat)]
        [InlineData(DismissalType.HitTheBallTwice)]
        [InlineData(DismissalType.NotOut)]
        [InlineData(DismissalType.Retired)]
        [InlineData(DismissalType.RetiredHurt)]
        [InlineData(DismissalType.TimedOut)]
        public void DismissedBy_invalid_for_some_dismissal_types(DismissalType dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                Batter = "Jo Bloggs",
                DismissalType = dismissalType,
                DismissedBy = "John Smith"
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.DismissedBy)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("DISMISSED"));
        }

        [Theory]
        [InlineData(DismissalType.Caught)]
        [InlineData(DismissalType.RunOut)]
        [InlineData(null)]
        public void DismissedBy_valid_for_some_dismissal_types(DismissalType? dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                Batter = "Jo Bloggs",
                DismissalType = dismissalType,
                DismissedBy = "John Smith"
            };

            Assert.DoesNotContain(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.DismissalType)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("DISMISSED"));
        }

        [Theory]
        [InlineData(DismissalType.DidNotBat)]
        [InlineData(DismissalType.NotOut)]
        [InlineData(DismissalType.Retired)]
        [InlineData(DismissalType.RetiredHurt)]
        [InlineData(DismissalType.RunOut)]
        [InlineData(DismissalType.TimedOut)]
        public void BowledBy_invalid_for_some_dismissal_types(DismissalType dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                Batter = "Jo Bloggs",
                DismissalType = dismissalType,
                Bowler = "John Smith"
            };

            Assert.Contains(ValidateModel(innings),
                v => (v.MemberNames.Contains(nameof(PlayerInningsViewModel.DismissalType)) &&
                     (v.ErrorMessage.ToUpperInvariant().Contains("JO BLOGGS DID NOT BAT") ||
                      v.ErrorMessage.ToUpperInvariant().Contains("JO BLOGGS WAS NOT OUT")))
                     ||
                     (v.MemberNames.Contains(nameof(PlayerInningsViewModel.Bowler)) &&
                      v.ErrorMessage.ToUpperInvariant().Contains("JO BLOGGS WAS RUN-OUT"))
                );
        }

        [Theory]
        [InlineData(DismissalType.BodyBeforeWicket)]
        [InlineData(DismissalType.Bowled)]
        [InlineData(DismissalType.Caught)]
        [InlineData(DismissalType.CaughtAndBowled)]
        [InlineData(DismissalType.HitTheBallTwice)]
        public void BowledBy_valid_for_some_dismissal_types(DismissalType dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                Batter = "Jo Bloggs",
                DismissalType = dismissalType,
                Bowler = "John Smith"
            };

            Assert.DoesNotContain(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.DismissalType)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("DISMISSED"));
        }

        [Theory]
        [InlineData(DismissalType.DidNotBat)]
        [InlineData(DismissalType.TimedOut)]
        public void Runs_scored_invalid_when_batter_did_not_bat(DismissalType dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                DismissalType = dismissalType,
                RunsScored = 10
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.DismissalType)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("YOU ADDED BATTING DETAILS"));
        }

        [Theory]
        [InlineData(DismissalType.DidNotBat)]
        [InlineData(DismissalType.TimedOut)]
        public void Balls_faced_invalid_when_batter_did_not_bat(DismissalType dismissalType)
        {
            var innings = new PlayerInningsViewModel
            {
                DismissalType = dismissalType,
                BallsFaced = 10
            };

            Assert.Contains(ValidateModel(innings),
                v => v.MemberNames.Contains(nameof(PlayerInningsViewModel.DismissalType)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("YOU ADDED BATTING DETAILS"));
        }
    }
}
