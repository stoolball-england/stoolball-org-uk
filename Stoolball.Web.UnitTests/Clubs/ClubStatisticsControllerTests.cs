using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Clubs;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class ClubStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IInningsStatisticsDataSource> _inningsDataSource = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _playerTotalDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumanizer = new();
        private readonly Club _club = new() { ClubId = Guid.NewGuid(), ClubName = "Example club" };
        private StatisticsFilter _parsedFilter = new();

        public ClubStatisticsControllerTests() : base()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<string>())).Returns(() => _parsedFilter);
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(string.Empty);
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(_club);
        }

        private ClubStatisticsController CreateController()
        {
            return new ClubStatisticsController(
                Mock.Of<ILogger<ClubStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _bestPerformanceDataSource.Object,
                _inningsDataSource.Object,
                _playerTotalDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<StatisticsSummaryViewModel<Club>> Run()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                return Assert.IsType<StatisticsSummaryViewModel<Club>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
                _inningsDataSource.Verify(x => x.ReadInningsStatistics(It.IsAny<StatisticsFilter>()), Times.Never);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_StatisticsSummaryViewModel_for_club()
        {
            var model = await Run();

            Assert.Equal(_club, model.Context);
            Assert.True(model.ShowInningsStatistics);
        }

        [Fact]
        public async Task Default_filter_is_for_club_with_top_ten_results()
        {
            var model = await Run();

            Assert.Equal(_club, model.DefaultFilter!.Club);
            Assert.Equal(10, model.DefaultFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual);
        }

        [Fact]
        public async Task Query_string_is_parsed_and_merged_into_applied_filter_without_changing_default_filter()
        {
            Request.SetupGet(x => x.QueryString).Returns(new QueryString("?from=2020-01-01"));
            _parsedFilter = new StatisticsFilter { FromDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };

            var model = await Run();

            _statisticsFilterQueryStringParser.Verify(x => x.ParseQueryString("?from=2020-01-01"), Times.Once);
            Assert.Equal(_parsedFilter.FromDate, model.AppliedFilter!.FromDate);
            Assert.Equal(_club, model.AppliedFilter.Club);
            Assert.Null(model.DefaultFilter!.FromDate);
        }

        [Fact]
        public async Task Team_in_query_string_is_replaced_by_matching_team_from_club()
        {
            var clubTeam = new Team { TeamId = Guid.NewGuid(), TeamRoute = "/teams/example" };
            _club.Teams.Add(clubTeam);
            _parsedFilter = new StatisticsFilter { Team = new Team { TeamRoute = "/teams/example" } };

            var model = await Run();

            Assert.Same(clubTeam, model.AppliedFilter!.Team);
            Assert.Equal(clubTeam.TeamRoute, model.FilterViewModel.TeamRoute);
        }

        [Fact]
        public async Task Team_in_query_string_not_in_club_is_removed_from_applied_filter()
        {
            _club.Teams.Add(new Team { TeamId = Guid.NewGuid(), TeamRoute = "/teams/example" });
            _parsedFilter = new StatisticsFilter { Team = new Team { TeamRoute = "/teams/other-club" } };

            var model = await Run();

            Assert.Null(model.AppliedFilter!.Team);
            Assert.Null(model.FilterViewModel.TeamRoute);
        }

        [Fact]
        public async Task Statistics_are_read_using_applied_filter()
        {
            var model = await Run();

            _inningsDataSource.Verify(x => x.ReadInningsStatistics(model.AppliedFilter!), Times.Once);
            _bestPerformanceDataSource.Verify(x => x.ReadPlayerInnings(model.AppliedFilter!, StatisticsSortOrder.BestFirst), Times.Once);
            _bestPerformanceDataSource.Verify(x => x.ReadBowlingFigures(model.AppliedFilter!, StatisticsSortOrder.BestFirst), Times.Once);
            _playerTotalDataSource.Verify(x => x.ReadMostRunsScored(model.AppliedFilter!), Times.Once);
            _playerTotalDataSource.Verify(x => x.ReadMostWickets(model.AppliedFilter!), Times.Once);
            _playerTotalDataSource.Verify(x => x.ReadMostCatches(model.AppliedFilter!), Times.Once);
        }

        [Fact]
        public async Task Statistics_results_are_added_to_view_model()
        {
            var innings = new InningsStatistics();
            _inningsDataSource.Setup(x => x.ReadInningsStatistics(It.IsAny<StatisticsFilter>())).ReturnsAsync(innings);
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(new[] { new StatisticsResult<PlayerInnings>() });
            _bestPerformanceDataSource.Setup(x => x.ReadBowlingFigures(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).ReturnsAsync(new[] { new StatisticsResult<BowlingFigures>(), new StatisticsResult<BowlingFigures>() });
            _playerTotalDataSource.Setup(x => x.ReadMostRunsScored(It.IsAny<StatisticsFilter>())).ReturnsAsync(new[] { new StatisticsResult<BestStatistic>() });
            _playerTotalDataSource.Setup(x => x.ReadMostWickets(It.IsAny<StatisticsFilter>())).ReturnsAsync(new[] { new StatisticsResult<BestStatistic>(), new StatisticsResult<BestStatistic>() });
            _playerTotalDataSource.Setup(x => x.ReadMostCatches(It.IsAny<StatisticsFilter>())).ReturnsAsync(new[] { new StatisticsResult<BestStatistic>(), new StatisticsResult<BestStatistic>(), new StatisticsResult<BestStatistic>() });

            var model = await Run();

            Assert.Same(innings, model.InningsStatistics);
            Assert.Single(model.PlayerInnings);
            Assert.Equal(2, model.BowlingFigures.Count);
            Assert.Single(model.MostRuns);
            Assert.Equal(2, model.MostWickets.Count);
            Assert.Equal(3, model.MostCatches.Count);
        }

        [Fact]
        public async Task Filter_view_model_is_populated_from_applied_filter_and_club_teams()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamRoute = "/teams/example" };
            _club.Teams.Add(team);
            _parsedFilter = new StatisticsFilter
            {
                FromDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UntilDate = new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero)
            };
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" in 2020");
            _statisticsFilterHumanizer.Setup(x => x.EntitiesMatchingFilter("Statistics", " in 2020")).Returns("Statistics in 2020");

            var model = await Run();

            Assert.Equal("Statistics", model.FilterViewModel.FilteredItemTypePlural);
            Assert.Equal("Statistics in 2020", model.FilterViewModel.FilterDescription);
            Assert.True(model.FilterViewModel.SupportsDateFilter);
            Assert.Equal(_parsedFilter.FromDate, model.FilterViewModel.FromDate);
            Assert.Equal(_parsedFilter.UntilDate, model.FilterViewModel.UntilDate);
            Assert.True(model.FilterViewModel.SupportsTeamFilter);
            Assert.Equal(new[] { team }, model.FilterViewModel.Teams);
        }

        [Fact]
        public async Task Club_without_teams_does_not_support_team_filter()
        {
            var model = await Run();

            Assert.False(model.FilterViewModel.SupportsTeamFilter);
            Assert.Empty(model.FilterViewModel.Teams);
        }

        [Fact]
        public async Task Page_title_and_description_include_club_name_and_user_filter()
        {
            _statisticsFilterHumanizer.Setup(x => x.MatchingUserFilter(It.IsAny<StatisticsFilter>())).Returns(" in 2020");

            var model = await Run();

            Assert.Equal("Statistics for Example club in 2020", model.Metadata.PageTitle);
            Assert.Equal("Statistics for matches played by all teams in Example club.", model.Metadata.Description);
        }

        [Fact]
        public async Task Adds_teams_breadcrumb()
        {
            var model = await Run();

            var breadcrumb = model.Breadcrumbs[^1];
            Assert.Equal(Constants.Pages.Teams, breadcrumb.Name);
            Assert.Equal(new Uri(Constants.Pages.TeamsUrl, UriKind.Relative), breadcrumb.Url);
        }
    }
}
