using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
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
        private readonly IPlayerDataSource _playerDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IRouteNormaliser _routeNormaliser;

        public IndividualScoresController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStatisticsDataSource statisticsDataSource,
           IPlayerDataSource playerDataSource,
           ITeamDataSource teamDataSource,
           IMatchLocationDataSource matchLocationDataSource,
           ICompetitionDataSource competitionDataSource,
           ISeasonDataSource seasonDataSource,
           IRouteNormaliser routeNormaliser)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
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
                model.StatisticsFilter.Player = await _playerDataSource.ReadPlayerByRoute(_routeNormaliser.NormaliseRouteToEntity(Request.RawUrl, "players")).ConfigureAwait(false);
            }
            else if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.StatisticsFilter.Team = await _teamDataSource.ReadTeamByRoute(_routeNormaliser.NormaliseRouteToEntity(Request.RawUrl, "teams"), false).ConfigureAwait(false);
            }
            else if (Request.RawUrl.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                model.StatisticsFilter.MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(_routeNormaliser.NormaliseRouteToEntity(Request.RawUrl, "locations"), false).ConfigureAwait(false);
            }
            else if (Regex.IsMatch(Request.RawUrl, @"^\/competitions\/[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?", RegexOptions.IgnoreCase))
            {
                model.StatisticsFilter.Season = await _seasonDataSource.ReadSeasonByRoute(_routeNormaliser.NormaliseRouteToEntity(Request.RawUrl, "competitions", @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$")).ConfigureAwait(false);
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.StatisticsFilter.Competition = await _competitionDataSource.ReadCompetitionByRoute(_routeNormaliser.NormaliseRouteToEntity(Request.RawUrl, "competitions")).ConfigureAwait(false);
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
                if (model.StatisticsFilter.Player != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.StatisticsFilter.Player.PlayerName(), Url = new Uri(model.StatisticsFilter.Player.PlayerRoute, UriKind.Relative) });
                    pageTitle += model.StatisticsFilter.ToString();
                }
                model.Metadata.PageTitle = pageTitle;

                return CurrentTemplate(model);
            }
        }
    }
}