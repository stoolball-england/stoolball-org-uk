using System;
using System.Collections.Generic;
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
    public class CompetitionStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestTotalDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumanizer = new();

        private CompetitionStatisticsController CreateController()
        {
            return new CompetitionStatisticsController(
                Mock.Of<ILogger<CompetitionStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                _bestPerformanceDataSource.Object,
                _bestTotalDataSource.Object,
                _statisticsFilterFactory.Object,
                _statisticsFilterHumanizer.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).Returns(Task.FromResult(new StatisticsFilter()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_StatisticsSummaryViewModel()
        {
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).Returns(Task.FromResult(new StatisticsFilter()));
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = Guid.NewGuid() });
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
            }
        }
        private Competition SetupCompetition(StatisticsFilter? queryStringFilter = null)
        {
            var competition = new Competition { CompetitionId = Guid.NewGuid(), CompetitionName = "Example league" };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _statisticsFilterFactory.Setup(x => x.FromQueryString(It.IsAny<string>())).ReturnsAsync(queryStringFilter ?? new StatisticsFilter());
            return competition;
        }

        [Fact]
        public async Task Default_filter_is_limited_to_competition_and_top_ten()
        {
            var competition = SetupCompetition();

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
            Assert.Equal(competition, model.Context);
            Assert.Equal(competition, model.DefaultFilter.Competition);
            Assert.Equal(10, model.DefaultFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual);
            Assert.Equal(competition, model.AppliedFilter.Competition);
        }

        [Fact]
        public async Task Query_string_filter_is_merged_into_applied_filter_but_not_default_filter()
        {
            var from = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            SetupCompetition(new StatisticsFilter { FromDate = from });

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
            Assert.Equal(from, model.AppliedFilter.FromDate);
            Assert.Null(model.DefaultFilter.FromDate);
            Assert.Equal(from, model.FilterViewModel.FromDate);
        }

        [Fact]
        public async Task Statistics_are_read_using_applied_filter()
        {
            SetupCompetition();
            var innings = new List<StatisticsResult<PlayerInnings>> { new() };
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(innings);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
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
            SetupCompetition(new StatisticsFilter { Team = new Stoolball.Teams.Team { TeamName = "Example team", TeamRoute = "/teams/example" } });
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" for Example team");
            _statisticsFilterHumanizer.Setup(x => x.EntitiesMatchingFilter("Statistics", " for Example team")).Returns("Statistics for Example team");

            using var controller = CreateController();

            var result = await controller.Index();

            var filterViewModel = Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model).FilterViewModel;
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
            SetupCompetition();
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" in 2020");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
            Assert.Equal("Statistics for Example league in 2020", model.Metadata.PageTitle);
            Assert.Equal("Statistics for stoolball matches played in all the years of the Example league.", model.Metadata.Description);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^1].Name);
        }
    }
}
