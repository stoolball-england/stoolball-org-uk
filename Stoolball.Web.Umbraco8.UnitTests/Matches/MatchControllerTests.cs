using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.UnitTests;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchControllerTests : UmbracoBaseTest
    {
        public MatchControllerTests()
        {
            Setup();
        }

        private class TestController : MatchController
        {
            public TestController(IMatchDataSource matchDataSource, ICommentsDataSource<Stoolball.Matches.Match> commentsDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                matchDataSource,
                commentsDataSource,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IEmailProtector>(),
                Mock.Of<IBadLanguageFilter>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }
            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Match", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));
            var commentsDataSource = new Mock<ICommentsDataSource<Stoolball.Matches.Match>>();

            using (var controller = new TestController(matchDataSource.Object, commentsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_returns_MatchViewModel()
        {
            var matchId = Guid.NewGuid();
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = matchId });

            var commentsDataSource = new Mock<ICommentsDataSource<Stoolball.Matches.Match>>();
            commentsDataSource.Setup(x => x.ReadComments(matchId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = new TestController(matchDataSource.Object, commentsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_match_reads_comments()
        {
            var matchId = Guid.NewGuid();
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = matchId });

            var commentsDataSource = new Mock<ICommentsDataSource<Stoolball.Matches.Match>>();
            commentsDataSource.Setup(x => x.ReadComments(matchId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = new TestController(matchDataSource.Object, commentsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                commentsDataSource.Verify(x => x.ReadComments(matchId), Times.Once);
            }
        }
    }
}
