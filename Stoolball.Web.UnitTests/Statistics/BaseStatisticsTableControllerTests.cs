using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class BaseStatisticsTableControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsBreadcrumbBuilder> _breadcrumbBuilder = new();
        private readonly Mock<IStatisticsFilterHumanizer> _filterHumanizer = new();
        private readonly Mock<IVerifiableMethodCalls<AnyClass>> _statisticsQueryMethods = new();
        private const string FILTER_ENTITY_PLURAL = "Things";
        private const string PAGE_TITLE = "Amazing things";

        public class AnyClass { }

        public interface IVerifiableMethodCalls<T>
        {
            Task<IEnumerable<StatisticsResult<T>>> ReadResults(StatisticsFilter filter);
            Task<int> ReadTotalResults(StatisticsFilter filter);
        }

        public class ConcreteControllerForTesting : BaseStatisticsTableController<AnyClass>
        {
            public ConcreteControllerForTesting(ILogger<BaseStatisticsTableController<AnyClass>> logger,
                ICompositeViewEngine compositeViewEngine,
                IUmbracoContextAccessor umbracoContextAccessor,
                IStatisticsFilterFactory statisticsFilterFactory,
                IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
                IStatisticsFilterHumanizer statisticsFilterHumanizer,
                Func<StatisticsFilter, Task<IEnumerable<StatisticsResult<AnyClass>>>> readResults,
                Func<StatisticsFilter, Task<int>> readTotalResults,
                int? minimumQualifyingInningsUnfiltered,
                int? minimumQualifyingInningsFiltered,
                Func<StatisticsFilter, bool>? validateFilter = null)
                : base(logger,
                      compositeViewEngine,
                      umbracoContextAccessor,
                      statisticsFilterFactory,
                      statisticsBreadcrumbBuilder,
                      statisticsFilterHumanizer,
                      readResults,
                      readTotalResults,
                      filter => PAGE_TITLE,
                      FILTER_ENTITY_PLURAL,
                      minimumQualifyingInningsUnfiltered,
                      minimumQualifyingInningsFiltered,
                      validateFilter)
            {
            }
        }

        private ConcreteControllerForTesting CreateController(int? minimumQualifyingInningsUnfiltered = null, int? minimumQualifyingInningsFiltered = null, Func<StatisticsFilter, bool>? validateFilter = null)
        {
            return new ConcreteControllerForTesting(
                Mock.Of<ILogger<ConcreteControllerForTesting>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _breadcrumbBuilder.Object,
                _filterHumanizer.Object,
                _statisticsQueryMethods.Object.ReadResults,
                _statisticsQueryMethods.Object.ReadTotalResults,
                minimumQualifyingInningsUnfiltered,
                minimumQualifyingInningsFiltered,
                validateFilter)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Player CreatePlayerWithIdentities()
        {
            return new Player
            {
                PlayerIdentities = new List<PlayerIdentity>
                    {
                        new PlayerIdentity
                        {
                            PlayerIdentityName = "Example player 1",
                            Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Team name 1" },
                            TotalMatches = 5,
                        },
                        new PlayerIdentity
                        {
                            PlayerIdentityName = "Example player 2",
                            Team = new Team { TeamId = Guid.NewGuid(), TeamName = "Team name 2" },
                            TotalMatches = 10, // deliberately higher than the first identity to test that they're sorted later
                        }
                    }
            };
        }

        private static Club CreateClub()
        {
            return new Club
            {
                Teams = new List<Team>
                    {
                        new Team{ TeamRoute = "/teams/team-a", TeamName = "Team A"},
                        new Team{ TeamRoute = "/teams/team-b", TeamName = "Team B"}
                    }
            };
        }

        private static Season CreateSeason()
        {
            return new Season
            {
                Teams = new List<TeamInSeason>
                    {
                        new TeamInSeason { Team = new Team{ TeamRoute = "/teams/team-a", TeamName = "Team A"} },
                        new TeamInSeason { Team = new Team{ TeamRoute = "/teams/team-b", TeamName = "Team B"} }
                    }
            };
        }

        private void SetupMocks(StatisticsFilter defaultFilter, StatisticsFilter appliedFilter, List<StatisticsResult<AnyClass>> results)
        {
            Request.Setup(x => x.Path).Returns("/play/statistics");
            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterFactory.Setup(x => x.FromQueryString(Request.Object.QueryString.Value)).Returns(Task.FromResult(appliedFilter));
            _statisticsQueryMethods.Setup(x => x.ReadResults(It.Is<StatisticsFilter>(x => x.FromDate == appliedFilter.FromDate))).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<AnyClass>>));
            _statisticsQueryMethods.Setup(x => x.ReadTotalResults(It.Is<StatisticsFilter>(x => x.FromDate == appliedFilter.FromDate))).Returns(Task.FromResult(results.Count));
        }

        private static StatisticsResult<AnyClass> CreateQueryResult()
        {
            return new StatisticsResult<AnyClass>
            {
                Player = CreatePlayerWithIdentities()
            };
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Always_returns_StatisticsViewModel_for_valid_filter(bool queryReturnsResults)
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            if (queryReturnsResults)
            {
                results.Add(CreateQueryResult());
                results.Add(CreateQueryResult());
            };
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<AnyClass>>(((ViewResult)result).Model);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Invalid_filter_returns_NotFound(bool filterResult)
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController(validateFilter: filter => filterResult))
            {
                var result = await controller.Index();

                if (filterResult == true)
                {
                    Assert.IsType<StatisticsViewModel<AnyClass>>(((ViewResult)result).Model);
                }
                else
                {
                    Assert.IsType<NotFoundResult>(result);
                }
            }
        }

        [Fact]
        public async Task Filter_applied_to_queries_comes_from_querystring_modifies_default_filter_from_route()
        {
            var defaultFilter = new StatisticsFilter { FromDate = DateTimeOffset.UtcNow, UntilDate = DateTimeOffset.UtcNow };
            var appliedFilter = new StatisticsFilter { FromDate = DateTimeOffset.UtcNow.AddMinutes(1) };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);
                Assert.Equal(defaultFilter.FromDate, model.DefaultFilter.FromDate);
                Assert.Equal(appliedFilter.FromDate, model.AppliedFilter.FromDate);
                Assert.Equal(defaultFilter.UntilDate, model.AppliedFilter.UntilDate);
                _statisticsFilterFactory.Verify(x => x.FromRoute(Request.Object.Path), Times.Once);
                _statisticsFilterFactory.Verify(x => x.FromQueryString(Request.Object.QueryString.Value), Times.Once);
                _statisticsQueryMethods.Verify(x => x.ReadResults(It.Is<StatisticsFilter>(x => x.FromDate == appliedFilter.FromDate)), Times.Once);
            }
        }

        [Fact]
        public async Task Configures_paging()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            results.Add(CreateQueryResult());
            results.Add(CreateQueryResult());
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(results.Count, model.AppliedFilter.Paging.Total);
                Assert.Equal(Request.Object.Scheme + "://" + Request.Object.Host + Request.Object.Path, model.AppliedFilter.Paging.PageUrl?.ToString());
            }
        }


        [Fact]
        public async Task Builds_breadcrumbs()
        {
            var defaultFilter = new StatisticsFilter { FromDate = DateTimeOffset.UtcNow };
            var appliedFilter = new StatisticsFilter { FromDate = DateTimeOffset.UtcNow.AddMinutes(1) };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            List<Breadcrumb>? breadcrumbsPassedToMethod = null;
            _breadcrumbBuilder.Setup(x => x.BuildBreadcrumbs(It.IsAny<List<Breadcrumb>>(), It.Is<StatisticsFilter>(x => x.FromDate == defaultFilter.FromDate)))
                .Callback<List<Breadcrumb>, StatisticsFilter>((breadcrumbs, filter) =>
                {
                    breadcrumbsPassedToMethod = breadcrumbs;
                });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(breadcrumbsPassedToMethod, model.Breadcrumbs);
            }
        }

        [Fact]
        public async Task Sets_filter_description_heading_and_page_title_using_filters()
        {
            var defaultFilter = new StatisticsFilter { FromDate = DateTimeOffset.UtcNow };
            var appliedFilter = new StatisticsFilter { FromDate = DateTimeOffset.UtcNow.AddMinutes(1) };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            var expectedFixedFilter = "the fixed filter and ";
            var expectedAppliedFilter = "the applied filter";
            var expectedHeading = PAGE_TITLE + expectedFixedFilter;
            var expectedFilterDescription = "Things matching " + expectedAppliedFilter;
            _filterHumanizer.Setup(x => x.MatchingUserFilter(It.Is<StatisticsFilter>(x => x.FromDate == appliedFilter.FromDate))).Returns(expectedAppliedFilter);
            _filterHumanizer.Setup(x => x.MatchingDefaultFilter(defaultFilter)).Returns(expectedFixedFilter);
            _filterHumanizer.Setup(x => x.EntitiesMatchingFilter(FILTER_ENTITY_PLURAL, expectedAppliedFilter)).Returns(expectedFilterDescription);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(expectedFilterDescription, model.FilterViewModel.FilterDescription);
                Assert.Equal(expectedHeading, model.Heading);
                Assert.Equal(PAGE_TITLE + expectedFixedFilter + expectedAppliedFilter, model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Sets_FilterViewModel_properties_from_filters()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();
            appliedFilter.FromDate = DateTime.UtcNow.AddYears(-1);
            appliedFilter.UntilDate = DateTime.UtcNow.AddMonths(-6);
            appliedFilter.Team = new Team { TeamRoute = "/teams/example-team" };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(FILTER_ENTITY_PLURAL, model.FilterViewModel.FilteredItemTypePlural);
                Assert.Equal(appliedFilter.FromDate, model.FilterViewModel.from);
                Assert.Equal(appliedFilter.UntilDate, model.FilterViewModel.to);
                Assert.Equal(appliedFilter.Team.TeamRoute.Substring(7), model.FilterViewModel.team);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Sets_FilterViewModel_Teams_sorted_by_total_matches_if_DefaultFilter_has_player(bool hasPlayer)
        {
            var defaultFilter = new StatisticsFilter { Player = hasPlayer ? CreatePlayerWithIdentities() : null };
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                if (hasPlayer)
                {
                    Assert.Equal(defaultFilter.Player!.PlayerIdentities.Count, model.FilterViewModel.Teams.Count());
                    int? previous = int.MaxValue;
                    foreach (var team in model.FilterViewModel.Teams)
                    {
                        var identityForTeam = defaultFilter.Player.PlayerIdentities.SingleOrDefault(x => x.Team?.TeamId == team.TeamId);
                        Assert.NotNull(identityForTeam);
                        Assert.True(identityForTeam!.TotalMatches <= previous);
                        previous = identityForTeam.TotalMatches;
                    }
                }
                else
                {
                    Assert.Empty(model.FilterViewModel.Teams);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Sets_FilterViewModel_Teams_if_DefaultFilter_has_Club(bool hasClub)
        {
            var defaultFilter = new StatisticsFilter { Club = hasClub ? CreateClub() : null };
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                if (hasClub)
                {
                    Assert.Equal(defaultFilter.Club!.Teams.Count, model.FilterViewModel.Teams.Count());
                    foreach (var team in model.FilterViewModel.Teams)
                    {
                        var teamFromClub = defaultFilter.Club.Teams.SingleOrDefault(x => x.TeamRoute == team.TeamRoute);
                        Assert.NotNull(teamFromClub);
                    }
                }
                else
                {
                    Assert.Empty(model.FilterViewModel.Teams);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Sets_FilterViewModel_Teams_if_DefaultFilter_has_Season(bool hasSeason)
        {
            var defaultFilter = new StatisticsFilter { Season = hasSeason ? CreateSeason() : null };
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                if (hasSeason)
                {
                    Assert.Equal(defaultFilter.Season!.Teams.Count, model.FilterViewModel.Teams.Count());
                    foreach (var team in model.FilterViewModel.Teams)
                    {
                        var teamFromSeason = defaultFilter.Season.Teams.SingleOrDefault(x => x.Team != null && x.Team.TeamRoute == team.TeamRoute);
                        Assert.NotNull(teamFromSeason);
                    }
                }
                else
                {
                    Assert.Empty(model.FilterViewModel.Teams);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Hide_teams_column_if_filtered_by_team(bool hasTeamFilter)
        {
            var defaultFilter = new StatisticsFilter { Team = hasTeamFilter ? new Team() : null };
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(!hasTeamFilter, model.ShowTeamsColumn);
            }
        }

        public async Task If_team_filter_is_applied_to_default_player_filter_player_team_is_copied_to_applied_filter_and_view_model()
        {
            var defaultFilter = new StatisticsFilter
            {
                Player = CreatePlayerWithIdentities()
            };
            var appliedFilter = defaultFilter.Clone();
            appliedFilter.Team = new Team { TeamRoute = defaultFilter.Player.PlayerIdentities[0].Team!.TeamRoute };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(defaultFilter.Player.PlayerIdentities[0].Team!.TeamName, model.AppliedFilter.Team?.TeamName);
                Assert.Equal(defaultFilter.Player.PlayerIdentities[0].Team!.TeamName, model.FilterViewModel.TeamName);
            }
        }

        [Fact]
        public async Task If_team_filter_is_applied_to_default_club_filter_team_name_is_copied_to_applied_filter_and_view_model()
        {
            var defaultFilter = new StatisticsFilter
            {
                Club = CreateClub()
            };
            var appliedFilter = defaultFilter.Clone();
            appliedFilter.Team = new Team { TeamRoute = defaultFilter.Club.Teams[0].TeamRoute };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(defaultFilter.Club.Teams[0].TeamName, model.AppliedFilter.Team?.TeamName);
                Assert.Equal(defaultFilter.Club.Teams[0].TeamName, model.FilterViewModel.TeamName);
            }
        }

        [Fact]
        public async Task If_team_filter_is_applied_to_default_season_filter_team_name_is_copied_to_applied_filter_and_view_model()
        {
            var defaultFilter = new StatisticsFilter
            {
                Season = CreateSeason()
            };
            var appliedFilter = defaultFilter.Clone();
            appliedFilter.Team = new Team { TeamRoute = defaultFilter.Season.Teams[0].Team!.TeamRoute };

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(defaultFilter.Season.Teams[0].Team!.TeamName, model.AppliedFilter.Team?.TeamName);
                Assert.Equal(defaultFilter.Season.Teams[0].Team!.TeamName, model.FilterViewModel.TeamName);
            }
        }

        [Theory]
        [InlineData(null, null, false, false, false, false, false, null)]
        [InlineData(10, 5, false, false, false, false, false, 10)]
        [InlineData(10, 5, true, false, false, false, false, 5)]
        [InlineData(10, 5, false, true, false, false, false, 5)]
        [InlineData(10, 5, false, false, true, false, false, 5)]
        [InlineData(10, 5, false, false, false, true, false, 5)]
        [InlineData(10, 5, false, false, false, false, true, 5)]
        public async Task Minimum_qualifying_innings_set_from_constructor_depending_on_filter(int? minimumUnfiltered, int? minimumFiltered, bool hasTeamFilter, bool hasClubFilter, bool hasCompetitionFilter, bool hasSeasonFilter, bool hasLocationFilter, int? expected)
        {
            var defaultFilter = new StatisticsFilter
            {
                Team = hasTeamFilter ? new Team() : null,
                Club = hasClubFilter ? new Club() : null,
                Competition = hasCompetitionFilter ? new Competition() : null,
                Season = hasSeasonFilter ? new Season() : null,
                MatchLocation = hasLocationFilter ? new MatchLocation() : null
            };
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController(minimumUnfiltered, minimumFiltered))
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(expected, model.AppliedFilter.MinimumQualifyingInnings);
            }
        }
    }
}
