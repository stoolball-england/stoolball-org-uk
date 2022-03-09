using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class DeleteMatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IPlayerIdentityFinder> _playerIdentityFinder = new();
        private readonly Mock<ICommentsDataSource<Stoolball.Matches.Match>> _commentsDataSource = new();

        public DeleteMatchControllerTests()
        {
            Setup();
        }

        private DeleteMatchController CreateController()
        {
            return new DeleteMatchController(
                Mock.Of<ILogger<DeleteMatchController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                _playerDataSource.Object,
                _playerIdentityFinder.Object,
                _commentsDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_returns_DeleteMatchViewModel()
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = Guid.NewGuid(), MatchRoute = "/matches/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<DeleteMatchViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
