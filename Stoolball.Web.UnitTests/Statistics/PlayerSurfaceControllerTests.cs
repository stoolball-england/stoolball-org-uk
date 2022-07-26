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
    public class PlayerSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerSummaryViewModelFactory> _viewModelFactory = new();
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<ICacheClearer<Player>> _cacheClearer = new();

        public PlayerSurfaceControllerTests() : base()
        {
        }

        private PlayerSurfaceController CreateController()
        {
            return new PlayerSurfaceController(
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
        public async Task Route_matching_player_if_ModelState_invalid_returns_PlayerSummaryViewModel_and_Player_view()
        {
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player() }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                var viewResult = ((ViewResult)result);
                Assert.IsType<PlayerSummaryViewModel>(viewResult.Model);
                Assert.Equal("Player", viewResult.ViewName);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_and_member_not_linked_if_ModelState_invalid_and_member_not_logged_in()
        {
            var player = new Player();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = player }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser?)null));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.False(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_and_member_not_linked_if_ModelState_valid_and_member_not_logged_in()
        {
            var player = new Player();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = player }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser?)null));

            using (var controller = CreateController())
            {
                controller.ModelState.Clear();
                var result = await controller.LinkPlayerToMemberAccount();

                var model = ((PlayerSummaryViewModel)(((ViewResult)result).Model));
                Assert.False(model.IsCurrentMember);
                Assert.False(model.LinkedByThisRequest);
                Assert.Null(model.Player?.MemberKey);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_if_ModelState_invalid_and_current_member_is_not_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player { MemberKey = assignedMemberKey } }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = Guid.NewGuid() }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.False(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_true_if_ModelState_invalid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player { MemberKey = assignedMemberKey } }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.True(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }


        [Fact]
        public async Task IsCurrentMember_is_true_and_member_linked_if_ModelState_valid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            var player = new Player { MemberKey = assignedMemberKey };
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = player }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey, Name = "Assigned member" }));

            using (var controller = CreateController())
            {
                controller.ModelState.Clear();
                var result = await controller.LinkPlayerToMemberAccount();

                var model = ((PlayerSummaryViewModel)(((ViewResult)result).Model));
                Assert.True(model.IsCurrentMember);
                Assert.True(model.LinkedByThisRequest);
                Assert.Equal(model.Player?.MemberKey, assignedMemberKey);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, assignedMemberKey, "Assigned member"), Times.Once);
            }
        }

        [Fact]
        public async Task Cache_not_cleared_if_ModelState_invalid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            var player = new Player { MemberKey = assignedMemberKey };
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = player }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                _cacheClearer.Verify(x => x.ClearCacheFor(player), Times.Never);
            }
        }

        [Fact]
        public async Task Cache_cleared_if_ModelState_valid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            var player = new Player { MemberKey = assignedMemberKey };
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = player }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey }));

            using (var controller = CreateController())
            {
                controller.ModelState.Clear();
                var result = await controller.LinkPlayerToMemberAccount();

                _cacheClearer.Verify(x => x.ClearCacheFor(player), Times.Once);
            }
        }
    }
}
