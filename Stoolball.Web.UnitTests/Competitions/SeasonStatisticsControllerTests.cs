using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Competitions;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class SeasonStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestTotalDataSource = new();
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumanizer = new();

        private SeasonStatisticsController CreateController()
        {
            return new SeasonStatisticsController(
                Mock.Of<ILogger<SeasonStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _seasonDataSource.Object,
                _bestPerformanceDataSource.Object,
                _bestTotalDataSource.Object,
                _statisticsFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Season SetupSeason(StatisticsFilter? queryStringFilter = null)
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                FromYear = 2026,
                UntilYear = 2026,
                Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example-competition" }
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(season);
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).ReturnsAsync(queryStringFilter ?? new StatisticsFilter());
            return season;
        }

        private async Task<StatisticsSummaryViewModel<Season>> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<StatisticsSummaryViewModel<Season>>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_season_without_competition_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season { SeasonId = Guid.NewGuid() });

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Default_filter_is_limited_to_season_and_top_ten()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(season, model.Context);
            Assert.Equal(season, model.DefaultFilter.Season);
            Assert.Equal(10, model.DefaultFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual);
            Assert.Equal(season, model.AppliedFilter.Season);
        }

        [Fact]
        public async Task Query_string_filter_is_merged_into_applied_filter_but_not_default_filter()
        {
            var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            SetupSeason(new StatisticsFilter { FromDate = from });

            var model = await ReadModel();

            Assert.Equal(from, model.AppliedFilter.FromDate);
            Assert.Null(model.DefaultFilter.FromDate);
        }

        [Fact]
        public async Task Statistics_are_read_using_applied_filter()
        {
            SetupSeason();
            var innings = new List<StatisticsResult<PlayerInnings>> { new() };
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(innings);

            var model = await ReadModel();

            Assert.Equal(innings, model.PlayerInnings);
            _bestPerformanceDataSource.Verify(x => x.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst), Times.Once);
            _bestPerformanceDataSource.Verify(x => x.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst), Times.Once);
            _bestTotalDataSource.Verify(x => x.ReadMostRunsScored(model.AppliedFilter), Times.Once);
            _bestTotalDataSource.Verify(x => x.ReadMostWickets(model.AppliedFilter), Times.Once);
            _bestTotalDataSource.Verify(x => x.ReadMostCatches(model.AppliedFilter), Times.Once);
        }

        [Fact]
        public async Task Filter_view_model_supports_team_filter()
        {
            SetupSeason(new StatisticsFilter { Team = new Stoolball.Teams.Team { TeamName = "Example team", TeamRoute = "/teams/example" } });
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" for Example team");
            _statisticsFilterHumanizer.Setup(x => x.EntitiesMatchingFilter("Statistics", " for Example team")).Returns("Statistics for Example team");

            var model = await ReadModel();

            Assert.Equal("Statistics for Example team", model.FilterViewModel.FilterDescription);
            Assert.Equal("Statistics", model.FilterViewModel.FilteredItemTypePlural);
            Assert.True(model.FilterViewModel.SupportsTeamFilter);
            Assert.Equal("/teams/example", model.FilterViewModel.TeamRoute);
            Assert.Equal("Example team", model.FilterViewModel.TeamName);
        }

        [Fact]
        public async Task Model_has_metadata_and_breadcrumbs()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal($"Statistics for {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal($"Statistics for stoolball matches played in the {season.SeasonFullName()}.", model.Metadata.Description);
            Assert.Equal([Constants.Pages.Competitions, "Example competition"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
        }
    }
}
