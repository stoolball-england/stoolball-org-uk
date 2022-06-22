using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class PlayerBowlingControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _playerSummaryDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumaniser = new();

        public PlayerBowlingControllerTests()
        {
            Setup();
        }

        private PlayerBowlingController CreateController()
        {
            return new PlayerBowlingController(
                Mock.Of<ILogger<PlayerBowlingController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _playerDataSource.Object,
                _playerSummaryDataSource.Object,
                _bestPerformanceDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumaniser.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_player_returns_404()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_returns_PlayerBowlingViewModel()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player>(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<PlayerBowlingViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Filter_is_default_filter_modified_by_querystring()
        {
            var player = new Player();
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value))
                .Callback<StatisticsFilter, string>((filter, queryString) =>
                {
                    Assert.Equal(player, filter.Player);
                })
                .Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(appliedFilter, ((PlayerBowlingViewModel)(((ViewResult)result).Model)).AppliedFilter);
            }
        }

        [Fact]
        public async Task Breadcrumbs_are_set()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((PlayerBowlingViewModel)(((ViewResult)result).Model)).Breadcrumbs.Count > 0);
            }
        }

        [Fact]
        public async Task Filter_is_added_to_filter_description_and_page_title()
        {
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult(new Player()));
            _statisticsFilterHumaniser.Setup(x => x.MatchingUserFilter(appliedFilter)).Returns("filter text");
            _statisticsFilterHumaniser.Setup(x => x.EntitiesMatchingFilter("Statistics", "filter text")).Returns("filter text");

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerBowlingViewModel)((ViewResult)result).Model;
                Assert.Contains("filter text", model.FilterDescription);
                Assert.Contains("filter text", model.Metadata.PageTitle);
            }
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
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerBowlingViewModel)((ViewResult)result).Model;
                Assert.Contains("Player one", model.Metadata.PageTitle);
                Assert.Contains("Player one", model.Metadata.Description);
            }
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
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerBowlingViewModel)((ViewResult)result).Model;
                Assert.Contains("Example team", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Statistics_are_filtered_and_added_to_model()
        {
            var appliedFilter = new StatisticsFilter();
            var bowlingStatistics = new BowlingStatistics();
            var bowlingFigures = new List<StatisticsResult<BowlingFigures>>();

            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult(new Player()));
            _playerSummaryDataSource.Setup(x => x.ReadBowlingStatistics(appliedFilter)).Returns(Task.FromResult(bowlingStatistics));
            _bestPerformanceDataSource.Setup(x => x.ReadBowlingFigures(appliedFilter, StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(bowlingFigures as IEnumerable<StatisticsResult<BowlingFigures>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerBowlingViewModel)((ViewResult)result).Model;
                Assert.Equal(bowlingStatistics, model.BowlingStatistics);
                Assert.Equal(bowlingFigures, model.BowlingFigures);
            }
        }
    }
}
