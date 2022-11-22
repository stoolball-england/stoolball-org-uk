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
    public class BowlingStrikeRateController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public BowlingStrikeRateController(ILogger<BowlingStrikeRateController> logger,
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
                  filter => statisticsDataSource.ReadBestBowlingStrikeRate(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithBowlingStrikeRate(filter),
                  filter => "Best bowling strike rate",
                  "Strike rates",
                  new HtmlString("Players must have overs recorded in at least <strong>{0} innings</strong> to appear in this list."),
                  10, 5
                  )
        { }
    }
}