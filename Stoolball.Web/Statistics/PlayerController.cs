using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IPlayerSummaryStatisticsDataSource _summaryStatisticsDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public PlayerController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IPlayerDataSource playerDataSource,
           IPlayerSummaryStatisticsDataSource summaryStatisticsDataSource,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _summaryStatisticsDataSource = summaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(summaryStatisticsDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
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

            var model = new PlayerBattingViewModel(contentModel.Content, Services?.UserService)
            {
                Player = await _playerDataSource.ReadPlayerByRoute(Request.Url.AbsolutePath).ConfigureAwait(false),
            };

            if (model.Player == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter = _statisticsFilterQueryStringParser.ParseQueryString(new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 5 }, HttpUtility.ParseQueryString(Request.Url.Query));
                model.StatisticsFilter.Player = model.Player;
                model.BattingStatistics = await _summaryStatisticsDataSource.ReadBattingStatistics(model.StatisticsFilter).ConfigureAwait(false);
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });

                model.FilterDescription = _statisticsFilterHumanizer.StatisticsMatchingFilter(model.StatisticsFilter);

                var teams = model.Player.PlayerIdentities.Select(x => x.Team.TeamName).Distinct().ToList();
                model.Metadata.PageTitle = $"Batting statistics for {model.Player.PlayerName()}" + _statisticsFilterHumanizer.MatchingFilter(model.StatisticsFilter);
                model.Metadata.Description = $"Batting statistics for {model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";

                return CurrentTemplate(model);
            }
        }
    }
}