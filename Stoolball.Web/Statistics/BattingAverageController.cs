using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class BattingAverageController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public BattingAverageController(ILogger<BattingAverageController> logger,
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
                  filter => statisticsDataSource.ReadBestBattingAverage(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithBattingAverage(filter),
                  filter => "Best batting average",
                  "Averages",
                  new HtmlString("Players must have batted at least <strong>{0} times</strong> to appear in this list."),
                  10, 5
                  )
        { }
    }
}