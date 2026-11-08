using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
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
    public class EditStartOfPlaySurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Match>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Dictionary<string, StringValues> _form = [];
        private readonly TeamInMatch _homeTeam = new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Home, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Home team" } };
        private readonly TeamInMatch _awayTeam = new() { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Away, Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Away team" } };
        private readonly Match _match;
        private readonly Match _updatedMatch = new() { MatchRoute = "/matches/updated-match" };

        public EditStartOfPlaySurfaceControllerTests()
        {
            _match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "Example match",
                MatchRoute = "/matches/example-match",
                MatchType = MatchType.LeagueMatch,
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                Teams = [_homeTeam, _awayTeam]
            };

            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match/edit/start-of-play"));
            Request.SetupGet(x => x.Form).Returns(() => new FormCollection(_form));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(_match);
            SetupAuthorization(true);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchRepository.Setup(x => x.UpdateStartOfPlay(It.IsAny<Match>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedMatch);
            _dateTimeFormatter.Setup(x => x.FormatDate(It.IsAny<DateTimeOffset>(), false, false, false)).Returns("1 June 2026");
        }

        private void SetupAuthorization(bool isAuthorized) =>
            _authorizationPolicy.Setup(x => x.IsAuthorized(_match)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchResult, isAuthorized } });

        private EditStartOfPlaySurfaceController CreateController()
        {
            return new EditStartOfPlaySurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _matchDataSource.Object,
                _matchRepository.Object,
                _seasonDataSource.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object,
                _editMatchHelper.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<IActionResult> UpdateMatch(Match? postedMatch = null)
        {
            using var controller = CreateController();
            return await controller.UpdateMatch(postedMatch ?? new Match());
        }

        private async Task<(EditStartOfPlayViewModel model, ModelStateDictionary modelState)> UpdateMatchExpectingView(Match? postedMatch = null, Action<EditStartOfPlaySurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.UpdateMatch(postedMatch ?? new Match());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditStartOfPlay", view.ViewName);
            return (Assert.IsType<EditStartOfPlayViewModel>(view.Model), controller.ModelState);
        }

        private static void AddModelError(EditStartOfPlaySurfaceController controller) => controller.ModelState.AddModelError("Any", "Any error");

        private void VerifyNotUpdated()
        {
            _matchRepository.Verify(x => x.UpdateStartOfPlay(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(It.IsAny<Match>(), It.IsAny<Match>()), Times.Never);
        }

        private Season SetupKnockoutMatchInSeason(params Team[] teamsInSeason)
        {
            _match.MatchType = MatchType.KnockoutMatch;
            _match.Season = new Season
            {
                FromYear = 2026,
                UntilYear = 2026,
                SeasonRoute = "/competitions/example-cup/2026",
                Competition = new Competition { CompetitionName = "Example cup", CompetitionRoute = "/competitions/example-cup" }
            };
            var season = new Season { SeasonRoute = _match.Season.SeasonRoute, Teams = teamsInSeason.Select(t => new TeamInSeason { Team = t }).ToList() };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(_match.Season.SeasonRoute, true)).ReturnsAsync(season);
            return season;
        }

        [Fact]
        public async Task Null_posted_match_is_treated_as_an_empty_match()
        {
            _match.MatchResultType = MatchResultType.HomeWin;

            using var controller = CreateController();

            var result = await controller.UpdateMatch(null!);

            Assert.IsType<RedirectResult>(result);
            Assert.Null(_match.MatchResultType);
        }

        [Fact]
        public async Task Match_in_the_future_returns_404()
        {
            _match.StartTime = DateTimeOffset.UtcNow.AddDays(1);

            Assert.IsType<NotFoundResult>(await UpdateMatch());
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Tournament_match_returns_404()
        {
            _match.Tournament = new Tournament();

            Assert.IsType<NotFoundResult>(await UpdateMatch());
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Match_result_type_is_copied_from_posted_match()
        {
            await UpdateMatch(new Match { MatchResultType = MatchResultType.AwayWin });

            Assert.Equal(MatchResultType.AwayWin, _match.MatchResultType);
        }

        [Fact]
        public async Task Knockout_match_with_missing_teams_gets_possible_teams_and_posted_teams_from_the_season()
        {
            _match.Teams.Clear();
            var homeTeam = new Team { TeamId = Guid.NewGuid(), TeamName = "Season team one" };
            var awayTeam = new Team { TeamId = Guid.NewGuid(), TeamName = "Season team two" };
            var season = SetupKnockoutMatchInSeason(homeTeam, awayTeam);
            var possibleTeams = new List<SelectListItem> { new() { Text = "Season team one" } };
            _editMatchHelper.Setup(x => x.PossibleTeamsAsListItems(season.Teams)).Returns(possibleTeams);
            _form["HomeTeamId"] = homeTeam.TeamId.ToString();
            _form["AwayTeamId"] = awayTeam.TeamId.ToString();

            var (model, _) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Equal(possibleTeams, model.PossibleHomeTeams);
            Assert.Equal(possibleTeams, model.PossibleAwayTeams);
            Assert.Equal(homeTeam.TeamId, model.HomeTeamId);
            Assert.Equal("Season team one", model.HomeTeamName);
            Assert.Equal(awayTeam.TeamId, model.AwayTeamId);
            Assert.Equal("Season team two", model.AwayTeamName);
        }

        [Fact]
        public async Task Knockout_match_with_missing_teams_saves_posted_teams_from_the_season()
        {
            _match.Teams.Remove(_awayTeam);
            var awayTeam = new Team { TeamId = Guid.NewGuid(), TeamName = "Season team" };
            SetupKnockoutMatchInSeason(_homeTeam.Team!, awayTeam);
            _form["AwayTeamId"] = awayTeam.TeamId.ToString();

            await UpdateMatch();

            Assert.Equal(2, _match.Teams.Count);
            var addedTeam = _match.Teams.Single(x => x.TeamRole == TeamRole.Away);
            Assert.Equal(awayTeam, addedTeam.Team);
            Assert.Equal("Season team", addedTeam.PlayingAsTeamName);
            _matchRepository.Verify(x => x.UpdateStartOfPlay(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Knockout_match_ignores_posted_team_not_in_the_season()
        {
            _match.Teams.Remove(_awayTeam);
            SetupKnockoutMatchInSeason(_homeTeam.Team!);
            _form["AwayTeamId"] = Guid.NewGuid().ToString();

            await UpdateMatch();

            Assert.Equal(_homeTeam, Assert.Single(_match.Teams));
        }

        [Fact]
        public async Task Knockout_match_with_both_teams_does_not_read_the_season()
        {
            SetupKnockoutMatchInSeason();

            await UpdateMatch();

            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Knockout_match_with_the_same_team_twice_is_a_model_error()
        {
            _match.MatchType = MatchType.KnockoutMatch;
            _awayTeam.Team = _homeTeam.Team;

            var (_, modelState) = await UpdateMatchExpectingView();

            Assert.Contains(modelState["AwayTeamId"]!.Errors, x => x.ErrorMessage == "The away team cannot be the same as the home team");
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Friendly_match_with_missing_teams_adds_posted_teams()
        {
            _match.MatchType = MatchType.FriendlyMatch;
            _match.Teams.Clear();
            var homeTeamId = Guid.NewGuid();
            var awayTeamId = Guid.NewGuid();
            _form["HomeTeamId"] = homeTeamId.ToString();
            _form["HomeTeamName"] = "Posted home team";
            _form["AwayTeamId"] = awayTeamId.ToString();

            await UpdateMatch();

            var home = _match.Teams.Single(x => x.TeamRole == TeamRole.Home);
            Assert.Equal(homeTeamId, home.Team?.TeamId);
            Assert.Equal("Posted home team", home.Team?.TeamName);
            Assert.Equal("Posted home team", home.PlayingAsTeamName);
            var away = _match.Teams.Single(x => x.TeamRole == TeamRole.Away);
            Assert.Equal(awayTeamId, away.Team?.TeamId);
            Assert.Null(away.PlayingAsTeamName);
        }

        [Fact]
        public async Task Friendly_match_does_not_replace_existing_teams()
        {
            _match.MatchType = MatchType.FriendlyMatch;
            _form["HomeTeamId"] = Guid.NewGuid().ToString();
            _form["AwayTeamId"] = Guid.NewGuid().ToString();

            await UpdateMatch();

            Assert.Equal([_homeTeam, _awayTeam], _match.Teams);
        }

        [Fact]
        public async Task Unsaved_teams_are_moved_to_the_view_model_when_returning_the_view()
        {
            _match.MatchType = MatchType.FriendlyMatch;
            _match.Teams.Remove(_awayTeam);
            var awayTeamId = Guid.NewGuid();
            _form["AwayTeamId"] = awayTeamId.ToString();
            _form["AwayTeamName"] = "Posted away team";

            var (model, _) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Equal(awayTeamId, model.AwayTeamId);
            Assert.Equal("Posted away team", model.AwayTeamName);
            Assert.Null(model.HomeTeamId);
            Assert.Equal(_homeTeam, Assert.Single(model.Match!.Teams));
        }

        [Fact]
        public async Task Match_location_is_read_from_form()
        {
            var locationId = Guid.NewGuid();
            _form["MatchLocationId"] = locationId.ToString();
            _form["MatchLocationName"] = "Example ground";

            var (model, _) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Equal(locationId, model.MatchLocationId);
            Assert.Equal("Example ground", model.MatchLocationName);
            Assert.Equal(locationId, _match.MatchLocation?.MatchLocationId);
        }

        [Fact]
        public async Task No_match_location_in_form_leaves_location_unchanged()
        {
            var location = new MatchLocation { MatchLocationId = Guid.NewGuid() };
            _match.MatchLocation = location;
            _form["MatchLocationId"] = string.Empty;

            await UpdateMatch();

            Assert.Equal(location, _match.MatchLocation);
        }

        [Theory]
        [InlineData("Home", true, false)]
        [InlineData("Away", false, true)]
        public async Task Toss_won_by_team_role_sets_WonToss(string tossWonBy, bool homeWonToss, bool awayWonToss)
        {
            _form["TossWonBy"] = tossWonBy;

            await UpdateMatch();

            Assert.Equal(homeWonToss, _homeTeam.WonToss);
            Assert.Equal(awayWonToss, _awayTeam.WonToss);
        }

        [Fact]
        public async Task Toss_won_by_match_team_id_sets_WonToss()
        {
            _form["TossWonBy"] = _awayTeam.MatchTeamId.ToString();

            await UpdateMatch();

            Assert.False(_homeTeam.WonToss);
            Assert.True(_awayTeam.WonToss);
        }

        [Fact]
        public async Task No_toss_winner_sets_WonToss_to_null()
        {
            _homeTeam.WonToss = true;
            _awayTeam.WonToss = false;

            await UpdateMatch();

            Assert.Null(_homeTeam.WonToss);
            Assert.Null(_awayTeam.WonToss);
        }

        [Theory]
        [InlineData("Home", true, false)]
        [InlineData("Away", false, true)]
        public async Task Batted_first_by_team_role_sets_BattedFirst_and_innings_order(string battedFirst, bool homeBattedFirst, bool awayBattedFirst)
        {
            _form["BattedFirst"] = battedFirst;

            await UpdateMatch();

            Assert.True(_match.InningsOrderIsKnown);
            Assert.Equal(homeBattedFirst, _homeTeam.BattedFirst);
            Assert.Equal(awayBattedFirst, _awayTeam.BattedFirst);
        }

        [Fact]
        public async Task Batted_first_by_match_team_id_sets_BattedFirst()
        {
            _form["BattedFirst"] = _awayTeam.MatchTeamId.ToString();

            await UpdateMatch();

            Assert.True(_match.InningsOrderIsKnown);
            Assert.False(_homeTeam.BattedFirst);
            Assert.True(_awayTeam.BattedFirst);
        }

        [Fact]
        public async Task No_team_batted_first_sets_BattedFirst_to_null_and_innings_order_unknown()
        {
            _match.InningsOrderIsKnown = true;
            _homeTeam.BattedFirst = true;
            _awayTeam.BattedFirst = false;

            await UpdateMatch();

            Assert.False(_match.InningsOrderIsKnown);
            Assert.Null(_homeTeam.BattedFirst);
            Assert.Null(_awayTeam.BattedFirst);
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin, MatchResultType.HomeWin)]
        [InlineData(MatchResultType.AwayWin, MatchResultType.AwayWin)]
        [InlineData(MatchResultType.Tie, MatchResultType.Tie)]
        [InlineData(MatchResultType.Postponed, null)]
        [InlineData(MatchResultType.Cancelled, null)]
        [InlineData(MatchResultType.HomeWinByForfeit, null)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndPostponed, null)]
        public async Task Match_went_ahead_resets_result_types_for_matches_that_did_not_go_ahead(MatchResultType posted, MatchResultType? expected)
        {
            _form["MatchWentAhead"] = "true";

            await UpdateMatch(new Match { MatchResultType = posted });

            Assert.Equal(expected, _match.MatchResultType);
        }

        [Fact]
        public async Task Match_did_not_go_ahead_resets_toss_and_batted_first_in_view_model()
        {
            _form["MatchWentAhead"] = "false";
            _form["TossWonBy"] = "Home";
            _form["BattedFirst"] = "Home";

            var (model, _) = await UpdateMatchExpectingView(new Match { MatchResultType = MatchResultType.Cancelled }, AddModelError);

            Assert.False(model.MatchWentAhead);
            Assert.Null(model.TossWonBy);
            Assert.Null(model.BattedFirst);
        }

        [Theory]
        [InlineData("Home")]
        [InlineData("Away")]
        public async Task Match_did_not_go_ahead_saves_without_toss_winner_or_batting_order(string selectedTeam)
        {
            _form["MatchWentAhead"] = "false";
            _form["TossWonBy"] = selectedTeam;
            _form["BattedFirst"] = selectedTeam;

            var result = await UpdateMatch(new Match { MatchResultType = MatchResultType.Cancelled });

            Assert.IsType<RedirectResult>(result);
            Assert.False(_match.InningsOrderIsKnown);
            Assert.All(_match.Teams, team =>
            {
                Assert.Null(team.WonToss);
                Assert.Null(team.BattedFirst);
            });
            _matchRepository.Verify(x => x.UpdateStartOfPlay(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Match_did_not_go_ahead_without_a_reason_is_a_model_error()
        {
            _form["MatchWentAhead"] = "false";

            var (_, modelState) = await UpdateMatchExpectingView();

            Assert.Contains(modelState["Match.MatchResultType"]!.Errors, x => x.ErrorMessage == "Select why the match didn't go ahead.");
            VerifyNotUpdated();
        }

        [Fact]
        public async Task No_selection_for_whether_match_went_ahead_is_not_validated_here()
        {
            var (model, modelState) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Null(model.MatchWentAhead);
            Assert.False(modelState.ContainsKey("Match.MatchResultType"));
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("not-a-bool", true)]
        public async Task HasScorecard_is_read_from_form_when_match_went_ahead_is_set(string hasScorecard, bool expected)
        {
            _form["MatchWentAhead"] = "true";
            _form["HasScorecard"] = hasScorecard;

            var (model, _) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Equal(expected, model.HasScorecard);
        }

        [Theory]
        [InlineData(MatchResultType.HomeWinByForfeit)]
        [InlineData(MatchResultType.AwayWinByForfeit)]
        [InlineData(MatchResultType.Postponed)]
        [InlineData(MatchResultType.Cancelled)]
        public async Task Valid_request_for_match_without_scorecard_redirects_to_match(MatchResultType resultType)
        {
            _form["MatchWentAhead"] = "false";

            var result = await UpdateMatch(new Match { MatchResultType = resultType });

            _matchRepository.Verify(x => x.UpdateStartOfPlay(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(_match, _updatedMatch), Times.Once);
            Assert.Equal(_updatedMatch.MatchRoute, Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Valid_request_with_scorecard_redirects_to_first_batting_scorecard()
        {
            _form["MatchWentAhead"] = "true";
            _form["HasScorecard"] = "true";

            var result = await UpdateMatch();

            _matchRepository.Verify(x => x.UpdateStartOfPlay(_match, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(_match, _updatedMatch), Times.Once);
            Assert.Equal(_updatedMatch.MatchRoute + "/edit/innings/1/batting", Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Valid_request_without_scorecard_redirects_to_close_of_play()
        {
            _form["MatchWentAhead"] = "true";
            _form["HasScorecard"] = "false";

            var result = await UpdateMatch();

            Assert.Equal(_updatedMatch.MatchRoute + "/edit/close-of-play", Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating()
        {
            SetupAuthorization(false);

            var (model, _) = await UpdateMatchExpectingView();

            Assert.Equal(_match, model.Match);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult]);
            VerifyNotUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_original_match_name_page_title_and_matches_breadcrumbs()
        {
            var (model, _) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Equal("Example match", model.Match!.MatchName);
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

            var (model, _) = await UpdateMatchExpectingView(arrange: AddModelError);

            Assert.Equal([Constants.Pages.Competitions, "Example league", "2026 season", "Example match"], model.Breadcrumbs.Select(x => x.Name).TakeLast(4));
            Assert.Equal(_match.Season.SeasonRoute, model.Breadcrumbs[^2].Url?.ToString());
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Url?.ToString() == Constants.Pages.MatchesUrl);
        }
    }
}
