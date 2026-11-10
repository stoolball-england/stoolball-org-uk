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
    public class EditBattingScorecardSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IMatchInningsUrlParser> _matchInningsUrlParser = new();
        private readonly Mock<IPlayerInningsScaffolder> _playerInningsScaffolder = new();
        private readonly Mock<IBowlingFiguresCalculator> _bowlingFiguresCalculator = new();
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly TeamInMatch _battingTeam = new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Home, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Home team" } };
        private readonly TeamInMatch _bowlingTeam = new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Away, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Away team" } };
        private readonly List<BowlingFigures> _bowlingFigures = [new BowlingFigures()];
        private readonly Match _match;

        public EditBattingScorecardSurfaceControllerTests()
        {
            _match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "Example match",
                MatchRoute = "/matches/example-match",
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                Teams = [_battingTeam, _bowlingTeam],
                MatchInnings =
                [
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 1, BattingTeam = _battingTeam, BowlingTeam = _bowlingTeam, OverSets = [new OverSet { Overs = 6 }, new OverSet { Overs = 4 }] },
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 2, BattingTeam = _bowlingTeam, BowlingTeam = _battingTeam, OverSets = [new OverSet { Overs = 10 }] }
                ]
            };

            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match/edit/innings/1/batting"));
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

        private EditBattingScorecardSurfaceController CreateController()
        {
            return new EditBattingScorecardSurfaceController(
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
                _playerInningsScaffolder.Object,
                _bowlingFiguresCalculator.Object,
                _playerCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static MatchInningsViewModel PostedData(params PlayerInningsViewModel[] playerInnings)
        {
            var postedData = new MatchInningsViewModel();
            postedData.PlayerInningsSearch.AddRange(playerInnings);
            return postedData;
        }

        private async Task<EditScorecardViewModel> UpdateMatchExpectingView(MatchInningsViewModel postedData, Action<EditBattingScorecardSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.UpdateMatch(postedData);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditBattingScorecard", view.ViewName);
            return Assert.IsType<EditScorecardViewModel>(view.Model);
        }

        private async Task<IActionResult> UpdateMatch(MatchInningsViewModel postedData)
        {
            using var controller = CreateController();
            return await controller.UpdateMatch(postedData);
        }

        private void VerifyNotUpdated()
        {
            _matchRepository.Verify(x => x.UpdateBattingScorecard(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(It.IsAny<IEnumerable<TeamInMatch>>()), Times.Never);
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

            _matchInningsUrlParser.Verify(x => x.ParseInningsOrderInMatchFromUrl(It.Is<Uri>(u => u.ToString() == "/matches/example-match/edit/innings/1/batting")), Times.Once);
        }

        [Fact]
        public async Task Posted_player_innings_are_trimmed_and_assigned_to_batting_and_bowling_teams()
        {
            await UpdateMatch(PostedData(new PlayerInningsViewModel
            {
                Batter = " Batter one ",
                DismissalType = DismissalType.Caught,
                DismissedBy = " Fielder one ",
                Bowler = " Bowler one ",
                RunsScored = 12,
                BallsFaced = 20
            }));

            var playerInnings = Assert.Single(FirstInnings.PlayerInnings);
            Assert.Equal("Batter one", playerInnings.Batter?.PlayerIdentityName);
            Assert.Equal(_battingTeam.Team, playerInnings.Batter?.Team);
            Assert.Equal(DismissalType.Caught, playerInnings.DismissalType);
            Assert.Equal("Fielder one", playerInnings.DismissedBy?.PlayerIdentityName);
            Assert.Equal(_bowlingTeam.Team, playerInnings.DismissedBy?.Team);
            Assert.Equal("Bowler one", playerInnings.Bowler?.PlayerIdentityName);
            Assert.Equal(_bowlingTeam.Team, playerInnings.Bowler?.Team);
            Assert.Equal(12, playerInnings.RunsScored);
            Assert.Equal(20, playerInnings.BallsFaced);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" ")]
        public async Task Blank_fielder_and_bowler_are_null(string? name)
        {
            await UpdateMatch(PostedData(new PlayerInningsViewModel { Batter = "Batter one", DismissedBy = name, Bowler = name }));

            var playerInnings = Assert.Single(FirstInnings.PlayerInnings);
            Assert.Null(playerInnings.DismissedBy);
            Assert.Null(playerInnings.Bowler);
        }

        [Fact]
        public async Task Player_innings_without_a_batter_are_removed_when_ModelState_is_valid()
        {
            await UpdateMatch(PostedData(new PlayerInningsViewModel { Batter = "Batter one" }, new PlayerInningsViewModel { Batter = " " }, new PlayerInningsViewModel()));

            Assert.Equal("Batter one", Assert.Single(FirstInnings.PlayerInnings).Batter?.PlayerIdentityName);
        }

        [Fact]
        public async Task Player_innings_without_a_batter_are_kept_when_ModelState_is_invalid()
        {
            await UpdateMatchExpectingView(PostedData(new PlayerInningsViewModel { Batter = "Batter one" }, new PlayerInningsViewModel { RunsScored = 4 }),
                c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(2, FirstInnings.PlayerInnings.Count);
            Assert.Equal(4, FirstInnings.PlayerInnings[1].RunsScored);
        }

        [Fact]
        public async Task Extras_and_totals_are_copied_from_posted_data()
        {
            var postedData = PostedData();
            postedData.MatchInnings = new MatchInnings { Byes = 1, Wides = 2, NoBalls = 3, BonusOrPenaltyRuns = 4, Runs = 100, Wickets = 5 };

            await UpdateMatch(postedData);

            Assert.Equal(1, FirstInnings.Byes);
            Assert.Equal(2, FirstInnings.Wides);
            Assert.Equal(3, FirstInnings.NoBalls);
            Assert.Equal(4, FirstInnings.BonusOrPenaltyRuns);
            Assert.Equal(100, FirstInnings.Runs);
            Assert.Equal(5, FirstInnings.Wickets);
        }

        [Fact]
        public async Task Missing_players_per_team_defaults_to_11_for_scaffolding()
        {
            await UpdateMatch(PostedData());

            Assert.Equal(11, _match.PlayersPerTeam);
            _playerInningsScaffolder.Verify(x => x.ScaffoldPlayerInnings(FirstInnings.PlayerInnings, 11), Times.Once);
        }

        [Fact]
        public async Task Players_per_team_from_the_match_is_used_for_scaffolding()
        {
            _match.PlayersPerTeam = 8;

            await UpdateMatch(PostedData());

            _playerInningsScaffolder.Verify(x => x.ScaffoldPlayerInnings(FirstInnings.PlayerInnings, 8), Times.Once);
        }

        [Fact]
        public async Task Players_per_team_is_increased_to_the_number_of_posted_batters()
        {
            _match.PlayersPerTeam = 2;

            await UpdateMatch(PostedData(
                new PlayerInningsViewModel { Batter = "Batter one" },
                new PlayerInningsViewModel { Batter = "Batter two" },
                new PlayerInningsViewModel { Batter = "Batter three" }));

            Assert.Equal(3, _match.PlayersPerTeam);
            _playerInningsScaffolder.Verify(x => x.ScaffoldPlayerInnings(FirstInnings.PlayerInnings, 3), Times.Once);
        }

        [Fact]
        public async Task Batter_who_bats_twice_counts_twice_towards_players_per_team()
        {
            _match.PlayersPerTeam = 2;

            await UpdateMatch(PostedData(
                new PlayerInningsViewModel { Batter = "Batter one" },
                new PlayerInningsViewModel { Batter = "Batter two" },
                new PlayerInningsViewModel { Batter = "Batter one" }));

            Assert.Equal(3, _match.PlayersPerTeam);
            _playerInningsScaffolder.Verify(x => x.ScaffoldPlayerInnings(FirstInnings.PlayerInnings, 3), Times.Once);
        }

        [Fact]
        public async Task Posted_rows_without_a_batter_count_towards_players_per_team_when_ModelState_is_valid()
        {
            _match.PlayersPerTeam = 1;

            await UpdateMatch(PostedData(
                new PlayerInningsViewModel { Batter = "Batter one" },
                new PlayerInningsViewModel(),
                new PlayerInningsViewModel()));

            Assert.Equal(3, _match.PlayersPerTeam);
            _playerInningsScaffolder.Verify(x => x.ScaffoldPlayerInnings(FirstInnings.PlayerInnings, 3), Times.Once);
        }

        [Fact]
        public async Task Posted_rows_without_a_batter_count_towards_players_per_team_when_ModelState_is_invalid()
        {
            _match.PlayersPerTeam = 1;

            await UpdateMatchExpectingView(PostedData(
                new PlayerInningsViewModel { Batter = "Batter one" },
                new PlayerInningsViewModel { RunsScored = 4 },
                new PlayerInningsViewModel()),
                c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(3, _match.PlayersPerTeam);
        }

        [Fact]
        public async Task Bowling_figures_are_calculated_for_the_current_innings()
        {
            await UpdateMatch(PostedData());

            _bowlingFiguresCalculator.Verify(x => x.CalculateBowlingFigures(FirstInnings), Times.Once);
            Assert.Equal(_bowlingFigures, FirstInnings.BowlingFigures);
        }

        [Fact]
        public async Task Valid_request_updates_batting_scorecard_clears_cache_and_redirects_to_bowling_scorecard()
        {
            var result = await UpdateMatch(PostedData(new PlayerInningsViewModel { Batter = "Batter one" }));

            _matchRepository.Verify(x => x.UpdateBattingScorecard(_match, FirstInnings.MatchInningsId!.Value, _currentMember.Key, _currentMember.Name!), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(_match.Teams), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_match.MatchRoute + "/edit/innings/1/bowling", redirect.Url);
        }

        [Fact]
        public async Task Valid_request_for_second_innings_updates_that_innings()
        {
            _matchInningsUrlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(2);

            var result = await UpdateMatch(PostedData(new PlayerInningsViewModel { Batter = "Batter one" }));

            Assert.Equal(_bowlingTeam.Team, Assert.Single(_match.MatchInnings[1].PlayerInnings).Batter?.Team);
            _matchRepository.Verify(x => x.UpdateBattingScorecard(_match, _match.MatchInnings[1].MatchInningsId!.Value, _currentMember.Key, _currentMember.Name!), Times.Once);
            Assert.Equal(_match.MatchRoute + "/edit/innings/2/bowling", Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating()
        {
            SetupAuthorization(false);

            var model = await UpdateMatchExpectingView(PostedData());

            Assert.Equal(_match, model.Match);
            Assert.Equal(1, model.InningsOrderInMatch);
            Assert.Equal(FirstInnings, model.CurrentInnings.MatchInnings);
            Assert.True(model.Autofocus);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult]);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_player_innings_as_search_view_models()
        {
            _playerInningsScaffolder.Setup(x => x.ScaffoldPlayerInnings(It.IsAny<List<PlayerInnings>>(), It.IsAny<int>()))
                .Callback((List<PlayerInnings> playerInnings, int _) => playerInnings.Add(new PlayerInnings()));

            var model = await UpdateMatchExpectingView(PostedData(new PlayerInningsViewModel
            {
                Batter = "Batter one",
                DismissalType = DismissalType.Bowled,
                DismissedBy = "Fielder one",
                Bowler = "Bowler one",
                RunsScored = 12,
                BallsFaced = 20
            }), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(2, model.CurrentInnings.PlayerInningsSearch.Count);
            var first = model.CurrentInnings.PlayerInningsSearch[0];
            Assert.Equal("Batter one", first.Batter);
            Assert.Equal(DismissalType.Bowled, first.DismissalType);
            Assert.Equal("Fielder one", first.DismissedBy);
            Assert.Equal("Bowler one", first.Bowler);
            Assert.Equal(12, first.RunsScored);
            Assert.Equal(20, first.BallsFaced);
            Assert.Null(model.CurrentInnings.PlayerInningsSearch[1].Batter);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_scaffolds_overs_bowled_to_the_total_overs_in_the_innings()
        {
            var model = await UpdateMatchExpectingView(PostedData(), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(10, model.CurrentInnings.MatchInnings.OversBowled.Count);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_and_matches_breadcrumbs()
        {
            var model = await UpdateMatchExpectingView(PostedData(), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal("Edit Example match, 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Matches, "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(Constants.Pages.MatchesUrl, model.Breadcrumbs[^2].Url?.ToString());
            Assert.Equal(_match.MatchRoute, model.Breadcrumbs.Last().Url?.ToString());
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

            var model = await UpdateMatchExpectingView(PostedData(), c => c.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.Equal(_match.Season.SeasonRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
