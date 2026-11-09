using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;
using Match = Stoolball.Matches.Match;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateKnockoutMatchSurfaceControllerTests : UmbracoBaseTest
    {
        private const string TEAM_PATH = "/teams/example-team/matches/add/knockout";
        private const string SEASON_PATH = "/competitions/example-league/2026/matches/add/knockout";

        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IStoolballEntityRouteParser> _routeParser = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ICreateMatchSeasonSelector> _createMatchSeasonSelector = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();
        private readonly Mock<IMatchValidator> _matchValidator = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Dictionary<string, StringValues> _form = [];
        private readonly DateTimeOffset _postedMatchDate = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        private readonly Team _team = new() { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example-team" };
        private readonly Season _season = new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            MatchTypes = [MatchType.KnockoutMatch],
            Teams = [new TeamInSeason { Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Team in season" } }],
            Competition = new Competition { CompetitionName = "Example league" }
        };
        private readonly Match _createdMatch = new() { MatchId = Guid.NewGuid(), MatchRoute = "/matches/created-match" };

        public CreateKnockoutMatchSurfaceControllerTests()
        {
            _routeParser.Setup(x => x.ParseRoute(TEAM_PATH)).Returns(StoolballEntityType.Team);
            _routeParser.Setup(x => x.ParseRoute(SEASON_PATH)).Returns(StoolballEntityType.Season);
            Request.SetupGet(x => x.Form).Returns(() => new FormCollection(_form));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(_team);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(_season);
            _seasonDataSource.Setup(x => x.ReadSeasonById(_season.SeasonId!.Value, false)).ReturnsAsync(_season);
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), It.IsAny<MatchType>())).Returns([_season]);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchRepository.Setup(x => x.CreateMatch(It.IsAny<Match>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_createdMatch);
            _editMatchHelper.Setup(x => x.ConfigureModelFromRequestData(It.IsAny<IEditMatchViewModel>(), It.IsAny<IFormCollection>(), It.IsAny<ModelStateDictionary>()))
                .Callback((IEditMatchViewModel model, IFormCollection _, ModelStateDictionary _) => model.MatchDate = _postedMatchDate);
            SetupCurrentMember(Mock.Of<IMember>(x => x.Name == "Jo Bloggs"));
            SetupPath(TEAM_PATH);
        }

        private void SetupPath(string path) => Request.SetupGet(x => x.Path).Returns(new PathString(path));

        private CreateKnockoutMatchSurfaceController CreateController()
        {
            return new CreateKnockoutMatchSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _routeParser.Object,
                _matchRepository.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object,
                _createMatchSeasonSelector.Object,
                _editMatchHelper.Object,
                _matchValidator.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<IActionResult> CreateMatch(Match postedMatch)
        {
            using var controller = CreateController();
            return await controller.CreateMatch(postedMatch);
        }

        private async Task<EditKnockoutMatchViewModel> CreateMatchExpectingView(Match postedMatch, Action<CreateKnockoutMatchSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.CreateMatch(postedMatch);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateKnockoutMatch", view.ViewName);
            return Assert.IsType<EditKnockoutMatchViewModel>(view.Model);
        }

        private static void AddModelError(CreateKnockoutMatchSurfaceController controller) => controller.ModelState.AddModelError("Any", "Any error");

        private void VerifyNothingCreated()
        {
            _matchRepository.Verify(x => x.CreateMatch(It.IsAny<Match>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(It.IsAny<Match>()), Times.Never);
        }

        [Fact]
        public async Task Null_match_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateMatch(null!));
        }

        [Fact]
        public async Task Match_type_is_set_to_knockout_match()
        {
            var postedMatch = new Match { MatchType = MatchType.FriendlyMatch };

            await CreateMatch(postedMatch);

            Assert.Equal(MatchType.KnockoutMatch, postedMatch.MatchType);
        }

        [Fact]
        public async Task Request_data_is_read_by_the_helper()
        {
            using var controller = CreateController();

            await controller.CreateMatch(new Match());

            _editMatchHelper.Verify(x => x.ConfigureModelFromRequestData(It.IsAny<EditKnockoutMatchViewModel>(), It.IsAny<IFormCollection>(), controller.ModelState), Times.Once);
        }

        [Fact]
        public async Task Valid_request_creates_match_clears_cache_and_redirects_to_match()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            var result = await CreateMatch(postedMatch);

            _matchRepository.Verify(x => x.CreateMatch(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(_createdMatch), Times.Once);
            Assert.Equal(_createdMatch.MatchRoute, Assert.IsType<RedirectResult>(result).Url);
            _editMatchHelper.Verify(x => x.ConfigureModelPossibleTeams(It.IsAny<IEditMatchViewModel>(), It.IsAny<IEnumerable<Season>>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_to_add_another_redirects_back_to_form_with_confirmation()
        {
            _form["AddAnother"] = "true";

            var result = await CreateMatch(new Match { Season = new Season { SeasonId = _season.SeasonId } });

            Assert.Equal(TEAM_PATH + "?confirm=" + _createdMatch.MatchRoute, Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Posted_season_with_an_id_is_read_from_the_data_source()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            await CreateMatch(postedMatch);

            Assert.Equal(_season, postedMatch.Season);
        }

        [Fact]
        public async Task Posted_season_without_an_id_is_not_read_from_the_data_source()
        {
            await CreateMatch(new Match { Season = new Season() });

            _seasonDataSource.Verify(x => x.ReadSeasonById(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Team_not_found_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(TEAM_PATH, true)).ReturnsAsync((Team?)null);

            var result = await CreateMatch(new Match { Season = new Season { SeasonId = _season.SeasonId } });

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Team_route_with_no_season_posted_is_a_model_error()
        {
            using var controller = CreateController();

            var result = await controller.CreateMatch(new Match());

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("Match.Season.SeasonId"));
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Team_route_with_a_season_the_team_cannot_add_a_knockout_match_to_is_a_model_error()
        {
            var otherSeason = new Season { SeasonId = Guid.NewGuid(), MatchTypes = [MatchType.LeagueMatch] };
            _seasonDataSource.Setup(x => x.ReadSeasonById(otherSeason.SeasonId!.Value, false)).ReturnsAsync(otherSeason);
            using var controller = CreateController();

            var result = await controller.CreateMatch(new Match { Season = new Season { SeasonId = otherSeason.SeasonId } });

            Assert.IsType<ViewResult>(result);
            Assert.Equal("Choose a season for the knockout match.", controller.ModelState["Match.Season.SeasonId"]?.Errors.Single().ErrorMessage);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Route_that_is_not_a_team_or_season_returns_404()
        {
            SetupPath("/matches/add");

            var result = await CreateMatch(new Match { Season = new Season { SeasonId = _season.SeasonId } });

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Season_route_does_not_read_the_team()
        {
            SetupPath(SEASON_PATH);

            await CreateMatch(new Match());

            _teamDataSource.Verify(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Season_route_creates_match_in_season_from_route()
        {
            SetupPath(SEASON_PATH);
            var postedMatch = new Match { Season = new Season { SeasonId = Guid.NewGuid() } };

            await CreateMatch(postedMatch);

            Assert.Equal(_season, postedMatch.Season);
            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(SEASON_PATH, true), Times.Once);
            _seasonDataSource.Verify(x => x.ReadSeasonById(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
            _matchRepository.Verify(x => x.CreateMatch(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Season_that_does_not_allow_knockout_matches_returns_404()
        {
            SetupPath(SEASON_PATH);
            _season.MatchTypes.Clear();
            _season.MatchTypes.Add(MatchType.FriendlyMatch);

            var result = await CreateMatch(new Match());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Season_not_found_returns_404()
        {
            SetupPath(SEASON_PATH);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(SEASON_PATH, true)).ReturnsAsync((Season?)null);

            var result = await CreateMatch(new Match());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Match_is_validated()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            using var controller = CreateController();

            await controller.CreateMatch(postedMatch);

            _matchValidator.Verify(x => x.DateIsValidForSqlServer(_postedMatchDate, controller.ModelState, "MatchDate", "match"), Times.Once);
            _matchValidator.Verify(x => x.DateIsWithinTheSeason(_postedMatchDate, _season, controller.ModelState, "MatchDate", "match"), Times.Once);
            _matchValidator.Verify(x => x.TeamsMustBeDifferent(It.Is<EditKnockoutMatchViewModel>(m => m.Match == postedMatch), controller.ModelState), Times.Once);
        }

        [Fact]
        public async Task Signed_out_member_returns_view_without_creating_match()
        {
            HttpContext.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));

            var model = await CreateMatchExpectingView(new Match());

            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch]);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_team_returns_team_possible_seasons_and_teams_page_title_and_breadcrumbs()
        {
            var otherSeason = new Season { SeasonId = Guid.NewGuid() };
            var possibleSeasonListItems = new List<SelectListItem> { new() { Text = "Example league, 2026 season" } };
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(_team.Seasons, MatchType.KnockoutMatch)).Returns([_season, otherSeason]);
            _editMatchHelper.Setup(x => x.PossibleSeasonsAsListItems(It.Is<IEnumerable<Season>>(s => s.SequenceEqual(new[] { _season, otherSeason })))).Returns(possibleSeasonListItems);
            var postedMatch = new Match();

            var model = await CreateMatchExpectingView(postedMatch, AddModelError);

            Assert.Equal(postedMatch, model.Match);
            Assert.Null(postedMatch.Season);
            Assert.Equal(_team, model.Team);
            _teamDataSource.Verify(x => x.ReadTeamByRoute(TEAM_PATH, true), Times.Once);
            Assert.Equal(possibleSeasonListItems, model.PossibleSeasons);
            _editMatchHelper.Verify(x => x.ConfigureModelPossibleTeams(model, It.Is<IEnumerable<Season>>(s => s.SequenceEqual(new[] { _season, otherSeason }))), Times.Once);
            Assert.Equal("Add a knockout match for Example team", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch]);
            Assert.Equal(Constants.Pages.MatchesUrl, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_team_with_one_possible_season_selects_that_season()
        {
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(_team.Seasons, MatchType.KnockoutMatch)).Returns([_season]);
            var postedMatch = new Match();

            await CreateMatchExpectingView(postedMatch, AddModelError);

            Assert.Equal(_season, postedMatch.Season);
        }

        [Fact]
        public async Task Invalid_ModelState_for_season_returns_season_possible_season_and_teams_and_page_title()
        {
            SetupPath(SEASON_PATH);
            var possibleSeasonListItems = new List<SelectListItem> { new() { Text = "Example league, 2026 season" } };
            var possibleTeamListItems = new List<SelectListItem> { new() { Text = "Team in season" } };
            _editMatchHelper.Setup(x => x.PossibleSeasonsAsListItems(It.Is<IEnumerable<Season>>(s => s.Single() == _season))).Returns(possibleSeasonListItems);
            _editMatchHelper.Setup(x => x.PossibleTeamsAsListItems(_season.Teams)).Returns(possibleTeamListItems);

            var model = await CreateMatchExpectingView(new Match(), AddModelError);

            Assert.Equal(_season, model.Season);
            Assert.Null(model.Team);
            Assert.Equal(possibleSeasonListItems, model.PossibleSeasons);
            Assert.Equal(possibleTeamListItems, model.PossibleHomeTeams);
            Assert.Equal(possibleTeamListItems, model.PossibleAwayTeams);
            Assert.Equal("Add a knockout match in the Example league, 2026 season", model.Metadata.PageTitle);
            _teamDataSource.Verify(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            VerifyNothingCreated();
        }
    }
}
