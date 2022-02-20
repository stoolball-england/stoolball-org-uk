using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Listings;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamsControllerTests : UmbracoBaseTest
    {
        private class TestController : TeamsController
        {
            public TestController(
                ITeamListingDataSource teamDataSource,
                IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel> listingsModelBuilder)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                teamDataSource,
                listingsModelBuilder)
            { }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Teams", model);
            }
        }

        private readonly string _queryString = string.Empty;
        private readonly Uri _pageUrl = new Uri("https://example.org/example");

        public TeamsControllerTests()
        {
            base.Setup();
        }

        private TestController CreateController(
            ITeamListingDataSource dataSource,
            IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel> listingsBuilder)
        {
            var controller = new TestController(dataSource, listingsBuilder);

            base.Request.SetupGet(x => x.Url).Returns(_pageUrl);

            controller.ControllerContext = new ControllerContext(base.HttpContext.Object, new RouteData(), controller);

            return controller;
        }


        [Fact]
        public async Task Returns_TeamsViewModel_from_builder()
        {
            var model = new TeamsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>()) { Filter = new TeamListingFilter() };
            var dataSource = new Mock<ITeamListingDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<TeamsViewModel>>(),
                dataSource.Object.ReadTotalTeams,
                dataSource.Object.ReadTeamListings,
                Constants.Pages.Teams,
                _pageUrl,
                _queryString
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Index_sets_TeamTypes_filter_including_but_not_only_null()
        {
            TeamsViewModel model = null;
            var dataSource = new Mock<ITeamListingDataSource>();
            var listingsBuilder = new Mock<IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>>();
            listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<TeamsViewModel>>(),
                dataSource.Object.ReadTotalTeams,
                dataSource.Object.ReadTeamListings,
                Constants.Pages.Teams,
                _pageUrl,
                _queryString
                ))
                .Callback<Func<TeamsViewModel>, Func<TeamListingFilter, Task<int>>, Func<TeamListingFilter, Task<List<TeamListing>>>, string, Uri, string>(
                    (buildInitialState, totalListings, listings, pageTitle, pageUrl, queryParameters) =>
                    {
                        model = buildInitialState();
                    }
                );

            using (var controller = CreateController(dataSource.Object, listingsBuilder.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(1, model.Filter.TeamTypes.Count(x => x == null));
                Assert.True(model.Filter.TeamTypes.Count > 1);
            }
        }
    }
}
