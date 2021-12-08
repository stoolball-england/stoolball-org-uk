using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Dates;
using Stoolball.Matches;
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
    public class MatchesControllerTests : UmbracoBaseTest
    {
        private class TestController : MatchesController
        {
            public TestController(IMatchListingDataSource matchesDataSource, IMatchFilterQueryStringParser matchFilterUrlParser, IMatchFilterHumanizer matchFilterHumanizer)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                matchesDataSource,
                Mock.Of<IDateTimeFormatter>(),
                matchFilterUrlParser,
                matchFilterHumanizer)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(string.Empty));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Matches", model);
            }
        }

        [Fact]
        public async Task Returns_MatchListingViewModel()
        {
            var dataSource = new Mock<IMatchListingDataSource>();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(new MatchFilter());
            var matchFilterHumanizer = new Mock<IMatchFilterHumanizer>();

            using (var controller = new TestController(dataSource.Object, matchFilterQueryStringParser.Object, matchFilterHumanizer.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Assigns_MatchFilter_to_view_model()
        {
            var dataSource = new Mock<IMatchListingDataSource>();
            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);
            var matchFilterHumanizer = new Mock<IMatchFilterHumanizer>();

            using (var controller = new TestController(dataSource.Object, matchFilterQueryStringParser.Object, matchFilterHumanizer.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(filter, ((MatchListingViewModel)((ViewResult)result).Model).AppliedMatchFilter);
            }
        }

        [Fact]
        public async Task Page_title_is_set_to_humanized_filter()
        {
            var filter = new MatchFilter();
            var dataSource = new Mock<IMatchListingDataSource>();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);
            var matchFilterHumanizer = new Mock<IMatchFilterHumanizer>();
            matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("matches");
            matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(" matching filter");

            using (var controller = new TestController(dataSource.Object, matchFilterQueryStringParser.Object, matchFilterHumanizer.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal("matches matching filter", ((MatchListingViewModel)((ViewResult)result).Model).Metadata.PageTitle);
            }
        }
    }
}
