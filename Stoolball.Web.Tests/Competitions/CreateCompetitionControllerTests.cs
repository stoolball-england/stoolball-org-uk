using Moq;
using Stoolball.Competitions;
using Stoolball.Web.Competitions;
using Stoolball.Web.Security;
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

namespace Stoolball.Web.Tests.Competitions
{
    public class CreateCompetitionControllerTests : UmbracoBaseTest
    {
        public CreateCompetitionControllerTests()
        {
            Setup();
        }

        private class TestController : CreateCompetitionController
        {
            public TestController(UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("AddCompetition", model);
            }
        }

        [Fact]
        public async Task Returns_CompetitionViewModel()
        {
            using (var controller = new TestController(UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task PlayersPerTeam_defaults_to_11()
        {
            using (var controller = new TestController(UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(11, ((CompetitionViewModel)((ViewResult)result).Model).Competition.PlayersPerTeam);
            }
        }
    }
}
