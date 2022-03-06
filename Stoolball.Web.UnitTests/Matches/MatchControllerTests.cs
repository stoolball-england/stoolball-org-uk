using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<ICommentsDataSource<Stoolball.Matches.Match>> _commentsDataSource = new();

        public MatchControllerTests()
        {
            Setup();
        }

        private MatchController CreateController()
        {
            return new MatchController(
                Mock.Of<ILogger<MatchController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                _commentsDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IEmailProtector>(),
                Mock.Of<IBadLanguageFilter>())
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
        public async Task Route_matching_match_returns_MatchViewModel()
        {
            var matchId = Guid.NewGuid();
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = matchId });

            _commentsDataSource.Setup(x => x.ReadComments(matchId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_match_reads_comments()
        {
            var matchId = Guid.NewGuid();
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = matchId });

            _commentsDataSource.Setup(x => x.ReadComments(matchId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _commentsDataSource.Verify(x => x.ReadComments(matchId), Times.Once);
            }
        }
    }
}
