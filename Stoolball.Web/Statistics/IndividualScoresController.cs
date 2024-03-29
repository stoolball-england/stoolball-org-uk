﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Statistics
{
    [ExcludeFromCodeCoverage]
    public class IndividualScoresController : BaseStatisticsTableController<PlayerInnings>, IRenderControllerAsync
    {
        public IndividualScoresController(ILogger<IndividualScoresController> logger,
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
                  filter => statisticsDataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst),
                  filter => statisticsDataSource.ReadTotalPlayerInnings(filter),
                  filter => "Highest individual scores",
                  "Scores")
        { }
    }
}