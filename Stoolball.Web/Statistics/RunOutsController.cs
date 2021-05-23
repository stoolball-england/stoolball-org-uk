using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Matches;
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
    public class RunOutsController : RenderMvcControllerAsync
    {
        private readonly IStatisticsFilterUrlParser _statisticsFilterUrlParser;
        private readonly IPlayerSummaryStatisticsDataSource _playerSummaryStatisticsDataSource;
        private readonly IPlayerPerformanceStatisticsDataSource _playerPerformanceDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;

        public RunOutsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsFilterUrlParser statisticsFilterUrlParser,
           IPlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource,
           IPlayerPerformanceStatisticsDataSource playerPerformanceDataSource,
           IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsFilterUrlParser = statisticsFilterUrlParser ?? throw new ArgumentNullException(nameof(statisticsFilterUrlParser));
            _playerSummaryStatisticsDataSource = playerSummaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(playerSummaryStatisticsDataSource));
            _playerPerformanceDataSource = playerPerformanceDataSource ?? throw new ArgumentNullException(nameof(playerPerformanceDataSource));
            _statisticsBreadcrumbBuilder = statisticsBreadcrumbBuilder ?? throw new ArgumentNullException(nameof(statisticsBreadcrumbBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsViewModel<PlayerInnings>(contentModel.Content, Services?.UserService) { ShowCaption = false };
            model.StatisticsFilter = await _statisticsFilterUrlParser.ParseUrl(new Uri(Request.Url, Request.RawUrl)).ConfigureAwait(false);
            model.StatisticsFilter.Paging.PageSize = Constants.Defaults.PageSize;
            var catchesFilter = new StatisticsFilter
            {
                RunOutByPlayerIdentityIds = model.StatisticsFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList(),
                Paging = model.StatisticsFilter.Paging
            };
            model.Results = (await _playerPerformanceDataSource.ReadPlayerInnings(catchesFilter).ConfigureAwait(false)).ToList();

            if (!model.Results.Any())
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter.Paging.PageUrl = Request.Url;
                model.StatisticsFilter.Paging.Total = (await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(model.StatisticsFilter).ConfigureAwait(false)).TotalRunOuts;

                _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.StatisticsFilter);
                model.Metadata.PageTitle = "Run-outs" + model.StatisticsFilter.ToString();

                return CurrentTemplate(model);
            }
        }
    }
}