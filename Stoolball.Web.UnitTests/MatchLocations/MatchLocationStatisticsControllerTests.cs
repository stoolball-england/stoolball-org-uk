using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchLocationStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestTotalDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumanizer = new();

        private MatchLocationStatisticsController CreateController()
        {
            return new MatchLocationStatisticsController(
                Mock.Of<ILogger<MatchLocationStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchLocationDataSource.Object,
                _bestPerformanceDataSource.Object,
                _bestTotalDataSource.Object,
                _statisticsFilterFactory.Object,
                _statisticsFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).Returns(Task.FromResult(new StatisticsFilter()));
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_StatisticsSummaryViewModel()
        {
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).Returns(Task.FromResult(new StatisticsFilter()));
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model);
            }
        }

        private MatchLocation SetupLocation(StatisticsFilter? queryStringFilter = null)
        {
            var location = new MatchLocation { MatchLocationId = Guid.NewGuid(), SecondaryAddressableObjectName = "Example ground", Town = "Exampleton" };
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(location);
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).ReturnsAsync(queryStringFilter ?? new StatisticsFilter());
            return location;
        }

        [Fact]
        public async Task Default_filter_is_limited_to_location_and_top_ten()
        {
            var location = SetupLocation();

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model);
            Assert.Equal(location, model.Context);
            Assert.Equal(location, model.DefaultFilter.MatchLocation);
            Assert.Equal(10, model.DefaultFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual);
            Assert.Equal(location, model.AppliedFilter.MatchLocation);
        }

        [Fact]
        public async Task Query_string_filter_is_merged_into_applied_filter_but_not_default_filter()
        {
            var from = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            SetupLocation(new StatisticsFilter { FromDate = from });

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model);
            Assert.Equal(from, model.AppliedFilter.FromDate);
            Assert.Null(model.DefaultFilter.FromDate);
            Assert.Equal(from, model.FilterViewModel.FromDate);
        }

        [Fact]
        public async Task Statistics_are_read_using_applied_filter()
        {
            SetupLocation();
            var innings = new List<StatisticsResult<PlayerInnings>> { new() };
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(innings);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model);
            Assert.Equal(innings, model.PlayerInnings);
            _bestPerformanceDataSource.Verify(x => x.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst), Times.Once);
            _bestPerformanceDataSource.Verify(x => x.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst), Times.Once);
            _bestTotalDataSource.Verify(x => x.ReadMostRunsScored(model.AppliedFilter), Times.Once);
            _bestTotalDataSource.Verify(x => x.ReadMostWickets(model.AppliedFilter), Times.Once);
            _bestTotalDataSource.Verify(x => x.ReadMostCatches(model.AppliedFilter), Times.Once);
        }

        [Fact]
        public async Task Filter_view_model_supports_date_and_team_filters()
        {
            SetupLocation(new StatisticsFilter { Team = new Stoolball.Teams.Team { TeamName = "Example team", TeamRoute = "/teams/example" } });
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" for Example team");
            _statisticsFilterHumanizer.Setup(x => x.EntitiesMatchingFilter("Statistics", " for Example team")).Returns("Statistics for Example team");

            using var controller = CreateController();

            var result = await controller.Index();

            var filterViewModel = Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model).FilterViewModel;
            Assert.Equal("Statistics for Example team", filterViewModel.FilterDescription);
            Assert.Equal("Statistics", filterViewModel.FilteredItemTypePlural);
            Assert.True(filterViewModel.SupportsDateFilter);
            Assert.True(filterViewModel.SupportsTeamFilter);
            Assert.Equal("/teams/example", filterViewModel.TeamRoute);
            Assert.Equal("Example team", filterViewModel.TeamName);
        }

        [Fact]
        public async Task Model_has_metadata_and_breadcrumb()
        {
            var location = SetupLocation();
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" in 2020");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model);
            Assert.Equal($"Statistics for {location.NameAndLocalityOrTown()} in 2020", model.Metadata.PageTitle);
            Assert.Equal($"Statistics for stoolball matches played at {location.NameAndLocalityOrTown()}.", model.Metadata.Description);
            Assert.Equal(Constants.Pages.MatchLocations, model.Breadcrumbs[^1].Name);
        }
    }
}
