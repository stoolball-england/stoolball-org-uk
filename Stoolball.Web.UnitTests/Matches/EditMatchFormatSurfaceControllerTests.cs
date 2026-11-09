using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public class EditMatchFormatSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IMatchInningsFactory> _matchInningsFactory = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Guid _homeMatchTeamId = Guid.NewGuid();
        private readonly Guid _awayMatchTeamId = Guid.NewGuid();
        private readonly Match _match;
        private readonly Match _updatedMatch = new() { MatchRoute = "/matches/updated-match" };

        public EditMatchFormatSurfaceControllerTests()
        {
            _match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "Example match",
                MatchRoute = "/matches/example-match",
                MatchType = MatchType.LeagueMatch,
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                MatchInnings = [CreateInnings(1, _homeMatchTeamId, _awayMatchTeamId, 12), CreateInnings(2, _awayMatchTeamId, _homeMatchTeamId, 12)]
            };

            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match/edit/format"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(() => _match);
            SetupAuthorization(true);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchRepository.Setup(x => x.UpdateMatchFormat(It.IsAny<Match>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedMatch);
            _dateTimeFormatter.Setup(x => x.FormatDate(It.IsAny<DateTimeOffset>(), false, false, false)).Returns("1 June 2026");
            _matchInningsFactory.Setup(x => x.CreateMatchInnings(It.IsAny<Match>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
                .Returns((Match match, Guid? batting, Guid? bowling) => CreateInnings(match.MatchInnings.Count + 1, batting, bowling, 10));
        }

        private static MatchInnings CreateInnings(int order, Guid? battingMatchTeamId, Guid? bowlingMatchTeamId, int overs) => new()
        {
            MatchInningsId = Guid.NewGuid(),
            InningsOrderInMatch = order,
            BattingMatchTeamId = battingMatchTeamId,
            BowlingMatchTeamId = bowlingMatchTeamId,
            OverSets = [new OverSet { Overs = overs }]
        };

        private void SetupMatchInThePast() => _match.StartTime = DateTimeOffset.UtcNow.AddDays(-1);

        private void SetupAuthorization(bool isAuthorized) =>
            _authorizationPolicy.Setup(x => x.IsAuthorized(_match)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatch, isAuthorized } });

        private EditMatchFormatSurfaceController CreateController()
        {
            return new EditMatchFormatSurfaceController(
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
                _matchInningsFactory.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<(EditMatchFormatViewModel model, ModelStateDictionary modelState)> UpdateMatchExpectingView(EditMatchFormatFormData formData, Action<EditMatchFormatSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.UpdateMatch(formData);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditMatchFormat", view.ViewName);
            return (Assert.IsType<EditMatchFormatViewModel>(view.Model), controller.ModelState);
        }

        private void VerifyNotUpdated() => _matchRepository.Verify(x => x.UpdateMatchFormat(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);

        [Fact]
        public async Task Null_form_data_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateMatch(null!));
        }

        [Fact]
        public async Task Match_not_found_returns_404()
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync((Match?)null!);

            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            Assert.IsType<NotFoundResult>(result);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Tournament_match_returns_404()
        {
            _match.Tournament = new Tournament();

            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            Assert.IsType<NotFoundResult>(result);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Training_session_returns_404()
        {
            _match.MatchType = MatchType.TrainingSession;

            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            Assert.IsType<NotFoundResult>(result);
            VerifyNotUpdated();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public async Task Odd_number_or_fewer_than_two_innings_is_a_model_error(int innings)
        {
            var (_, modelState) = await UpdateMatchExpectingView(new EditMatchFormatFormData { MatchInnings = innings, Overs = 12 });

            Assert.Contains(modelState["FormData.MatchInnings"]!.Errors, x => x.ErrorMessage == "A match cannot have an odd number of innings or less than 2 innings.");
            Assert.Equal(2, _match.MatchInnings.Count);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Reducing_innings_in_the_past_is_a_model_error()
        {
            SetupMatchInThePast();
            _match.MatchInnings.Add(CreateInnings(3, _homeMatchTeamId, _awayMatchTeamId, 12));
            _match.MatchInnings.Add(CreateInnings(4, _awayMatchTeamId, _homeMatchTeamId, 12));

            var (_, modelState) = await UpdateMatchExpectingView(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            Assert.Contains(modelState["FormData.MatchInnings"]!.Errors, x => x.ErrorMessage == "You cannot reduce the number of innings after a match has happened.");
            Assert.Equal(4, _match.MatchInnings.Count);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Reducing_innings_in_the_future_removes_innings_from_the_end()
        {
            var firstInnings = _match.MatchInnings[0];
            var secondInnings = _match.MatchInnings[1];
            _match.MatchInnings.Add(CreateInnings(3, _homeMatchTeamId, _awayMatchTeamId, 12));
            _match.MatchInnings.Add(CreateInnings(4, _awayMatchTeamId, _homeMatchTeamId, 12));

            using var controller = CreateController();

            await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            Assert.Equal([firstInnings, secondInnings], _match.MatchInnings);
            _matchRepository.Verify(x => x.UpdateMatchFormat(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Adding_innings_creates_innings_alternating_the_batting_team_with_the_posted_overs(bool matchIsInTheFuture)
        {
            if (!matchIsInTheFuture) { SetupMatchInThePast(); }

            using var controller = CreateController();

            await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 4, Overs = 15 });

            Assert.Equal(4, _match.MatchInnings.Count);
            Assert.Equal(_homeMatchTeamId, _match.MatchInnings[2].BattingMatchTeamId);
            Assert.Equal(_awayMatchTeamId, _match.MatchInnings[2].BowlingMatchTeamId);
            Assert.Equal(_awayMatchTeamId, _match.MatchInnings[3].BattingMatchTeamId);
            Assert.Equal(_homeMatchTeamId, _match.MatchInnings[3].BowlingMatchTeamId);
            _matchInningsFactory.Verify(x => x.CreateMatchInnings(_match, _homeMatchTeamId, _awayMatchTeamId), Times.Once);
            _matchInningsFactory.Verify(x => x.CreateMatchInnings(_match, _awayMatchTeamId, _homeMatchTeamId), Times.Once);
            Assert.All(_match.MatchInnings, innings => Assert.Equal(15, innings.OverSets[0].Overs));
            _matchRepository.Verify(x => x.UpdateMatchFormat(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Same_number_of_innings_does_not_add_or_remove_innings()
        {
            var innings = _match.MatchInnings.ToList();

            using var controller = CreateController();

            await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            Assert.Equal(innings, _match.MatchInnings);
            _matchInningsFactory.Verify(x => x.CreateMatchInnings(It.IsAny<Match>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()), Times.Never);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(20)]
        public async Task Overs_in_the_future_are_updated_for_every_over_set(int overs)
        {
            _match.MatchInnings[0].OverSets.Add(new OverSet { Overs = 4 });

            using var controller = CreateController();

            await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = overs });

            Assert.All(_match.MatchInnings.SelectMany(x => x.OverSets), overSet => Assert.Equal(overs, overSet.Overs));
            _matchRepository.Verify(x => x.UpdateMatchFormat(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Increasing_overs_in_the_past_is_allowed()
        {
            SetupMatchInThePast();

            using var controller = CreateController();

            await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 16 });

            Assert.All(_match.MatchInnings.SelectMany(x => x.OverSets), overSet => Assert.Equal(16, overSet.Overs));
            _matchRepository.Verify(x => x.UpdateMatchFormat(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Reducing_overs_in_the_past_is_a_model_error()
        {
            SetupMatchInThePast();

            var (_, modelState) = await UpdateMatchExpectingView(new EditMatchFormatFormData { MatchInnings = 2, Overs = 6 });

            Assert.Contains(modelState["FormData.Overs"]!.Errors, x => x.ErrorMessage == "You cannot reduce the number of overs after a match has happened.");
            Assert.All(_match.MatchInnings.SelectMany(x => x.OverSets), overSet => Assert.Equal(12, overSet.Overs));
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Valid_request_updates_match_format_and_redirects_to_edit_match_page()
        {
            using var controller = CreateController();

            var result = await controller.UpdateMatch(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 });

            _matchRepository.Verify(x => x.UpdateMatchFormat(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_updatedMatch.MatchRoute + "/edit", redirect.Url);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating()
        {
            SetupAuthorization(false);
            var formData = new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 };

            var (model, _) = await UpdateMatchExpectingView(formData);

            Assert.Equal(_match, model.Match);
            Assert.Equal(formData, model.FormData);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatch]);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_and_matches_breadcrumbs()
        {
            var (model, _) = await UpdateMatchExpectingView(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 }, c => c.ModelState.AddModelError("Any", "Any error"));

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

            var (model, _) = await UpdateMatchExpectingView(new EditMatchFormatFormData { MatchInnings = 2, Overs = 12 }, c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.Equal(_match.Season.SeasonRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
