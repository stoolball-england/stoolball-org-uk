using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Teams
{
    public class PlayersForTeamControllerTests : UmbracoBaseTest
    {
        public PlayersForTeamControllerTests()
        {
            Setup();
        }

        private class TestController : PlayersForTeamController
        {
            public TestController(ITeamDataSource teamDataSource, IPlayerDataSource playerDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                teamDataSource,
                Mock.Of<IAuthorizationPolicy<Team>>(),
                playerDataSource)
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
                return View("Team", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            var playerDataSource = new Mock<IPlayerDataSource>();

            using (var controller = new TestController(teamDataSource.Object, playerDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamId = Guid.NewGuid() });

            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(new List<PlayerIdentity>()));

            using (var controller = new TestController(teamDataSource.Object, playerDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
