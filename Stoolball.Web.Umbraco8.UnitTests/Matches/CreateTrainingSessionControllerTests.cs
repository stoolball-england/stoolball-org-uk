using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
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
    public class CreateTrainingSessionControllerTests : UmbracoBaseTest
    {
        private class TestController : CreateTrainingSessionController
        {
            public TestController(ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource, Uri requestUrl)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                teamDataSource,
                seasonDataSource,
                Mock.Of<ICreateMatchSeasonSelector>(),
                Mock.Of<IEditMatchHelper>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.RawUrl).Returns(requestUrl.AbsolutePath);

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("CreateTrainingSession", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/teams/example/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season>(null));

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/competitions/example/2020/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_EditTrainingSessionViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team()));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/teams/example/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditTrainingSessionViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_training_season_returns_EditTrainingSessionViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season>(new Season
            {
                MatchTypes = new List<MatchType> { MatchType.TrainingSession }
            }));

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/competitions/example/2020/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditTrainingSessionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
