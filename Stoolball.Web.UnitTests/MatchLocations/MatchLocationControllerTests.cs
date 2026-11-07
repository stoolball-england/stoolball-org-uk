using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchLocationControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();

        private MatchLocationController CreateController()
        {
            return new MatchLocationController(
                Mock.Of<ILogger<MatchLocationController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchLocationDataSource.Object,
                _authorizationPolicy.Object,
                _apiKeyProvider.Object,
                _emailProtector.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<MatchLocation?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_MatchLocationViewModel()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(new MatchLocation());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Model_has_location_and_Google_Maps_API_key()
        {
            var location = new MatchLocation();
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(location);
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("maps-key");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(location, model.MatchLocation);
            Assert.Equal("maps-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public async Task Model_has_authorization_metadata_and_breadcrumb()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground", Town = "Exampleton" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchLocation, true } };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(location);
            _authorizationPolicy.Setup(x => x.IsAuthorized(location)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.Equal(location.Description(), model.Metadata.Description);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^1].Name);
        }

        [Fact]
        public async Task Email_addresses_in_notes_are_protected_for_signed_out_user()
        {
            var location = new MatchLocation { MatchLocationNotes = "<p>test@example.org</p>" };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(location);
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>test@example.org</p>", false)).Returns("<p>protected</p>");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal("<p>protected</p>", model.MatchLocation!.MatchLocationNotes);
        }
    }
}
