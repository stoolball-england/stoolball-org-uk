using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class DeleteCompetitionSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<ICompetitionRepository> _competitionRepository = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Competition>> _competitionCacheClearer = new();
        private readonly Mock<IMatchListingCacheInvalidator> _matchListingCacheClearer = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly Mock<IMemberGroup> _memberGroup = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Competition _competition = new()
        {
            CompetitionId = Guid.NewGuid(),
            CompetitionName = "Example league",
            CompetitionRoute = "/competitions/example-league",
            MemberGroupKey = Guid.NewGuid()
        };

        public DeleteCompetitionSurfaceControllerTests()
        {
            _competition.Seasons.Add(new Season { SeasonId = Guid.NewGuid() });
            _competition.Seasons.Add(new Season { SeasonId = Guid.NewGuid() });

            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(_competition);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_competition)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberGroupService.Setup(x => x.GetById(_competition.MemberGroupKey!.Value)).Returns(_memberGroup.Object);
            _matchDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(3);
            _teamDataSource.Setup(x => x.ReadTotalTeams(It.IsAny<TeamFilter>())).ReturnsAsync(2);
        }

        private DeleteCompetitionSurfaceController CreateController()
        {
            return new DeleteCompetitionSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _competitionDataSource.Object,
                _competitionRepository.Object,
                _matchDataSource.Object,
                _teamDataSource.Object,
                _authorizationPolicy.Object,
                _competitionCacheClearer.Object,
                _matchListingCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_model_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteCompetition(null!));
        }

        [Fact]
        public async Task Not_authorised_does_not_delete_and_returns_view_with_totals_and_competition_breadcrumb()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_competition)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteCompetition, false } });

            using var controller = CreateController();

            var result = await controller.DeleteCompetition(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteCompetitionViewModel>(view.Model);
            Assert.Equal("DeleteCompetition", view.ViewName);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.Equal(2, model.TotalTeams);
            Assert.Equal("Delete Example league", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("Example league", model.Breadcrumbs[^1].Name);
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.CompetitionIds.SequenceEqual(new[] { _competition.CompetitionId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
            _teamDataSource.Verify(x => x.ReadTotalTeams(It.Is<TeamFilter>(f => f.CompetitionIds.SequenceEqual(new[] { _competition.CompetitionId!.Value }))), Times.Once);
            _competitionRepository.Verify(x => x.DeleteCompetition(It.IsAny<Competition>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _competitionCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForSeason(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_does_not_delete()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var result = await controller.DeleteCompetition(new MatchingTextConfirmation());

            var model = Assert.IsType<DeleteCompetitionViewModel>(((ViewResult)result).Model);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.Equal(2, model.TotalTeams);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteCompetition]);
            _competitionRepository.Verify(x => x.DeleteCompetition(It.IsAny<Competition>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_deletes_member_group_and_competition_and_clears_caches()
        {
            using var controller = CreateController();

            await controller.DeleteCompetition(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(_memberGroup.Object), Times.Once);
            _competitionRepository.Verify(x => x.DeleteCompetition(_competition, _currentMember.Key, _currentMember.Name!), Times.Once);
            _competitionCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
        }

        [Fact]
        public async Task Valid_request_clears_match_listing_cache_for_every_season()
        {
            using var controller = CreateController();

            await controller.DeleteCompetition(new MatchingTextConfirmation());

            foreach (var season in _competition.Seasons)
            {
                _matchListingCacheClearer.Verify(x => x.InvalidateCacheForSeason(season.SeasonId!.Value), Times.Once);
            }
        }

        [Fact]
        public async Task Valid_request_with_missing_member_group_still_deletes_competition()
        {
            _memberGroupService.Setup(x => x.GetById(_competition.MemberGroupKey!.Value)).Returns((IMemberGroup?)null);

            using var controller = CreateController();

            await controller.DeleteCompetition(new MatchingTextConfirmation());

            _memberGroupService.Verify(x => x.Delete(It.IsAny<IMemberGroup>()), Times.Never);
            _competitionRepository.Verify(x => x.DeleteCompetition(_competition, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Valid_request_returns_deleted_view_model_without_competition_breadcrumb()
        {
            using var controller = CreateController();

            var result = await controller.DeleteCompetition(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteCompetitionViewModel>(view.Model);
            Assert.Equal("DeleteCompetition", view.ViewName);
            Assert.True(model.Deleted);
            Assert.Equal("Delete Example league", model.Metadata.PageTitle);
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Name == "Example league");
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.IsAny<MatchFilter>()), Times.Never);
            _teamDataSource.Verify(x => x.ReadTotalTeams(It.IsAny<TeamFilter>()), Times.Never);
        }
    }
}
