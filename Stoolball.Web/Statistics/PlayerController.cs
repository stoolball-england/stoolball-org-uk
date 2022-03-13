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
    public class PlayerController : RenderController, IRenderControllerAsync
    {
        private readonly ICompositeViewEngine _compositeViewEngine;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerSummaryStatisticsDataSource _summaryStatisticsDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public PlayerController(ILogger<PlayerController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IPlayerDataSource playerDataSource,
            IPlayerSummaryStatisticsDataSource summaryStatisticsDataSource,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _compositeViewEngine = compositeViewEngine ?? throw new ArgumentNullException(nameof(compositeViewEngine));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _summaryStatisticsDataSource = summaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(summaryStatisticsDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new PlayerBattingViewModel(CurrentPage)
            {
                Player = await _playerDataSource.ReadPlayerByRoute(Request.Path),
            };

            if (model.Player == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 5, Player = model.Player };
                model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, Request.QueryString.Value);
                model.BattingStatistics = await _summaryStatisticsDataSource.ReadBattingStatistics(model.AppliedFilter);
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });

                model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));

                var teams = model.Player.PlayerIdentities.Select(x => x.Team.TeamName).Distinct().ToList();
                model.Metadata.PageTitle = $"Batting statistics for {model.Player.PlayerName()}" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.Metadata.Description = $"Batting statistics for {model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";

                return CurrentTemplate(model);
            }
        }
    }
}