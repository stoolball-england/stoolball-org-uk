using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.MatchLocations;
using System;
using System.Collections.Generic;
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

namespace Stoolball.Web.Tests.MatchLocations
{
    public class MatchesForMatchLocationControllerTests : UmbracoBaseTest
    {
        private class TestController : MatchesForMatchLocationController
        {
            public TestController(IMatchLocationDataSource matchLocationDataSource, IMatchDataSource matchDataSource)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, matchLocationDataSource, matchDataSource, Mock.Of<IDateTimeFormatter>(), Mock.Of<IEstimatedSeason>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MatchesForMatchLocation", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation>(null));

            var matchesDataSource = new Mock<IMatchDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchQuery>())).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(locationDataSource.Object, matchesDataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_MatchLocationViewModel()
        {
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });

            var matchesDataSource = new Mock<IMatchDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchQuery>())).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(locationDataSource.Object, matchesDataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
