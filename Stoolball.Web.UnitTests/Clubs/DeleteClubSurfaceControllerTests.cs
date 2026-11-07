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
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class DeleteClubSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IClubRepository> _clubRepository = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Team>> _cacheClearer = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Club _club = new() { ClubName = "Example club", ClubRoute = "/clubs/example-club", MemberGroupKey = Guid.NewGuid() };
        private readonly Mock<IMemberGroup> _memberGroup = new();

        public DeleteClubSurfaceControllerTests()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(_club);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_club)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteClub, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberGroupService.Setup(x => x.GetById(_club.MemberGroupKey!.Value)).Returns(_memberGroup.Object);
        }

        private DeleteClubSurfaceController CreateController()
        {
            return new DeleteClubSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
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
        public async Task Null_model_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteClub(null!));
        }

        [Fact]
        public async Task Not_authorised_does_not_delete_and_returns_view_with_club_breadcrumb()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_club)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteClub, false } });

            using var controller = CreateController();

            var result = await controller.DeleteClub(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteClubViewModel>(view.Model);
            Assert.Equal("DeleteClub", view.ViewName);
            Assert.False(model.Deleted);
            Assert.Equal(_club.ClubName, model.Breadcrumbs[^1].Name);
            _clubRepository.Verify(x => x.DeleteClub(It.IsAny<Club>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_does_not_delete()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var result = await controller.DeleteClub(new MatchingTextConfirmation());

            var model = Assert.IsType<DeleteClubViewModel>(((ViewResult)result).Model);
            Assert.False(model.Deleted);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteClub]);
            _clubRepository.Verify(x => x.DeleteClub(It.IsAny<Club>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_deletes_member_group_and_club_and_clears_cache()
        {
            using var controller = CreateController();

            await controller.DeleteClub(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(_memberGroup.Object), Times.Once);
            _clubRepository.Verify(x => x.DeleteClub(_club, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
        }

        [Fact]
        public async Task Valid_request_with_missing_member_group_still_deletes_club()
        {
            _memberGroupService.Setup(x => x.GetById(_club.MemberGroupKey!.Value)).Returns((IMemberGroup?)null);

            using var controller = CreateController();

            await controller.DeleteClub(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _clubRepository.Verify(x => x.DeleteClub(_club, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Valid_request_returns_deleted_view_model_without_club_breadcrumb()
        {
            using var controller = CreateController();

            var result = await controller.DeleteClub(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteClubViewModel>(view.Model);
            Assert.Equal("DeleteClub", view.ViewName);
            Assert.True(model.Deleted);
            Assert.Equal("Delete Example club", model.Metadata.PageTitle);
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Name == _club.ClubName);
        }
    }
}
