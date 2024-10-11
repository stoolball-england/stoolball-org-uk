using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class LinkedPlayersForMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<IPlayerCacheInvalidator> _cacheClearer = new();

        private LinkedPlayersForMemberSurfaceController CreateController()
        {
            return new LinkedPlayersForMemberSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _playerDataSource.Object,
                _playerRepository.Object,
                _cacheClearer.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        private static Player CreatePlayerWith4PlayerIdentities()
        {
            return new Player
            {
                PlayerRoute = "/example-player",
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity{ PlayerIdentityId = Guid.NewGuid(), LinkedBy = PlayerIdentityLinkedBy.Member },
                    new PlayerIdentity{ PlayerIdentityId = Guid.NewGuid(), LinkedBy = PlayerIdentityLinkedBy.Member },
                    new PlayerIdentity{ PlayerIdentityId = Guid.NewGuid(), LinkedBy = PlayerIdentityLinkedBy.Member },
                    new PlayerIdentity{ PlayerIdentityId = Guid.NewGuid(), LinkedBy = PlayerIdentityLinkedBy.Member }
                }
            };
        }

        [Fact]
        public async Task Not_logged_in_does_not_unlink_player_and_returns_Forbidden()
        {
            var player = CreatePlayerWith4PlayerIdentities();
            var formData = new LinkedPlayersFormData { PlayerIdentities = player.PlayerIdentities.Take(2).ToList() };

            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser?)null));

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(formData);

                Assert.IsType<ForbidResult>(result);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(It.IsAny<PlayerIdentity>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task Player_not_found_by_route_returns_NotFound()
        {
            var player = CreatePlayerWith4PlayerIdentities();
            var memberKey = Guid.NewGuid();
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = "Member name" }));
            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(new LinkedPlayersFormData());

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_not_called_for_identity_not_linked_by_member()
        {
            var player = CreatePlayerWith4PlayerIdentities();
            player.PlayerIdentities[0].LinkedBy = PlayerIdentityLinkedBy.Team;
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));
            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute!, null)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(new LinkedPlayersFormData());

                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[0], memberKey, memberName), Times.Never);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[1], memberKey, memberName), Times.Once);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[2], memberKey, memberName), Times.Once);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[3], memberKey, memberName), Times.Once);
            }
        }

        [Fact]
        public async Task Member_without_linked_player_does_not_unlink_player()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));

            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).Returns(Task.FromResult((Player?)null));

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(new LinkedPlayersFormData());

                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(It.IsAny<PlayerIdentity>(), memberKey, memberName), Times.Never);
            }
        }

        [Fact]
        public async Task Only_identities_missing_from_post_data_are_unlinked()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));

            var player = CreatePlayerWith4PlayerIdentities();
            var formData = new LinkedPlayersFormData { PlayerIdentities = player.PlayerIdentities.Take(2).ToList() };

            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute!, null)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(formData);

                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[0], memberKey, memberName), Times.Never);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[1], memberKey, memberName), Times.Never);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[2], memberKey, memberName), Times.Once);
                _playerRepository.Verify(x => x.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities[3], memberKey, memberName), Times.Once);
            }
        }

        [Fact]
        public async Task No_identities_to_unlink_does_not_clear_cache()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));

            var player = CreatePlayerWith4PlayerIdentities();
            var formData = new LinkedPlayersFormData { PlayerIdentities = player.PlayerIdentities };

            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute!, null)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(formData);

                _cacheClearer.Verify(x => x.InvalidateCacheForPlayer(player), Times.Never);
            }
        }

        [Fact]
        public async Task Unlinking_player_identity_clears_cache()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));

            var player = CreatePlayerWith4PlayerIdentities();
            var formData = new LinkedPlayersFormData { PlayerIdentities = player.PlayerIdentities.Take(2).ToList() };

            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute!, null)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(formData);

                _cacheClearer.Verify(x => x.InvalidateCacheForPlayer(player), Times.Once);
            }
        }

        [Fact]
        public async Task Redirects_to_preferred_route_if_valid()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));

            var player = CreatePlayerWith4PlayerIdentities();
            var formData = new LinkedPlayersFormData { PlayerIdentities = player.PlayerIdentities, PreferredNextRoute = "/players/valid-next-route" };

            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute!, null)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(formData);

                Assert.Equal(formData.PreferredNextRoute, ((RedirectResult)result).Url.ToString());
            }
        }


        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("https://example.org/offsite-link")]
        [InlineData("/wrong-page")]
        public async Task Missing_or_invalid_preferred_route_redirects_to_account(string? invalidNextRoute)
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));

            var player = CreatePlayerWith4PlayerIdentities();
            var formData = new LinkedPlayersFormData { PlayerIdentities = player.PlayerIdentities, PreferredNextRoute = invalidNextRoute };

            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute!, null)).ReturnsAsync(player);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateLinkedPlayers(formData);

                Assert.Equal(Constants.Pages.AccountUrl, ((RedirectResult)result).Url.ToString());
            }
        }
    }
}
