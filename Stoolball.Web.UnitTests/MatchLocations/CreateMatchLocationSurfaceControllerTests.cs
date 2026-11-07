using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class CreateMatchLocationSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchLocationRepository> _matchLocationRepository = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly MatchLocation _createdLocation = new() { MatchLocationRoute = "/locations/example-ground" };

        public CreateMatchLocationSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
            {
                { "MatchLocation.MatchLocationNotes", "<p>Some notes</p>" }
            }));
            _routeGenerator.Setup(x => x.GenerateRoute("location", It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns("location/example-ground");
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<MatchLocation>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateMatchLocation, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(true);
            _matchLocationRepository.Setup(x => x.CreateMatchLocation(It.IsAny<MatchLocation>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_createdLocation);
        }

        private CreateMatchLocationSurfaceController CreateController()
        {
            return new CreateMatchLocationSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchLocationRepository.Object,
                _authorizationPolicy.Object,
                _routeGenerator.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_location_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateMatchLocation(null!));
        }

        [Fact]
        public async Task Notes_are_read_from_form_so_that_HTML_is_allowed()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground" };

            using var controller = CreateController();

            await controller.CreateMatchLocation(location);

            Assert.Equal("<p>Some notes</p>", location.MatchLocationNotes);
        }

        [Fact]
        public async Task Not_authorised_returns_CreateMatchLocation_view_without_creating_location()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<MatchLocation>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateMatchLocation, false } });

            using var controller = CreateController();

            var result = await controller.CreateMatchLocation(new MatchLocation { SecondaryAddressableObjectName = "Example ground" });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateMatchLocation", view.ViewName);
            _matchLocationRepository.Verify(x => x.CreateMatchLocation(It.IsAny<MatchLocation>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Save(It.IsAny<IMemberGroup>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_authorization_and_breadcrumb()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground" };

            using var controller = CreateController();
            controller.ModelState.AddModelError("MatchLocation.Postcode", "Any error");

            var result = await controller.CreateMatchLocation(location);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MatchLocationViewModel>(view.Model);
            Assert.Equal("CreateMatchLocation", view.ViewName);
            Assert.Equal(location, model.MatchLocation);
            Assert.Equal("Add a ground or sports centre", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatchLocation]);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^1].Name);
            _matchLocationRepository.Verify(x => x.CreateMatchLocation(It.IsAny<MatchLocation>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_creates_member_group_and_assigns_it_to_location()
        {
            _memberGroupService.Setup(x => x.GetByName("location/example-ground")).Returns((IMemberGroup?)null);
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground" };

            using var controller = CreateController();

            await controller.CreateMatchLocation(location);

            _memberGroupService.Verify(x => x.Save(It.Is<IMemberGroup>(g => g.Name == "location/example-ground")), Times.Once);
            Assert.Equal("location/example-ground", location.MemberGroupName);
            Assert.NotNull(location.MemberGroupKey);
        }

        [Fact]
        public async Task Existing_member_group_name_is_incremented_until_unique()
        {
            _routeGenerator.Setup(x => x.IncrementRoute("location/example-ground")).Returns("location/example-ground-1");
            _memberGroupService.Setup(x => x.GetByName("location/example-ground")).Returns(Mock.Of<IMemberGroup>());
            _memberGroupService.Setup(x => x.GetByName("location/example-ground-1")).Returns((IMemberGroup?)null);
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground" };

            using var controller = CreateController();

            await controller.CreateMatchLocation(location);

            _memberGroupService.Verify(x => x.Save(It.Is<IMemberGroup>(g => g.Name == "location/example-ground-1")), Times.Once);
            Assert.Equal("location/example-ground-1", location.MemberGroupName);
        }

        [Fact]
        public async Task Valid_request_creates_location_and_redirects_to_it()
        {
            var location = new MatchLocation { SecondaryAddressableObjectName = "Example ground" };

            using var controller = CreateController();

            var result = await controller.CreateMatchLocation(location);

            _matchLocationRepository.Verify(x => x.CreateMatchLocation(location, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_createdLocation.MatchLocationRoute, redirect.Url);
        }

        [Fact]
        public async Task Administrator_is_not_assigned_to_member_group()
        {
            using var controller = CreateController();

            await controller.CreateMatchLocation(new MatchLocation { SecondaryAddressableObjectName = "Example ground" });

            MemberService.Verify(x => x.AssignRole(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Non_administrator_is_assigned_to_member_group()
        {
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(false);

            using var controller = CreateController();

            await controller.CreateMatchLocation(new MatchLocation { SecondaryAddressableObjectName = "Example ground" });

            MemberService.Verify(x => x.AssignRole(It.IsAny<string>(), "location/example-ground"), Times.Once);
        }
    }
}
