using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
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

namespace Stoolball.Web.Tests.Competitions
{
    public class SeasonResultsControllerTests : UmbracoBaseTest
    {
        public SeasonResultsControllerTests()
        {
            Setup();
        }

        private class TestController : SeasonResultsController
        {
            public TestController(ISeasonDataSource seasonDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                seasonDataSource,
                Mock.Of<IMatchListingDataSource>(),
                Mock.Of<IAuthorizationPolicy<Competition>>(),
                Mock.Of<IEmailProtector>(),
                Mock.Of<IDateTimeFormatter>())
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
                return View("SeasonTable", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season>(null));

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_season_without_results_returns_404()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season { Competition = new Competition { CompetitionName = "Example" } });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_practice_matches_returns_404()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                Competition = new Competition { CompetitionName = "Example" },
                MatchTypes = new List<MatchType>() { MatchType.Practice }
            });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_results_returns_SeasonViewModel()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition { CompetitionName = "Example" },
                Results = "Example"
            });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_league_matches_returns_SeasonViewModel()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition { CompetitionName = "Example" },
                MatchTypes = new List<MatchType>() { MatchType.LeagueMatch }
            });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

    }
}
