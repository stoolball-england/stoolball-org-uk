using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
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
    public class EditFriendlyMatchSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();
        private readonly Mock<IMatchValidator> _matchValidator = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly FormCollection _form = new(new Dictionary<string, StringValues>());
        private readonly DateTimeOffset _postedMatchDate = DateTimeOffset.UtcNow.AddDays(7).Date;
        private readonly Season _season = new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            SeasonRoute = "/competitions/example-league/2026",
            Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" }
        };
        private readonly Match _beforeUpdate = new()
        {
            MatchId = Guid.NewGuid(),
            MatchName = "Example match",
            MatchRoute = "/matches/example-match",
            MatchType = MatchType.FriendlyMatch,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            UpdateMatchNameAutomatically = true
        };
        private readonly Match _updatedMatch = new() { MatchRoute = "/matches/updated-match" };

        public EditFriendlyMatchSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match/edit/friendly"));
            Request.SetupGet(x => x.Form).Returns(_form);
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _seasonDataSource.Setup(x => x.ReadSeasonById(_season.SeasonId!.Value, false)).ReturnsAsync(_season);
            SetupAuthorization(true);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchRepository.Setup(x => x.UpdateMatchInTheFuture(It.IsAny<Match>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedMatch);
            _dateTimeFormatter.Setup(x => x.FormatDate(It.IsAny<DateTimeOffset>(), false, false, false)).Returns("1 June 2026");
            _editMatchHelper.Setup(x => x.ConfigureModelFromRequestData(It.IsAny<IEditMatchViewModel>(), It.IsAny<IFormCollection>(), It.IsAny<ModelStateDictionary>()))
                .Callback((IEditMatchViewModel model, IFormCollection _, ModelStateDictionary _) =>
                {
                    model.MatchDate = _postedMatchDate;
                    model.Match!.StartTime = _postedMatchDate;
                    model.Match.MatchName = "Posted match name";
                });
        }

        private void SetupAuthorization(bool isAuthorized) =>
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatch, isAuthorized } });

        private EditFriendlyMatchSurfaceController CreateController()
        {
            return new EditFriendlyMatchSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchDataSource.Object,
                _seasonDataSource.Object,
                _matchRepository.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object,
                _editMatchHelper.Object,
                _matchValidator.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<IActionResult> UpdateMatch(Match postedMatch)
        {
            using var controller = CreateController();
            return await controller.UpdateMatch(postedMatch);
        }

        private async Task<EditFriendlyMatchViewModel> UpdateMatchExpectingView(Match postedMatch, Action<EditFriendlyMatchSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.UpdateMatch(postedMatch);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditFriendlyMatch", view.ViewName);
            return Assert.IsType<EditFriendlyMatchViewModel>(view.Model);
        }

        private static void AddModelError(EditFriendlyMatchSurfaceController controller) => controller.ModelState.AddModelError("Any", "Any error");

        private void VerifyNotUpdated()
        {
            _matchRepository.Verify(x => x.UpdateMatchInTheFuture(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(It.IsAny<Match>(), It.IsAny<Match>()), Times.Never);
        }

        [Fact]
        public async Task Null_match_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateMatch(null!));
        }

        [Fact]
        public async Task Match_in_the_past_returns_404()
        {
            _beforeUpdate.StartTime = DateTimeOffset.UtcNow.AddDays(-1);

            Assert.IsType<NotFoundResult>(await UpdateMatch(new Match()));
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Identity_and_match_name_setting_are_copied_from_the_match_before_update()
        {
            var postedMatch = new Match();

            await UpdateMatch(postedMatch);

            Assert.Equal(_beforeUpdate.MatchId, postedMatch.MatchId);
            Assert.Equal(_beforeUpdate.MatchRoute, postedMatch.MatchRoute);
            Assert.True(postedMatch.UpdateMatchNameAutomatically);
        }

        [Fact]
        public async Task Request_data_is_read_by_the_helper()
        {
            using var controller = CreateController();

            await controller.UpdateMatch(new Match());

            _editMatchHelper.Verify(x => x.ConfigureModelFromRequestData(It.IsAny<EditFriendlyMatchViewModel>(), _form, controller.ModelState), Times.Once);
        }

        [Fact]
        public async Task Posted_season_without_an_id_is_removed()
        {
            var postedMatch = new Match { Season = new Season() };

            await UpdateMatch(postedMatch);

            Assert.Null(postedMatch.Season);
            _seasonDataSource.Verify(x => x.ReadSeasonById(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Posted_season_with_an_id_is_read_from_the_data_source()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            await UpdateMatch(postedMatch);

            Assert.Equal(_season, postedMatch.Season);
        }

        [Fact]
        public async Task Match_is_validated()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            using var controller = CreateController();

            await controller.UpdateMatch(postedMatch);

            _matchValidator.Verify(x => x.DateIsValidForSqlServer(_postedMatchDate, controller.ModelState, "MatchDate", "match"), Times.Once);
            _matchValidator.Verify(x => x.DateIsWithinTheSeason(_postedMatchDate, _season, controller.ModelState, "MatchDate", "match"), Times.Once);
            _matchValidator.Verify(x => x.AtLeastOneTeamId(It.Is<EditFriendlyMatchViewModel>(m => m.Match == postedMatch), controller.ModelState), Times.Once);
        }

        [Fact]
        public async Task Valid_request_updates_match_clears_cache_and_redirects_to_updated_match()
        {
            var postedMatch = new Match { MatchResultType = MatchResultType.Postponed };

            var result = await UpdateMatch(postedMatch);

            _matchRepository.Verify(x => x.UpdateMatchInTheFuture(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(_beforeUpdate, _updatedMatch), Times.Once);
            Assert.Equal(MatchResultType.Postponed, postedMatch.MatchResultType);
            Assert.Equal(_updatedMatch.MatchRoute, Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Result_type_of_minus_one_is_saved_as_null()
        {
            var postedMatch = new Match { MatchResultType = (MatchResultType)(-1) };

            await UpdateMatch(postedMatch);

            Assert.Null(postedMatch.MatchResultType);
            _matchRepository.Verify(x => x.UpdateMatchInTheFuture(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating()
        {
            SetupAuthorization(false);
            var postedMatch = new Match();

            var model = await UpdateMatchExpectingView(postedMatch);

            Assert.Equal(postedMatch, model.Match);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatch]);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_original_match_name_page_title_and_matches_breadcrumbs()
        {
            var model = await UpdateMatchExpectingView(new Match(), AddModelError);

            Assert.Equal("Example match", model.Match!.MatchName);
            Assert.Equal("Edit Example match, 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Matches, "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(Constants.Pages.MatchesUrl, model.Breadcrumbs[^2].Url?.ToString());
            Assert.Equal(_beforeUpdate.MatchRoute, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_match_in_season_returns_competition_and_season_breadcrumbs()
        {
            var model = await UpdateMatchExpectingView(new Match { Season = new Season { SeasonId = _season.SeasonId } }, AddModelError);

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.Equal(_season.SeasonRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
