using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class TournamentsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchListingDataSource> _matchesDataSource = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();

        private TournamentsController CreateController()
        {
            return new TournamentsController(
                Mock.Of<ILogger<TournamentsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchesDataSource.Object,
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private void SetupParserToReturnDefaultFilter()
        {
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns((MatchFilter filter, string _) => filter);
        }

        private async Task<MatchListingViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Returns_MatchListingViewModel()
        {
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(new MatchFilter());

            await ReadModel();
        }

        [Fact]
        public async Task Default_filter_is_upcoming_tournaments_only()
        {
            SetupParserToReturnDefaultFilter();

            var model = await ReadModel();

            Assert.Equal(DateTimeOffset.UtcNow.Date, model.DefaultMatchFilter.FromDate);
            Assert.False(model.DefaultMatchFilter.IncludeMatches);
            Assert.False(model.DefaultMatchFilter.IncludeTournamentMatches);
            Assert.True(model.DefaultMatchFilter.IncludeTournaments);
            Assert.Equal(Constants.Defaults.PageSize, model.DefaultMatchFilter.Paging.PageSize);
            Assert.Equal(new Uri("https://www.stoolball.org.uk/"), model.DefaultMatchFilter.Paging.PageUrl);
        }

        [Theory]
        [InlineData(null, 1)]
        [InlineData("3", 3)]
        [InlineData("0", 1)]
        [InlineData("-2", 1)]
        [InlineData("not-a-number", 1)]
        public async Task Page_number_is_read_from_query_string(string? page, int expectedPage)
        {
            if (page is not null)
            {
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "page", page } }));
            }
            SetupParserToReturnDefaultFilter();
            _matchesDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(1000);

            var model = await ReadModel();

            Assert.Equal(expectedPage, model.DefaultMatchFilter.Paging.PageNumber);
        }

        [Fact]
        public async Task Assigns_MatchFilter_to_view_model()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            var model = await ReadModel();

            Assert.Equal(filter, model.AppliedMatchFilter);
        }

        [Fact]
        public async Task Total_matches_and_matches_are_read_using_the_applied_filter()
        {
            var filter = new MatchFilter();
            var matches = new List<MatchListing> { new() { MatchName = "Example tournament" } };
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchesDataSource.Setup(x => x.ReadTotalMatches(filter)).ReturnsAsync(12);
            _matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(matches);

            var model = await ReadModel();

            Assert.Equal(12, model.AppliedMatchFilter.Paging.Total);
            Assert.Equal(matches, model.Matches);
        }

        [Fact]
        public async Task Page_title_and_filter_description_are_set_to_humanized_filter()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("tournaments");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(" matching filter");

            var model = await ReadModel();

            Assert.Equal("tournaments matching filter", model.Metadata.PageTitle);
            Assert.Equal("tournaments matching filter", model.FilterDescription);
        }

        [Fact]
        public async Task Page_title_without_user_filter_has_no_filter_description()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("tournaments");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(string.Empty);

            var model = await ReadModel();

            Assert.Equal("tournaments", model.Metadata.PageTitle);
            Assert.Null(model.FilterDescription);
        }

        [Theory]
        [InlineData(51, false)]
        [InlineData(50, true)]
        public async Task Page_beyond_last_page_returns_NotFound_without_reading_matches(int totalMatches, bool expectNotFound)
        {
            var filter = new MatchFilter { Paging = new Paging { PageNumber = 2, PageSize = 50 } };
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchesDataSource.Setup(x => x.ReadTotalMatches(filter)).Returns(Task.FromResult(totalMatches));

            using var controller = CreateController();

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
