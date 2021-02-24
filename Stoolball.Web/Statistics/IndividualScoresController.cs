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
    public class IndividualScoresController : RenderMvcControllerAsync
    {
        private readonly IStatisticsDataSource _statisticsDataSource;

        public IndividualScoresController(IGlobalSettings globalSettings,
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

            if (!Guid.TryParse(Request.QueryString["player"], out Guid playerId))
            {
                return new HttpStatusCodeResult(400);
            }

            var model = new IndividualScoresViewModel(contentModel.Content, Services?.UserService) { ShowCaption = false };

            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            model.StatisticsFilter = new StatisticsFilter { PageNumber = pageNumber > 0 ? pageNumber : 1 };
            model.StatisticsFilter.PlayerIds.Add(playerId);

            model.Results = (await _statisticsDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            if (!model.Results.Any())
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalResults = await _statisticsDataSource.ReadTotalPlayerInnings(model.StatisticsFilter).ConfigureAwait(false);
                model.Metadata.PageTitle = $"Highest individual scores for {model.Results.First().Player.PlayerName()}";

                return CurrentTemplate(model);
            }
        }
    }
}