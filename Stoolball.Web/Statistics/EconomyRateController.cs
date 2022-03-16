using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public class EconomyRateController : RenderController, IRenderControllerAsync
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IBestPlayerAverageStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public EconomyRateController(ILogger<EconomyRateController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPlayerAverageStatisticsDataSource statisticsDataSource,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsBreadcrumbBuilder = statisticsBreadcrumbBuilder ?? throw new ArgumentNullException(nameof(statisticsBreadcrumbBuilder));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsViewModel<BestStatistic>(CurrentPage) { ShowCaption = false };
            model.DefaultFilter = await _statisticsFilterFactory.FromRoute(Request.Path);
            model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, Request.QueryString.Value);
            if (model.AppliedFilter.Team != null) { model.ShowTeamsColumn = false; }
            model.AppliedFilter.MinimumQualifyingInnings = 10;
            if (model.AppliedFilter.Team != null ||
                model.AppliedFilter.Club != null ||
                model.AppliedFilter.Competition != null ||
                model.AppliedFilter.Season != null ||
                model.AppliedFilter.MatchLocation != null) { model.AppliedFilter.MinimumQualifyingInnings = 5; }

            model.Results = (await _statisticsDataSource.ReadBestEconomyRate(model.AppliedFilter)).ToList();

            model.AppliedFilter.Paging.PageUrl = new Uri(Request.GetEncodedUrl());
            model.AppliedFilter.Paging.Total = await _statisticsDataSource.ReadTotalPlayersWithEconomyRate(model.AppliedFilter);

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);

            model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Economy rates", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
            model.Metadata.PageTitle = "Best economy rate" + _statisticsFilterHumanizer.MatchingFixedFilter(model.AppliedFilter) + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }

    }
}