using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class DeleteTeamSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Team>> _teamCacheClearer = new();
        private readonly Mock<IMatchListingCacheInvalidator> _matchListingCacheClearer = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly Mock<IMemberGroup> _memberGroup = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Team _team = new()
        {
            TeamId = Guid.NewGuid(),
            TeamName = "Example team",
            TeamRoute = "/teams/example-team",
            MemberGroupKey = Guid.NewGuid()
        };

        public DeleteTeamSurfaceControllerTests()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(_team);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_team)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteTeam, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberGroupService.Setup(x => x.GetById(_team.MemberGroupKey!.Value)).Returns(_memberGroup.Object);
            _matchDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(3);
            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>())).ReturnsAsync(new List<PlayerIdentity> { new() { PlayerIdentityName = "Player one" }, new() { PlayerIdentityName = "Player two" } });
        }

        private DeleteTeamSurfaceController CreateController()
        {
            return new DeleteTeamSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _teamDataSource.Object,
                _teamRepository.Object,
                _matchDataSource.Object,
                _playerDataSource.Object,
                _authorizationPolicy.Object,
                _teamCacheClearer.Object,
                _matchListingCacheClearer.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_model_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteTeam(null!));
        }

        [Fact]
        public async Task Not_authorised_does_not_delete_and_returns_view_with_totals_players_and_team_breadcrumb()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_team)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteTeam, false } });

            using var controller = CreateController();

            var result = await controller.DeleteTeam(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteTeamViewModel>(view.Model);
            Assert.Equal("DeleteTeam", view.ViewName);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.Equal(2, model.Team!.Players.Count);
            Assert.Equal("Delete Example team", model.Metadata.PageTitle);
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.TeamIds.SequenceEqual(new[] { _team.TeamId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
            _playerDataSource.Verify(x => x.ReadPlayerIdentities(It.Is<PlayerFilter>(f => f.TeamIds.SequenceEqual(new[] { _team.TeamId!.Value }))), Times.Once);
            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, _team, true), Times.Once);
            _teamRepository.Verify(x => x.DeleteTeam(It.IsAny<Team>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _teamCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForTeam(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_does_not_delete()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var result = await controller.DeleteTeam(new MatchingTextConfirmation());

            var model = Assert.IsType<DeleteTeamViewModel>(((ViewResult)result).Model);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteTeam]);
            _teamRepository.Verify(x => x.DeleteTeam(It.IsAny<Team>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_deletes_member_group_and_team_and_clears_caches()
        {
            using var controller = CreateController();

            await controller.DeleteTeam(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(_memberGroup.Object), Times.Once);
            _teamRepository.Verify(x => x.DeleteTeam(_team, _currentMember.Key, _currentMember.Name!), Times.Once);
            _teamCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForTeam(_team.TeamId!.Value), Times.Once);
        }

        [Fact]
        public async Task Valid_request_with_missing_member_group_still_deletes_team()
        {
            _memberGroupService.Setup(x => x.GetById(_team.MemberGroupKey!.Value)).Returns((IMemberGroup?)null);

            using var controller = CreateController();

            await controller.DeleteTeam(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _teamRepository.Verify(x => x.DeleteTeam(_team, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Valid_request_returns_deleted_view_model_without_team_breadcrumb_or_totals()
        {
            using var controller = CreateController();

            var result = await controller.DeleteTeam(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteTeamViewModel>(view.Model);
            Assert.Equal("DeleteTeam", view.ViewName);
            Assert.True(model.Deleted);
            Assert.Equal("Delete Example team", model.Metadata.PageTitle);
            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, _team, false), Times.Once);
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.IsAny<MatchFilter>()), Times.Never);
            _playerDataSource.Verify(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>()), Times.Never);
        }
    }
}
