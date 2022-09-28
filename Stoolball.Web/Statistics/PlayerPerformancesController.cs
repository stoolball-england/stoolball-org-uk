using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class PlayerPerformancesController : BaseStatisticsTableController<PlayerIdentityPerformance>, IRenderControllerAsync
    {
        public PlayerPerformancesController(ILogger<PlayerPerformancesController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPerformanceInAMatchStatisticsDataSource statisticsDataSource,
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
                  filter => statisticsDataSource.ReadPlayerIdentityPerformances(filter),
                  filter => statisticsDataSource.ReadTotalPlayerIdentityPerformances(filter),
                  "Player performances",
                  "Player performances"
                  )
        { }
    }
}