using System;
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
    public class EditCloseOfPlayControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();

        public EditCloseOfPlayControllerTests() : base()
        {
        }

        private EditCloseOfPlayController CreateController()
        {
            return new EditCloseOfPlayController(
                Mock.Of<ILogger<EditCloseOfPlayController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IMatchResultEvaluator>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/not-a-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match?>(null));

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
        public async Task Route_matching_match_played_in_the_past_returns_EditCloseOfPlayViewModel(MatchResultType? matchResultType)
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchResultType = matchResultType,
                MatchRoute = "/matches/example"
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditCloseOfPlayViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
