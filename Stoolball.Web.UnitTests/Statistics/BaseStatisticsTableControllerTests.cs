using System;
using System.Collections.Generic;
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
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
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
                IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
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
                      statisticsFilterQueryStringParser,
                      statisticsFilterHumanizer,
                      readResults,
                      readTotalResults,
                      PAGE_TITLE,
                      FILTER_ENTITY_PLURAL,
                      minimumQualifyingInningsUnfiltered,
                      minimumQualifyingInningsFiltered,
                      validateFilter)
            {
            }
        }

        public BaseStatisticsTableControllerTests() : base()
        {
        }

        private ConcreteControllerForTesting CreateController(int? minimumQualifyingInningsUnfiltered = null, int? minimumQualifyingInningsFiltered = null, Func<StatisticsFilter, bool>? validateFilter = null)
        {
            return new ConcreteControllerForTesting(
                Mock.Of<ILogger<ConcreteControllerForTesting>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _breadcrumbBuilder.Object,
                _statisticsFilterQueryStringParser.Object,
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
        private void SetupMocks(StatisticsFilter defaultFilter, StatisticsFilter appliedFilter, List<StatisticsResult<AnyClass>> results)
        {
            Request.Setup(x => x.Path).Returns("/play/statistics");
            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, Request.Object.QueryString.Value)).Returns(appliedFilter);
            _statisticsQueryMethods.Setup(x => x.ReadResults(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<AnyClass>>));
            _statisticsQueryMethods.Setup(x => x.ReadTotalResults(appliedFilter)).Returns(Task.FromResult(results.Count));
        }

        private static StatisticsResult<AnyClass> CreateQueryResult()
        {
            return new StatisticsResult<AnyClass>
            {
                Player = new Player
                {
                    PlayerIdentities = new List<PlayerIdentity>{
                                        new PlayerIdentity{
                                            PlayerIdentityName = "Example player"
                                        }
                                    }
                }
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
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);
                Assert.Equal(model.DefaultFilter, defaultFilter);
                Assert.Equal(model.AppliedFilter, appliedFilter);
                _statisticsFilterFactory.Verify(x => x.FromRoute(Request.Object.Path), Times.Once);
                _statisticsFilterQueryStringParser.Verify(x => x.ParseQueryString(defaultFilter, Request.Object.QueryString.Value), Times.Once);
                _statisticsQueryMethods.Verify(x => x.ReadResults(appliedFilter), Times.Once);
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
                Assert.Equal(Request.Object.Scheme + "://" + Request.Object.Host + Request.Object.Path, model.AppliedFilter.Paging.PageUrl.ToString());
            }
        }


        [Fact]
        public async Task Builds_breadcrumbs()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            List<Breadcrumb>? breadcrumbsPassedToMethod = null;
            _breadcrumbBuilder.Setup(x => x.BuildBreadcrumbs(It.IsAny<List<Breadcrumb>>(), appliedFilter)).Callback<List<Breadcrumb>, StatisticsFilter>((breadcrumbs, filter) => { breadcrumbsPassedToMethod = breadcrumbs; });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(breadcrumbsPassedToMethod, model.Breadcrumbs);
            }
        }

        [Fact]
        public async Task Sets_description_and_page_title_using_filters()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            var results = new List<StatisticsResult<AnyClass>>();
            SetupMocks(defaultFilter, appliedFilter, results);

            var expectedFixedFilter = "the fixed filter and ";
            var expectedAppliedFilter = "the applied filter";
            var expectedFilterDescription = "Things matching " + expectedAppliedFilter;
            _filterHumanizer.Setup(x => x.MatchingUserFilter(appliedFilter)).Returns(expectedAppliedFilter);
            _filterHumanizer.Setup(x => x.MatchingFixedFilter(appliedFilter)).Returns(expectedFixedFilter);
            _filterHumanizer.Setup(x => x.EntitiesMatchingFilter(FILTER_ENTITY_PLURAL, expectedAppliedFilter)).Returns(expectedFilterDescription);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = ((StatisticsViewModel<AnyClass>)((ViewResult)result).Model);

                Assert.Equal(expectedFilterDescription, model.FilterDescription);
                Assert.Equal(PAGE_TITLE + expectedFixedFilter + expectedAppliedFilter, model.Metadata.PageTitle);
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
