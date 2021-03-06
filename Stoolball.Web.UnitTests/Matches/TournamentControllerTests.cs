﻿using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
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
    public class TournamentControllerTests : UmbracoBaseTest
    {
        public TournamentControllerTests()
        {
            Setup();
        }

        private class TestController : TournamentController
        {
            public TestController(ITournamentDataSource tournamentDataSource, IMatchListingDataSource matchDataSource, ICommentsDataSource<Tournament> commentsDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                tournamentDataSource,
                matchDataSource,
                Mock.Of<IMatchFilterFactory>(),
                commentsDataSource,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IEmailProtector>(),
                Mock.Of<IBadLanguageFilter>())
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
                return View("Tournament", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            var tournamentDataSource = new Mock<ITournamentDataSource>();
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null));
            var commentsDataSource = new Mock<ICommentsDataSource<Tournament>>();

            using (var controller = new TestController(tournamentDataSource.Object, Mock.Of<IMatchListingDataSource>(), commentsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_tournament_returns_TournamentViewModel()
        {
            var tournamentId = Guid.NewGuid();
            var tournamentDataSource = new Mock<ITournamentDataSource>();
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentId = tournamentId, TournamentName = "Example tournament" });

            var matchDataSource = new Mock<IMatchListingDataSource>();
            matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            var commentsDataSource = new Mock<ICommentsDataSource<Tournament>>();
            commentsDataSource.Setup(x => x.ReadComments(tournamentId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = new TestController(tournamentDataSource.Object, matchDataSource.Object, commentsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TournamentViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_tournament_reads_comments()
        {
            var tournamentId = Guid.NewGuid();
            var tournamentDataSource = new Mock<ITournamentDataSource>();
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentId = tournamentId, TournamentName = "Example tournament" });

            var matchDataSource = new Mock<IMatchListingDataSource>();
            matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            var commentsDataSource = new Mock<ICommentsDataSource<Tournament>>();
            commentsDataSource.Setup(x => x.ReadComments(tournamentId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = new TestController(tournamentDataSource.Object, matchDataSource.Object, commentsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                commentsDataSource.Verify(x => x.ReadComments(tournamentId), Times.Once);
            }
        }
    }
}
