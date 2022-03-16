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
    public class MostRunsController : RenderController, IRenderControllerAsync
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IBestPlayerTotalStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public MostRunsController(ILogger<MostRunsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IBestPlayerTotalStatisticsDataSource statisticsDataSource,
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

            model.Results = (await _statisticsDataSource.ReadMostRunsScored(model.AppliedFilter)).ToList();

            model.AppliedFilter.Paging.PageUrl = new Uri(Request.GetEncodedUrl());
            model.AppliedFilter.Paging.Total = await _statisticsDataSource.ReadTotalPlayersWithRunsScored(model.AppliedFilter);

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);

            model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Runs", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
            model.Metadata.PageTitle = "Most runs" + _statisticsFilterHumanizer.MatchingFixedFilter(model.AppliedFilter) + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }
    }
}