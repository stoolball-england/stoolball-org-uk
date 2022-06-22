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
    public class PlayerControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _summaryStatisticsDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumaniser = new();

        public PlayerControllerTests()
        {
            Setup();
        }

        private PlayerController CreateController()
        {
            return new PlayerController(
                Mock.Of<ILogger<PlayerController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _playerDataSource.Object,
                _summaryStatisticsDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumaniser.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_player_returns_404()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, null)).Returns(Task.FromResult<Player>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_returns_PlayerSummaryViewModel()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<PlayerSummaryViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Filter_for_ReadPlayerByRoute_is_default_filter_modified_by_querystring()
        {
            var player = new Player();
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, appliedFilter))
                .Callback<string, StatisticsFilter>((queryString, filter) =>
                {
                    Assert.Null(filter.Player); // Player filter is unwanted because we're selecting the player by route
                })
                .Returns(Task.FromResult(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(appliedFilter, ((PlayerSummaryViewModel)(((ViewResult)result).Model)).AppliedFilter);
            }
        }

        [Fact]
        public async Task Breadcrumbs_are_set()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((PlayerSummaryViewModel)(((ViewResult)result).Model)).Breadcrumbs.Count > 0);
            }
        }

        [Fact]
        public async Task Filter_is_added_to_filter_description_and_page_title()
        {
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));
            _statisticsFilterHumaniser.Setup(x => x.MatchingUserFilter(appliedFilter)).Returns("filter text");
            _statisticsFilterHumaniser.Setup(x => x.EntitiesMatchingFilter("Statistics", "filter text")).Returns("filter text");

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
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
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
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
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
                Assert.Contains("Example team", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Statistics_are_filtered_and_added_to_model()
        {
            var appliedFilter = new StatisticsFilter();
            var battingStatistics = new BattingStatistics();
            var bowlingStatistics = new BowlingStatistics();
            var fieldingStatistics = new FieldingStatistics();
            var player = new Player();

            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
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

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
                Assert.Equal(battingStatistics, model.BattingStatistics);
                Assert.Equal(bowlingStatistics, model.BowlingStatistics);
                Assert.Equal(fieldingStatistics, model.FieldingStatistics);
            }
        }
    }
}
