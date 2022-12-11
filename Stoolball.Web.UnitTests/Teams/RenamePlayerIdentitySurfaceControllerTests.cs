using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class RenamePlayerIdentitySurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private RenamePlayerIdentitySurfaceController CreateController()
        {
            return new RenamePlayerIdentitySurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                Mock.Of<IMemberManager>(),
                _playerDataSource.Object,
                _authorizationPolicy.Object,
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
                Team = new Team()
            };
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(Request.Object.Path)).Returns(Task.FromResult<PlayerIdentity?>(identity));
            _authorizationPolicy.Setup(x => x.IsAuthorized(identity.Team)).Returns(Task.FromResult(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } }));
            return identity;
        }

        [Fact]
        public async Task Route_not_matching_identity_returns_404()
        {
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(Request.Object.Path)).Returns(Task.FromResult<PlayerIdentity?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.RenamePlayerIdentity(new PlayerIdentityFormData());

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_invalid_sets_authorization()
        {
            _ = SetupMocks();

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(new PlayerIdentityFormData());

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam]);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_invalid_returns_identity()
        {
            var identity = SetupMocks();
            var formData = new PlayerIdentityFormData { PlayerSearch = "ModifiedName" };

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(formData);

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.Equal(identity, model.PlayerIdentity);
                Assert.Equal(identity.PlayerIdentityName, model.PlayerIdentity!.PlayerIdentityName);
                Assert.Equal(formData.PlayerSearch, model.FormData.PlayerSearch);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_invalid_sets_page_title()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(new PlayerIdentityFormData());

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.Equal($"Rename {identity.PlayerIdentityName}", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_invalid_sets_breadcrumbs()
        {
            var identity = SetupMocks();

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(new PlayerIdentityFormData());

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbs(model.Breadcrumbs, identity.Team!, true), Times.Once());
            }
        }
    }
}
