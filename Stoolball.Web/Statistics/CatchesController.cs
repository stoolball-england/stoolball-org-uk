using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public class CatchesController : RenderController, IRenderControllerAsync
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IPlayerSummaryStatisticsDataSource _playerSummaryStatisticsDataSource;
        private readonly IPlayerPerformanceStatisticsDataSource _playerPerformanceDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public CatchesController(ILogger<CatchesController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IPlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource,
            IPlayerPerformanceStatisticsDataSource playerPerformanceDataSource,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
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
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsViewModel<PlayerInnings>(CurrentPage) { ShowCaption = false };
            model.DefaultFilter = await _statisticsFilterFactory.FromRoute(Request.Path);
            model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, Request.QueryString.Value);

            var catchesFilter = model.AppliedFilter.Clone();
            catchesFilter.Player = null;
            catchesFilter.CaughtByPlayerIdentityIds = model.AppliedFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList();
            model.Results = (await _playerPerformanceDataSource.ReadPlayerInnings(catchesFilter)).ToList();

            model.AppliedFilter.Paging.PageUrl = new Uri(Request.GetEncodedUrl());
            model.AppliedFilter.Paging.Total = (await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(model.AppliedFilter)).TotalCatches;

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);

            model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Catches", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
            model.Metadata.PageTitle = "Catches" + _statisticsFilterHumanizer.MatchingFixedFilter(model.AppliedFilter) + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }
    }
}