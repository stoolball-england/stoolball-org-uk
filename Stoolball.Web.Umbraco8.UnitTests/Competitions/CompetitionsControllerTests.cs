using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Competitions;
using Stoolball.Listings;
using Stoolball.Web.Competitions;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionsControllerTests : UmbracoBaseTest
    {
        private class TestController : CompetitionsController
        {
            public TestController(
                ICompetitionDataSource competitionDataSource,
                IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel> listingsModelBuilder)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                competitionDataSource,
                listingsModelBuilder)
            {
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Competitions", model);
            }
        }

        private readonly NameValueCollection _queryString = new NameValueCollection();
        private readonly Uri _pageUrl = new Uri("https://example.org/example");

        public CompetitionsControllerTests()
        {
            base.Setup();
        }

        private TestController CreateController(
            ICompetitionDataSource dataSource,
            IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel> listingsBuilder)
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
            var method = typeof(CompetitionsController).GetMethod(nameof(CompetitionsController.Index));
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
            using (var controller = CreateController(Mock.Of<ICompetitionDataSource>(), Mock.Of<IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>>()))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Index(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Returns_CompetitionsViewModel_from_builder()
        {
            var model = new CompetitionsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>()) { Filter = new CompetitionFilter() };
            var dataSource = new Mock<ICompetitionDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<CompetitionsViewModel>>(),
                dataSource.Object.ReadTotalCompetitions,
                dataSource.Object.ReadCompetitions,
                Constants.Pages.Competitions,
                _pageUrl,
                _queryString
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                listingsBuilder.Verify(x => x.BuildModel(
                    It.IsAny<Func<CompetitionsViewModel>>(),
                    dataSource.Object.ReadTotalCompetitions,
                    dataSource.Object.ReadCompetitions,
                    Constants.Pages.Competitions,
                    _pageUrl,
                    _queryString
                    ), Times.Once);
                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }

    }
}
