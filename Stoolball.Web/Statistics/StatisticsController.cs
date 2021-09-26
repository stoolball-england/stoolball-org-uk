using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics
{
    public class StatisticsController : RenderMvcControllerAsync
    {
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceInAMatchStatisticsDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestTotalStatisticsDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public StatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceInAMatchStatisticsDataSource,
           IBestPlayerTotalStatisticsDataSource bestTotalStatisticsDataSource,
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _bestPerformanceInAMatchStatisticsDataSource = bestPerformanceInAMatchStatisticsDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceInAMatchStatisticsDataSource));
            _bestTotalStatisticsDataSource = bestTotalStatisticsDataSource ?? throw new ArgumentNullException(nameof(bestTotalStatisticsDataSource));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsSummaryViewModel(contentModel.Content, Services?.UserService);
            model.IsAuthorized[AuthorizedAction.EditStatistics] = Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);

            model.DefaultFilter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
            model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, HttpUtility.ParseQueryString(Request.Url.Query));
            model.PlayerInnings = (await _bestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
            model.MostRuns = (await _bestTotalStatisticsDataSource.ReadMostRunsScored(model.AppliedFilter).ConfigureAwait(false)).ToList();
            model.MostWickets = (await _bestTotalStatisticsDataSource.ReadMostWickets(model.AppliedFilter).ConfigureAwait(false)).ToList();
            model.BowlingFigures = (await _bestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
            model.MostCatches = (await _bestTotalStatisticsDataSource.ReadMostCatches(model.AppliedFilter).ConfigureAwait(false)).ToList();

            model.FilterDescription = _statisticsFilterHumanizer.StatisticsMatchingUserFilter(model.AppliedFilter);
            model.Metadata.PageTitle = "Statistics for all teams" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }
    }
}