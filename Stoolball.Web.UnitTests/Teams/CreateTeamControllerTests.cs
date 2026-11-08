using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class CreateTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();

        private CreateTeamController CreateController()
        {
            return new CreateTeamController(
                Mock.Of<ILogger<CreateTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<TeamViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public void Null_authorization_policy_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CreateTeamController(
                Mock.Of<ILogger<CreateTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                null!));
        }

        [Fact]
        public async Task Returns_TeamViewModel()
        {
            var model = await ReadModel();

            Assert.NotNull(model.Team);
        }

        [Fact]
        public async Task New_team_has_default_team_type_player_type_and_minimum_age()
        {
            var model = await ReadModel();

            Assert.Equal(TeamType.Regular, model.Team!.TeamType);
            Assert.Equal(PlayerType.Mixed, model.Team.PlayerType);
            Assert.Equal(11, model.Team.AgeRangeLower);
        }

        [Fact]
        public async Task Model_has_authorization_for_the_new_team()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateTeam, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Team>())).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            _authorizationPolicy.Verify(x => x.IsAuthorized(model.Team!), Times.Once);
        }

        [Fact]
        public async Task Model_has_page_title_and_teams_breadcrumb()
        {
            var model = await ReadModel();

            Assert.Equal("Add a team", model.Metadata.PageTitle);
            var breadcrumb = model.Breadcrumbs[^1];
            Assert.Equal(Constants.Pages.Teams, breadcrumb.Name);
            Assert.Equal(Constants.Pages.TeamsUrl, breadcrumb.Url?.ToString());
        }
    }
}
