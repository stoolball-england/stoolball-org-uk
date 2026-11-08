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
using Stoolball.Email;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class SeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();

        private SeasonController CreateController()
        {
            return new SeasonController(
                Mock.Of<ILogger<SeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _authorizationPolicy.Object,
                _emailProtector.Object)
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
                Introduction = "<p>season@example.org</p>",
                Competition = new Competition
                {
                    CompetitionName = "Example",
                    CompetitionRoute = "/competitions/example",
                    Introduction = "<p>intro@example.org</p>",
                    PublicContactDetails = "<p>contact@example.org</p>"
                },
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
        public async Task Model_has_authorization()
        {
            var season = SetupSeason();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(season.Competition!)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_metadata_and_breadcrumbs()
        {
            var season = SetupSeason();
            var expectedDescription = season.Description();

            var model = await ReadModel();

            Assert.Equal(season.SeasonFullNameAndPlayerType(), model.Metadata.PageTitle);
            Assert.Equal(expectedDescription, model.Metadata.Description);
            Assert.Equal([Constants.Pages.Competitions, "Example"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
        }

        [Fact]
        public async Task Email_addresses_are_protected_for_signed_out_user()
        {
            SetupSeason();
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>intro@example.org</p>", false)).Returns("<p>protected intro</p>");
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>contact@example.org</p>", false)).Returns("<p>protected contact</p>");
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>season@example.org</p>", false)).Returns("<p>protected season</p>");

            var model = await ReadModel();

            Assert.Equal("<p>protected intro</p>", model.Season!.Competition!.Introduction);
            Assert.Equal("<p>protected contact</p>", model.Season.Competition.PublicContactDetails);
            Assert.Equal("<p>protected season</p>", model.Season.Introduction);
        }
    }
}
