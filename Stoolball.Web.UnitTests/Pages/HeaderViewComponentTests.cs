using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Metadata;
using Stoolball.Statistics;
using Stoolball.Web.Models;
using Stoolball.Web.Pages;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Stoolball.Web.UnitTests.Pages
{
    public class HeaderViewComponentTests
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();

        private HeaderViewComponent CreateComponent() => new(_memberManager.Object, _playerDataSource.Object);

        [Fact]
        public void Constructor_throws_ArgumentNullException_if_memberManager_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new HeaderViewComponent(null!, _playerDataSource.Object));
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_if_playerDataSource_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new HeaderViewComponent(_memberManager.Object, null!));
        }

        [Fact]
        public async Task Returns_view_with_HeaderViewModel_wrapping_the_page_metadata()
        {
            var page = new Mock<IHasViewMetadata>();
            var metadata = new ViewMetadata { PageTitle = "Title" };
            page.SetupGet(x => x.Metadata).Returns(metadata);

            var result = await CreateComponent().InvokeAsync(page.Object);

            var model = Assert.IsType<HeaderViewModel>(Assert.IsType<ViewViewComponentResult>(result).ViewData!.Model);
            Assert.Same(metadata, model.Metadata);
        }

        [Fact]
        public async Task Does_not_read_player_when_there_is_no_current_member()
        {
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync((MemberIdentityUser?)null);

            var result = await CreateComponent().InvokeAsync(Mock.Of<IHasViewMetadata>());

            var model = (HeaderViewModel)((ViewViewComponentResult)result).ViewData!.Model!;
            Assert.Null(model.Player);
            _playerDataSource.Verify(x => x.ReadPlayerByMemberKey(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Reads_player_for_current_member()
        {
            var memberKey = Guid.NewGuid();
            var player = new Player { PlayerId = Guid.NewGuid() };
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(new MemberIdentityUser { Key = memberKey });
            _playerDataSource.Setup(x => x.ReadPlayerByMemberKey(memberKey)).ReturnsAsync(player);

            var result = await CreateComponent().InvokeAsync(Mock.Of<IHasViewMetadata>());

            var model = (HeaderViewModel)((ViewViewComponentResult)result).ViewData!.Model!;
            Assert.Same(player, model.Player);
        }
    }
}
