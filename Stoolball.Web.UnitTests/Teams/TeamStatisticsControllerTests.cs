using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Statistics.Models;
using Stoolball.Web.Teams;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IInningsStatisticsDataSource> _inningsStatisticsDataSource = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestPlayerTotalDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumanizer = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private TeamStatisticsController CreateController()
        {
            return new TeamStatisticsController(
                Mock.Of<ILogger<TeamStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _bestPerformanceDataSource.Object,
                _inningsStatisticsDataSource.Object,
                _bestPlayerTotalDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumanizer.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam(StatisticsFilter? queryStringFilter = null)
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example" };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<string?>())).Returns(queryStringFilter ?? new StatisticsFilter());
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(new List<StatisticsResult<PlayerInnings>>() as IEnumerable<StatisticsResult<PlayerInnings>>);
            _bestPerformanceDataSource.Setup(x => x.ReadBowlingFigures(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(new List<StatisticsResult<BowlingFigures>>());
            _bestPlayerTotalDataSource.Setup(x => x.ReadMostRunsScored(It.IsAny<StatisticsFilter>())).ReturnsAsync(new List<StatisticsResult<BestStatistic>>());
            _bestPlayerTotalDataSource.Setup(x => x.ReadMostWickets(It.IsAny<StatisticsFilter>())).ReturnsAsync(new List<StatisticsResult<BestStatistic>>());
            _bestPlayerTotalDataSource.Setup(x => x.ReadMostCatches(It.IsAny<StatisticsFilter>())).ReturnsAsync(new List<StatisticsResult<BestStatistic>>());
            return team;
        }

        private async Task<StatisticsSummaryViewModel<Team>> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<StatisticsSummaryViewModel<Team>>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<string?>())).Returns(new StatisticsFilter());

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_team_returns_model_with_team_as_context_and_innings_statistics_shown()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team, model.Context);
            Assert.True(model.ShowInningsStatistics);
            Assert.False(model.ShowTeamsColumn);
        }

        [Fact]
        public async Task Default_filter_is_limited_to_team_and_top_ten()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team, model.DefaultFilter.Team);
            Assert.Equal(10, model.DefaultFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual);
            Assert.Equal(team, model.AppliedFilter.Team);
        }

        [Fact]
        public async Task Query_string_filter_is_merged_into_applied_filter_but_not_default_filter()
        {
            var from = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var until = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
            SetupTeam(new StatisticsFilter { FromDate = from, UntilDate = until });

            var model = await ReadModel();

            Assert.Equal(from, model.AppliedFilter.FromDate);
            Assert.Equal(until, model.AppliedFilter.UntilDate);
            Assert.Null(model.DefaultFilter.FromDate);
            Assert.Equal(from, model.FilterViewModel.FromDate);
            Assert.Equal(until, model.FilterViewModel.UntilDate);
        }

        [Fact]
        public async Task Statistics_are_read_using_applied_filter()
        {
            SetupTeam();
            var innings = new List<StatisticsResult<PlayerInnings>> { new() };
            var bowling = new List<StatisticsResult<BowlingFigures>> { new() };
            var runs = new List<StatisticsResult<BestStatistic>> { new() };
            var wickets = new List<StatisticsResult<BestStatistic>> { new() };
            var catches = new List<StatisticsResult<BestStatistic>> { new() };
            var inningsStatistics = new InningsStatistics();
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(innings);
            _bestPerformanceDataSource.Setup(x => x.ReadBowlingFigures(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(bowling);
            _bestPlayerTotalDataSource.Setup(x => x.ReadMostRunsScored(It.IsAny<StatisticsFilter>())).ReturnsAsync(runs);
            _bestPlayerTotalDataSource.Setup(x => x.ReadMostWickets(It.IsAny<StatisticsFilter>())).ReturnsAsync(wickets);
            _bestPlayerTotalDataSource.Setup(x => x.ReadMostCatches(It.IsAny<StatisticsFilter>())).ReturnsAsync(catches);
            _inningsStatisticsDataSource.Setup(x => x.ReadInningsStatistics(It.IsAny<StatisticsFilter>())).ReturnsAsync(inningsStatistics);

            var model = await ReadModel();

            Assert.Equal(innings, model.PlayerInnings);
            Assert.Equal(bowling, model.BowlingFigures);
            Assert.Equal(runs, model.MostRuns);
            Assert.Equal(wickets, model.MostWickets);
            Assert.Equal(catches, model.MostCatches);
            Assert.Equal(inningsStatistics, model.InningsStatistics);
            _inningsStatisticsDataSource.Verify(x => x.ReadInningsStatistics(model.AppliedFilter), Times.Once);
            _bestPerformanceDataSource.Verify(x => x.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst), Times.Once);
            _bestPerformanceDataSource.Verify(x => x.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst), Times.Once);
            _bestPlayerTotalDataSource.Verify(x => x.ReadMostRunsScored(model.AppliedFilter), Times.Once);
            _bestPlayerTotalDataSource.Verify(x => x.ReadMostWickets(model.AppliedFilter), Times.Once);
            _bestPlayerTotalDataSource.Verify(x => x.ReadMostCatches(model.AppliedFilter), Times.Once);
        }

        [Fact]
        public async Task Filter_description_and_page_title_ignore_the_team_in_the_applied_filter()
        {
            SetupTeam();
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.Is<StatisticsFilter>(f => f.Team == null))).Returns(" in 2020");
            _statisticsFilterHumanizer.Setup(x => x.EntitiesMatchingFilter("Statistics", " in 2020")).Returns("Statistics in 2020");

            var model = await ReadModel();

            Assert.Equal("Statistics in 2020", model.FilterViewModel.FilterDescription);
            Assert.Equal("Statistics", model.FilterViewModel.FilteredItemTypePlural);
            Assert.True(model.FilterViewModel.SupportsDateFilter);
            Assert.Equal("Statistics for Example team stoolball team in 2020", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Model_has_description_based_on_team_description()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal($"Statistics for Example team, a {team.Description().Substring(2)}", model.Metadata.Description);
        }

        [Fact]
        public async Task Model_has_breadcrumbs_excluding_the_team()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, team, false), Times.Once);
        }
    }
}
