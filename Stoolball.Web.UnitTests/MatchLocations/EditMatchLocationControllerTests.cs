using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class EditMatchLocationControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();

        private EditMatchLocationController CreateController()
        {
            return new EditMatchLocationController(
                Mock.Of<ILogger<EditMatchLocationController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchLocationDataSource.Object,
                _authorizationPolicy.Object,
                _apiKeyProvider.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_MatchLocationViewModel()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationRoute = "/locations/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Model_has_location_and_Google_Maps_API_key()
        {
            var location = new MatchLocation { MatchLocationRoute = "/locations/example" };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(location);
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("maps-key");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(location, model.MatchLocation);
            Assert.Equal("maps-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public async Task Model_has_authorization_page_title_and_breadcrumbs()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground", MatchLocationRoute = "/locations/example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchLocation, true } };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(location);
            _authorizationPolicy.Setup(x => x.IsAuthorized(location)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal("Edit " + location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^2].Name);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), model.Breadcrumbs[^1].Name);
            Assert.Equal(location.MatchLocationRoute, model.Breadcrumbs[^1].Url!.ToString());
        }
    }
}
