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
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class TournamentsRssControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();
        private readonly MatchFilter _filter = new();

        public TournamentsRssControllerTests()
        {
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(_filter);
            _matchDataSource.Setup(x => x.ReadMatchListings(_filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Returns(string.Empty);
        }

        private TournamentsRssController CreateController()
        {
            return new TournamentsRssController(
                Mock.Of<ILogger<TournamentsRssController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<MatchListingViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("TournamentsRss", view.ViewName);
            return Assert.IsType<MatchListingViewModel>(view.Model);
        }

        [Fact]
        public async Task No_matches_returns_MatchListingViewModel()
        {
            var model = await ReadModel();

            Assert.Empty(model.Matches);
        }

        [Fact]
        public async Task Matches_are_read_latest_update_first_using_applied_filter()
        {
            var matches = new List<MatchListing> { new() { MatchName = "Example tournament" } };
            _matchDataSource.Setup(x => x.ReadMatchListings(_filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(matches);

            var model = await ReadModel();

            Assert.Equal(_filter, model.AppliedMatchFilter);
            Assert.Equal(matches, model.Matches);
        }

        [Fact]
        public async Task Filter_includes_tournaments_only()
        {
            _filter.IncludeTournaments = false;
            _filter.IncludeTournamentMatches = true;

            var model = await ReadModel();

            Assert.True(model.AppliedMatchFilter.IncludeTournaments);
            Assert.False(model.AppliedMatchFilter.IncludeTournamentMatches);
            Assert.False(model.AppliedMatchFilter.IncludeMatches);
        }

        [Fact]
        public async Task Default_date_range_is_from_yesterday_for_365_days()
        {
            var before = DateTimeOffset.UtcNow;

            var model = await ReadModel();

            var after = DateTimeOffset.UtcNow;
            Assert.InRange(model.AppliedMatchFilter.FromDate!.Value, before.AddDays(-1), after.AddDays(-1));
            Assert.InRange(model.AppliedMatchFilter.UntilDate!.Value, before.AddDays(365), after.AddDays(365));
        }

        [Fact]
        public async Task Days_in_query_string_sets_until_date()
        {
            Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "days", "7" } }));
            var before = DateTimeOffset.UtcNow;

            var model = await ReadModel();

            var after = DateTimeOffset.UtcNow;
            Assert.InRange(model.AppliedMatchFilter.UntilDate!.Value, before.AddDays(7), after.AddDays(7));
        }

        [Fact]
        public async Task Invalid_days_in_query_string_uses_365_days()
        {
            Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "days", "not-a-number" } }));
            var before = DateTimeOffset.UtcNow;

            var model = await ReadModel();

            var after = DateTimeOffset.UtcNow;
            Assert.InRange(model.AppliedMatchFilter.UntilDate!.Value, before.AddDays(365), after.AddDays(365));
        }

        [Fact]
        public async Task Dates_from_query_string_are_not_replaced()
        {
            var fromDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var untilDate = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
            _filter.FromDate = fromDate;
            _filter.UntilDate = untilDate;

            var model = await ReadModel();

            Assert.Equal(fromDate, model.AppliedMatchFilter.FromDate);
            Assert.Equal(untilDate, model.AppliedMatchFilter.UntilDate);
        }

        [Fact]
        public async Task Path_without_player_type_does_not_filter_by_player_type()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/tournaments/rss"));

            var model = await ReadModel();

            Assert.Empty(model.AppliedMatchFilter.PlayerTypes);
            Assert.Equal("Stoolball tournaments", model.Metadata.PageTitle);
            Assert.Equal("New or updated stoolball tournaments on the Stoolball England website", model.Metadata.Description);
        }

        [Theory]
        [InlineData("/tournaments/mixed/rss")]
        [InlineData("/tournaments/mixed.rss")]
        public async Task Mixed_path_filters_by_mixed_player_type(string path)
        {
            Request.SetupGet(x => x.Path).Returns(new PathString(path));

            var model = await ReadModel();

            Assert.Equal([PlayerType.Mixed], model.AppliedMatchFilter.PlayerTypes);
            Assert.Equal("Mixed stoolball tournaments", model.Metadata.PageTitle);
            Assert.Equal("New or updated mixed stoolball tournaments on the Stoolball England website", model.Metadata.Description);
        }

        [Fact]
        public async Task Ladies_path_filters_by_ladies_player_type()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/tournaments/ladies/rss"));

            var model = await ReadModel();

            Assert.Equal([PlayerType.Ladies], model.AppliedMatchFilter.PlayerTypes);
            Assert.Equal("Ladies stoolball tournaments", model.Metadata.PageTitle);
            Assert.Equal("New or updated ladies stoolball tournaments on the Stoolball England website", model.Metadata.Description);
        }

        [Fact]
        public async Task Junior_path_filters_by_all_junior_player_types()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/tournaments/junior/rss"));

            var model = await ReadModel();

            Assert.Equal([PlayerType.JuniorMixed, PlayerType.JuniorGirls, PlayerType.JuniorBoys], model.AppliedMatchFilter.PlayerTypes);
            Assert.Equal("Junior stoolball tournaments", model.Metadata.PageTitle);
            Assert.Equal("New or updated junior stoolball tournaments on the Stoolball England website", model.Metadata.Description);
        }

        [Fact]
        public async Task Page_title_describes_filter_without_dates()
        {
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.Is<MatchFilter>(f => f != _filter && f.FromDate == null && f.UntilDate == null))).Returns(" matching 'example'");

            var model = await ReadModel();

            Assert.Equal("Stoolball tournaments matching 'example'", model.Metadata.PageTitle);
            Assert.NotNull(model.AppliedMatchFilter.FromDate);
            Assert.NotNull(model.AppliedMatchFilter.UntilDate);
        }

        [Fact]
        public async Task Page_title_with_player_type_is_lower_cased_after_player_type()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/tournaments/ladies/rss"));
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Returns(" matching 'Example'");

            var model = await ReadModel();

            Assert.Equal("Ladies stoolball tournaments matching 'example'", model.Metadata.PageTitle);
        }
    }
}
