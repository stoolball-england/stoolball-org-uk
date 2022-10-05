using System;
using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class BowlingScorecardComparerTests
    {
        private BowlingScorecardComparer CreateComparer()
        {
            return new BowlingScorecardComparer();
        }

        [Fact]
        public void Null_before_overs_throws_ArgumentNullException()
        {
            var comparer = CreateComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.CompareScorecards(null, new List<Over>()));
        }

        [Fact]
        public void Null_after_overs_throws_ArgumentNullException()
        {
            var comparer = CreateComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.CompareScorecards(new List<Over>(), null));
        }

        [Fact]
        public void Over_number_zero_in_before_overs_throws_ArgumentException()
        {
            var comparer = CreateComparer();
            var firstOver = new Over { OverNumber = 0, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over> { firstOver }, new List<Over>()));
        }


        [Fact]
        public void Over_number_zero_in_after_overs_throws_ArgumentException()
        {
            var comparer = CreateComparer();
            var firstOver = new Over { OverNumber = 0, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over>(), new List<Over> { firstOver }));
        }

        [Fact]
        public void Duplicate_over_number_in_after_overs_throws_ArgumentException()
        {
            var comparer = CreateComparer();
            var firstOver = new Over { OverNumber = 1, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverDuplicate = new Over { OverNumber = 1, Bowler = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 10, NoBalls = 2, Wides = 2, RunsConceded = 12 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over>(), new List<Over> { firstOver, firstOverDuplicate }));
        }

        [Fact]
        public void Added_over_is_identified_by_over_index()
        {
            var firstOver = new Over { OverNumber = 1, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOver = new Over { OverNumber = 2, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOver }, new List<Over> { firstOver, secondOver });

            Assert.Single(result.OversAdded);
            Assert.Contains(secondOver, result.OversAdded);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_OverNumber_for_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 1, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_PlayerIdentityName_for_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = new PlayerIdentity { PlayerIdentityName = "Before" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = new PlayerIdentity { PlayerIdentityName = "Changed" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_BallsBowled_for_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 9, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_NoBalls_for_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 3, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_Wides_for_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 2, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_RunsConceded_for_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 12 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Unchanged_over_is_identified_by_over_number_and_all_fields()
        {
            var firstOverBefore = new Over { OverNumber = 1, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Equal(2, result.OversUnchanged.Count);
        }

        [Fact]
        public void Removed_over_is_identified_by_over_index()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter });

            Assert.Single(result.OversRemoved);
            Assert.Contains(secondOverBefore, result.OversRemoved);
        }

        [Fact]
        public void Added_PlayerIdentity_is_identified_and_added_to_affected_identities()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerThree, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.PlayerIdentitiesAdded);
            Assert.Contains(playerThree.PlayerIdentityName, result.PlayerIdentitiesAdded);

            Assert.Equal(2, result.PlayerIdentitiesAffected.Count);
            Assert.Contains(playerThree.PlayerIdentityName, result.PlayerIdentitiesAffected);
        }

        [Fact]
        public void Changed_PlayerIdentity_is_identified_and_added_to_affected_identities()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 12 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.PlayerIdentitiesAffected);
            Assert.Contains(playerOne.PlayerIdentityName, result.PlayerIdentitiesAffected);
        }

        [Fact]
        public void Removed_PlayerIdentity_is_identified_and_added_to_affected_identities()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstOverBefore = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, Bowler = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, Bowler = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, Bowler = playerThree, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = CreateComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.PlayerIdentitiesRemoved);
            Assert.Contains(playerTwo.PlayerIdentityName, result.PlayerIdentitiesRemoved);

            Assert.Equal(2, result.PlayerIdentitiesAffected.Count);
            Assert.Contains(playerTwo.PlayerIdentityName, result.PlayerIdentitiesAffected);
        }
    }
}
