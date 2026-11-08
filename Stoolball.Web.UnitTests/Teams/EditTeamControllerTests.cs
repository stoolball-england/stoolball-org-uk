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
    public class EditTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private EditTeamController CreateController()
        {
            return new EditTeamController(
                Mock.Of<ILogger<EditTeamController>>(),
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
        public async Task Model_has_authorization()
        {
            var team = SetupTeam();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
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
