using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class CatchesController : BaseStatisticsTableController<PlayerInnings>, IRenderControllerAsync
    {
        public CatchesController(ILogger<CatchesController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IPlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource,
            IPlayerPerformanceStatisticsDataSource playerPerformanceDataSource,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger,
                  compositeViewEngine,
                  umbracoContextAccessor,
                  statisticsFilterFactory,
                  statisticsBreadcrumbBuilder,
                  statisticsFilterQueryStringParser,
                  statisticsFilterHumanizer,
                  async filter =>
                  {
                      if (filter.Player == null) { throw new ArgumentNullException(nameof(filter)); }
                      var catchesFilter = filter.Clone();
                      catchesFilter.Player = null;
                      catchesFilter.CaughtByPlayerIdentityIds = filter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList();
                      return (await playerPerformanceDataSource.ReadPlayerInnings(catchesFilter)).ToList();
                  },
                  async filter => (await playerSummaryStatisticsDataSource.ReadFieldingStatistics(filter)).TotalCatches,
                  filter => "Catches",
                  "Catches",
                  validateFilter: filter => filter.Player != null
                  )
        { }
    }
}