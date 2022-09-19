using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Caching;
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
    public class LinkPlayerToMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerSummaryViewModelFactory> _viewModelFactory = new();
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<ICacheClearer<Player>> _cacheClearer = new();

        public LinkPlayerToMemberSurfaceControllerTests() : base()
        {
        }

        private LinkPlayerToMemberSurfaceController CreateController()
        {
            return new LinkPlayerToMemberSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _viewModelFactory.Object,
                _memberManager.Object,
                _playerRepository.Object,
                _cacheClearer.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_player_returns_404()
        {
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = null }));

            using (var controller = CreateController())
            {
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Player_with_linked_member_returns_404()
        {
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player { MemberKey = Guid.NewGuid() } }));

            using (var controller = CreateController())
            {
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Not_logged_in_does_not_link_player_and_returns_Forbidden()
        {
            var player = new Player();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = player }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser?)null));

            using (var controller = CreateController())
            {
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.IsType<ForbidResult>(result);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task Links_player_and_redirects_to_route_returned_by_repository()
        {
            var playerToLink = new Player();
            var linkedPlayer = new Player { PlayerRoute = "/after" };
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = playerToLink }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));
            _playerRepository.Setup(x => x.LinkPlayerToMemberAccount(playerToLink, memberKey, memberName)).Returns(Task.FromResult(linkedPlayer));

            using (var controller = CreateController())
            {
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.IsType<RedirectResult>(result);
                Assert.Equal(linkedPlayer.PlayerRoute + "?" + Constants.QueryParameters.ConfirmPlayerLinkedToMember, ((RedirectResult)result).Url);
            }
        }

        [Fact]
        public async Task Clears_cache_for_both_players()
        {
            var playerToLink = new Player();
            var linkedPlayer = new Player { PlayerRoute = "/after" };
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = playerToLink }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey, Name = memberName }));
            _playerRepository.Setup(x => x.LinkPlayerToMemberAccount(playerToLink, memberKey, memberName)).Returns(Task.FromResult(linkedPlayer));

            using (var controller = CreateController())
            {
                var result = await controller.LinkPlayerToMemberAccount();

                _cacheClearer.Verify(x => x.ClearCacheFor(playerToLink), Times.Once);
                _cacheClearer.Verify(x => x.ClearCacheFor(linkedPlayer), Times.Once);
            }
        }
    }
}
