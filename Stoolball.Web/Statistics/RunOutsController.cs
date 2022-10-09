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
    public class RunOutsController : BaseStatisticsTableController<PlayerInnings>, IRenderControllerAsync
    {
        public RunOutsController(ILogger<RunOutsController> logger,
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
                      var runOutsFilter = filter.Clone();
                      runOutsFilter.Player = null;
                      runOutsFilter.RunOutByPlayerIdentityIds = filter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList();
                      return (await playerPerformanceDataSource.ReadPlayerInnings(runOutsFilter)).ToList();
                  },
                  async filter => (await playerSummaryStatisticsDataSource.ReadFieldingStatistics(filter)).TotalRunOuts,
                  filter => "Run-outs",
                  "Run-outs",
                  validateFilter: filter => filter.Player != null
                  )
        { }
    }
}