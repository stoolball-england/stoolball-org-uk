using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class BowlingFiguresCalculatorTests
    {
        [Fact]
        public void Throws_ArgumentNullException_when_MatchInnings_is_null()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            Assert.Throws<ArgumentNullException>(() => calculator.CalculateBowlingFigures(null));
        }

        [Fact]
        public void Bowlers_are_sorted_by_their_first_OverNumber()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var firstBowler = "Bowler 1";
            var secondBowler = "Bowler 2";
            var thirdBowler = "Bowler 3";
            var innings = new MatchInnings
            {
                OversBowled = new List<Over> {
                    new Over { OverNumber = 2, Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler } },
                    new Over { OverNumber = 3, Bowler = new PlayerIdentity { PlayerIdentityName = thirdBowler } },
                    new Over { OverNumber = 1, Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler } }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal(firstBowler, result[0].Bowler.PlayerIdentityName);
            Assert.Equal(secondBowler, result[1].Bowler.PlayerIdentityName);
            Assert.Equal(thirdBowler, result[2].Bowler.PlayerIdentityName);
        }

        [Theory]
        [InlineData(DismissalType.BodyBeforeWicket, true)]
        [InlineData(DismissalType.Bowled, true)]
        [InlineData(DismissalType.Caught, true)]
        [InlineData(DismissalType.CaughtAndBowled, true)]
        [InlineData(DismissalType.DidNotBat, false)]
        [InlineData(DismissalType.HitTheBallTwice, true)]
        [InlineData(DismissalType.NotOut, false)]
        [InlineData(DismissalType.Retired, false)]
        [InlineData(DismissalType.RetiredHurt, false)]
        [InlineData(DismissalType.RunOut, false)]
        [InlineData(DismissalType.TimedOut, false)]
        [InlineData(null, false)]
        public void Wicket_takers_with_no_overs_are_included_for_valid_dismissals(DismissalType? dismissalType, bool creditedToBowler)
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var bowler1 = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "Bowler 1" };
            var bowler2 = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "Bowler 2" };

            var innings = new MatchInnings
            {
                // Test for multiple bowlers taking wickets, and multiple wickets per bowler
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = dismissalType,
                        Bowler = bowler1
                    },
                    new PlayerInnings{
                        DismissalType = dismissalType,
                        Bowler = bowler1
                    },
                    new PlayerInnings{
                        DismissalType = dismissalType,
                        Bowler = bowler2
                    }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            if (creditedToBowler)
            {
                Assert.Equal(2, result.Count);
                Assert.Equal(result[0].Bowler.PlayerIdentityId, bowler1.PlayerIdentityId);
                Assert.Equal(result[1].Bowler.PlayerIdentityId, bowler2.PlayerIdentityId);
            }
            else
            {
                Assert.Empty(result);
            }
        }

        [Fact]
        public void Wicket_is_not_included_if_bowler_is_null()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled, // DismissalType which should be credited to the bowler
                        Bowler = null
                    }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Empty(result);
        }

        [Fact]
        public void Overs_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var firstBowler = "Bowler 1";
            var secondBowler = "Bowler 2";
            var thirdBowler = "Bowler 3";
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler }
                    }
                },
                OversBowled = new List<Over> {
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8 }, // complete over counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8 }, // multiple complete overs counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = null }, // over with missing balls data counted as a complete over
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = thirdBowler }, BallsBowled = 8 }, // complete over counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = thirdBowler }, BallsBowled = 4 } // partial over counted
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).Overs); // from wickets only, no bowling
            Assert.Equal(3, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).Overs); // whole overs only
            Assert.Equal((decimal)1.4, result.Single(x => x.Bowler.PlayerIdentityName == thirdBowler).Overs); // with partial over
        }

        [Fact]
        /// <remarks>
        /// Clearly this is not an ideal result, but it represents the fact that all statistics on the site currently assume 8 ball overs.
        /// See https://github.com/stoolball-england/stoolball-org-uk/issues/457.
        /// </remarks>
        public void Overs_total_assumes_8_ball_overs()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var bowler = "Bowler 1";
            var innings = new MatchInnings
            {
                OversBowled = new List<Over> {
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = bowler }, BallsBowled = 8 }, // complete over counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = bowler }, BallsBowled = 10 }, // longer complete over counted as more than one
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal((decimal)2.2, result.Single(x => x.Bowler.PlayerIdentityName == bowler).Overs);
        }

        [Fact]
        public void Maidens_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var firstBowler = "Bowler 1";
            var secondBowler = "Bowler 2";
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler }
                    }
                },
                OversBowled = new List<Over> {
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8, RunsConceded = 5 }, // over with runs not counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = null, RunsConceded = 5 }, // over with missing balls data not counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8, RunsConceded = null }, // over with missing runs data not counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 10, RunsConceded = 0 }, // longer overs counted, zero runs counted
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 4, RunsConceded = 0 } // partial overs not counted
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).Maidens); // from wickets only, no bowling
            Assert.Equal(1, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).Maidens);
        }

        [Fact]
        public void RunsConceded_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var firstBowler = "Bowler 1";
            var secondBowler = "Bowler 2";
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler }
                    }
                },
                OversBowled = new List<Over> {
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, RunsConceded = 5 },
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, RunsConceded = null },
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }, RunsConceded = 15 }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).RunsConceded); // from wickets only, no bowling
            Assert.Equal(20, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).RunsConceded); // sum of multiple overs, including missing data
        }

        [Fact]
        public void Overs_without_RunsConceded_set_RunsConceded_to_be_null()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var firstBowler = "Bowler 1";
            var innings = new MatchInnings
            {
                OversBowled = new List<Over> {
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler }, RunsConceded = null },
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler }, RunsConceded = null },
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).RunsConceded);
        }

        [Fact]
        public void Wickets_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var firstBowler = "Bowler 1";
            var secondBowler = "Bowler 2";
            var thirdBowler = "Bowler 3";
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler }
                    },
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = new PlayerIdentity { PlayerIdentityName = thirdBowler }
                    },
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = new PlayerIdentity { PlayerIdentityName = thirdBowler }
                    }
                },
                OversBowled = new List<Over> {
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = firstBowler } },
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = secondBowler } },
                    new Over { Bowler = new PlayerIdentity { PlayerIdentityName = thirdBowler } }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal(0, result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).Wickets);
            Assert.Equal(1, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).Wickets);
            Assert.Equal(2, result.Single(x => x.Bowler.PlayerIdentityName == thirdBowler).Wickets);
        }

        [Fact]
        public void PlayerIdentityId_should_be_preserved()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var bowler1 = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "Bowler 1" };
            var bowler2 = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "Bowler 2" };
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = bowler2
                    }
                },
                OversBowled = new List<Over> {
                    new Over { Bowler = bowler1 },
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal(bowler1.PlayerIdentityId, result[0].Bowler.PlayerIdentityId);
            Assert.Equal(bowler2.PlayerIdentityId, result[1].Bowler.PlayerIdentityId);
        }


        [Fact]
        public void TeamId_should_be_preserved()
        {
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());
            var teamId1 = Guid.NewGuid();
            var teamId2 = Guid.NewGuid();
            var bowler1 = new PlayerIdentity { PlayerIdentityName = "Bowler 1", Team = new Team { TeamId = teamId1 } };
            var bowler2 = new PlayerIdentity { PlayerIdentityName = "Bowler 2", Team = new Team { TeamId = teamId2 } };
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = DismissalType.Bowled,
                        Bowler = bowler2
                    }
                },
                OversBowled = new List<Over> {
                    new Over { Bowler = bowler1 },
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal(teamId1.ToString(), result[0].Bowler.Team.TeamId.ToString());
            Assert.Equal(teamId2.ToString(), result[1].Bowler.Team.TeamId.ToString());
        }

        [Fact]
        public void BowlingAverage_is_null_when_wickets_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                RunsConceded = 10,
                Wickets = 0
            };
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            var result = calculator.BowlingAverage(bowlingFigures);

            Assert.Null(result);
        }

        [Fact]
        public void BowlingAverage_is_correct_for_integers()
        {
            var bowlingFigures = new BowlingFigures
            {
                RunsConceded = 30,
                Wickets = 2
            };
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            var result = calculator.BowlingAverage(bowlingFigures);

            Assert.Equal(15, result);
        }

        [Fact]
        public void BowlingAverage_is_correct_to_two_decimal_places_when_returning_fractions()
        {
            var bowlingFigures = new BowlingFigures
            {
                RunsConceded = 32,
                Wickets = 3
            };
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            var result = calculator.BowlingAverage(bowlingFigures);

            Assert.Equal(10.67M, result);
        }

        [Fact]
        public void BowlingEconomy_is_null_when_overs_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 0,
                RunsConceded = 10
            };
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            var result = calculator.BowlingEconomy(bowlingFigures);

            Assert.Null(result);
        }

        [Fact]
        public void BowlingEconomy_is_correct_for_integers()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 3,
                RunsConceded = 30
            };
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(bowlingFigures.Overs.Value)).Returns(24);
            var calculator = new BowlingFiguresCalculator(oversHelper.Object);

            var result = calculator.BowlingEconomy(bowlingFigures);

            Assert.Equal(10, result);
        }

        [Fact]
        public void BowlingEconomy_is_correct_to_two_decimal_places_when_returning_fractions()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 4,
                RunsConceded = 41
            };
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(bowlingFigures.Overs.Value)).Returns(32);
            var calculator = new BowlingFiguresCalculator(oversHelper.Object);

            var result = calculator.BowlingEconomy(bowlingFigures);

            Assert.Equal(10.25M, result);
        }

        [Fact]
        public void BowlingEconomy_is_correct_for_incomplete_overs()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 3.4M,
                RunsConceded = 28
            };
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(bowlingFigures.Overs.Value)).Returns(28);
            var calculator = new BowlingFiguresCalculator(oversHelper.Object);

            var result = calculator.BowlingEconomy(bowlingFigures);

            Assert.Equal(8, result);
        }

        [Fact]
        public void BowlingStrikeRate_is_null_when_overs_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 0,
                Wickets = 5
            };
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            var result = calculator.BowlingStrikeRate(bowlingFigures);

            Assert.Null(result);
        }

        [Fact]
        public void BowlingStrikeRate_is_null_when_wickets_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 5,
                Wickets = 0
            };
            var calculator = new BowlingFiguresCalculator(Mock.Of<IOversHelper>());

            var result = calculator.BowlingStrikeRate(bowlingFigures);

            Assert.Null(result);
        }

        [Fact]
        public void BowlingStrikeRate_is_correct_for_integers()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 2,
                Wickets = 2
            };
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(bowlingFigures.Overs.Value)).Returns(16);
            var calculator = new BowlingFiguresCalculator(oversHelper.Object);

            var result = calculator.BowlingStrikeRate(bowlingFigures);

            Assert.Equal(8, result);
        }


        [Fact]
        public void BowlingStrikeRate_is_correct_for_incomplete_overs()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 2.4M,
                Wickets = 2
            };
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(bowlingFigures.Overs.Value)).Returns(20);
            var calculator = new BowlingFiguresCalculator(oversHelper.Object);

            var result = calculator.BowlingStrikeRate(bowlingFigures);

            Assert.Equal(10, result);
        }

        [Fact]
        public void BowlingStrikeRate_is_correct_to_two_decimal_places_when_returning_fractions()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 10,
                Wickets = 9
            };
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(bowlingFigures.Overs.Value)).Returns(80);
            var calculator = new BowlingFiguresCalculator(oversHelper.Object);

            var result = calculator.BowlingStrikeRate(bowlingFigures);

            Assert.Equal(8.89M, result);
        }
    }
}
