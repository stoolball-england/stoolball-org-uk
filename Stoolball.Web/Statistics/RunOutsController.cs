using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Statistics
{
    public class RunOutsController : RenderMvcControllerAsync
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IPlayerSummaryStatisticsDataSource _playerSummaryStatisticsDataSource;
        private readonly IPlayerPerformanceStatisticsDataSource _playerPerformanceDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public RunOutsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsFilterFactory statisticsFilterFactory,
           IPlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource,
           IPlayerPerformanceStatisticsDataSource playerPerformanceDataSource,
           IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _playerSummaryStatisticsDataSource = playerSummaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(playerSummaryStatisticsDataSource));
            _playerPerformanceDataSource = playerPerformanceDataSource ?? throw new ArgumentNullException(nameof(playerPerformanceDataSource));
            _statisticsBreadcrumbBuilder = statisticsBreadcrumbBuilder ?? throw new ArgumentNullException(nameof(statisticsBreadcrumbBuilder));
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

            var model = new StatisticsViewModel<PlayerInnings>(contentModel.Content, Services?.UserService) { ShowCaption = false };
            model.AppliedFilter = await _statisticsFilterFactory.FromRoute(Request.Url.AbsolutePath).ConfigureAwait(false);
            var catchesFilter = new StatisticsFilter
            {
                RunOutByPlayerIdentityIds = model.AppliedFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList(),
                Paging = model.AppliedFilter.Paging
            };
            model.Results = (await _playerPerformanceDataSource.ReadPlayerInnings(catchesFilter).ConfigureAwait(false)).ToList();

            model.AppliedFilter.Paging.PageUrl = Request.Url;
            model.AppliedFilter.Paging.Total = (await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(model.AppliedFilter).ConfigureAwait(false)).TotalRunOuts;

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);
            model.Metadata.PageTitle = "Run-outs" + _statisticsFilterHumanizer.MatchingFixedFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }
    }
}