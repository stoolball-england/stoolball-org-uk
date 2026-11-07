using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class CreateMatchLocationControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();

        private CreateMatchLocationController CreateController()
        {
            return new CreateMatchLocationController(
                Mock.Of<ILogger<CreateMatchLocationController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _apiKeyProvider.Object,
                _authorizationPolicy.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_MatchLocationViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Model_has_new_match_location_and_Google_Maps_API_key()
        {
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("maps-key");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.NotNull(model.MatchLocation);
            Assert.Null(model.MatchLocation!.MatchLocationId);
            Assert.Equal("maps-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public async Task Model_has_authorization_page_title_and_breadcrumb()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateMatchLocation, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<MatchLocation>())).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal("Add a ground or sports centre", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^1].Name);
        }
    }
}
