using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentMatchesControllerTests : UmbracoBaseTest
    {
        public EditTournamentMatchesControllerTests()
        {
            Setup();
        }

        private class TestController : EditTournamentMatchesController
        {
            public TestController(ITournamentDataSource tournamentDataSource, IMatchListingDataSource matchListingDataSource, Uri requestUrl, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                tournamentDataSource,
                matchListingDataSource,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                Mock.Of<IDateTimeFormatter>())
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
                return View("EditTournamentMatches", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            var tournamentDataSource = new Mock<ITournamentDataSource>();
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null));

            using (var controller = new TestController(tournamentDataSource.Object, Mock.Of<IMatchListingDataSource>(), new Uri("https://example.org/not-a-match"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_tournament_returns_EditTournamentViewModel()
        {
            var tournamentDataSource = new Mock<ITournamentDataSource>();
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentId = Guid.NewGuid(), TournamentName = "Example tournament", TournamentRoute = "/tournaments/example" });

            var matchListingDataSource = new Mock<IMatchListingDataSource>();
            matchListingDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).Returns(Task.FromResult(new List<MatchListing>()));

            using (var controller = new TestController(tournamentDataSource.Object, matchListingDataSource.Object, new Uri("https://example.org/tournaments/example-tournament"), UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
