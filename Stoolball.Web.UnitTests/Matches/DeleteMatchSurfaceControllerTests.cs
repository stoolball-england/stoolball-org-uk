using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;
using Match = Stoolball.Matches.Match;

namespace Stoolball.Web.UnitTests.Matches
{
    public class DeleteMatchSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<ICommentsDataSource<Match>> _commentsDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Match _match = new()
        {
            MatchId = Guid.NewGuid(),
            MatchName = "Example match",
            MatchRoute = "/matches/example-match",
            StartTime = new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero)
        };

        public DeleteMatchSurfaceControllerTests()
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(_match);
            SetupAuthorization(true);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _commentsDataSource.Setup(x => x.ReadTotalComments(_match.MatchId!.Value)).ReturnsAsync(3);
            _dateTimeFormatter.Setup(x => x.FormatDate(_match.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private void SetupAuthorization(bool isAuthorized) =>
            _authorizationPolicy.Setup(x => x.IsAuthorized(_match)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteMatch, isAuthorized } });

        private DeleteMatchSurfaceController CreateController()
        {
            return new DeleteMatchSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchDataSource.Object,
                _matchRepository.Object,
                _commentsDataSource.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<DeleteMatchViewModel> DeleteMatchExpectingView(Action<DeleteMatchSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.DeleteMatch(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("DeleteMatch", view.ViewName);
            return Assert.IsType<DeleteMatchViewModel>(view.Model);
        }

        private void VerifyNothingDeleted()
        {
            _matchRepository.Verify(x => x.DeleteMatch(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(It.IsAny<Match>()), Times.Never);
        }

        [Fact]
        public async Task Null_model_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteMatch(null!));
        }

        [Fact]
        public async Task Not_authorised_does_not_delete_and_returns_view_with_comments_and_match_breadcrumb()
        {
            SetupAuthorization(false);

            var model = await DeleteMatchExpectingView();

            Assert.Equal(_match, model.Match);
            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalComments);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteMatch]);
            Assert.Equal([Constants.Pages.Matches, "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_match.MatchRoute, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNothingDeleted();
        }

        [Fact]
        public async Task Invalid_ModelState_does_not_delete()
        {
            var model = await DeleteMatchExpectingView(controller => controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error"));

            Assert.False(model.Deleted);
            Assert.Equal(3, model.TotalComments);
            VerifyNothingDeleted();
        }

        [Fact]
        public async Task Valid_request_deletes_match_and_clears_cache()
        {
            using var controller = CreateController();

            await controller.DeleteMatch(new MatchingTextConfirmation());

            _matchRepository.Verify(x => x.DeleteMatch(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(_match), Times.Once);
        }

        [Fact]
        public async Task Valid_request_returns_deleted_view_model_without_match_breadcrumb_or_comments()
        {
            var model = await DeleteMatchExpectingView();

            Assert.True(model.Deleted);
            Assert.Equal(Constants.Pages.Matches, model.Breadcrumbs.Last().Name);
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Name == _match.MatchName);
            _commentsDataSource.Verify(x => x.ReadTotalComments(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Page_title_is_set_whether_or_not_deleted(bool isAuthorized)
        {
            SetupAuthorization(isAuthorized);

            var model = await DeleteMatchExpectingView();

            Assert.Equal("Delete Example match, 1 June 2026 - stoolball match", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Match_in_season_has_competition_and_season_breadcrumbs()
        {
            _match.Season = new Season
            {
                FromYear = 2026,
                UntilYear = 2026,
                SeasonRoute = "/competitions/example-league/2026",
                Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" }
            };
            SetupAuthorization(false);

            var model = await DeleteMatchExpectingView();

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Name == Constants.Pages.Matches);
        }

        [Fact]
        public async Task Match_in_tournament_has_tournament_breadcrumbs()
        {
            _match.Tournament = new Tournament { TournamentName = "Example tournament", TournamentRoute = "/tournaments/example-tournament" };
            SetupAuthorization(false);

            var model = await DeleteMatchExpectingView();

            Assert.Equal([Constants.Pages.Tournaments, "Example tournament", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
            Assert.Equal(Constants.Pages.TournamentsUrl, model.Breadcrumbs[^3].Url?.ToString());
            Assert.Equal(_match.Tournament.TournamentRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
