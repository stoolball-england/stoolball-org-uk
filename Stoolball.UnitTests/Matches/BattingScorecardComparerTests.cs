using System;
using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class BattingScorecardComparerTests
    {
        [Fact]
        public void Null_before_innings_throws_ArgumentNullException()
        {
            var comparer = new BattingScorecardComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.CompareScorecards(null, new List<PlayerInnings>()));
        }

        [Fact]
        public void Null_after_innings_throws_ArgumentNullException()
        {
            var comparer = new BattingScorecardComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.CompareScorecards(new List<PlayerInnings>(), null));
        }

        [Fact]
        public void Batting_position_zero_in_before_innings_throws_ArgumentException()
        {
            var comparer = new BattingScorecardComparer();
            var firstInnings = new PlayerInnings { BattingPosition = 0, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissalType = DismissalType.NotOut, RunsScored = 10, BallsFaced = 10 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<PlayerInnings> { firstInnings }, new List<PlayerInnings>()));
        }


        [Fact]
        public void Batting_position_zero_in_after_innings_throws_ArgumentException()
        {
            var comparer = new BattingScorecardComparer();
            var firstInnings = new PlayerInnings { BattingPosition = 0, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissalType = DismissalType.NotOut, RunsScored = 10, BallsFaced = 10 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<PlayerInnings>(), new List<PlayerInnings> { firstInnings }));
        }

        [Fact]
        public void Duplicate_batting_position_in_after_innings_throws_ArgumentException()
        {
            var comparer = new BattingScorecardComparer();
            var firstInnings = new PlayerInnings { BattingPosition = 1, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsDuplicate = new PlayerInnings { BattingPosition = 1, Batter = new PlayerIdentity { PlayerIdentityName = "Player two" }, DismissalType = DismissalType.NotOut, RunsScored = 2, BallsFaced = 12 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<PlayerInnings>(), new List<PlayerInnings> { firstInnings, firstInningsDuplicate }));
        }

        [Fact]
        public void Added_innings_is_identified_by_index()
        {
            var firstInnings = new PlayerInnings { BattingPosition = 1, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInnings = new PlayerInnings { BattingPosition = 2, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInnings }, new List<PlayerInnings> { firstInnings, secondInnings });

            Assert.Single(result.PlayerInningsAdded);
            Assert.Contains(secondInnings, result.PlayerInningsAdded);
        }

        [Fact]
        public void Changed_innings_is_identified_from_changed_BattingPosition_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }

        [Fact]
        public void Changed_innings_is_identified_from_changed_PlayerIdentityName_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = new PlayerIdentity { PlayerIdentityName = "Before" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = new PlayerIdentity { PlayerIdentityName = "Changed" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }

        [Fact]
        public void Changed_innings_is_identified_from_changed_DismissalType_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.Caught, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }

        [Fact]
        public void Changed_innings_is_identified_from_changed_DismissedByU002EPlayerIdentityName_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerTwo, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerTwo, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerOne, DismissedBy = new PlayerIdentity { PlayerIdentityName = "Before" }, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerOne, DismissedBy = new PlayerIdentity { PlayerIdentityName = "Changed" }, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }


        [Fact]
        public void Changed_innings_is_identified_from_changed_BowlerU002EPlayerIdentityName_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerTwo, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerTwo, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = new PlayerIdentity { PlayerIdentityName = "Before" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = new PlayerIdentity { PlayerIdentityName = "Changed" }, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }


        [Fact]
        public void Changed_innings_is_identified_from_changed_RunsScored_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 2, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }

        [Fact]
        public void Changed_innings_is_identified_from_changed_BallsFaced_for_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 12 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.PlayerInningsChanged);
            Assert.Contains((secondInningsBefore, secondInningsAfter), result.PlayerInningsChanged);
        }

        [Fact]
        public void Unchanged_innings_is_identified_by_all_fields()
        {
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissedBy = new PlayerIdentity { PlayerIdentityName = "Player three" }, Bowler = new PlayerIdentity { PlayerIdentityName = "Player four" }, DismissalType = DismissalType.Caught, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = new PlayerIdentity { PlayerIdentityName = "Player one" }, DismissedBy = new PlayerIdentity { PlayerIdentityName = "Player three" }, Bowler = new PlayerIdentity { PlayerIdentityName = "Player four" }, DismissalType = DismissalType.Caught, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = new PlayerIdentity { PlayerIdentityName = "Player two" }, DismissedBy = new PlayerIdentity { PlayerIdentityName = "Player three" }, Bowler = new PlayerIdentity { PlayerIdentityName = "Player four" }, DismissalType = DismissalType.Caught, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = new PlayerIdentity { PlayerIdentityName = "Player two" }, DismissedBy = new PlayerIdentity { PlayerIdentityName = "Player three" }, Bowler = new PlayerIdentity { PlayerIdentityName = "Player four" }, DismissalType = DismissalType.Caught, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Equal(2, result.PlayerInningsUnchanged.Count);
        }

        [Fact]
        public void Removed_innings_is_identified_by_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter });

            Assert.Single(result.PlayerInningsRemoved);
            Assert.Contains(secondInningsBefore, result.PlayerInningsRemoved);
        }

        [Fact]
        public void Added_batter_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerThree, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BattingPlayerIdentitiesAdded);
            Assert.Contains(playerThree.PlayerIdentityName, result.BattingPlayerIdentitiesAdded);
        }

        [Fact]
        public void Affected_batter_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 12 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BattingPlayerIdentitiesAffected);
            Assert.Contains(playerOne.PlayerIdentityName, result.BattingPlayerIdentitiesAffected);
        }

        [Fact]
        public void Removed_batter_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerThree, DismissalType = DismissalType.NotOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BattingPlayerIdentitiesRemoved);
            Assert.Contains(playerTwo.PlayerIdentityName, result.BattingPlayerIdentitiesRemoved);
        }


        [Fact]
        public void Added_fielder_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var playerFour = new PlayerIdentity { PlayerIdentityName = "Player four" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissedBy = playerFour, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BowlingPlayerIdentitiesAdded);
            Assert.Contains(playerFour.PlayerIdentityName, result.BowlingPlayerIdentitiesAdded);
        }

        [Fact]
        public void Affected_fielder_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 12 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BowlingPlayerIdentitiesAffected);
            Assert.Contains(playerThree.PlayerIdentityName, result.BowlingPlayerIdentitiesAffected);
        }

        [Fact]
        public void Removed_fielder_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var playerFour = new PlayerIdentity { PlayerIdentityName = "Player four" };
            var playerFive = new PlayerIdentity { PlayerIdentityName = "Player five" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, DismissedBy = playerThree, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissedBy = playerFour, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, DismissedBy = playerFive, DismissalType = DismissalType.RunOut, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BowlingPlayerIdentitiesRemoved);
            Assert.Contains(playerFour.PlayerIdentityName, result.BowlingPlayerIdentitiesRemoved);
        }


        [Fact]
        public void Added_bowler_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var playerFour = new PlayerIdentity { PlayerIdentityName = "Player four" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = playerFour, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BowlingPlayerIdentitiesAdded);
            Assert.Contains(playerFour.PlayerIdentityName, result.BowlingPlayerIdentitiesAdded);
        }

        [Fact]
        public void Affected_bowler_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 12 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BowlingPlayerIdentitiesAffected);
            Assert.Contains(playerThree.PlayerIdentityName, result.BowlingPlayerIdentitiesAffected);
        }

        [Fact]
        public void Removed_bowler_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var playerFour = new PlayerIdentity { PlayerIdentityName = "Player four" };
            var playerFive = new PlayerIdentity { PlayerIdentityName = "Player five" };
            var firstInningsBefore = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var firstInningsAfter = new PlayerInnings { BattingPosition = 1, Batter = playerOne, Bowler = playerThree, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var secondInningsBefore = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = playerFour, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var secondInningsAfter = new PlayerInnings { BattingPosition = 2, Batter = playerTwo, Bowler = playerFive, DismissalType = DismissalType.Bowled, RunsScored = 0, BallsFaced = 10 };
            var comparer = new BattingScorecardComparer();

            var result = comparer.CompareScorecards(new List<PlayerInnings> { firstInningsBefore, secondInningsBefore }, new List<PlayerInnings> { firstInningsAfter, secondInningsAfter });

            Assert.Single(result.BowlingPlayerIdentitiesRemoved);
            Assert.Contains(playerFour.PlayerIdentityName, result.BowlingPlayerIdentitiesRemoved);
        }
    }
}
