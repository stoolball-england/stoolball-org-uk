using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class PlayerOfTheMatchAwardsController : BaseStatisticsTableController<PlayerIdentityPerformance>, IRenderControllerAsync
    {
        public PlayerOfTheMatchAwardsController(ILogger<PlayerOfTheMatchAwardsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPerformanceInAMatchStatisticsDataSource statisticsDataSource,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger,
                  compositeViewEngine,
                  umbracoContextAccessor,
                  statisticsFilterFactory,
                  statisticsBreadcrumbBuilder,
                  statisticsFilterHumanizer,
                  filter => statisticsDataSource.ReadPlayerIdentityPerformances(filter),
                  filter => statisticsDataSource.ReadTotalPlayerIdentityPerformances(filter),
                  filter => "Player of the match awards",
                  "Awards"
                  )
        { }
    }
}