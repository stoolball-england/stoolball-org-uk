using Moq;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Competitions;
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

namespace Stoolball.Web.Tests.Competitions
{
    public class CompetitionActionsControllerTests : UmbracoBaseTest
    {
        public CompetitionActionsControllerTests()
        {
            Setup();
        }

        private class TestController : CompetitionActionsController
        {
            public TestController(ICompetitionDataSource competitionDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                competitionDataSource,
                Mock.Of<IAuthorizationPolicy<Competition>>())
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
                return View("CompetitionActions", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            var dataSource = new Mock<ICompetitionDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition>(null));

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_CompetitionViewModel()
        {
            var dataSource = new Mock<ICompetitionDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example" });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
