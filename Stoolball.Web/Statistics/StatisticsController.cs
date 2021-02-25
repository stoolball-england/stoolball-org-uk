using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics
{
    public class StatisticsController : RenderMvcControllerAsync
    {
        private readonly IStatisticsDataSource _statisticsDataSource;

        public StatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsDataSource statisticsDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsViewModel(contentModel.Content, Services?.UserService);
            model.IsAuthorized[AuthorizedAction.EditStatistics] = Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);

            model.StatisticsFilter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
            model.PlayerInnings = (await _statisticsDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            model.Metadata.PageTitle = $"Statistics for all teams";

            return CurrentTemplate(model);
        }
    }
}