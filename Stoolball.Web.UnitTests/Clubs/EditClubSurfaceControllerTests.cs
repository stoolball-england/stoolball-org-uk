using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Clubs;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class EditClubSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IClubRepository> _clubRepository = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Team>> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Club _beforeUpdate = new() { ClubId = Guid.NewGuid(), ClubName = "Old name", ClubRoute = "/clubs/old-name" };

        public EditClubSurfaceControllerTests()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditClub, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _clubRepository.Setup(x => x.UpdateClub(It.IsAny<Club>(), _currentMember.Key, _currentMember.Name)).ReturnsAsync(_beforeUpdate);
        }

        private EditClubSurfaceController CreateController()
        {
            return new EditClubSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _clubDataSource.Object,
                _clubRepository.Object,
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

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateClub(null!));
        }

        [Fact]
        public async Task Club_id_and_route_are_copied_from_existing_club()
        {
            var club = new Club { ClubName = "New name" };

            using var controller = CreateController();

            await controller.UpdateClub(club);

            Assert.Equal(_beforeUpdate.ClubId, club.ClubId);
            Assert.Equal(_beforeUpdate.ClubRoute, club.ClubRoute);
        }

        [Fact]
        public async Task Not_authorised_returns_EditClub_view_without_updating_club()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditClub, false } });

            using var controller = CreateController();

            var result = await controller.UpdateClub(new Club { ClubName = "New name" });

            Assert.Equal("EditClub", Assert.IsType<ViewResult>(result).ViewName);
            _clubRepository.Verify(x => x.UpdateClub(It.IsAny<Club>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_club_page_title_authorization_and_breadcrumbs()
        {
            var club = new Club { ClubName = "New name" };

            using var controller = CreateController();
            controller.ModelState.AddModelError("Club.ClubName", "Any error");

            var result = await controller.UpdateClub(club);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClubViewModel>(view.Model);
            Assert.Equal("EditClub", view.ViewName);
            Assert.Equal(club, model.Club);
            Assert.Equal("Edit New name", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditClub]);
            Assert.Equal(club.ClubName, model.Breadcrumbs[^1].Name);
            Assert.Equal(_beforeUpdate.ClubRoute, model.Breadcrumbs[^1].Url!.ToString());
            _clubRepository.Verify(x => x.UpdateClub(It.IsAny<Club>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_for_selected_teams_only_is_ignored()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Club.Teams[0].TeamName", "Any error");

            var result = await controller.UpdateClub(new Club { ClubName = "New name" });

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Valid_request_updates_club_clears_cache_and_redirects_to_edit_page()
        {
            var club = new Club { ClubName = "New name" };

            using var controller = CreateController();

            var result = await controller.UpdateClub(club);

            _clubRepository.Verify(x => x.UpdateClub(club, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_beforeUpdate.ClubRoute + "/edit", redirect.Url);
        }
    }
}
