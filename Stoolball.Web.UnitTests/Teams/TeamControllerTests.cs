using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private TeamController CreateController()
        {
            return new TeamController(
                Mock.Of<ILogger<TeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _authorizationPolicy.Object,
                _emailProtector.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam()
        {
            var team = new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = "Example team",
                TeamRoute = "/teams/example",
                Cost = "cost",
                Introduction = "introduction",
                PlayingTimes = "playing times",
                PublicContactDetails = "contact",
                PrivateContactDetails = "private"
            };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
            _emailProtector.Setup(x => x.ProtectEmailAddresses(It.IsAny<string>(), It.IsAny<bool>())).Returns((string html, bool authenticated) => $"{html}|{authenticated}");
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
        public async Task Model_has_page_title_and_description()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Example team stoolball team", model.Metadata.PageTitle);
            Assert.Equal(team.Description(), model.Metadata.Description);
        }

        [Fact]
        public async Task Email_addresses_in_public_text_are_protected_for_signed_out_users()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("cost|False", model.Team!.Cost);
            Assert.Equal("introduction|False", model.Team.Introduction);
            Assert.Equal("playing times|False", model.Team.PlayingTimes);
            Assert.Equal("contact|False", model.Team.PublicContactDetails);
        }

        [Fact]
        public async Task Private_contact_details_are_not_passed_to_email_protector()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("private", model.Team!.PrivateContactDetails);
            _emailProtector.Verify(x => x.ProtectEmailAddresses("private", It.IsAny<bool>()), Times.Never);
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
