using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Teams;
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

namespace Stoolball.Web.Tests.Teams
{
    public class MatchesForTeamControllerTests : UmbracoBaseTest
    {
        private class TestController : MatchesForTeamController
        {
            public TestController(ITeamDataSource teamDataSource, IMatchListingDataSource matchDataSource, ICreateMatchSeasonSelector createMatchSeasonSelector)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, teamDataSource, matchDataSource,
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IEstimatedSeason>(),
                createMatchSeasonSelector)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }
            protected override bool IsAuthorized(TeamViewModel model)
            {
                return true;
            }
            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MatchesForTeam", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Team>(null));

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchQuery>())).ReturnsAsync(new List<MatchListing>());

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();

            using (var controller = new TestController(teamDataSource.Object, matchesDataSource.Object, seasonSelector.Object))
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

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchQuery>())).ReturnsAsync(new List<MatchListing>());

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();
            seasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), It.IsAny<MatchType>())).Returns(new List<Season>());

            using (var controller = new TestController(teamDataSource.Object, matchesDataSource.Object, seasonSelector.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
