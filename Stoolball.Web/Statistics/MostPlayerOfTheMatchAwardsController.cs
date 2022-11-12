using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class MostPlayerOfTheMatchAwardsController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public MostPlayerOfTheMatchAwardsController(ILogger<MostPlayerOfTheMatchAwardsController> logger,
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
                  filter => statisticsDataSource.ReadMostPlayerOfTheMatchAwards(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter),
                  filter => "Most player of the match awards",
                  "Awards"
                  )
        { }
    }
}