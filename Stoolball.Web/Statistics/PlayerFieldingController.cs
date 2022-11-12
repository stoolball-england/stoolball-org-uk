using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public class PlayerFieldingController : RenderController, IRenderControllerAsync
    {
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerSummaryStatisticsDataSource _summaryStatisticsDataSource;
        private readonly IPlayerPerformanceStatisticsDataSource _playerPerformanceStatisticsDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public PlayerFieldingController(ILogger<PlayerFieldingController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IPlayerDataSource playerDataSource,
            IPlayerSummaryStatisticsDataSource summaryStatisticsDataSource,
            IPlayerPerformanceStatisticsDataSource playerPerformanceStatisticsDataSource,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _summaryStatisticsDataSource = summaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(summaryStatisticsDataSource));
            _playerPerformanceStatisticsDataSource = playerPerformanceStatisticsDataSource ?? throw new ArgumentNullException(nameof(playerPerformanceStatisticsDataSource));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new PlayerFieldingViewModel(CurrentPage)
            {
                Player = await _playerDataSource.ReadPlayerByRoute(Request.Path),
            };

            if (model.Player == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { Player = model.Player, Paging = new Paging { PageSize = 5 } };
                model.AppliedFilter = model.DefaultFilter.Clone().Merge(_statisticsFilterQueryStringParser.ParseQueryString(Request.QueryString.Value));
                model.FieldingStatistics = await _summaryStatisticsDataSource.ReadFieldingStatistics(model.AppliedFilter);

                var catchesFilter = model.AppliedFilter.Clone();
                catchesFilter.Player = null;
                catchesFilter.SwapTeamAndOppositionFilters = true;
                catchesFilter.CaughtByPlayerIdentityIds = model.AppliedFilter.Player!.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList();

                model.Catches = (await _playerPerformanceStatisticsDataSource.ReadPlayerInnings(catchesFilter)).ToList();

                var runOutsFilter = model.AppliedFilter.Clone();
                runOutsFilter.Player = null;
                runOutsFilter.SwapTeamAndOppositionFilters = true;
                runOutsFilter.RunOutByPlayerIdentityIds = model.AppliedFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList();
                model.RunOuts = (await _playerPerformanceStatisticsDataSource.ReadPlayerInnings(runOutsFilter)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });


                if (model.AppliedFilter.Team != null)
                {
                    var teamWithName = model.Player.PlayerIdentities.FirstOrDefault(x => x.Team != null && x.Team.TeamRoute == model.AppliedFilter.Team.TeamRoute)?.Team;
                    if (teamWithName != null)
                    {
                        model.AppliedFilter.Team = teamWithName;
                    }
                }
                model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));

                var teams = model.Player.PlayerIdentities.Select(x => x.Team?.TeamName).OfType<string>().Distinct().ToList();
                model.Metadata.PageTitle = $"Fielding statistics for {model.Player.PlayerName()}" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.Metadata.Description = $"Fielding statistics for {model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";

                return CurrentTemplate(model);
            }
        }
    }
}