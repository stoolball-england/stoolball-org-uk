using System;
using System.Collections.Generic;
using System.Linq;
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
            var calculator = new BowlingFiguresCalculator();

            Assert.Throws<ArgumentNullException>(() => calculator.CalculateBowlingFigures(null));
        }

        [Fact]
        public void Bowlers_are_sorted_by_their_first_OverNumber()
        {
            var calculator = new BowlingFiguresCalculator();
            var firstBowler = "Bowler 1";
            var secondBowler = "Bowler 2";
            var thirdBowler = "Bowler 3";
            var innings = new MatchInnings
            {
                OversBowled = new List<Over> {
                    new Over { OverNumber = 2, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler } },
                    new Over { OverNumber = 3, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = thirdBowler } },
                    new Over { OverNumber = 1, PlayerIdentity = new PlayerIdentity { PlayerIdentityName = firstBowler } }
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
            var calculator = new BowlingFiguresCalculator();
            var bowler = "Bowler 1";
            var innings = new MatchInnings
            {
                PlayerInnings = new List<PlayerInnings> {
                    new PlayerInnings{
                        DismissalType = dismissalType,
                        Bowler = new PlayerIdentity { PlayerIdentityName = bowler }
                    }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            if (creditedToBowler)
            {
                Assert.Equal(1, result.Count);
                Assert.Equal(result[0].Bowler.PlayerIdentityName, bowler);
            }
            else
            {
                Assert.Equal(0, result.Count);
            }
        }

        [Fact]
        public void Wicket_is_not_included_if_bowler_is_null()
        {
            var calculator = new BowlingFiguresCalculator();
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

            Assert.Equal(0, result.Count);
        }

        [Fact]
        public void Overs_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator();
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
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8 }, // complete over counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8 }, // multiple complete overs counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = null }, // over with missing balls data not counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = thirdBowler }, BallsBowled = 8 }, // complete over counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = thirdBowler }, BallsBowled = 4 } // partial over counted
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).Overs); // from wickets only, no bowling
            Assert.Equal(2, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).Overs); // whole overs only
            Assert.Equal((decimal)1.4, result.Single(x => x.Bowler.PlayerIdentityName == thirdBowler).Overs); // with partial over
        }

        [Fact]
        /// <remarks>
        /// Clearly this is not an ideal result, but it represents the fact that all statistics on the site currently assume 8 ball overs.
        /// See https://github.com/stoolball-england/stoolball-org-uk/issues/457.
        /// </remarks>
        public void Overs_total_assumes_8_ball_overs()
        {
            var calculator = new BowlingFiguresCalculator();
            var bowler = "Bowler 1";
            var innings = new MatchInnings
            {
                OversBowled = new List<Over> {
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = bowler }, BallsBowled = 8 }, // complete over counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = bowler }, BallsBowled = 10 }, // longer complete over counted as more than one
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal((decimal)2.2, result.Single(x => x.Bowler.PlayerIdentityName == bowler).Overs);
        }

        [Fact]
        public void Maidens_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator();
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
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8, RunsConceded = 5 }, // over with runs not counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = null, RunsConceded = 5 }, // over with missing balls data not counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 8, RunsConceded = null }, // over with missing runs data not counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 10, RunsConceded = 0 }, // longer overs counted, zero runs counted
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, BallsBowled = 4, RunsConceded = 0 } // partial overs not counted
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).Maidens); // from wickets only, no bowling
            Assert.Equal(1, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).Maidens);
        }

        [Fact]
        public void RunsConceded_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator();
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
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, RunsConceded = 5 },
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, RunsConceded = null },
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler }, RunsConceded = 15 }
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Null(result.Single(x => x.Bowler.PlayerIdentityName == firstBowler).RunsConceded); // from wickets only, no bowling
            Assert.Equal(20, result.Single(x => x.Bowler.PlayerIdentityName == secondBowler).RunsConceded); // sum of multiple overs, including missing data
        }

        [Fact]
        public void Wickets_total_is_correct()
        {
            var calculator = new BowlingFiguresCalculator();
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
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = firstBowler } },
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = secondBowler } },
                    new Over { PlayerIdentity = new PlayerIdentity { PlayerIdentityName = thirdBowler } }
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
            var calculator = new BowlingFiguresCalculator();
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
                    new Over { PlayerIdentity = bowler1 },
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal(bowler1.PlayerIdentityId, result[0].Bowler.PlayerIdentityId);
            Assert.Equal(bowler2.PlayerIdentityId, result[1].Bowler.PlayerIdentityId);
        }


        [Fact]
        public void TeamId_should_be_preserved()
        {
            var calculator = new BowlingFiguresCalculator();
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
                    new Over { PlayerIdentity = bowler1 },
                }
            };

            var result = calculator.CalculateBowlingFigures(innings);

            Assert.Equal(teamId1.ToString(), result[0].Bowler.Team.TeamId.ToString());
            Assert.Equal(teamId2.ToString(), result[1].Bowler.Team.TeamId.ToString());
        }
    }
}
