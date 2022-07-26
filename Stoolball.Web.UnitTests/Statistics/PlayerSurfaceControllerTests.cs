using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Caching;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class PlayerSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _summaryStatisticsDataSource = new();
        private readonly Mock<IStatisticsFilterHumanizer> _statisticsFilterHumaniser = new();
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<ICacheClearer<Player>> _cacheClearer = new();

        public PlayerSurfaceControllerTests()
        {
            Setup();
        }

        private PlayerSurfaceController CreateController()
        {
            return new PlayerSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _playerDataSource.Object,
                _summaryStatisticsDataSource.Object,
                _playerRepository.Object,
                _statisticsFilterQueryStringParser.Object,
                _statisticsFilterHumaniser.Object,
                _cacheClearer.Object
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
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_if_ModelState_invalid_returns_PlayerSummaryViewModel_and_Player_view()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                var viewResult = ((ViewResult)result);
                Assert.IsType<PlayerSummaryViewModel>(viewResult.Model);
                Assert.Equal("Player", viewResult.ViewName);
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
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.Equal(appliedFilter, ((PlayerSummaryViewModel)(((ViewResult)result).Model)).AppliedFilter);
            }
        }

        [Fact]
        public async Task Breadcrumbs_are_set_if_ModelState_invalid()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.True(((PlayerSummaryViewModel)(((ViewResult)result).Model)).Breadcrumbs.Count > 0);
            }
        }

        [Fact]
        public async Task Filter_is_added_to_filter_description_and_page_title_if_ModelState_invalid()
        {
            var appliedFilter = new StatisticsFilter();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(appliedFilter);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player()));
            _statisticsFilterHumaniser.Setup(x => x.MatchingUserFilter(appliedFilter)).Returns("filter text");
            _statisticsFilterHumaniser.Setup(x => x.EntitiesMatchingFilter("Statistics", "filter text")).Returns("filter text");

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
                Assert.Contains("filter text", model.FilterDescription);
                Assert.Contains("filter text", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Player_name_is_in_page_title_and_description_if_ModelState_invalid()
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
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
                Assert.Contains("Player one", model.Metadata.PageTitle);
                Assert.Contains("Player one", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Player_team_is_in_page_description_if_ModelState_invalid()
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
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
                Assert.Contains("Example team", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Statistics_are_filtered_and_added_to_model_if_ModelState_invalid()
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
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                var model = (PlayerSummaryViewModel)((ViewResult)result).Model;
                Assert.Equal(battingStatistics, model.BattingStatistics);
                Assert.Equal(bowlingStatistics, model.BowlingStatistics);
                Assert.Equal(fieldingStatistics, model.FieldingStatistics);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_and_member_not_linked_if_ModelState_invalid_and_member_not_logged_in()
        {
            var player = new Player();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser)null));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.False(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_and_member_not_linked_if_ModelState_valid_and_member_not_logged_in()
        {
            var player = new Player();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult((MemberIdentityUser)null));

            using (var controller = CreateController())
            {
                controller.ModelState.Clear();
                var result = await controller.LinkPlayerToMemberAccount();

                var model = ((PlayerSummaryViewModel)(((ViewResult)result).Model));
                Assert.False(model.IsCurrentMember);
                Assert.False(model.LinkedByThisRequest);
                Assert.Null(model.Player.MemberKey);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_false_if_ModelState_invalid_and_current_member_is_not_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player { MemberKey = assignedMemberKey }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = Guid.NewGuid() }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.False(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }

        [Fact]
        public async Task IsCurrentMember_is_true_if_ModelState_invalid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new Player { MemberKey = assignedMemberKey }));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                Assert.True(((PlayerSummaryViewModel)(((ViewResult)result).Model)).IsCurrentMember);
            }
        }


        [Fact]
        public async Task IsCurrentMember_is_true_and_member_linked_if_ModelState_valid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            var player = new Player { MemberKey = assignedMemberKey };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey, Name = "Assigned member" }));

            using (var controller = CreateController())
            {
                controller.ModelState.Clear();
                var result = await controller.LinkPlayerToMemberAccount();

                var model = ((PlayerSummaryViewModel)(((ViewResult)result).Model));
                Assert.True(model.IsCurrentMember);
                Assert.True(model.LinkedByThisRequest);
                Assert.Equal(model.Player.MemberKey, assignedMemberKey);
                _playerRepository.Verify(x => x.LinkPlayerToMemberAccount(player, assignedMemberKey, "Assigned member"), Times.Once);
            }
        }

        [Fact]
        public async Task Cache_not_cleared_if_ModelState_invalid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            var player = new Player { MemberKey = assignedMemberKey };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey }));

            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");
                var result = await controller.LinkPlayerToMemberAccount();

                _cacheClearer.Verify(x => x.ClearCacheFor(player), Times.Never);
            }
        }

        [Fact]
        public async Task Cache_cleared_if_ModelState_valid_and_current_member_is_assigned_member()
        {
            var assignedMemberKey = Guid.NewGuid();
            var player = new Player { MemberKey = assignedMemberKey };
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), Request.Object.QueryString.Value)).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(Request.Object.Path, It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(player));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = assignedMemberKey }));

            using (var controller = CreateController())
            {
                controller.ModelState.Clear();
                var result = await controller.LinkPlayerToMemberAccount();

                _cacheClearer.Verify(x => x.ClearCacheFor(player), Times.Once);
            }
        }
    }
}
