using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Umbraco.Data.Competitions;
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
    public class EditLeagueMatchControllerTests : UmbracoBaseTest
    {
        private class TestController : EditLeagueMatchController
        {
            public TestController(IMatchDataSource matchDataSource, ISeasonDataSource seasonDataSource, IEditMatchHelper editMatchHelper, Uri requestUrl)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                matchDataSource,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                seasonDataSource,
                editMatchHelper)
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
                return View("EditLeagueMatch", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/not-a-match")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_match_returns_EditMatchViewModel()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                Season = new Season()
            });

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season()));

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/matches/example-match")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditLeagueMatchViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
