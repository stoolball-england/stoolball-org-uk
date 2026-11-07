using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchLocationActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();

        private MatchLocationActionsController CreateController()
        {
            return new MatchLocationActionsController(
                Mock.Of<ILogger<MatchLocationActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchLocationDataSource.Object,
                _authorizationPolicy.Object)
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
        public async Task Model_has_authorization_details_for_location()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground", MatchLocationRoute = "/locations/example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchLocation, true } };
            var groups = new List<string> { "Example group" };
            var members = new List<string> { "Jo Bloggs" };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(location);
            _authorizationPolicy.Setup(x => x.IsAuthorized(location)).ReturnsAsync(authorization);
            _authorizationPolicy.Setup(x => x.AuthorizedGroupNames(location)).Returns(groups);
            _authorizationPolicy.Setup(x => x.AuthorizedMemberNames(location)).ReturnsAsync(members);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), model.Authorization.AuthorizationFor);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(groups, model.Authorization.AuthorizedGroupNames);
            Assert.Equal(members, model.Authorization.AuthorizedMemberNames);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground", MatchLocationRoute = "/locations/example" };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(location);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal("Edit " + location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^2].Name);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), model.Breadcrumbs[^1].Name);
            Assert.Equal(location.MatchLocationRoute, model.Breadcrumbs[^1].Url!.ToString());
        }
    }
}
