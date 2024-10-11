using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class PlayerIdentityActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private PlayerIdentityActionsController CreateController()
        {
            return new PlayerIdentityActionsController(
                Mock.Of<ILogger<PlayerIdentityActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _authorizationPolicy.Object,
                _playerDataSource.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private PlayerIdentity SetupMocks()
        {
            var identity = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                PlayerIdentityName = "John Smith",
                Team = new Team(),
                Player = new Player { PlayerRoute = "/player/john-smith" }
            };
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(Request.Object.Path)).ReturnsAsync(identity);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(identity.Player.PlayerRoute, null)).ReturnsAsync(identity.Player);
            _authorizationPolicy.Setup(x => x.IsAuthorized(identity.Team)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } });
            return identity;
        }

        [Fact]
        public async Task Route_not_matching_identity_returns_404()
        {
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(Request.Object.Path)).Returns(Task.FromResult<PlayerIdentity?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_identity_sets_authorization()
        {
            _ = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam]);
            }
        }

        [Fact]
        public async Task Route_matching_identity_returns_identity()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.Equal(identity, model.PlayerIdentity);
            }
        }

        [Fact]
        public async Task Route_matching_identity_sets_page_title()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.Equal($"Edit {identity.PlayerIdentityName}", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Route_matching_identity_sets_breadcrumbs()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForEditPlayers(model.Breadcrumbs, identity.Team!), Times.Once());
            }
        }
    }
}
