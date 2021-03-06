﻿using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Competitions
{
    public class CreateSeasonControllerTests : UmbracoBaseTest
    {
        public CreateSeasonControllerTests()
        {
            Setup();
        }

        private class TestController : CreateSeasonController
        {
            public TestController(ICompetitionDataSource competitionDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                competitionDataSource,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("AddSeason", model);
            }
        }

        [Fact]
        public async Task Returns_SeasonViewModel()
        {
            var dataSource = new Mock<ICompetitionDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task FromYear_defaults_to_current_year()
        {
            var dataSource = new Mock<ICompetitionDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(DateTime.Today.Year, ((SeasonViewModel)((ViewResult)result).Model).Season.FromYear);
            }
        }

        [Fact]
        public async Task UntilYear_defaults_to_zero()
        {
            var dataSource = new Mock<ICompetitionDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(0, ((SeasonViewModel)((ViewResult)result).Model).Season.UntilYear);
            }
        }

        [Fact]
        public async Task PlayersPerTeam_defaults_to_11()
        {
            var dataSource = new Mock<ICompetitionDataSource>();
            dataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(11, ((SeasonViewModel)((ViewResult)result).Model).Season.PlayersPerTeam);
            }
        }
    }
}
