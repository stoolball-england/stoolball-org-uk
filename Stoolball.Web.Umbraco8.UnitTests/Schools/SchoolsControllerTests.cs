using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Listings;
using Stoolball.Schools;
using Stoolball.Web.Routing;
using Stoolball.Web.Schools;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Schools
{
    public class SchoolsControllerTests : UmbracoBaseTest
    {
        private class TestController : SchoolsController
        {
            public TestController(
                ISchoolDataSource schoolDataSource,
                IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel> listingsModelBuilder)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                schoolDataSource,
                listingsModelBuilder)
            {
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Schools", model);
            }
        }

        private readonly NameValueCollection _queryString = new NameValueCollection();
        private readonly Uri _pageUrl = new Uri("https://example.org/example");

        public SchoolsControllerTests()
        {
            base.Setup();
        }

        private TestController CreateController(
            ISchoolDataSource dataSource,
            IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel> listingsBuilder)
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
            var method = typeof(SchoolsController).GetMethod(nameof(SchoolsController.Index));
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
            using (var controller = CreateController(Mock.Of<ISchoolDataSource>(), Mock.Of<IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>>()))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Index(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Returns_SchoolsViewModel_from_builder()
        {
            var model = new SchoolsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>()) { Filter = new SchoolFilter() };
            var dataSource = new Mock<ISchoolDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<SchoolsViewModel>>(),
                dataSource.Object.ReadTotalSchools,
                dataSource.Object.ReadSchools,
                Constants.Pages.Schools,
                _pageUrl,
                _queryString
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                listingsBuilder.Verify(x => x.BuildModel(
                    It.IsAny<Func<SchoolsViewModel>>(),
                    dataSource.Object.ReadTotalSchools,
                    dataSource.Object.ReadSchools,
                    Constants.Pages.Schools,
                    _pageUrl,
                    _queryString
                    ), Times.Once);
                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Sets_schools_breadcrumb()
        {
            var model = new SchoolsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>()) { Filter = new SchoolFilter() };
            var dataSource = new Mock<ISchoolDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<SchoolsViewModel>>(),
                dataSource.Object.ReadTotalSchools,
                dataSource.Object.ReadSchools,
                Constants.Pages.Schools,
                _pageUrl,
                _queryString
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                var breadcrumbs = (((ViewResult)result).Model as BaseViewModel).Breadcrumbs;
                Assert.Equal(2, breadcrumbs.Count);
                Assert.Equal(Constants.Pages.Home, breadcrumbs[0].Name);
                Assert.Equal(Constants.Pages.Schools, breadcrumbs[1].Name);
            }
        }
    }
}
