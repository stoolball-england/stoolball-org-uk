using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class EditMatchLocationSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IMatchLocationRepository> _matchLocationRepository = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<MatchLocation>> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly MatchLocation _beforeUpdate = new() { MatchLocationId = Guid.NewGuid(), SecondaryAddressableObjectName = "Old name", MatchLocationRoute = "/locations/old-name" };

        public EditMatchLocationSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
            {
                { "MatchLocation.MatchLocationNotes", "<p>Some notes</p>" }
            }));
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchLocation, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchLocationRepository.Setup(x => x.UpdateMatchLocation(It.IsAny<MatchLocation>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_beforeUpdate);
        }

        private EditMatchLocationSurfaceController CreateController()
        {
            return new EditMatchLocationSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchLocationDataSource.Object,
                _matchLocationRepository.Object,
                _authorizationPolicy.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Id_and_route_are_copied_from_existing_location()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "New name" };

            using var controller = CreateController();

            await controller.UpdateMatchLocation(location);

            Assert.Equal(_beforeUpdate.MatchLocationId, location.MatchLocationId);
            Assert.Equal(_beforeUpdate.MatchLocationRoute, location.MatchLocationRoute);
        }

        [Fact]
        public async Task Notes_are_read_from_form_so_that_HTML_is_allowed()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "New name" };

            using var controller = CreateController();

            await controller.UpdateMatchLocation(location);

            Assert.Equal("<p>Some notes</p>", location.MatchLocationNotes);
        }

        [Fact]
        public async Task Not_authorised_returns_EditMatchLocation_view_without_updating_location()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchLocation, false } });

            using var controller = CreateController();

            var result = await controller.UpdateMatchLocation(new MatchLocation { SecondaryAddressableObjectName = "New name" });

            Assert.Equal("EditMatchLocation", Assert.IsType<ViewResult>(result).ViewName);
            _matchLocationRepository.Verify(x => x.UpdateMatchLocation(It.IsAny<MatchLocation>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_authorization_and_breadcrumbs()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "New name" };

            using var controller = CreateController();
            controller.ModelState.AddModelError("MatchLocation.Postcode", "Any error");

            var result = await controller.UpdateMatchLocation(location);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MatchLocationViewModel>(view.Model);
            Assert.Equal("EditMatchLocation", view.ViewName);
            Assert.Equal(location, model.MatchLocation);
            Assert.Equal("Edit " + location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchLocation]);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), model.Breadcrumbs[^1].Name);
            Assert.Equal(_beforeUpdate.MatchLocationRoute, model.Breadcrumbs[^1].Url!.ToString());
            _matchLocationRepository.Verify(x => x.UpdateMatchLocation(It.IsAny<MatchLocation>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_updates_location_clears_cache_and_redirects_to_edit_page()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "New name" };

            using var controller = CreateController();

            var result = await controller.UpdateMatchLocation(location);

            _matchLocationRepository.Verify(x => x.UpdateMatchLocation(location, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_beforeUpdate.MatchLocationRoute + "/edit", redirect.Url);
        }
    }
}
