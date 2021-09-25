using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Statistics
{
    public class MostRunsController : RenderMvcControllerAsync
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IBestPlayerTotalStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public MostRunsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsFilterFactory statisticsFilterFactory,
           IBestPlayerTotalStatisticsDataSource statisticsDataSource,
           IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsBreadcrumbBuilder = statisticsBreadcrumbBuilder ?? throw new ArgumentNullException(nameof(statisticsBreadcrumbBuilder));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsViewModel<BestStatistic>(contentModel.Content, Services?.UserService) { ShowCaption = false };
            model.AppliedFilter = await _statisticsFilterFactory.FromRoute(Request.Url.AbsolutePath).ConfigureAwait(false);
            if (model.AppliedFilter.Team != null) { model.ShowTeamsColumn = false; }

            model.Results = (await _statisticsDataSource.ReadMostRunsScored(model.AppliedFilter).ConfigureAwait(false)).ToList();

            model.AppliedFilter.Paging.PageUrl = Request.Url;
            model.AppliedFilter.Paging.Total = await _statisticsDataSource.ReadTotalPlayersWithRunsScored(model.AppliedFilter).ConfigureAwait(false);

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);
            model.Metadata.PageTitle = "Most runs" + _statisticsFilterHumanizer.MatchingFixedFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }

    }
}