using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class EconomyRateController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public EconomyRateController(ILogger<EconomyRateController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPlayerAverageStatisticsDataSource statisticsDataSource,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger,
                  compositeViewEngine,
                  umbracoContextAccessor,
                  statisticsFilterFactory,
                  statisticsBreadcrumbBuilder,
                  statisticsFilterHumanizer,
                  filter => statisticsDataSource.ReadBestEconomyRate(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithEconomyRate(filter),
                  filter => "Best economy rate",
                  "Economy rates",
                  new HtmlString("Players must have bowled at least <strong>{0} times</strong> to appear in this list."),
                  10, 5
                  )
        { }
    }
}