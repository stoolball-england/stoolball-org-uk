using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Matches;
using Stoolball.Web.Security;
using System;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Matches
{
    public class EditStartOfPlayControllerTests : UmbracoBaseTest
    {
        private class TestController : EditStartOfPlayController
        {
            public TestController(IMatchDataSource matchDataSource, Uri requestUrl)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                matchDataSource,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(requestUrl);

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override bool IsAuthorized(Stoolball.Matches.Match match)
            {
                return true;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("EditStartOfPlay", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/not-a-match")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_match_in_the_future_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season() });

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_in_the_past_returns_EdiStartOfPlayViewModel()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(-1), Season = new Season() });

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditStartOfPlayViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
