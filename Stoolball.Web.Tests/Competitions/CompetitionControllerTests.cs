using Moq;
using Stoolball.Competitions;
using Stoolball.Email;
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
    public class CompetitionControllerTests : UmbracoBaseTest
    {
        private class TestController : CompetitionController
        {
            public TestController(ISeasonDataSource seasonDataSource)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, seasonDataSource, Mock.Of<IEmailProtector>())
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

            protected override bool IsAuthorized(CompetitionViewModel model)
            {
                return true;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Competition", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition>(null));

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_CompetitionViewModel()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition());

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
