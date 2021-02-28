using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Statistics
{
    public class IndividualScoresControllerTests : UmbracoBaseTest
    {
        public IndividualScoresControllerTests()
        {
            Setup();
        }

        private class TestController : IndividualScoresController
        {
            public TestController(IStatisticsDataSource statisticsDataSource, IPlayerDataSource playerDataSource, IClubDataSource clubDataSource, ITeamDataSource teamDataSource, IMatchLocationDataSource matchLocationDataSource,
                ICompetitionDataSource competitionDataSource, ISeasonDataSource seasonDataSource, UmbracoHelper umbracoHelper, string queryString)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                statisticsDataSource,
                playerDataSource,
                clubDataSource,
                teamDataSource,
                matchLocationDataSource,
                competitionDataSource,
                seasonDataSource,
                Mock.Of<IRouteNormaliser>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.RawUrl).Returns(Stoolball.Constants.Pages.StatisticsUrl + "/individual-scores");
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(queryString));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("IndividualScores", model);
            }
        }

        [Fact]
        public async Task Player_with_no_innings_returns_404()
        {
            var statisticsDataSource = new Mock<IStatisticsDataSource>();
            var playerDataSource = new Mock<IPlayerDataSource>();
            var clubDataSource = new Mock<IClubDataSource>();
            var teamDataSource = new Mock<ITeamDataSource>();
            var matchLocationDataSource = new Mock<IMatchLocationDataSource>();
            var competitionDataSource = new Mock<ICompetitionDataSource>();
            var seasonDataSource = new Mock<ISeasonDataSource>();

            var playerId = Guid.NewGuid();
            var results = new List<StatisticsResult<PlayerInnings>>();
            _ = statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = new TestController(statisticsDataSource.Object, playerDataSource.Object, clubDataSource.Object, teamDataSource.Object, matchLocationDataSource.Object,
                competitionDataSource.Object, seasonDataSource.Object, UmbracoHelper, $"player={playerId}"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Player_with_innings_returns_IndividualScoresViewModel()
        {
            var statisticsDataSource = new Mock<IStatisticsDataSource>();
            var playerDataSource = new Mock<IPlayerDataSource>();
            var clubDataSource = new Mock<IClubDataSource>();
            var teamDataSource = new Mock<ITeamDataSource>();
            var matchLocationDataSource = new Mock<IMatchLocationDataSource>();
            var competitionDataSource = new Mock<ICompetitionDataSource>();
            var seasonDataSource = new Mock<ISeasonDataSource>();

            var playerId = Guid.NewGuid();
            var results = new List<StatisticsResult<PlayerInnings>> {
                new StatisticsResult<PlayerInnings> {
                    Player = new Player {
                        PlayerIdentities = new List<PlayerIdentity>{
                            new PlayerIdentity{
                                PlayerIdentityName = "Example player"
                            }
                        }
                    }
                }
            };
            statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = new TestController(statisticsDataSource.Object, playerDataSource.Object, clubDataSource.Object, teamDataSource.Object, matchLocationDataSource.Object,
                competitionDataSource.Object, seasonDataSource.Object, UmbracoHelper, $"player={playerId}"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<IndividualScoresViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
