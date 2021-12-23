using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Listings;
using Stoolball.MatchLocations;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchLocationsControllerTests : UmbracoBaseTest
    {
        private class TestController : MatchLocationsController
        {
            public TestController(
                IMatchLocationDataSource matchLocationDataSource,
                IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel> listingsModelBuilder)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                matchLocationDataSource,
                listingsModelBuilder)
            {
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MatchLocations", model);
            }
        }

        private readonly NameValueCollection _queryString = new NameValueCollection();
        private readonly Uri _pageUrl = new Uri("https://example.org/example");

        public MatchLocationsControllerTests()
        {
            base.Setup();
        }

        private TestController CreateController(
            IMatchLocationDataSource dataSource,
            IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel> listingsBuilder)
        {
            var controller = new TestController(dataSource, listingsBuilder);

            base.Request.SetupGet(x => x.Url).Returns(_pageUrl);
            base.Request.SetupGet(x => x.QueryString).Returns(_queryString);
            controller.ControllerContext = new ControllerContext(base.HttpContext.Object, new RouteData(), controller);

            return controller;
        }


        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(MatchLocationsController).GetMethod(nameof(MatchLocationsController.Index));
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.False(attribute.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        [Fact]
        public async Task Null_model_throws_ArgumentNullException()
        {
            using (var controller = CreateController(Mock.Of<IMatchLocationDataSource>(), Mock.Of<IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>>()))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Index(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Returns_MatchLocationsViewModel_from_builder()
        {
            var model = new MatchLocationsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>()) { Filter = new MatchLocationFilter() };
            var dataSource = new Mock<IMatchLocationDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<MatchLocationsViewModel>>(),
                dataSource.Object.ReadTotalMatchLocations,
                dataSource.Object.ReadMatchLocations,
                Constants.Pages.MatchLocations,
                _pageUrl,
                _queryString
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                listingsBuilder.Verify(x => x.BuildModel(
                    It.IsAny<Func<MatchLocationsViewModel>>(),
                    dataSource.Object.ReadTotalMatchLocations,
                    dataSource.Object.ReadMatchLocations,
                    Constants.Pages.MatchLocations,
                    _pageUrl,
                    _queryString
                    ), Times.Once);
                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Index_sets_TeamTypes_filter()
        {
            MatchLocationsViewModel model = null;
            var dataSource = new Mock<IMatchLocationDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<MatchLocationsViewModel>>(),
                dataSource.Object.ReadTotalMatchLocations,
                dataSource.Object.ReadMatchLocations,
                Constants.Pages.MatchLocations,
                _pageUrl,
                _queryString
                ))
                .Callback<Func<MatchLocationsViewModel>, Func<MatchLocationFilter, Task<int>>, Func<MatchLocationFilter, Task<List<MatchLocation>>>, string, Uri, NameValueCollection>(
                    (buildInitialState, totalListings, listings, pageTitle, pageUrl, queryParameters) =>
                    {
                        model = buildInitialState();
                    }
                );

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.True(model.Filter.TeamTypes.Any());
            }
        }
    }
}
