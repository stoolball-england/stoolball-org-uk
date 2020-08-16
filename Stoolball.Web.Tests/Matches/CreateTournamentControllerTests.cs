using Moq;
using Stoolball.Competitions;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
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
    public class CreateTournamentControllerTests : UmbracoBaseTest
    {
        private class TestController : CreateTournamentController
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
                seasonDataSource)
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

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("CreateTournament", model);
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
        public async Task Route_matching_season_without_tournaments_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season { EnableTournaments = false }));

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/competitions/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_team_returns_EditTournamentViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team { TeamName = "Example team" }));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/teams/example/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_season_with_tournaments_returns_EditTournamentViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult(new Season
            {
                EnableTournaments = true,
                Competition = new Competition { CompetitionName = "Example competition" }
            }));

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, new Uri("https://example.org/competitions/example/2020/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
