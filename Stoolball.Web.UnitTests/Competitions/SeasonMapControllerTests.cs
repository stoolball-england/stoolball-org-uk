using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Configuration;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class SeasonMapControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();

        private SeasonMapController CreateController()
        {
            return new SeasonMapController(
                Mock.Of<ILogger<SeasonMapController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _apiKeyProvider.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Season SetupSeason(string competitionName = "Example")
        {
            var season = new Season
            {
                FromYear = 2026,
                UntilYear = 2026,
                Competition = new Competition { CompetitionName = competitionName, CompetitionRoute = "/competitions/example" },
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
        public async Task Model_has_Google_Maps_API_key()
        {
            SetupSeason();
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("example-key");

            var model = await ReadModel();

            Assert.Equal("example-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public async Task Page_title_includes_the_before_competition_name()
        {
            var season = SetupSeason("Example league");

            var model = await ReadModel();

            Assert.Equal($"Map of teams in the {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal(season.Description(), model.Metadata.Description);
        }

        [Fact]
        public async Task Page_title_does_not_repeat_the_when_competition_name_starts_with_it()
        {
            var season = SetupSeason("The Example league");

            var model = await ReadModel();

            Assert.Equal($"Map of teams in {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Model_has_breadcrumbs()
        {
            SetupSeason();

            var model = await ReadModel();

            Assert.Equal([Constants.Pages.Competitions, "Example"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
        }
    }
}
