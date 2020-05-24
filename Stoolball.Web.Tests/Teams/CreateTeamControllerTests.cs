using Moq;
using Stoolball.Web.Teams;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Teams
{
    public class CreateTeamControllerTests : UmbracoBaseTest
    {
        private class TestController : CreateTeamController
        {
            public TestController()
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override bool IsAuthorized()
            {
                return true;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("AddTeam", model);
            }
        }

        [Fact]
        public async Task Returns_TeamViewModel()
        {
            using (var controller = new TestController())
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
