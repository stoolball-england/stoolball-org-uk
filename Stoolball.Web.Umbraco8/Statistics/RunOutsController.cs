using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
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
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _playerSummaryStatisticsDataSource = playerSummaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(playerSummaryStatisticsDataSource));
            _playerPerformanceDataSource = playerPerformanceDataSource ?? throw new ArgumentNullException(nameof(playerPerformanceDataSource));
            _statisticsBreadcrumbBuilder = statisticsBreadcrumbBuilder ?? throw new ArgumentNullException(nameof(statisticsBreadcrumbBuilder));
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

            var model = new StatisticsViewModel<PlayerInnings>(contentModel.Content, Services?.UserService) { ShowCaption = false };
            model.DefaultFilter = await _statisticsFilterFactory.FromRoute(Request.Url.AbsolutePath).ConfigureAwait(false);
            model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, HttpUtility.ParseQueryString(Request.Url.Query));

            var runOutsFilter = model.AppliedFilter.Clone();
            runOutsFilter.Player = null;
            runOutsFilter.RunOutByPlayerIdentityIds = model.AppliedFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList();
            model.Results = (await _playerPerformanceDataSource.ReadPlayerInnings(runOutsFilter).ConfigureAwait(false)).ToList();

            model.AppliedFilter.Paging.PageUrl = Request.Url;
            model.AppliedFilter.Paging.Total = (await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(model.AppliedFilter).ConfigureAwait(false)).TotalRunOuts;

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);

            model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Run-outs", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
            model.Metadata.PageTitle = "Run-outs" + _statisticsFilterHumanizer.MatchingFixedFilter(model.AppliedFilter) + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }
    }
}