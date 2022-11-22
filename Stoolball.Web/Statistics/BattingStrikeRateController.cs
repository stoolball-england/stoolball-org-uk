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
    public class BattingStrikeRateController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public BattingStrikeRateController(ILogger<BattingStrikeRateController> logger,
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
                  filter => statisticsDataSource.ReadBestBattingStrikeRate(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithBattingStrikeRate(filter),
                  filter => "Best batting strike rate",
                  "Strike rates",
                  new HtmlString("Players must have runs and balls faced recorded in at least <strong>{0} innings</strong> to appear in this list."),
                  10, 5
                  )
        { }
    }
}