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
    public class DeleteSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private DeleteSeasonController CreateController()
        {
            return new DeleteSeasonController(
                Mock.Of<ILogger<DeleteSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _matchListingDataSource.Object,
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
                Competition = new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/1234"
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(season);
            return season;
        }

        private async Task<DeleteSeasonViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<DeleteSeasonViewModel>(((ViewResult)result).Model);
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
        public async Task Route_matching_season_returns_DeleteSeasonViewModel_with_season()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(season, model.Season);
        }

        [Fact]
        public async Task Total_matches_includes_tournament_matches_for_the_season()
        {
            var season = SetupSeason();
            _matchListingDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(5);

            var model = await ReadModel();

            Assert.Equal(5, model.TotalMatches);
            _matchListingDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.SeasonIds.SequenceEqual(new[] { season.SeasonId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
        }

        [Fact]
        public async Task Confirmation_requires_the_full_season_name()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(season.SeasonFullName(), model.ConfirmDeleteRequest.RequiredText);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var season = SetupSeason();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteCompetition, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(season.Competition!)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal($"Delete {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example", season.SeasonName()], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
        }
    }
}
