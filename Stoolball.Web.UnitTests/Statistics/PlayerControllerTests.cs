using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class PlayerControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerSummaryViewModelFactory> _viewModelFactory = new();
        private readonly Mock<IMemberManager> _memberManager = new();

        public PlayerControllerTests() : base()
        {
        }

        private PlayerController CreateController()
        {
            return new PlayerController(
                Mock.Of<ILogger<PlayerController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _viewModelFactory.Object,
                _memberManager.Object
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
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_returns_PlayerSummaryViewModel()
        {
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player() }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<PlayerSummaryViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_if_member_not_logged_in()
        {
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player() }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser?)null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.False(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_if_current_member_is_not_linked_member()
        {
            var linkedMemberKey = Guid.NewGuid();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player { MemberKey = linkedMemberKey } }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = Guid.NewGuid() }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.False(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_true_if_current_member_is_linked_member()
        {
            var linkedMemberKey = Guid.NewGuid();
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player { MemberKey = linkedMemberKey } }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = linkedMemberKey }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShowPlayerLinkedToMemberConfirmation_is_set_from_querystring(bool queryStringPresent)
        {
            _viewModelFactory.Setup(x => x.CreateViewModel(CurrentPage.Object, Request.Object.Path, Request.Object.QueryString.Value)).Returns(Task.FromResult(new PlayerSummaryViewModel { Player = new Player() }));

            if (queryStringPresent)
            {
                base.Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { Constants.QueryParameters.ConfirmPlayerLinkedToMember, new StringValues() } }));
            }

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var expectedResult = queryStringPresent;
                Assert.Equal(expectedResult, ((PlayerSummaryViewModel)(((ViewResult)result).Model)).ShowPlayerLinkedToMemberConfirmation);
            }
        }
    }
}
