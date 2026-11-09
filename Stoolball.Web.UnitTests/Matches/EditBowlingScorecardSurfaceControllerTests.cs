using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public class EditBowlingScorecardSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IMatchInningsUrlParser> _matchInningsUrlParser = new();
        private readonly Mock<IBowlingFiguresCalculator> _bowlingFiguresCalculator = new();
        private readonly Mock<IOverSetScaffolder> _overSetScaffolder = new();
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly TeamInMatch _homeTeam = new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Home, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Home team" } };
        private readonly TeamInMatch _awayTeam = new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Away, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Away team" } };
        private readonly List<BowlingFigures> _bowlingFigures = [new BowlingFigures()];
        private readonly Match _match;

        public EditBowlingScorecardSurfaceControllerTests()
        {
            _match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "Example match",
                MatchRoute = "/matches/example-match",
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                Teams = [_homeTeam, _awayTeam],
                MatchInnings =
                [
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 1, BattingTeam = _homeTeam, BowlingTeam = _awayTeam, OverSets = [new OverSet { Overs = 6 }, new OverSet { Overs = 4 }] },
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 2, BattingTeam = _awayTeam, BowlingTeam = _homeTeam, OverSets = [new OverSet { Overs = 10 }] }
                ]
            };

            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match/edit/innings/1/bowling"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(_match);
            _matchInningsUrlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);
            _bowlingFiguresCalculator.Setup(x => x.CalculateBowlingFigures(It.IsAny<MatchInnings>())).Returns(_bowlingFigures);
            SetupAuthorization(true);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _dateTimeFormatter.Setup(x => x.FormatDate(It.IsAny<DateTimeOffset>(), false, false, false)).Returns("1 June 2026");
        }

        private MatchInnings FirstInnings => _match.MatchInnings[0];

        private void SetupAuthorization(bool isAuthorized) =>
            _authorizationPolicy.Setup(x => x.IsAuthorized(_match)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchResult, isAuthorized } });

        private EditBowlingScorecardSurfaceController CreateController()
        {
            return new EditBowlingScorecardSurfaceController(
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
                _matchInningsUrlParser.Object,
                _bowlingFiguresCalculator.Object,
                _overSetScaffolder.Object,
                _playerCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static MatchInningsViewModel PostedData(params OverViewModel[] overs)
        {
            var postedData = new MatchInningsViewModel();
            postedData.OversBowledSearch.AddRange(overs);
            return postedData;
        }

        private async Task<(EditScorecardViewModel model, ModelStateDictionary modelState)> UpdateMatchExpectingView(MatchInningsViewModel postedData, Action<EditBowlingScorecardSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.UpdateMatch(postedData);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditBowlingScorecard", view.ViewName);
            return (Assert.IsType<EditScorecardViewModel>(view.Model), controller.ModelState);
        }

        private async Task<IActionResult> UpdateMatch(MatchInningsViewModel postedData)
        {
            using var controller = CreateController();
            return await controller.UpdateMatch(postedData);
        }

        private void VerifyNotUpdated()
        {
            _matchRepository.Verify(x => x.UpdateBowlingScorecard(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(It.IsAny<Team[]>()), Times.Never);
        }

        [Fact]
        public async Task Null_posted_data_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateMatch(null!));
        }

        [Fact]
        public async Task Match_in_the_future_returns_404()
        {
            _match.StartTime = DateTimeOffset.UtcNow.AddDays(1);

            Assert.IsType<NotFoundResult>(await UpdateMatch(PostedData()));
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Tournament_match_returns_404()
        {
            _match.Tournament = new Tournament();

            Assert.IsType<NotFoundResult>(await UpdateMatch(PostedData()));
            VerifyNotUpdated();
        }

        [Theory]
        [InlineData(MatchResultType.HomeWinByForfeit)]
        [InlineData(MatchResultType.AwayWinByForfeit)]
        [InlineData(MatchResultType.Postponed)]
        [InlineData(MatchResultType.Cancelled)]
        public async Task Match_result_with_no_scorecard_returns_404(MatchResultType resultType)
        {
            _match.MatchResultType = resultType;

            Assert.IsType<NotFoundResult>(await UpdateMatch(PostedData()));
            VerifyNotUpdated();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(3)]
        public async Task Innings_not_in_match_returns_404(int? inningsOrderInMatch)
        {
            _matchInningsUrlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(inningsOrderInMatch);

            Assert.IsType<NotFoundResult>(await UpdateMatch(PostedData()));
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Innings_order_is_parsed_from_the_request_path()
        {
            await UpdateMatch(PostedData());

            _matchInningsUrlParser.Verify(x => x.ParseInningsOrderInMatchFromUrl(It.Is<Uri>(u => u.ToString() == "/matches/example-match/edit/innings/1/bowling")), Times.Once);
        }

        [Theory]
        [InlineData(6, null, null, null)]
        [InlineData(null, 1, null, null)]
        [InlineData(null, null, 1, null)]
        [InlineData(null, null, null, 10)]
        public async Task Over_with_details_but_no_bowler_is_a_model_error(int? ballsBowled, int? wides, int? noBalls, int? runsConceded)
        {
            var (_, modelState) = await UpdateMatchExpectingView(PostedData(
                new OverViewModel { BowledBy = "Bowler one", BallsBowled = 8 },
                new OverViewModel { BowledBy = " ", BallsBowled = ballsBowled, Wides = wides, NoBalls = noBalls, RunsConceded = runsConceded }));

            var expectedMessage = $"You've added the {2.Ordinalize(CultureInfo.CurrentCulture)} over. Please name the bowler.";
            Assert.Contains(modelState["CurrentInnings.OversBowledSearch[1].Bowler"]!.Errors, x => x.ErrorMessage == expectedMessage);
            Assert.False(modelState.ContainsKey("CurrentInnings.OversBowledSearch[0].Bowler"));
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Empty_over_without_a_bowler_is_not_a_model_error()
        {
            var result = await UpdateMatch(PostedData(new OverViewModel { BowledBy = "Bowler one", BallsBowled = 8 }, new OverViewModel()));

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Posted_overs_with_a_bowler_are_trimmed_numbered_and_assigned_to_the_bowling_team()
        {
            await UpdateMatch(PostedData(
                new OverViewModel(),
                new OverViewModel { BowledBy = " Bowler one ", BallsBowled = 8, Wides = 1, NoBalls = 2, RunsConceded = 10 },
                new OverViewModel { BowledBy = "Bowler two", BallsBowled = 9 }));

            Assert.Equal(2, FirstInnings.OversBowled.Count);
            var first = FirstInnings.OversBowled[0];
            Assert.Equal("Bowler one", first.Bowler?.PlayerIdentityName);
            Assert.Equal(_awayTeam.Team, first.Bowler?.Team);
            Assert.Equal(1, first.OverNumber);
            Assert.Equal(8, first.BallsBowled);
            Assert.Equal(1, first.Wides);
            Assert.Equal(2, first.NoBalls);
            Assert.Equal(10, first.RunsConceded);
            Assert.Equal("Bowler two", FirstInnings.OversBowled[1].Bowler?.PlayerIdentityName);
            Assert.Equal(2, FirstInnings.OversBowled[1].OverNumber);
        }

        [Fact]
        public async Task Over_sets_are_scaffolded_with_12_overs_by_default()
        {
            await UpdateMatch(PostedData());

            _overSetScaffolder.Verify(x => x.ScaffoldOverSets(FirstInnings.OverSets, 12, FirstInnings.OversBowled), Times.Once);
        }

        [Fact]
        public async Task Bowling_figures_are_calculated_for_the_current_innings()
        {
            await UpdateMatch(PostedData());

            _bowlingFiguresCalculator.Verify(x => x.CalculateBowlingFigures(FirstInnings), Times.Once);
            Assert.Equal(_bowlingFigures, FirstInnings.BowlingFigures);
        }

        [Fact]
        public async Task Valid_request_updates_bowling_scorecard_and_clears_cache_for_bowling_team()
        {
            await UpdateMatch(PostedData(new OverViewModel { BowledBy = "Bowler one" }));

            _matchRepository.Verify(x => x.UpdateBowlingScorecard(_match, FirstInnings.MatchInningsId!.Value, _currentMember.Key, _currentMember.Name!), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(_awayTeam.Team!), Times.Once);
        }

        [Fact]
        public async Task Valid_request_for_innings_before_the_last_redirects_to_next_batting_scorecard()
        {
            var result = await UpdateMatch(PostedData());

            Assert.Equal(_match.MatchRoute + "/edit/innings/2/batting", Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Valid_request_for_last_innings_redirects_to_close_of_play()
        {
            _matchInningsUrlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(2);

            var result = await UpdateMatch(PostedData());

            _matchRepository.Verify(x => x.UpdateBowlingScorecard(_match, _match.MatchInnings[1].MatchInningsId!.Value, _currentMember.Key, _currentMember.Name!), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(_homeTeam.Team!), Times.Once);
            Assert.Equal(_match.MatchRoute + "/edit/close-of-play", Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating()
        {
            SetupAuthorization(false);
            var postedData = PostedData(new OverViewModel { BowledBy = "Bowler one" });

            var (model, _) = await UpdateMatchExpectingView(postedData);

            Assert.Equal(_match, model.Match);
            Assert.Equal(1, model.InningsOrderInMatch);
            Assert.Equal(FirstInnings, model.CurrentInnings.MatchInnings);
            Assert.Equal(postedData.OversBowledSearch, model.CurrentInnings.OversBowledSearch);
            Assert.True(model.Autofocus);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult]);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_scaffolds_overs_bowled_to_the_total_overs_in_the_innings()
        {
            var (model, _) = await UpdateMatchExpectingView(PostedData(new OverViewModel { BowledBy = "Bowler one" }), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(10, model.CurrentInnings.MatchInnings.OversBowled.Count);
            Assert.Equal("Bowler one", model.CurrentInnings.MatchInnings.OversBowled[0].Bowler?.PlayerIdentityName);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_and_matches_breadcrumbs()
        {
            var (model, _) = await UpdateMatchExpectingView(PostedData(), c => c.ModelState.AddModelError("Any", "Any error"));

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

            var (model, _) = await UpdateMatchExpectingView(PostedData(), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.Equal(_match.Season.SeasonRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
