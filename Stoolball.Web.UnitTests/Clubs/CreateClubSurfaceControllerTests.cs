using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Clubs;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class CreateClubSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IClubRepository> _clubRepository = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Team>> _cacheClearer = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Club _createdClub = new() { ClubRoute = "/clubs/example-club" };

        public CreateClubSurfaceControllerTests()
        {
            _routeGenerator.Setup(x => x.GenerateRoute("club", It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns("club/example-club");
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Club>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateClub, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(true);
            _clubRepository.Setup(x => x.CreateClub(It.IsAny<Club>(), _currentMember.Key, _currentMember.Name)).ReturnsAsync(_createdClub);
        }

        private CreateClubSurfaceController CreateController()
        {
            return new CreateClubSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _clubRepository.Object,
                _routeGenerator.Object,
                _authorizationPolicy.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_club_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateClub(null!));
        }

        [Fact]
        public async Task Not_authorised_returns_CreateClub_view_without_creating_club()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Club>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateClub, false } });

            using var controller = CreateController();

            var result = await controller.CreateClub(new Club { ClubName = "Example club" });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateClub", view.ViewName);
            _clubRepository.Verify(x => x.CreateClub(It.IsAny<Club>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_club_page_title_and_authorization()
        {
            var club = new Club { ClubName = "Example club" };

            using var controller = CreateController();
            controller.ModelState.AddModelError("Club.ClubName", "Any error");

            var result = await controller.CreateClub(club);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClubViewModel>(view.Model);
            Assert.Equal("CreateClub", view.ViewName);
            Assert.Equal(club, model.Club);
            Assert.Equal("Add a club", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateClub]);
            _clubRepository.Verify(x => x.CreateClub(It.IsAny<Club>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_for_selected_teams_only_is_ignored()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Club.Teams[0].TeamName", "Any error");

            var result = await controller.CreateClub(new Club { ClubName = "Example club" });

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Valid_request_creates_member_group_and_assigns_it_to_club()
        {
            _memberGroupService.Setup(x => x.GetByName("club/example-club")).Returns((IMemberGroup?)null);
            var club = new Club { ClubName = "Example club" };

            using var controller = CreateController();

            await controller.CreateClub(club);

            _memberGroupService.Verify(x => x.Save(It.Is<IMemberGroup>(g => g.Name == "club/example-club")), Times.Once);
            Assert.Equal("club/example-club", club.MemberGroupName);
            Assert.NotNull(club.MemberGroupKey);
        }

        [Fact]
        public async Task Existing_member_group_name_is_incremented_until_unique()
        {
            _routeGenerator.Setup(x => x.IncrementRoute("club/example-club")).Returns("club/example-club-1");
            _memberGroupService.Setup(x => x.GetByName("club/example-club")).Returns(Mock.Of<IMemberGroup>());
            _memberGroupService.Setup(x => x.GetByName("club/example-club-1")).Returns((IMemberGroup?)null);
            var club = new Club { ClubName = "Example club" };

            using var controller = CreateController();

            await controller.CreateClub(club);

            _memberGroupService.Verify(x => x.Save(It.Is<IMemberGroup>(g => g.Name == "club/example-club-1")), Times.Once);
            Assert.Equal("club/example-club-1", club.MemberGroupName);
        }

        [Fact]
        public async Task Valid_request_creates_club_clears_cache_and_redirects_to_club()
        {
            var club = new Club { ClubName = "Example club" };

            using var controller = CreateController();

            var result = await controller.CreateClub(club);

            _clubRepository.Verify(x => x.CreateClub(club, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_createdClub.ClubRoute, redirect.Url);
        }

        [Fact]
        public async Task Administrator_is_not_assigned_to_member_group()
        {
            using var controller = CreateController();

            await controller.CreateClub(new Club { ClubName = "Example club" });

            MemberService.Verify(x => x.AssignRole(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Non_administrator_is_assigned_to_member_group()
        {
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(false);

            using var controller = CreateController();

            await controller.CreateClub(new Club { ClubName = "Example club" });

            MemberService.Verify(x => x.AssignRole(It.IsAny<string>(), "club/example-club"), Times.Once);
        }
    }
}
