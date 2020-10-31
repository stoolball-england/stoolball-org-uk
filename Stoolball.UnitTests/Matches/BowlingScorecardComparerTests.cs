using System;
using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Tests.Matches
{
    public class BowlingScorecardComparerTests
    {
        [Fact]
        public void Null_before_overs_throws_ArgumentNullException()
        {
            var comparer = new BowlingScorecardComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.CompareScorecards(null, new List<Over>()));
        }

        [Fact]
        public void Null_after_overs_throws_ArgumentNullException()
        {
            var comparer = new BowlingScorecardComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.CompareScorecards(new List<Over>(), null));
        }

        [Fact]
        public void Over_number_zero_in_before_overs_throws_ArgumentException()
        {
            var comparer = new BowlingScorecardComparer();
            var firstOver = new Over { OverNumber = 0, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over> { firstOver }, new List<Over>()));
        }


        [Fact]
        public void Over_number_zero_in_after_overs_throws_ArgumentException()
        {
            var comparer = new BowlingScorecardComparer();
            var firstOver = new Over { OverNumber = 0, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over>(), new List<Over> { firstOver }));
        }

        [Fact]
        public void Duplicate_over_number_in_before_overs_throws_ArgumentException()
        {
            var comparer = new BowlingScorecardComparer();
            var firstOver = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverDuplicate = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 10, NoBalls = 2, Wides = 2, RunsConceded = 12 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over> { firstOver, firstOverDuplicate }, new List<Over>()));
        }


        [Fact]
        public void Duplicate_over_number_in_after_overs_throws_ArgumentException()
        {
            var comparer = new BowlingScorecardComparer();
            var firstOver = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverDuplicate = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 10, NoBalls = 2, Wides = 2, RunsConceded = 12 };

            Assert.Throws<ArgumentException>(() => comparer.CompareScorecards(new List<Over>(), new List<Over> { firstOver, firstOverDuplicate }));
        }

        [Fact]
        public void Added_over_is_identified_by_over_number()
        {
            var firstOver = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOver = new Over { OverNumber = 2, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOver }, new List<Over> { firstOver, secondOver });

            Assert.Single(result.OversAdded);
            Assert.Contains(secondOver, result.OversAdded);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_PlayerIdentityName_for_over_number()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Before" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Changed" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_BallsBowled_for_over_number()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 9, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_NoBalls_for_over_number()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 3, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_Wides_for_over_number()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 2, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Changed_over_is_identified_from_changed_RunsConceded_for_over_number()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 12 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.OversChanged);
            Assert.Contains((secondOverBefore, secondOverAfter), result.OversChanged);
        }

        [Fact]
        public void Unchanged_over_is_identified_by_over_number_and_all_fields()
        {
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player one" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = "Player two" }, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Equal(2, result.OversUnchanged.Count);
        }

        [Fact]
        public void Removed_over_is_identified_by_over_number()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter });

            Assert.Single(result.OversRemoved);
            Assert.Contains(secondOverBefore, result.OversRemoved);
        }

        [Fact]
        public void Added_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerThree, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.PlayerIdentitiesAdded);
            Assert.Contains(playerThree.PlayerIdentityName, result.PlayerIdentitiesAdded);
        }

        [Fact]
        public void Affected_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 12 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.PlayerIdentitiesAffected);
            Assert.Contains(playerOne.PlayerIdentityName, result.PlayerIdentitiesAffected);
        }

        [Fact]
        public void Removed_PlayerIdentity_is_identified()
        {
            var playerOne = new PlayerIdentity { PlayerIdentityName = "Player one" };
            var playerTwo = new PlayerIdentity { PlayerIdentityName = "Player two" };
            var playerThree = new PlayerIdentity { PlayerIdentityName = "Player three" };
            var firstOverBefore = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var firstOverAfter = new Over { OverNumber = 1, PlayerIdentity = playerOne, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverBefore = new Over { OverNumber = 2, PlayerIdentity = playerTwo, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var secondOverAfter = new Over { OverNumber = 2, PlayerIdentity = playerThree, BallsBowled = 8, NoBalls = 0, Wides = 0, RunsConceded = 10 };
            var comparer = new BowlingScorecardComparer();

            var result = comparer.CompareScorecards(new List<Over> { firstOverBefore, secondOverBefore }, new List<Over> { firstOverAfter, secondOverAfter });

            Assert.Single(result.PlayerIdentitiesRemoved);
            Assert.Contains(playerTwo.PlayerIdentityName, result.PlayerIdentitiesRemoved);
        }
    }
}
