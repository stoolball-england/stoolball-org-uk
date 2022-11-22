using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class MostXWicketsController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public MostXWicketsController(ILogger<MostXWicketsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPlayerTotalStatisticsDataSource statisticsDataSource,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger,
                  compositeViewEngine,
                  umbracoContextAccessor,
                  statisticsFilterFactory,
                  statisticsBreadcrumbBuilder,
                  statisticsFilterHumanizer,
                  filter => statisticsDataSource.ReadMostInningsWithBowling(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithWickets(filter),
                  filter => $"Most times taking {filter.MinimumWicketsTaken} or more wickets in an innings",
                  "Bowling figures"
                  )
        { }
    }
}