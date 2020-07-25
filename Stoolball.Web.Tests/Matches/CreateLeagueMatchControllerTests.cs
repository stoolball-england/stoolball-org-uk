using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
using System;
using System.Collections.Generic;
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
    public class CreateLeagueMatchControllerTests : UmbracoBaseTest
    {
        private class TestController : CreateLeagueMatchController
        {
            public TestController(ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource, ICreateLeagueMatchSeasonSelector createLeagueMatchEligibleSeasons, Uri requestUrl)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                teamDataSource,
                seasonDataSource,
                createLeagueMatchEligibleSeasons)
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
                return View("CreateLeagueMatch", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_season_or_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season>(null));

            var eligibleSeasons = new Mock<ICreateLeagueMatchSeasonSelector>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, eligibleSeasons.Object, new Uri("https://example.org")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_without_a_league_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team()));

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season>(null));

            var eligibleSeasons = new Mock<ICreateLeagueMatchSeasonSelector>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, eligibleSeasons.Object, new Uri("https://example.org/teams/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_non_league_season_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult(new Season()));

            var eligibleSeasons = new Mock<ICreateLeagueMatchSeasonSelector>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, eligibleSeasons.Object, new Uri("https://example.org/competitions/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_league_team_returns_CreateMatchViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team()));

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season>(null));

            var eligibleSeasons = new Mock<ICreateLeagueMatchSeasonSelector>();
            eligibleSeasons.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>())).Returns(new List<Season>
                {
                        new Season{
                            SeasonId = Guid.NewGuid(),
                            MatchTypes = new List<MatchType>{ MatchType.LeagueMatch},
                            UntilYear = DateTime.Now.Year
                        }
                });

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, eligibleSeasons.Object, new Uri("https://example.org/teams/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<CreateMatchViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_league_season_returns_CreateMatchViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult(new Season
            {
                MatchTypes = new List<MatchType> { MatchType.LeagueMatch }
            }));

            var eligibleSeasons = new Mock<ICreateLeagueMatchSeasonSelector>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, eligibleSeasons.Object, new Uri("https://example.org/competitions/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<CreateMatchViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
