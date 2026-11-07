using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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
    public class DeleteMatchLocationSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IMatchLocationRepository> _matchLocationRepository = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<MatchLocation>> _matchLocationCacheClearer = new();
        private readonly Mock<IMatchListingCacheInvalidator> _matchListingCacheClearer = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly Mock<IMemberGroup> _memberGroup = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly MatchLocation _location = new()
        {
            MatchLocationId = Guid.NewGuid(),
            SecondaryAddressableObjectName = "Example ground",
            MatchLocationRoute = "/locations/example-ground",
            MemberGroupKey = Guid.NewGuid()
        };

        public DeleteMatchLocationSurfaceControllerTests()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(_location);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_location)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteMatchLocation, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberGroupService.Setup(x => x.GetById(_location.MemberGroupKey!.Value)).Returns(_memberGroup.Object);
            _matchDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(3);
        }

        private DeleteMatchLocationSurfaceController CreateController()
        {
            return new DeleteMatchLocationSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchLocationDataSource.Object,
                _matchLocationRepository.Object,
                _matchDataSource.Object,
                _authorizationPolicy.Object,
                _matchLocationCacheClearer.Object,
                _matchListingCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_model_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteMatchLocation(null!));
        }

        [Fact]
        public async Task Not_authorised_does_not_delete_and_returns_view_with_total_matches_and_location_breadcrumb()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_location)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteMatchLocation, false } });

            using var controller = CreateController();

            var result = await controller.DeleteMatchLocation(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteMatchLocationViewModel>(view.Model);
            Assert.Equal("DeleteMatchLocation", view.ViewName);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.Equal(_location.NameAndLocalityOrTownIfDifferent(), model.Breadcrumbs[^1].Name);
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.MatchLocationIds.SequenceEqual(new[] { _location.MatchLocationId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
            _matchLocationRepository.Verify(x => x.DeleteMatchLocation(It.IsAny<MatchLocation>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _matchLocationCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForMatchLocation(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_does_not_delete()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var result = await controller.DeleteMatchLocation(new MatchingTextConfirmation());

            var model = Assert.IsType<DeleteMatchLocationViewModel>(((ViewResult)result).Model);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteMatchLocation]);
            _matchLocationRepository.Verify(x => x.DeleteMatchLocation(It.IsAny<MatchLocation>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_deletes_member_group_and_location_and_clears_caches()
        {
            using var controller = CreateController();

            await controller.DeleteMatchLocation(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(_memberGroup.Object), Times.Once);
            _matchLocationRepository.Verify(x => x.DeleteMatchLocation(_location, _currentMember.Key, _currentMember.Name!), Times.Once);
            _matchLocationCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForMatchLocation(_location.MatchLocationId!.Value), Times.Once);
        }

        [Fact]
        public async Task Valid_request_with_missing_member_group_still_deletes_location()
        {
            _memberGroupService.Setup(x => x.GetById(_location.MemberGroupKey!.Value)).Returns((IMemberGroup?)null);

            using var controller = CreateController();

            await controller.DeleteMatchLocation(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _matchLocationRepository.Verify(x => x.DeleteMatchLocation(_location, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Valid_request_returns_deleted_view_model_without_location_breadcrumb()
        {
            using var controller = CreateController();

            var result = await controller.DeleteMatchLocation(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteMatchLocationViewModel>(view.Model);
            Assert.Equal("DeleteMatchLocation", view.ViewName);
            Assert.True(model.Deleted);
            Assert.Equal("Delete " + _location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Name == _location.NameAndLocalityOrTownIfDifferent());
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.IsAny<MatchFilter>()), Times.Never);
        }
    }
}
