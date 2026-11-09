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
    public class CreateTrainingSessionSurfaceControllerTests : UmbracoBaseTest
    {
        private const string TEAM_PATH = "/teams/example-team/matches/add/training";
        private const string SEASON_PATH = "/competitions/example-league/2026/matches/add/training";

        private readonly Mock<IMemberManager> _memberManager = new();
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
            MatchTypes = [MatchType.TrainingSession],
            Competition = new Competition { CompetitionName = "Example league" }
        };
        private readonly Match _createdMatch = new() { MatchId = Guid.NewGuid(), MatchRoute = "/matches/created-match" };

        public CreateTrainingSessionSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(() => new FormCollection(_form));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(_team);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(_season);
            _seasonDataSource.Setup(x => x.ReadSeasonById(_season.SeasonId!.Value, false)).ReturnsAsync(_season);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _matchRepository.Setup(x => x.CreateMatch(It.IsAny<Match>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_createdMatch);
            _editMatchHelper.Setup(x => x.ConfigureModelFromRequestData(It.IsAny<IEditMatchViewModel>(), It.IsAny<IFormCollection>(), It.IsAny<ModelStateDictionary>()))
                .Callback((IEditMatchViewModel model, IFormCollection _, ModelStateDictionary _) => model.MatchDate = _postedMatchDate);
            SetupCurrentMember(Mock.Of<IMember>(x => x.Name == "Jo Bloggs"));
            SetupPath(TEAM_PATH);
        }

        private void SetupPath(string path) => Request.SetupGet(x => x.Path).Returns(new PathString(path));

        private CreateTrainingSessionSurfaceController CreateController()
        {
            return new CreateTrainingSessionSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
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

        private async Task<EditTrainingSessionViewModel> CreateMatchExpectingView(Match postedMatch, Action<CreateTrainingSessionSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.CreateMatch(postedMatch);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateTrainingSession", view.ViewName);
            return Assert.IsType<EditTrainingSessionViewModel>(view.Model);
        }

        private static void AddModelError(CreateTrainingSessionSurfaceController controller) => controller.ModelState.AddModelError("Any", "Any error");

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
        public async Task Match_type_is_set_to_training_session()
        {
            var postedMatch = new Match { MatchType = MatchType.LeagueMatch };

            await CreateMatch(postedMatch);

            Assert.Equal(MatchType.TrainingSession, postedMatch.MatchType);
        }

        [Fact]
        public async Task Teams_are_given_the_training_role()
        {
            var postedMatch = new Match { Teams = [new TeamInMatch { TeamRole = TeamRole.Home }, new TeamInMatch { TeamRole = TeamRole.Away }] };

            await CreateMatch(postedMatch);

            Assert.All(postedMatch.Teams, team => Assert.Equal(TeamRole.Training, team.TeamRole));
        }

        [Fact]
        public async Task Request_data_is_read_by_the_helper()
        {
            using var controller = CreateController();

            await controller.CreateMatch(new Match());

            _editMatchHelper.Verify(x => x.ConfigureModelFromRequestData(It.IsAny<EditTrainingSessionViewModel>(), It.IsAny<IFormCollection>(), controller.ModelState), Times.Once);
        }

        [Fact]
        public async Task Valid_request_creates_match_clears_cache_and_redirects_to_match()
        {
            var postedMatch = new Match();

            var result = await CreateMatch(postedMatch);

            _matchRepository.Verify(x => x.CreateMatch(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForMatch(_createdMatch), Times.Once);
            Assert.Equal(_createdMatch.MatchRoute, Assert.IsType<RedirectResult>(result).Url);
            _teamDataSource.Verify(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_to_add_another_redirects_back_to_form_with_confirmation()
        {
            _form["AddAnother"] = "true";

            var result = await CreateMatch(new Match());

            Assert.Equal(TEAM_PATH + "?confirm=" + _createdMatch.MatchRoute, Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Posted_season_without_an_id_is_removed()
        {
            var postedMatch = new Match { Season = new Season() };

            await CreateMatch(postedMatch);

            Assert.Null(postedMatch.Season);
            _seasonDataSource.Verify(x => x.ReadSeasonById(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Posted_season_with_an_id_is_read_from_the_data_source()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            await CreateMatch(postedMatch);

            Assert.Equal(_season, postedMatch.Season);
            _matchRepository.Verify(x => x.CreateMatch(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Season_route_creates_match_in_season_from_route()
        {
            SetupPath(SEASON_PATH);
            var postedMatch = new Match { Season = new Season { SeasonId = Guid.NewGuid() } };

            await CreateMatch(postedMatch);

            Assert.Equal(_season, postedMatch.Season);
            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(SEASON_PATH, false), Times.Once);
            _seasonDataSource.Verify(x => x.ReadSeasonById(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
            _matchRepository.Verify(x => x.CreateMatch(postedMatch, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Season_that_does_not_allow_training_sessions_returns_view_without_creating_match()
        {
            SetupPath(SEASON_PATH);
            _season.MatchTypes.Clear();
            _season.MatchTypes.Add(MatchType.LeagueMatch);

            await CreateMatchExpectingView(new Match());

            VerifyNothingCreated();
        }

        [Fact]
        public async Task Match_is_validated()
        {
            var postedMatch = new Match { Season = new Season { SeasonId = _season.SeasonId } };

            using var controller = CreateController();

            await controller.CreateMatch(postedMatch);

            _matchValidator.Verify(x => x.DateIsValidForSqlServer(_postedMatchDate, controller.ModelState, "MatchDate", "training session"), Times.Once);
            _matchValidator.Verify(x => x.DateIsWithinTheSeason(_postedMatchDate, _season, controller.ModelState, "MatchDate", "training session"), Times.Once);
            _matchValidator.Verify(x => x.AtLeastOneTeamInMatch(postedMatch.Teams, controller.ModelState), Times.Once);
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
        public async Task Invalid_ModelState_for_team_returns_team_possible_seasons_page_title_and_breadcrumbs()
        {
            var possibleSeasons = new List<Season> { _season };
            var possibleSeasonListItems = new List<SelectListItem> { new() { Text = "Example league, 2026 season" } };
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(_team.Seasons, MatchType.TrainingSession)).Returns(possibleSeasons);
            _editMatchHelper.Setup(x => x.PossibleSeasonsAsListItems(possibleSeasons)).Returns(possibleSeasonListItems);
            var postedMatch = new Match();

            var model = await CreateMatchExpectingView(postedMatch, AddModelError);

            Assert.Equal(postedMatch, model.Match);
            Assert.Equal(_team, model.Team);
            _teamDataSource.Verify(x => x.ReadTeamByRoute(TEAM_PATH, true), Times.Once);
            Assert.Equal(possibleSeasonListItems, model.PossibleSeasons);
            Assert.Equal("Add a training session for Example team", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch]);
            Assert.Equal(Constants.Pages.MatchesUrl, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_season_returns_season_and_page_title()
        {
            SetupPath(SEASON_PATH);

            var model = await CreateMatchExpectingView(new Match(), AddModelError);

            Assert.Equal(_season, model.Season);
            Assert.Null(model.Team);
            Assert.Equal("Add a training session in the Example league, 2026 season", model.Metadata.PageTitle);
            VerifyNothingCreated();
        }
    }
}
