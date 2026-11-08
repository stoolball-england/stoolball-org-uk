using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class SeasonActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private SeasonActionsController CreateController()
        {
            return new SeasonActionsController(
                Mock.Of<ILogger<SeasonActionsController>>(),
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
                FromYear = 2026,
                UntilYear = 2026,
                Competition = new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/1234"
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(season);
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
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season?>(null));

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
        public async Task Model_has_authorization_details_for_the_competition()
        {
            var season = SetupSeason();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            var groupNames = new List<string> { "Example group" };
            var memberNames = new List<string> { "Jo Bloggs" };
            _authorizationPolicy.Setup(x => x.IsAuthorized(season.Competition!)).ReturnsAsync(authorization);
            _authorizationPolicy.Setup(x => x.AuthorizedGroupNames(season.Competition!)).Returns(groupNames);
            _authorizationPolicy.Setup(x => x.AuthorizedMemberNames(season.Competition!)).ReturnsAsync(memberNames);

            var model = await ReadModel();

            Assert.Equal("Example", model.Authorization.AuthorizationFor);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(groupNames, model.Authorization.AuthorizedGroupNames);
            Assert.Equal(memberNames, model.Authorization.AuthorizedMemberNames);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal($"Edit {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example", season.SeasonName()], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
        }
    }
}
