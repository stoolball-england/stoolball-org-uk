using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Statistics
{
    public class PlayerSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerSummaryStatisticsDataSource _summaryStatisticsDataSource;
        private readonly IPlayerRepository _playerRepository;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;
        private readonly ICacheClearer<Player> _playerCacheClearer;

        public PlayerSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IPlayerDataSource playerDataSource,
            IPlayerSummaryStatisticsDataSource summaryStatisticsDataSource,
            IPlayerRepository playerRepository,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer,
            ICacheClearer<Player> playerCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _summaryStatisticsDataSource = summaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(summaryStatisticsDataSource));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
            _playerCacheClearer = playerCacheClearer ?? throw new ArgumentNullException(nameof(playerCacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public async Task<IActionResult> LinkPlayerToMemberAccount()
        {
            var model = new PlayerSummaryViewModel(CurrentPage, Services.UserService);
            model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, Request.QueryString.Value);
            model.Player = await _playerDataSource.ReadPlayerByRoute(Request.Path, model.AppliedFilter);

            if (model.Player == null)
            {
                return NotFound();
            }

            var currentMember = await _memberManager.GetCurrentMemberAsync();
            if (ModelState.IsValid && currentMember != null)
            {
                await _playerRepository.LinkPlayerToMemberAccount(model.Player, currentMember.Key, currentMember.Name);
                model.Player.MemberKey = currentMember.Key;
                model.IsCurrentMember = true;
                model.LinkedByThisRequest = true;

                await _playerCacheClearer.ClearCacheFor(model.Player);
            }
            else
            {
                model.IsCurrentMember = currentMember != null && model.Player.MemberKey == currentMember.Key;
            }

            model.AppliedFilter.Player = model.Player;

            var battingTask = _summaryStatisticsDataSource.ReadBattingStatistics(model.AppliedFilter);
            var bowlingTask = _summaryStatisticsDataSource.ReadBowlingStatistics(model.AppliedFilter);
            var fieldingTask = _summaryStatisticsDataSource.ReadFieldingStatistics(model.AppliedFilter);
            Task.WaitAll(battingTask, bowlingTask, fieldingTask);

            model.BattingStatistics = battingTask.Result;
            model.BowlingStatistics = bowlingTask.Result;
            model.FieldingStatistics = fieldingTask.Result;

            var filter = _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
            model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", filter);

            var teams = model.Player.PlayerIdentities.Select(x => x.Team.TeamName).Distinct().ToList();
            model.Metadata.PageTitle = $"{model.Player.PlayerName()}{filter}";
            model.Metadata.Description = $"{model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });

            return View("Player", model);
        }
    }
}