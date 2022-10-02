using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class MostScoresOfXController : BaseStatisticsTableController<BestStatistic>, IRenderControllerAsync
    {
        public MostScoresOfXController(ILogger<MostScoresOfXController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPlayerTotalStatisticsDataSource statisticsDataSource,
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
                  filter => statisticsDataSource.ReadMostPlayerInnings(filter),
                  filter => statisticsDataSource.ReadTotalPlayersWithRunsScored(filter),
                  filter => $"Most scores of {filter.MinimumRunsScored} or more runs",
                  "Innings"
                  )
        { }
    }
}