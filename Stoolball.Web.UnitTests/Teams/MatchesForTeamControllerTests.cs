using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class MatchesForTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IMatchFilterFactory> _matchFilterFactory = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<ICreateMatchSeasonSelector> _createMatchSeasonSelector = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();
        private readonly Mock<IAddMatchMenuViewModelFactory> _addMatchMenuViewModelFactory = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private MatchesForTeamController CreateController()
        {
            return new MatchesForTeamController(
                Mock.Of<ILogger<MatchesForTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _matchFilterFactory.Object,
                _matchListingDataSource.Object,
                Mock.Of<IDateTimeFormatter>(),
                _createMatchSeasonSelector.Object,
                _authorizationPolicy.Object,
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object,
                _addMatchMenuViewModelFactory.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example" };
            var defaultFilter = new MatchFilter();
            var appliedFilter = new MatchFilter();
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
            _matchFilterFactory.Setup(x => x.MatchesForTeams(It.IsAny<List<Guid>>())).Returns((defaultFilter, MatchSortOrder.MatchDateEarliestFirst));
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string?>())).Returns(appliedFilter);
            _matchListingDataSource.Setup(x => x.ReadMatchListings(appliedFilter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), It.IsAny<MatchType>())).Returns(new List<Season>());
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(appliedFilter)).Returns("Matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(appliedFilter)).Returns(string.Empty);
            _addMatchMenuViewModelFactory.Setup(x => x.CreateModel(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(new AddMatchMenuViewModel());
            return team;
        }

        private async Task<TeamViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel_with_team()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team, model.Team);
        }

        [Fact]
        public async Task Matches_are_read_using_the_default_filter_merged_with_the_query_string()
        {
            var team = SetupTeam();
            var defaultFilter = new MatchFilter();
            var appliedFilter = new MatchFilter();
            var matches = new List<MatchListing> { new() { MatchName = "Example match" } };
            _matchFilterFactory.Setup(x => x.MatchesForTeams(It.Is<List<Guid>>(ids => ids.Count == 1 && ids[0] == team.TeamId))).Returns((defaultFilter, MatchSortOrder.LatestUpdateFirst));
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string?>())).Returns(appliedFilter);
            _matchListingDataSource.Setup(x => x.ReadMatchListings(appliedFilter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(matches);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(appliedFilter)).Returns("Matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(appliedFilter)).Returns(string.Empty);

            var model = await ReadModel();

            Assert.Equal(defaultFilter, model.DefaultMatchFilter);
            Assert.Equal(appliedFilter, model.AppliedMatchFilter);
            Assert.Equal(matches, model.Matches.Matches);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var team = SetupTeam();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task Team_is_in_a_current_league_or_knockout_competition_if_seasons_are_available_to_create_matches(bool league, bool knockout)
        {
            SetupTeam();
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.LeagueMatch)).Returns(league ? [new Season()] : []);
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.KnockoutMatch)).Returns(knockout ? [new Season()] : []);

            var model = await ReadModel();

            Assert.Equal(league, model.IsInACurrentLeague);
            Assert.Equal(knockout, model.IsInACurrentKnockoutCompetition);
        }

        [Fact]
        public async Task Add_match_menu_is_created_from_the_team_and_its_competitions()
        {
            var team = SetupTeam();
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.LeagueMatch)).Returns([new Season()]);
            var menu = new AddMatchMenuViewModel();
            _addMatchMenuViewModelFactory.Setup(x => x.CreateModel(team.TeamRoute, false, true, true, false, true, true)).Returns(menu);

            var model = await ReadModel();

            Assert.Equal(menu, model.AddMatchMenu);
        }

        [Fact]
        public async Task Without_user_filter_the_page_title_has_no_filter_and_there_is_no_filter_description()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Matches for Example team stoolball team", model.Metadata.PageTitle);
            Assert.Null(model.FilterDescription);
        }

        [Fact]
        public async Task With_user_filter_the_page_title_and_filter_description_include_the_filter()
        {
            SetupTeam();
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Returns(" in 2026");
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(It.IsAny<MatchFilter>())).Returns("Matches");

            var model = await ReadModel();

            Assert.Equal("Matches for Example team stoolball team in 2026", model.Metadata.PageTitle);
            Assert.Equal("Matches in 2026", model.FilterDescription);
        }

        [Fact]
        public async Task Model_has_breadcrumbs_excluding_the_team()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, team, false), Times.Once);
        }
    }
}
