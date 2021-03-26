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
    public class IndividualScoresController : RenderMvcControllerAsync
    {
        private readonly IStatisticsFilterUrlParser _statisticsFilterUrlParser;
        private readonly IStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;

        public IndividualScoresController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsFilterUrlParser statisticsFilterUrlParser,
           IStatisticsDataSource statisticsDataSource,
           IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsFilterUrlParser = statisticsFilterUrlParser ?? throw new ArgumentNullException(nameof(statisticsFilterUrlParser));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
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
            model.Results = (await _statisticsDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            if (!model.Results.Any())
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter.Paging.PageUrl = Request.Url;
                model.StatisticsFilter.Paging.Total = await _statisticsDataSource.ReadTotalPlayerInnings(model.StatisticsFilter).ConfigureAwait(false);

                _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.StatisticsFilter);
                model.Metadata.PageTitle = "Highest individual scores" + model.StatisticsFilter.ToString();

                return CurrentTemplate(model);
            }
        }

    }
}