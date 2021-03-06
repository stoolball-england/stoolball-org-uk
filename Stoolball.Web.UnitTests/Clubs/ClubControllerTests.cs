﻿using Moq;
using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Clubs;
using System;
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

namespace Stoolball.Web.Tests.Clubs
{
    public class ClubControllerTests : UmbracoBaseTest
    {
        public ClubControllerTests()
        {
            Setup();
        }

        private class TestController : ClubController
        {
            public TestController(IClubDataSource clubDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                clubDataSource,
                Mock.Of<IAuthorizationPolicy<Club>>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Club", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            var dataSource = new Mock<IClubDataSource>();
            dataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club>(null));

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_ClubViewModel()
        {
            var dataSource = new Mock<IClubDataSource>();
            dataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club());

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
