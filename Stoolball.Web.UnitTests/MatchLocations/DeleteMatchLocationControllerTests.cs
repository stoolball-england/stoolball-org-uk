using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class DeleteMatchLocationControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();

        private DeleteMatchLocationController CreateController()
        {
            return new DeleteMatchLocationController(
                Mock.Of<ILogger<DeleteMatchLocationController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchLocationDataSource.Object,
                _matchListingDataSource.Object,
                _authorizationPolicy.Object
                )
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
        public async Task Route_matching_location_returns_DeleteMatchLocationViewModel()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid(), MatchLocationRoute = "/locations/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<DeleteMatchLocationViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Model_has_location_total_matches_and_required_confirmation_text()
        {
            var location = new MatchLocation { MatchLocationId = Guid.NewGuid(), SecondaryAddressableObjectName = "Example ground", MatchLocationRoute = "/locations/example" };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(location);
            _matchListingDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(7);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<DeleteMatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(location, model.MatchLocation);
            Assert.Equal(7, model.TotalMatches);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), model.ConfirmDeleteRequest.RequiredText);
            _matchListingDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.MatchLocationIds.SequenceEqual(new[] { location.MatchLocationId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
        }

        [Fact]
        public async Task Model_has_authorization_page_title_and_breadcrumbs()
        {
            var location = new MatchLocation { MatchLocationId = Guid.NewGuid(), SecondaryAddressableObjectName = "Example ground", MatchLocationRoute = "/locations/example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteMatchLocation, true } };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(location);
            _authorizationPolicy.Setup(x => x.IsAuthorized(location)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<DeleteMatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal("Delete " + location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^2].Name);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), model.Breadcrumbs[^1].Name);
            Assert.Equal(location.MatchLocationRoute, model.Breadcrumbs[^1].Url!.ToString());
        }
    }
}
