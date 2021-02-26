using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Navigation;
using Stoolball.Routing;
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
        private readonly IRouteNormaliser _routeNormaliser;

        public IndividualScoresController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsDataSource statisticsDataSource,
           IRouteNormaliser routeNormaliser)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new IndividualScoresViewModel(contentModel.Content, Services?.UserService) { ShowCaption = false };
            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            model.StatisticsFilter = new StatisticsFilter { PageNumber = pageNumber > 0 ? pageNumber : 1 };

            var pageTitle = "Highest individual scores";


            if (Request.RawUrl.StartsWith("/players/", StringComparison.OrdinalIgnoreCase))
            {
                model.StatisticsFilter.PlayerRoutes.Add(_routeNormaliser.NormaliseRouteToEntity(Request.RawUrl, "players"));
            }

            model.Results = (await _statisticsDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();


            if (!model.Results.Any())
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalResults = await _statisticsDataSource.ReadTotalPlayerInnings(model.StatisticsFilter).ConfigureAwait(false);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });
                if (model.StatisticsFilter.PlayerRoutes.Any())
                {
                    var player = model.Results.First().Player;
                    model.Breadcrumbs.Add(new Breadcrumb { Name = player.PlayerName(), Url = new Uri(player.PlayerRoute, UriKind.Relative) });
                    pageTitle += $" for {player.PlayerName()}";
                }
                model.Metadata.PageTitle = pageTitle;

                return CurrentTemplate(model);
            }
        }
    }
}