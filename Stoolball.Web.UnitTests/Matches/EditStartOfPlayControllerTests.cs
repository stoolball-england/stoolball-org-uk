using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Awards;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditStartOfPlayControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();

        public EditStartOfPlayControllerTests() : base()
        {
        }

        private EditStartOfPlayController CreateController()
        {
            return new EditStartOfPlayController(
                Mock.Of<ILogger<EditStartOfPlayController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                _seasonDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                _editMatchHelper.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/not-a-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).Returns(Task.FromResult<Stoolball.Matches.Match?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_match_in_the_future_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season() });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_in_the_past_returns_EditStartOfPlayViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchRoute = Request.Object.Path
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditStartOfPlayViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Match_with_toss_result_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                Teams = new List<TeamInMatch> {
                    new TeamInMatch
                    {
                        MatchTeamId = Guid.NewGuid(),
                        Team = new Team(),
                        TeamRole = TeamRole.Home,
                        WonToss = true
                    },
                    new TeamInMatch
                    {
                        MatchTeamId = Guid.NewGuid(),
                        Team = new Team(),
                        TeamRole = TeamRole.Away,
                        WonToss = false
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_batted_first_result_went_ahead()
        {
            var team1 = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = new Team(),
                TeamRole = TeamRole.Home,
                BattedFirst = true
            };
            var team2 = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = new Team(),
                TeamRole = TeamRole.Away,
                BattedFirst = false
            };
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                InningsOrderIsKnown = true,
                Teams = new List<TeamInMatch> {
                    team1,
                    team2
                },
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings { BattingTeam = team1 },
                    new MatchInnings { BattingTeam = team2 }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_batting_data_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        PlayerInnings = new List<PlayerInnings>
                        {
                            new PlayerInnings{ Batter = new PlayerIdentity{ PlayerIdentityName = "Player one" }, DismissalType = DismissalType.NotOut, RunsScored = 50 }
                        }
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_innings_total_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        Runs = 150
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_innings_wickets_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        Wickets = 5
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_innings_no_balls_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        NoBalls = 10
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_innings_wides_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        Wides = 10
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_innings_byes_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        Byes = 10
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_innings_bonus_runs_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        BonusOrPenaltyRuns = 10
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_overs_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings
                    {
                        OversBowled = new List<Over>
                        {
                            new Over{ Bowler = new PlayerIdentity{ PlayerIdentityName = "Player one" }, RunsConceded = 10 }
                        }
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_awards_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                Awards = new List<MatchAward>
                {
                    new MatchAward{ Award = new Award{ AwardName = "Player of the match" }, PlayerIdentity = new PlayerIdentity{ PlayerIdentityName = "Player one" } }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin)]
        [InlineData(MatchResultType.AwayWin)]
        [InlineData(MatchResultType.Tie)]
        public async Task Match_with_played_result_went_ahead(MatchResultType matchResultType)
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchResultType = matchResultType
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWinByForfeit)]
        [InlineData(MatchResultType.AwayWinByForfeit)]
        [InlineData(MatchResultType.Postponed)]
        [InlineData(MatchResultType.Cancelled)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndCancelled)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndPostponed)]
        public async Task Match_with_not_played_result_did_not_go_ahead(MatchResultType matchResultType)
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchResultType = matchResultType
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.False(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }

        [Fact]
        public async Task Match_with_no_data_has_null_went_ahead()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(Request.Object.Path)).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                MatchRoute = Request.Object.Path,
                MatchInnings = new List<MatchInnings>()
                {
                    new MatchInnings(),
                    new MatchInnings()
                },
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch
                    {
                        TeamRole = TeamRole.Home,
                    },
                    new TeamInMatch
                    {
                        TeamRole = TeamRole.Away
                    }
                }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Null(((EditStartOfPlayViewModel)((ViewResult)result).Model).MatchWentAhead);
            }
        }
    }
}
