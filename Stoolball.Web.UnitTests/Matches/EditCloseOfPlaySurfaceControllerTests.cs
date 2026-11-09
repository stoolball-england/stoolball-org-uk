using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
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
    public class EditCloseOfPlaySurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IMatchListingCacheInvalidator> _matchListingCacheClearer = new();
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly List<TeamInMatch> _teams =
        [
            new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Home, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Home team" } },
            new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Away, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Away team" } }
        ];
        private readonly Match _match;
        private readonly Match _updatedMatch = new() { MatchRoute = "/matches/updated-match" };

        public EditCloseOfPlaySurfaceControllerTests()
        {
            _match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "Example match",
                MatchRoute = "/matches/example-match",
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                UpdateMatchNameAutomatically = true,
                Teams = _teams
            };

            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match/edit/close-of-play"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(_match);
            SetupAuthorization(true);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchRepository.Setup(x => x.UpdateCloseOfPlay(It.IsAny<Match>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedMatch);
            _dateTimeFormatter.Setup(x => x.FormatDate(It.IsAny<DateTimeOffset>(), false, false, false)).Returns("1 June 2026");
        }

        private void SetupAuthorization(bool isAuthorized) =>
            _authorizationPolicy.Setup(x => x.IsAuthorized(_match)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchResult, isAuthorized } });

        private EditCloseOfPlaySurfaceController CreateController()
        {
            return new EditCloseOfPlaySurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchDataSource.Object,
                _matchRepository.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object,
                _matchListingCacheClearer.Object,
                _playerCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<EditCloseOfPlayViewModel> UpdateMatchExpectingView(EditCloseOfPlayFormData formData, Action<EditCloseOfPlaySurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.UpdateMatch(formData);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditCloseOfPlay", view.ViewName);
            return Assert.IsType<EditCloseOfPlayViewModel>(view.Model);
        }

        private void VerifyNotUpdated()
        {
            _matchRepository.Verify(x => x.UpdateCloseOfPlay(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForMatch(It.IsAny<Match>(), It.IsAny<Match>()), Times.Never);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(It.IsAny<IEnumerable<TeamInMatch>>()), Times.Never);
        }

        [Fact]
        public async Task Null_form_data_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateMatch(null!));
        }

        [Fact]
        public async Task Match_in_the_future_returns_404()
        {
            _match.StartTime = DateTimeOffset.UtcNow.AddDays(1);

            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditCloseOfPlayFormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Tournament_match_returns_404()
        {
            _match.Tournament = new Tournament();

            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditCloseOfPlayFormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Valid_request_updates_match_clears_caches_and_redirects_to_match()
        {
            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditCloseOfPlayFormData { MatchResultType = MatchResultType.HomeWin });

            _matchRepository.Verify(x => x.UpdateCloseOfPlay(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
            _matchListingCacheClearer.Verify(x => x.InvalidateCacheForMatch(_match, _updatedMatch), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(_teams), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_updatedMatch.MatchRoute, redirect.Url);
        }

        [Fact]
        public async Task Result_type_is_copied_from_form_data()
        {
            using var controller = CreateController();

            await controller.UpdateMatch(new EditCloseOfPlayFormData { MatchResultType = MatchResultType.Tie });

            Assert.Equal(MatchResultType.Tie, _match.MatchResultType);
        }

        [Fact]
        public async Task Result_type_of_minus_one_is_saved_as_null()
        {
            _match.MatchResultType = MatchResultType.HomeWin;

            using var controller = CreateController();

            await controller.UpdateMatch(new EditCloseOfPlayFormData { MatchResultType = (MatchResultType)(-1) });

            _matchRepository.Verify(x => x.UpdateCloseOfPlay(It.Is<Match>(m => m.MatchResultType == null), _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Awards_are_mapped_from_form_data_as_player_of_the_match()
        {
            var formData = new EditCloseOfPlayFormData();
            var award = new MatchAwardViewModel { MatchAwardId = Guid.NewGuid(), PlayerSearch = "Jo Bloggs", TeamId = _teams[0].Team!.TeamId, Reason = "Great catch" };
            formData.Awards.Add(award);

            using var controller = CreateController();

            await controller.UpdateMatch(formData);

            var savedAward = Assert.Single(_match.Awards);
            Assert.Equal(award.MatchAwardId, savedAward.AwardedToId);
            Assert.Equal(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, savedAward.Award?.AwardName);
            Assert.Equal("Jo Bloggs", savedAward.PlayerIdentity?.PlayerIdentityName);
            Assert.Equal(award.TeamId, savedAward.PlayerIdentity?.Team?.TeamId);
            Assert.Equal("Great catch", savedAward.Reason);
        }

        [Fact]
        public async Task Teams_and_match_name_setting_are_preserved_from_the_match_before_update()
        {
            using var controller = CreateController();

            await controller.UpdateMatch(new EditCloseOfPlayFormData());

            _matchRepository.Verify(x => x.UpdateCloseOfPlay(It.Is<Match>(m => m.Teams == _teams && m.UpdateMatchNameAutomatically), _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating()
        {
            SetupAuthorization(false);
            var formData = new EditCloseOfPlayFormData { MatchResultType = MatchResultType.HomeWin };

            var model = await UpdateMatchExpectingView(formData);

            Assert.Equal(_match, model.Match);
            Assert.Equal(formData, model.FormData);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult]);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_and_matches_breadcrumbs()
        {
            var model = await UpdateMatchExpectingView(new EditCloseOfPlayFormData(), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal("Edit Example match, 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Matches, "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(Constants.Pages.MatchesUrl, model.Breadcrumbs[^2].Url?.ToString());
            Assert.Equal(_match.MatchRoute, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_match_in_season_returns_competition_and_season_breadcrumbs()
        {
            _match.Season = new Season
            {
                FromYear = 2026,
                UntilYear = 2026,
                SeasonRoute = "/competitions/example-league/2026",
                Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" }
            };

            var model = await UpdateMatchExpectingView(new EditCloseOfPlayFormData(), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.Equal(_match.Season.SeasonRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
