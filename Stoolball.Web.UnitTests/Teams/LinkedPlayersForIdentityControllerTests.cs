﻿using System;
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
using Stoolball.Web.Statistics.Models;
using Stoolball.Web.Teams;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class LinkedPlayersForIdentityControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private LinkedPlayersForIdentityController CreateController()
        {
            return new LinkedPlayersForIdentityController(
                Mock.Of<ILogger<LinkedPlayersForIdentityController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _memberManager.Object,
                _authorizationPolicy.Object,
                _teamDataSource.Object,
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
                Team = new Team { TeamRoute = "/teams/example" },
                Player = new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/example-player" }
            };
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(Request.Object.Path)).ReturnsAsync(identity);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(identity.Player.PlayerRoute, null)).ReturnsAsync(identity.Player);
            _teamDataSource.Setup(x => x.ReadTeamByRoute(identity.Team.TeamRoute, false)).ReturnsAsync(identity.Team);
            _authorizationPolicy.Setup(x => x.IsAuthorized(identity.Team)).Returns(Task.FromResult(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } }));
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

                var model = (LinkedPlayersViewModel)((ViewResult)result).Model;

                Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam]);
            }
        }

        [Fact]
        public async Task Route_matching_identity_returns_player_and_identity()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (LinkedPlayersViewModel)((ViewResult)result).Model;

                Assert.Equal(identity, model.ContextIdentity);
                Assert.Equal(identity.Player, model.Player);
            }
        }

        [Fact]
        public async Task Route_matching_identity_sets_page_title()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (LinkedPlayersViewModel)((ViewResult)result).Model;

                Assert.Equal($"Link {identity.PlayerIdentityName} to the same player listed under another name", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Route_matching_identity_sets_breadcrumbs()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (LinkedPlayersViewModel)((ViewResult)result).Model;

                _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForEditPlayers(model.Breadcrumbs, identity.Team!), Times.Once());
            }
        }
    }
}
