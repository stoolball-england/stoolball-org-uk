using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Listings;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamListingDataSource> _teamDataSource = new();
        private readonly Mock<IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>> _listingsModelBuilder = new();

        private TeamsController CreateController()
        {
            return new TeamsController(
                Mock.Of<ILogger<TeamsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _listingsModelBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }


        [Fact]
        public async Task Returns_TeamsViewModel_from_builder()
        {
            var model = new TeamsViewModel();
            _listingsModelBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<TeamsViewModel>>(),
                _teamDataSource.Object.ReadTotalTeams,
               _teamDataSource.Object.ReadTeamListings,
                Constants.Pages.Teams,
                new Uri(Request.Object.GetEncodedUrl()),
                Request.Object.QueryString.Value
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Index_sets_TeamTypes_filter_including_but_not_only_null()
        {
            TeamsViewModel? model = null;
            _listingsModelBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<TeamsViewModel>>(),
                _teamDataSource.Object.ReadTotalTeams,
                _teamDataSource.Object.ReadTeamListings,
                Constants.Pages.Teams,
                new Uri(Request.Object.GetEncodedUrl()),
                Request.Object.QueryString.Value
                ))
                .Callback<Func<TeamsViewModel>, Func<TeamListingFilter, Task<int>>, Func<TeamListingFilter, Task<List<TeamListing>>>, string, Uri, string>(
                    (buildInitialState, totalListings, listings, pageTitle, pageUrl, queryParameters) =>
                    {
                        model = buildInitialState();
                    }
                );

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(1, model!.Filter.TeamTypes.Count(x => x == null));
                Assert.True(model.Filter.TeamTypes.Count > 1);
            }
        }
    }
}
