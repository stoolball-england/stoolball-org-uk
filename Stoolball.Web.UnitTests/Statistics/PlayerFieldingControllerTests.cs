using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class PlayerFieldingControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _playerSummaryDataSource = new();
        private readonly Mock<IPlayerPerformanceStatisticsDataSource> _playerPerformanceDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumaniser = new();

        private PlayerFieldingController CreateController()
        {
            return new PlayerFieldingController(
                Mock.Of<ILogger<PlayerFieldingController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _playerDataSource.Object,
                _playerSummaryDataSource.Object,
                _playerPerformanceDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumaniser.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Player CreatePlayer()
        {
            return new Player
            {
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity
                    {
                        PlayerIdentityId = Guid.NewGuid(),
                        PlayerIdentityName = "Player one",
                        Team = new Team{ TeamName = "Example team"}
                    }
                }
            };
        }

        [Fact]
        public async Task Route_not_matching_player_returns_404()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_returns_PlayerFieldingViewModel()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter { Player = new Player() });
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<PlayerFieldingViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Filter_is_default_filter_modified_by_querystring()
        {
            var player = CreatePlayer();
            var appliedFilter = new StatisticsFilter { Player = player };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value))
                .Callback<StatisticsFilter, string>((filter, queryString) =>
                {
                    Assert.Equal(player, filter.Player);
                })
                .Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(appliedFilter, ((PlayerFieldingViewModel)(((ViewResult)result).Model)).AppliedFilter);
            }
        }


        [Fact]
        public async Task Breadcrumbs_are_set()
        {
            var player = CreatePlayer();
            var appliedFilter = new StatisticsFilter { Player = player };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.True(((PlayerFieldingViewModel)(((ViewResult)result).Model)).Breadcrumbs.Count > 0);
            }
        }

        [Fact]
        public async Task Filter_is_added_to_filter_description_and_page_title()
        {
            var player = CreatePlayer();
            var appliedFilter = new StatisticsFilter { Player = player };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(player));
            _statisticsFilterHumaniser.Setup(x => x.MatchingUserFilter(appliedFilter)).Returns("filter text");
            _statisticsFilterHumaniser.Setup(x => x.EntitiesMatchingFilter("Statistics", "filter text")).Returns("filter text");

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerFieldingViewModel)((ViewResult)result).Model;
                Assert.Contains("filter text", model.FilterDescription);
                Assert.Contains("filter text", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Player_name_is_in_page_title_and_description()
        {
            var player = CreatePlayer();
            var appliedFilter = new StatisticsFilter { Player = player };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerFieldingViewModel)((ViewResult)result).Model;
                Assert.Contains("Player one", model.Metadata.PageTitle);
                Assert.Contains("Player one", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Player_team_is_in_page_description()
        {
            var player = CreatePlayer();
            var appliedFilter = new StatisticsFilter { Player = player };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(player));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerFieldingViewModel)((ViewResult)result).Model;
                Assert.Contains("Example team", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Filter_team_is_passed_to_humaniser_with_name_added()
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity> {
                    new PlayerIdentity
                    {
                        PlayerIdentityName = "Player one",
                        Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team" }
                    }
                }
            };
            var appliedFilterWithoutNameFromQueryString = new StatisticsFilter { Player = CreatePlayer(), Team = new Team { TeamId = player.PlayerIdentities[0].Team!.TeamId } };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilterWithoutNameFromQueryString);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult((Player?)player));
            _statisticsFilterHumaniser.Setup(x => x.MatchingUserFilter(appliedFilterWithoutNameFromQueryString)).Callback<StatisticsFilter>(x =>
            {
                Assert.Equal(player.PlayerIdentities[0].Team!.TeamName, x.Team?.TeamName);
            });

            using (var controller = CreateController())
            {
                _ = await controller.Index();
            }
        }

        [Fact]
        public async Task Statistics_are_filtered_and_added_to_model()
        {
            var player = CreatePlayer();
            var appliedFilter = new StatisticsFilter { Player = player };
            var fieldingStatistics = new FieldingStatistics();
            var catches = new List<StatisticsResult<PlayerInnings>>();
            var runouts = new List<StatisticsResult<PlayerInnings>>();

            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>(), null)).Returns(Task.FromResult<Player?>(player));
            _playerSummaryDataSource.Setup(x => x.ReadFieldingStatistics(appliedFilter)).Returns(Task.FromResult(fieldingStatistics));

            var firstCall = true;
            _playerPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>()))
                .Callback<StatisticsFilter>(filter =>
                {
                    if (firstCall) { Assert.Contains(player.PlayerIdentities[0].PlayerIdentityId!.Value, filter.CaughtByPlayerIdentityIds); firstCall = false; }
                    else { Assert.Contains(player.PlayerIdentities[0].PlayerIdentityId!.Value, filter.RunOutByPlayerIdentityIds); }
                })
                .Returns(firstCall
                        ? Task.FromResult(catches as IEnumerable<StatisticsResult<PlayerInnings>>)
                        : Task.FromResult(runouts as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (PlayerFieldingViewModel)((ViewResult)result).Model;
                Assert.Equal(fieldingStatistics, model.FieldingStatistics);
                Assert.Equal(catches, model.Catches);
                Assert.Equal(runouts, model.RunOuts);
            }
        }
    }
}
