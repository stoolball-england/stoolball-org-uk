using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class EditTransientTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly MatchListing _match = new()
        {
            MatchName = "Example match",
            MatchRoute = "/matches/example-match",
            StartTime = new DateTimeOffset(2026, 5, 1, 14, 0, 0, TimeSpan.Zero)
        };

        private EditTransientTeamController CreateController()
        {
            return new EditTransientTeamController(
                Mock.Of<ILogger<EditTransientTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _matchDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example" };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing> { _match });
            _dateFormatter.Setup(x => x.FormatDate(_match.StartTime, false, false, false)).Returns("1 May 2026");
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
        public async Task Model_has_authorization()
        {
            var team = SetupTeam();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Matches_are_read_for_the_team_without_match_details()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal([_match], model.Matches.Matches);
            _matchDataSource.Verify(x => x.ReadMatchListings(It.Is<MatchFilter>(f => f.TeamIds.Count == 1 && f.TeamIds[0] == team.TeamId && !f.IncludeMatches), MatchSortOrder.MatchDateEarliestFirst), Times.Once);
        }

        [Fact]
        public async Task Page_title_has_team_name_and_date_of_first_match()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Edit Example team, 1 May 2026", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Breadcrumb_is_the_first_match()
        {
            SetupTeam();

            var model = await ReadModel();

            var breadcrumb = model.Breadcrumbs[^1];
            Assert.Equal("Example match", breadcrumb.Name);
            Assert.Equal("/matches/example-match", breadcrumb.Url?.ToString());
        }
    }
}
