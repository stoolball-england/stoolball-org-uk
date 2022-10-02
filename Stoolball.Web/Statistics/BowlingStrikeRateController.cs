using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class BowlingStrikeRateController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public BowlingStrikeRateController(ILogger<BowlingStrikeRateController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPlayerAverageStatisticsDataSource statisticsDataSource,
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
                  filter => statisticsDataSource.ReadBestBowlingStrikeRate(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithBowlingStrikeRate(filter),
                  filter => "Best bowling strike rate",
                  "Strike rates",
                  10, 5
                  )
        { }
    }
}