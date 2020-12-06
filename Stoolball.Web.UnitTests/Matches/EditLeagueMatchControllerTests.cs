using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Matches
{
    public class EditLeagueMatchControllerTests : UmbracoBaseTest
    {
        public EditLeagueMatchControllerTests()
        {
            Setup();
        }

        private class TestController : EditLeagueMatchController
        {
            public TestController(IMatchDataSource matchDataSource, ISeasonDataSource seasonDataSource, IEditMatchHelper editMatchHelper, Uri requestUrl, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                matchDataSource,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                seasonDataSource,
                editMatchHelper)
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
                return View("EditLeagueMatch", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/not-a-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_in_the_past_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(-1), Season = new Season() });

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season()));

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/matches/example-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_in_the_future_returns_EditLeagueMatchViewModel()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season(), MatchRoute = "/matches/example" });

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season()));

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/matches/example-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditLeagueMatchViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task MatchU002ESeason_gets_SeasonId_from_Route()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season(), MatchRoute = "/matches/example" });

            var season = new Season { SeasonId = Guid.NewGuid() };
            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/matches/example-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(season.SeasonId, ((IEditMatchViewModel)((ViewResult)result).Model).Match.Season.SeasonId);
            }
        }

        [Fact]
        public async Task ModelU002ESeason_gets_SeasonId_from_Route()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season(), MatchRoute = "/matches/example" });

            var season = new Season { SeasonId = Guid.NewGuid() };
            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/matches/example-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(season.SeasonId, ((IEditMatchViewModel)((ViewResult)result).Model).Season.SeasonId);
            }
        }

        [Fact]
        public async Task ModelU002EPossibleSeasons_gets_SeasonId_from_Route()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season(), MatchRoute = "/matches/example" });

            var season = new Season { SeasonId = Guid.NewGuid() };
            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            var helper = new Mock<IEditMatchHelper>();
            helper.Setup(x => x.PossibleSeasonsAsListItems(It.IsAny<IEnumerable<Season>>())).Returns((IEnumerable<Season> x) => new List<SelectListItem> { new SelectListItem { Value = season.SeasonId.ToString() } });

            using (var controller = new TestController(matchDataSource.Object, seasonDataSource.Object, helper.Object, new Uri("https://example.org/matches/example-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(season.SeasonId.ToString(), ((IEditMatchViewModel)((ViewResult)result).Model).PossibleSeasons.First().Value);
            }
        }
    }
}
