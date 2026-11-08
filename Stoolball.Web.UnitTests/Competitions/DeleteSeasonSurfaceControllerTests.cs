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
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class DeleteSeasonSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ISeasonRepository> _seasonRepository = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Season _season = new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            SeasonRoute = "/competitions/example-league/2026",
            Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" }
        };
        private readonly MatchingTextConfirmation _confirmation = new() { RequiredText = "Example league, 2026 season", ConfirmationText = "Example league, 2026 season" };

        public DeleteSeasonSurfaceControllerTests()
        {
            _season.Teams.Add(new TeamInSeason { Season = _season, Team = new Team { TeamName = "Team 1" } });
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(_season);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_season.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(3);
        }

        private DeleteSeasonSurfaceController CreateController()
        {
            return new DeleteSeasonSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _seasonDataSource.Object,
                _seasonRepository.Object,
                _matchDataSource.Object,
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

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteSeason(null!));
        }

        [Fact]
        public async Task Not_authorised_returns_view_with_total_matches_without_deleting_season()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_season.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteCompetition, false } });

            using var controller = CreateController();

            var result = await controller.DeleteSeason(_confirmation);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteSeasonViewModel>(view.Model);
            Assert.Equal("DeleteSeason", view.ViewName);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            _seasonRepository.Verify(x => x.DeleteSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForSeason(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_with_total_matches_without_deleting_season()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var result = await controller.DeleteSeason(_confirmation);

            var model = Assert.IsType<DeleteSeasonViewModel>(((ViewResult)result).Model);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalMatches);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteCompetition]);
            _seasonRepository.Verify(x => x.DeleteSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForSeason(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Total_matches_includes_tournament_matches_for_the_season()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            await controller.DeleteSeason(_confirmation);

            _matchDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.SeasonIds.SequenceEqual(new[] { _season.SeasonId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
        }

        [Fact]
        public async Task Valid_request_deletes_season_clears_cache_and_marks_model_as_deleted()
        {
            using var controller = CreateController();

            var result = await controller.DeleteSeason(_confirmation);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteSeasonViewModel>(view.Model);
            Assert.Equal("DeleteSeason", view.ViewName);
            Assert.True(model.Deleted);
            _seasonRepository.Verify(x => x.DeleteSeason(_season, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForSeason(_season.SeasonId!.Value), Times.Once);
            _matchDataSource.Verify(x => x.ReadTotalMatches(It.IsAny<MatchFilter>()), Times.Never);
        }

        [Fact]
        public async Task Teams_are_copied_without_circular_references_before_audit()
        {
            using var controller = CreateController();

            await controller.DeleteSeason(_confirmation);

            var team = Assert.Single(_season.Teams);
            Assert.Null(team.Season);
            Assert.Equal("Team 1", team.Team!.TeamName);
        }

        [Fact]
        public async Task Page_title_is_set()
        {
            using var controller = CreateController();

            var result = await controller.DeleteSeason(_confirmation);

            var model = Assert.IsType<DeleteSeasonViewModel>(((ViewResult)result).Model);
            Assert.Equal($"Delete {_season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Breadcrumbs_include_season_unless_it_was_deleted()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var notDeleted = Assert.IsType<DeleteSeasonViewModel>(((ViewResult)await controller.DeleteSeason(_confirmation)).Model);

            Assert.Equal([Constants.Pages.Competitions, "Example league", _season.SeasonName()], notDeleted.Breadcrumbs.Select(x => x.Name).TakeLast(3));

            controller.ModelState.Clear();

            var deleted = Assert.IsType<DeleteSeasonViewModel>(((ViewResult)await controller.DeleteSeason(_confirmation)).Model);

            Assert.Equal([Constants.Pages.Competitions, "Example league"], deleted.Breadcrumbs.Select(x => x.Name).TakeLast(2));
        }
    }
}
