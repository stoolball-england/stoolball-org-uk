using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private TeamActionsController CreateController()
        {
            return new TeamActionsController(
                Mock.Of<ILogger<TeamActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _authorizationPolicy.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example" };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
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
        public async Task Model_has_authorization_for_team_including_groups_and_members()
        {
            var team = SetupTeam();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } };
            var groups = new List<string> { "Administrators" };
            var members = new List<string> { "Jo Bloggs" };
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).ReturnsAsync(authorization);
            _authorizationPolicy.Setup(x => x.AuthorizedGroupNames(team)).Returns(groups);
            _authorizationPolicy.Setup(x => x.AuthorizedMemberNames(team)).ReturnsAsync(members);

            var model = await ReadModel();

            Assert.Equal("Example team", model.Authorization.AuthorizationFor);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(groups, model.Authorization.AuthorizedGroupNames);
            Assert.Equal(members, model.Authorization.AuthorizedMemberNames);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Edit Example team", model.Metadata.PageTitle);
            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, team, true), Times.Once);
        }
    }
}
