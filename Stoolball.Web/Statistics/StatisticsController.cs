using System;
using System.Linq;
using System.Threading.Tasks;
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

        public StatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceInAMatchStatisticsDataSource,
           IBestPlayerTotalStatisticsDataSource bestTotalStatisticsDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _bestPerformanceInAMatchStatisticsDataSource = bestPerformanceInAMatchStatisticsDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceInAMatchStatisticsDataSource));
            _bestTotalStatisticsDataSource = bestTotalStatisticsDataSource ?? throw new ArgumentNullException(nameof(bestTotalStatisticsDataSource));
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

            model.StatisticsFilter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
            model.PlayerInnings = (await _bestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
            model.MostRuns = (await _bestTotalStatisticsDataSource.ReadMostRunsScored(model.StatisticsFilter).ConfigureAwait(false)).ToList();
            model.BowlingFigures = (await _bestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            model.Metadata.PageTitle = $"Statistics for all teams";

            return CurrentTemplate(model);
        }
    }
}