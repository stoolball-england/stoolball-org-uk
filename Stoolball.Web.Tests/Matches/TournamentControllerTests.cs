﻿using Moq;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
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
    public class TournamentControllerTests : UmbracoBaseTest
    {
        private class TestController : TournamentController
        {
            public TestController(ITournamentDataSource tournamentDataSource, IMatchDataSource matchDataSource)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, tournamentDataSource, matchDataSource, Mock.Of<IDateTimeFormatter>(), Mock.Of<IEmailProtector>())
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
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Tournament>(null));

            using (var controller = new TestController(tournamentDataSource.Object, Mock.Of<IMatchDataSource>()))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_tournament_returns_TournamentViewModel()
        {
            var tournamentDataSource = new Mock<ITournamentDataSource>();
            tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentName = "Example tournament" });

            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchQuery>())).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(tournamentDataSource.Object, matchDataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}