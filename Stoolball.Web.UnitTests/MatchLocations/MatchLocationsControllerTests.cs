using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Listings;
using Stoolball.MatchLocations;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchLocationsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>> _listingsModelBuilder = new();

        public MatchLocationsControllerTests()
        {
            base.Setup();
        }

        private MatchLocationsController CreateController()
        {
            return new MatchLocationsController(
                Mock.Of<ILogger<MatchLocationsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchLocationDataSource.Object,
                _listingsModelBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
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
        public async Task Returns_MatchLocationsViewModel_from_builder()
        {
            var model = new MatchLocationsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>());
            _listingsModelBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<MatchLocationsViewModel>>(),
                _matchLocationDataSource.Object.ReadTotalMatchLocations,
                _matchLocationDataSource.Object.ReadMatchLocations,
                Constants.Pages.MatchLocations,
                new Uri(Request.Object.GetEncodedUrl()),
                Request.Object.QueryString.Value
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _listingsModelBuilder.Verify(x => x.BuildModel(
                    It.IsAny<Func<MatchLocationsViewModel>>(),
                    _matchLocationDataSource.Object.ReadTotalMatchLocations,
                    _matchLocationDataSource.Object.ReadMatchLocations,
                    Constants.Pages.MatchLocations,
                    new Uri(Request.Object.GetEncodedUrl()),
                    Request.Object.QueryString.Value
                    ), Times.Once);
                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Index_sets_TeamTypes_filter()
        {
            MatchLocationsViewModel model = null;
            _listingsModelBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<MatchLocationsViewModel>>(),
                _matchLocationDataSource.Object.ReadTotalMatchLocations,
                _matchLocationDataSource.Object.ReadMatchLocations,
                Constants.Pages.MatchLocations,
                new Uri(Request.Object.GetEncodedUrl()),
                Request.Object.QueryString.Value
                ))
                .Callback<Func<MatchLocationsViewModel>, Func<MatchLocationFilter, Task<int>>, Func<MatchLocationFilter, Task<List<MatchLocation>>>, string, Uri, string>(
                    (buildInitialState, totalListings, listings, pageTitle, pageUrl, queryParameters) =>
                    {
                        model = buildInitialState();
                    }
                );

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(model.Filter.TeamTypes.Any());
            }
        }
    }
}
