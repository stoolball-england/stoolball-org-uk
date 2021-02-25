using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Navigation;
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
    public class PlayerController : RenderMvcControllerAsync
    {
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IStatisticsDataSource _statisticsDataSource;

        public PlayerController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IPlayerDataSource playerDataSource,
           IStatisticsDataSource statisticsDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
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

            var model = new PlayerViewModel(contentModel.Content, Services?.UserService)
            {
                Player = await _playerDataSource.ReadPlayerByRoute(Request.RawUrl).ConfigureAwait(false),
            };

            var filter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 5 };
            filter.PlayerIds.Add(model.Player.PlayerId.Value);
            model.PlayerInnings = (await _statisticsDataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            if (model.Player == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });

                var teams = model.Player.PlayerIdentities.Select(x => x.Team.TeamName).Distinct().ToList();
                model.Metadata.PageTitle = $"{model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";
                //model.Metadata.Description = model.Player.Description();

                return CurrentTemplate(model);
            }
        }
    }
}