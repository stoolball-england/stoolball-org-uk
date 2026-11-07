using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchesForMatchLocationControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterFactory> _matchFilterFactory = new();
        private readonly Mock<IAuthorizationPolicy<MatchLocation>> _authorizationPolicy = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();

        private MatchesForMatchLocationController CreateController()
        {
            return new MatchesForMatchLocationController(
                Mock.Of<ILogger<MatchesForMatchLocationController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchFilterFactory.Object,
                _matchLocationDataSource.Object,
                _matchDataSource.Object,
                _authorizationPolicy.Object,
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation?>(null));

            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            _matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_MatchLocationViewModel()
        {
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });

            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            _matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }

        private (MatchLocation location, MatchFilter defaultFilter, MatchFilter appliedFilter) SetupLocation(MatchSortOrder sortOrder = MatchSortOrder.MatchDateEarliestFirst)
        {
            var location = new MatchLocation { MatchLocationId = Guid.NewGuid(), SecondaryAddressableObjectName = "Example ground", Town = "Exampleton" };
            var defaultFilter = new MatchFilter { MatchLocationIds = new List<Guid> { location.MatchLocationId.Value } };
            var appliedFilter = new MatchFilter();
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(location);
            _matchFilterFactory.Setup(x => x.MatchesForMatchLocation(location.MatchLocationId.Value)).Returns((defaultFilter, sortOrder));
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);
            return (location, defaultFilter, appliedFilter);
        }

        [Fact]
        public async Task Matches_are_read_using_applied_filter_and_sort_order_from_factory()
        {
            var (location, defaultFilter, appliedFilter) = SetupLocation(MatchSortOrder.LatestUpdateFirst);
            var listings = new List<MatchListing> { new() };
            _matchDataSource.Setup(x => x.ReadMatchListings(appliedFilter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(listings);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(location, model.MatchLocation);
            Assert.Equal(defaultFilter, model.DefaultMatchFilter);
            Assert.Equal(appliedFilter, model.AppliedMatchFilter);
            Assert.Equal(listings, model.Matches!.Matches);
        }

        [Fact]
        public async Task Model_has_authorization_and_breadcrumb()
        {
            var (location, _, _) = SetupLocation();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatchLocation, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(location)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^1].Name);
        }

        [Fact]
        public async Task Without_user_filter_page_title_has_no_filter_and_no_filter_description()
        {
            var (location, _, appliedFilter) = SetupLocation();
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(appliedFilter)).Returns("Matches and tournaments");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(appliedFilter)).Returns(string.Empty);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Null(model.FilterDescription);
            Assert.Equal($"Matches and tournaments at {location.NameAndLocalityOrTownIfDifferent()}", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task With_user_filter_page_title_and_filter_description_include_filter()
        {
            var (location, _, appliedFilter) = SetupLocation();
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(appliedFilter)).Returns("Matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(appliedFilter)).Returns(" in 2020");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            Assert.Equal("Matches in 2020", model.FilterDescription);
            Assert.Equal($"Matches at {location.NameAndLocalityOrTownIfDifferent()} in 2020", model.Metadata.PageTitle);
        }
    }
}
