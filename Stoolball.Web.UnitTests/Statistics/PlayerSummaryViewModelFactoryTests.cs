using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class PlayerSummaryViewModelFactoryTests
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _summaryStatisticsDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumaniser = new();
        private readonly Mock<IStatisticsBreadcrumbBuilder> _breadcrumbBuilder = new();
        private readonly Mock<IPublishedContent> _currentPage = new();
        private const string REQUEST_PATH = "/example";
        private const string? REQUEST_QUERYSTRING = null;

        private PlayerSummaryViewModelFactory CreateFactory()
        {
            return new PlayerSummaryViewModelFactory(
                _playerDataSource.Object,
                _summaryStatisticsDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumaniser.Object,
                _breadcrumbBuilder.Object,
                Mock.Of<IUserService>());
        }


        [Fact]
        public async Task Filter_for_ReadPlayerByRoute_is_default_filter_modified_by_querystring()
        {
            var player = new Player();
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), REQUEST_QUERYSTRING)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(REQUEST_PATH, appliedFilter))
                .Callback<string, StatisticsFilter>((queryString, filter) =>
                {
                    Assert.Null(filter.Player); // Player filter is unwanted because we're selecting the player by route
                })
                .Returns(Task.FromResult(player));

            var factory = CreateFactory();
            var result = await factory.CreateViewModel(_currentPage.Object, REQUEST_PATH, REQUEST_QUERYSTRING);

            Assert.Equal(appliedFilter, result.AppliedFilter);
        }


        [Fact]
        public async Task Breadcrumbs_are_set_without_player_filter()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), REQUEST_QUERYSTRING)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(REQUEST_PATH, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));

            var callbackWasCalled = false;
            _breadcrumbBuilder.Setup(x => x.BuildBreadcrumbs(It.IsAny<List<Breadcrumb>>(), It.IsAny<StatisticsFilter>()))
                .Callback<List<Breadcrumb>, StatisticsFilter>((breadcrumbs, filter) =>
                {
                    callbackWasCalled = true;
                    Assert.Null(filter.Player);
                });

            var factory = CreateFactory();
            var result = await factory.CreateViewModel(_currentPage.Object, REQUEST_PATH, REQUEST_QUERYSTRING);

            Assert.True(callbackWasCalled);
        }

        [Fact]
        public async Task Filter_is_added_to_filter_description_and_page_title()
        {
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), REQUEST_QUERYSTRING)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(REQUEST_PATH, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));
            _statisticsFilterHumaniser.Setup(x => x.MatchingUserFilter(appliedFilter)).Returns("filter text");
            _statisticsFilterHumaniser.Setup(x => x.EntitiesMatchingFilter("Statistics", "filter text")).Returns("filter text");

            var factory = CreateFactory();
            var result = await factory.CreateViewModel(_currentPage.Object, REQUEST_PATH, REQUEST_QUERYSTRING);

            Assert.Contains("filter text", result.FilterDescription);
            Assert.Contains("filter text", result.Metadata.PageTitle);
        }

        [Fact]
        public async Task Player_name_is_in_page_title_and_description()
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity> {
                    new PlayerIdentity
                    {
                        PlayerIdentityName = "Player one",
                        Team = new Team { TeamName = "Example team" }
                    }
                }
            };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), REQUEST_QUERYSTRING)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(REQUEST_PATH, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));

            var factory = CreateFactory();
            var result = await factory.CreateViewModel(_currentPage.Object, REQUEST_PATH, REQUEST_QUERYSTRING);

            Assert.Contains("Player one", result.Metadata.PageTitle);
            Assert.Contains("Player one", result.Metadata.Description);
        }


        [Fact]
        public async Task Player_team_is_in_page_description()
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity> {
                    new PlayerIdentity
                    {
                        PlayerIdentityName = "Player one",
                        Team = new Team { TeamName = "Example team" }
                    }
                }
            };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), REQUEST_QUERYSTRING)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(REQUEST_PATH, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));

            var factory = CreateFactory();
            var result = await factory.CreateViewModel(_currentPage.Object, REQUEST_PATH, REQUEST_QUERYSTRING);

            Assert.Contains("Example team", result.Metadata.Description);
        }

        [Fact]
        public async Task Statistics_are_filtered_and_added_to_model()
        {
            var appliedFilter = new StatisticsFilter();
            var battingStatistics = new BattingStatistics();
            var bowlingStatistics = new BowlingStatistics();
            var fieldingStatistics = new FieldingStatistics();
            var player = new Player();

            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), REQUEST_QUERYSTRING)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(REQUEST_PATH, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
            _summaryStatisticsDataSource.Setup(x => x.ReadBattingStatistics(appliedFilter))
                .Callback<StatisticsFilter>((filter) =>
                {
                    Assert.Equal(player, filter.Player);
                })
                .Returns(Task.FromResult(battingStatistics));
            _summaryStatisticsDataSource.Setup(x => x.ReadBowlingStatistics(appliedFilter))
                .Callback<StatisticsFilter>((filter) =>
                {
                    Assert.Equal(player, filter.Player);
                })
                .Returns(Task.FromResult(bowlingStatistics));
            _summaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(appliedFilter))
                .Callback<StatisticsFilter>((filter) =>
                {
                    Assert.Equal(player, filter.Player);
                })
                .Returns(Task.FromResult(fieldingStatistics));

            var factory = CreateFactory();
            var result = await factory.CreateViewModel(_currentPage.Object, REQUEST_PATH, REQUEST_QUERYSTRING);

            Assert.Equal(battingStatistics, result.BattingStatistics);
            Assert.Equal(bowlingStatistics, result.BowlingStatistics);
            Assert.Equal(fieldingStatistics, result.FieldingStatistics);
        }
    }
}
