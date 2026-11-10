using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchesControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchListingDataSource> _matchesDataSource = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();

        private MatchesController CreateController()
        {
            return new MatchesController(
                Mock.Of<ILogger<MatchesController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchesDataSource.Object,
                Mock.Of<IDateTimeFormatter>(),
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_MatchListingViewModel()
        {
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(new MatchFilter());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Assigns_MatchFilter_to_view_model()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(filter, ((MatchListingViewModel)((ViewResult)result).Model).AppliedMatchFilter);
            }
        }

        [Fact]
        public async Task Page_title_is_set_to_humanized_filter()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(" matching filter");

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal("matches matching filter", ((MatchListingViewModel)((ViewResult)result).Model).Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Page_title_is_just_matches_description_when_there_is_no_filter()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(" ");

            using var controller = CreateController();
            var result = await controller.Index();
            var model = (MatchListingViewModel)((ViewResult)result).Model!;

            Assert.Equal("matches", model.Metadata.PageTitle);
            Assert.Null(model.FilterDescription);
        }

        [Fact]
        public async Task Filter_description_is_set_when_filter_is_applied()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(" matching filter");

            using var controller = CreateController();
            var result = await controller.Index();
            var model = (MatchListingViewModel)((ViewResult)result).Model!;

            Assert.Equal("matches matching filter", model.FilterDescription);
        }

        [Fact]
        public async Task Default_filter_includes_future_matches_but_not_tournaments_or_tournament_matches()
        {
            MatchFilter? defaultFilter = null;
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>()))
                .Callback<MatchFilter, string>((f, _) => defaultFilter = f)
                .Returns(new MatchFilter());

            using var controller = CreateController();
            await controller.Index();

            Assert.NotNull(defaultFilter);
            Assert.True(defaultFilter!.IncludeMatches);
            Assert.False(defaultFilter.IncludeTournaments);
            Assert.False(defaultFilter.IncludeTournamentMatches);
            Assert.Equal(DateTimeOffset.UtcNow.Date, defaultFilter.FromDate);
            Assert.Equal(Constants.Defaults.PageSize, defaultFilter.Paging.PageSize);
        }

        [Theory]
        [InlineData("3", 3)]
        [InlineData("0", 1)]
        [InlineData("-2", 1)]
        [InlineData("abc", 1)]
        public async Task Page_number_is_read_from_query_string(string page, int expectedPageNumber)
        {
            Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "page", page } }));
            MatchFilter? defaultFilter = null;
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>()))
                .Callback<MatchFilter, string>((f, _) => defaultFilter = f)
                .Returns(new MatchFilter());

            using var controller = CreateController();
            await controller.Index();

            Assert.Equal(expectedPageNumber, defaultFilter!.Paging.PageNumber);
        }

        [Fact]
        public async Task Matches_are_read_earliest_first_and_assigned_to_view_model()
        {
            var filter = new MatchFilter();
            var listings = new List<MatchListing> { new MatchListing() };
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(listings);

            using var controller = CreateController();
            var result = await controller.Index();

            Assert.Same(listings, ((MatchListingViewModel)((ViewResult)result).Model!).Matches);
        }

        [Fact]
        public async Task Total_matches_is_assigned_to_paging()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchesDataSource.Setup(x => x.ReadTotalMatches(filter)).ReturnsAsync(123);

            using var controller = CreateController();
            await controller.Index();

            Assert.Equal(123, filter.Paging.Total);
        }

        [Theory]
        [InlineData(51, false)]
        [InlineData(50, true)]
        public async Task Page_beyond_last_page_returns_NotFound_without_reading_matches(int totalMatches, bool expectNotFound)
        {
            var filter = new MatchFilter { Paging = new Paging { PageNumber = 2, PageSize = 50 } };
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchesDataSource.Setup(x => x.ReadTotalMatches(filter)).Returns(Task.FromResult(totalMatches));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                if (expectNotFound)
                {
                    Assert.IsType<NotFoundResult>(result);
                    _matchesDataSource.Verify(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>()), Times.Never);
                }
                else
                {
                    Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
                }
            }
        }
    }
}
