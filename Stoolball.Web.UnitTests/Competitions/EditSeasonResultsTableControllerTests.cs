using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class EditSeasonResultsTableControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private EditSeasonResultsTableController CreateController()
        {
            return new EditSeasonResultsTableController(
                Mock.Of<ILogger<EditSeasonResultsTableController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Season SetupSeason()
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                FromYear = 2026,
                UntilYear = 2026,
                Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/1234"
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(season);
            return season;
        }

        private async Task<SeasonViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_season_returns_SeasonViewModel_with_season()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(season, model.Season);
        }

        [Fact]
        public async Task Points_rules_are_read_for_the_season()
        {
            var season = SetupSeason();
            var rule = new PointsRule { MatchResultType = MatchResultType.HomeWin, HomePoints = 3 };
            _seasonDataSource.Setup(x => x.ReadPointsRules(season.SeasonId!.Value)).ReturnsAsync([rule]);

            var model = await ReadModel();

            Assert.Equal([rule], model.Season!.PointsRules);
        }

        [Fact]
        public async Task UrlReferrer_is_read_from_Referer_header()
        {
            SetupSeason();
            Request.Object.Headers["Referer"] = "https://www.stoolball.org.uk/competitions/example/1234/table";

            var model = await ReadModel();

            Assert.Equal(new Uri("https://www.stoolball.org.uk/competitions/example/1234/table"), model.UrlReferrer);
        }

        [Fact]
        public async Task UrlReferrer_is_null_without_Referer_header()
        {
            SetupSeason();

            var model = await ReadModel();

            Assert.Null(model.UrlReferrer);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var season = SetupSeason();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(season.Competition!)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal($"Edit results table for {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example competition", season.SeasonName()], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
        }
    }
}
