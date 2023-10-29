using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheInvalidator = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly MemberIdentityUser _currentMember = new MemberIdentityUser
        {
            Key = Guid.NewGuid(),
            UserName = "jo.bloggs@example.org"
        };
        private readonly PlayerIdentity _identityToUpdate = new PlayerIdentity
        {
            PlayerIdentityId = Guid.NewGuid(),
            PlayerIdentityName = "John Smith",
            Team = new Team { TeamId = Guid.NewGuid() }
        };
        public RenamePlayerIdentitySurfaceControllerTests()
        {
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(Request.Object.Path)).Returns(Task.FromResult<PlayerIdentity?>(_identityToUpdate));
            _authorizationPolicy.Setup(x => x.IsAuthorized(_identityToUpdate.Team)).Returns(Task.FromResult(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(_currentMember));
        }

        private RenamePlayerIdentitySurfaceController CreateController()
        {
            return new RenamePlayerIdentitySurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _playerDataSource.Object,
                _playerRepository.Object,
                _playerCacheInvalidator.Object,
                _authorizationPolicy.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
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
            var formData = new PlayerIdentityFormData { PlayerSearch = "ModifiedName" };

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(formData);

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.Equal(_identityToUpdate, model.PlayerIdentity);
                Assert.Equal(_identityToUpdate.PlayerIdentityName, model.PlayerIdentity!.PlayerIdentityName);
                Assert.Equal(formData.PlayerSearch, model.FormData.PlayerSearch);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_invalid_sets_page_title()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(new PlayerIdentityFormData());

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                Assert.Equal($"Rename {_identityToUpdate.PlayerIdentityName}", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_invalid_sets_breadcrumbs()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RenamePlayerIdentity(new PlayerIdentityFormData());

                var model = (PlayerIdentityViewModel)((ViewResult)result).Model;

                _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForEditPlayers(model.Breadcrumbs, _identityToUpdate.Team!), Times.Once());
            }
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Route_matching_identity_with_unchanged_name_redirects_even_if_ModelState_invalid(bool modelStateIsValid)
        {
            var formData = new PlayerIdentityFormData { PlayerSearch = _identityToUpdate.PlayerIdentityName };

            using (var controller = CreateController())
            {
                if (!modelStateIsValid)
                {
                    controller.ModelState.AddModelError(string.Empty, "Any error");
                }

                var result = await controller.RenamePlayerIdentity(formData);

                Assert.IsAssignableFrom<RedirectResult>(result);
                _playerCacheInvalidator.Verify(x => x.InvalidateCacheForTeams(_identityToUpdate.Team!), Times.Never);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_valid_and_name_changed_attempts_update_clears_cache_and_redirects_on_success()
        {
            var formData = new PlayerIdentityFormData { PlayerSearch = Guid.NewGuid().ToString() };

            _playerRepository.Setup(x => x.UpdatePlayerIdentity(
                    It.Is<PlayerIdentity>(x => x.PlayerIdentityId == _identityToUpdate.PlayerIdentityId && x.PlayerIdentityName == formData.PlayerSearch),
                    _currentMember.Key,
                    _currentMember.UserName)).ReturnsAsync(new RepositoryResult<PlayerIdentityUpdateResult, PlayerIdentity> { Status = PlayerIdentityUpdateResult.Success, Result = _identityToUpdate });

            using (var controller = CreateController())
            {
                var result = await controller.RenamePlayerIdentity(formData);

                _playerRepository.Verify(x => x.UpdatePlayerIdentity(
                    It.Is<PlayerIdentity>(x => x.PlayerIdentityId == _identityToUpdate.PlayerIdentityId && x.PlayerIdentityName == formData.PlayerSearch),
                    _currentMember.Key,
                    _currentMember.UserName),
                    Times.Once);

                _playerCacheInvalidator.Verify(x => x.InvalidateCacheForTeams(_identityToUpdate.Team!), Times.Once);

                Assert.IsAssignableFrom<RedirectResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_identity_with_ModelState_valid_and_name_changed_attempts_update_and_sets_ModelState_if_name_not_unique()
        {
            var formData = new PlayerIdentityFormData { PlayerSearch = Guid.NewGuid().ToString() };

            _playerRepository.Setup(x => x.UpdatePlayerIdentity(
                    It.Is<PlayerIdentity>(x => x.PlayerIdentityId == _identityToUpdate.PlayerIdentityId && x.PlayerIdentityName == formData.PlayerSearch),
                    _currentMember.Key,
                    _currentMember.UserName)).ReturnsAsync(new RepositoryResult<PlayerIdentityUpdateResult, PlayerIdentity> { Status = PlayerIdentityUpdateResult.NotUnique, Result = _identityToUpdate });

            using (var controller = CreateController())
            {
                var result = await controller.RenamePlayerIdentity(formData);

                _playerRepository.Verify(x => x.UpdatePlayerIdentity(
                    It.Is<PlayerIdentity>(x => x.PlayerIdentityId == _identityToUpdate.PlayerIdentityId && x.PlayerIdentityName == formData.PlayerSearch),
                    _currentMember.Key,
                    _currentMember.UserName),
                    Times.Once);

                Assert.IsAssignableFrom<ViewResult>(result);

                var playerIdentityFieldName = string.Join(".", nameof(PlayerIdentityViewModel.FormData), nameof(formData.PlayerSearch));
                Assert.True(controller.ModelState.ContainsKey(playerIdentityFieldName) && controller.ModelState[playerIdentityFieldName].ValidationState == ModelValidationState.Invalid);
            }
        }
    }
}
