using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditBattingScorecardControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchInningsUrlParser> _urlParser = new();
        private readonly Mock<IPlayerInningsScaffolder> _playerInningsScaffolder = new();

        public EditBattingScorecardControllerTests()
        {
            Setup();
        }

        private EditBattingScorecardController CreateController()
        {
            return new EditBattingScorecardController(
                Mock.Of<ILogger<EditBattingScorecardController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                _urlParser.Object,
                _playerInningsScaffolder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/not-a-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

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
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season() });

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWinByForfeit)]
        [InlineData(MatchResultType.AwayWinByForfeit)]
        [InlineData(MatchResultType.Postponed)]
        [InlineData(MatchResultType.Cancelled)]
        public async Task Route_matching_match_with_not_played_result_returns_404(MatchResultType matchResultType)
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                MatchResultType = matchResultType,
                Season = new Season()
            });

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_not_matching_innings_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                MatchResultType = MatchResultType.HomeWin,
                Season = new Season()
            });

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns<int?>(null);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin)]
        [InlineData(MatchResultType.AwayWin)]
        [InlineData(MatchResultType.Tie)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndPostponed)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndCancelled)]
        [InlineData(null)]
        public async Task Route_matching_match_played_in_the_past_returns_EditScorecardViewModel(MatchResultType? matchResultType)
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchResultType = matchResultType,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1 }
                },
                MatchRoute = "/matches/example"
            });

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditScorecardViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task ModelU002EMatchU002EPlayersPerTeam_defaults_to_11()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchResultType = MatchResultType.HomeWin,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1 }
                },
                MatchRoute = "/matches/example"
            });

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(11, ((EditScorecardViewModel)((ViewResult)result).Model).Match.PlayersPerTeam);
            }
        }

        [Fact(Skip = "Tournament match editing disabled until it's tested")]
        public async Task ModelU002EMatchU002EPlayersPerTeam_defaults_to_8_for_tournaments()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchResultType = MatchResultType.HomeWin,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1 }
                },
                MatchRoute = "/matches/example",
                Tournament = new Tournament { TournamentName = "Example tournament" }
            });

            _urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(8, ((EditScorecardViewModel)((ViewResult)result).Model).Match.PlayersPerTeam);
            }
        }
    }
}
