using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class LinkedPlayersForMemberControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();

        private LinkedPlayersForMemberController CreateController()
        {
            return new LinkedPlayersForMemberController(
                Mock.Of<ILogger<LinkedPlayersForMemberController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _memberManager.Object,
                _playerDataSource.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Unauthenticated_returns_view_model_without_player()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Null(((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).Player);
            }
        }

        [Fact]
        public async Task Member_without_linked_player_returns_view_model_without_player()
        {
            var memberKey = Guid.NewGuid();
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _playerDataSource.Verify(x => x.ReadPlayerByMemberKey(memberKey), Times.Once);
                _playerDataSource.Verify(x => x.ReadPlayerByRoute(It.IsAny<string>(), null), Times.Never);
                Assert.Null(((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).Player);
            }
        }

        [Fact]
        public async Task Member_with_linked_player_sets_player_in_view_model()
        {
            var memberKey = Guid.NewGuid();
            var player1 = new Player { PlayerRoute = "/players/example-player" };
            var player2 = new Player { PlayerRoute = "/players/example-player-2" };
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = memberKey }));
            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).Returns(Task.FromResult(player1));
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player1.PlayerRoute, null)).Returns(Task.FromResult(player2));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(player2, ((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).Player);
            }
        }

        [Fact]
        public async Task Sets_breadcrumbs()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var breadcrumbs = ((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).Breadcrumbs;
                Assert.Equal(2, breadcrumbs.Count);
                Assert.Equal("Home", breadcrumbs[0].Name);
                Assert.Equal("My account", breadcrumbs[1].Name);
            }
        }

        [Fact]
        public async Task Sets_page_title()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.False(string.IsNullOrWhiteSpace(((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).Metadata.PageTitle));
            }
        }

        [Fact]
        public async Task Sets_PreferredNextRoute_from_referer_if_removing_domain()
        {
            Request.Object.Headers.Add("Referer", "https://example.org/from-referer");

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal("/from-referer", ((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).PreferredNextRoute);
            }
        }

        [Fact]
        public async Task Sets_PreferredNextRoute_to_account_if_no_referer()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(Constants.Pages.AccountUrl, ((LinkedPlayersForMemberViewModel)((ViewResult)result).Model).PreferredNextRoute);
            }
        }
    }
}
