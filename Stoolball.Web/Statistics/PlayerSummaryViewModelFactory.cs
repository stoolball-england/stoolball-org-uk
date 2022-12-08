using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Web.Navigation;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class PlayerSummaryViewModelFactory : IPlayerSummaryViewModelFactory
    {
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerSummaryStatisticsDataSource _summaryStatisticsDataSource;
        private readonly IStatisticsBreadcrumbBuilder _breadcrumbBuilder;
        private readonly IUserService _userService;

        public PlayerSummaryViewModelFactory(
            IPlayerDataSource playerDataSource,
            IPlayerSummaryStatisticsDataSource summaryStatisticsDataSource,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer,
            IStatisticsBreadcrumbBuilder breadcrumbBuilder,
            IUserService userService)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _summaryStatisticsDataSource = summaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(summaryStatisticsDataSource));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task<PlayerSummaryViewModel> CreateViewModel(IPublishedContent currentPage, string path, string? queryString)
        {
            var model = new PlayerSummaryViewModel(currentPage, _userService);
            model.AppliedFilter = model.DefaultFilter.Clone().Merge(_statisticsFilterQueryStringParser.ParseQueryString(queryString));
            model.Player = await _playerDataSource.ReadPlayerByRoute(path, model.AppliedFilter);

            if (model.Player != null)
            {
                _breadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.DefaultFilter);
                model.DefaultFilter.Player = model.Player;
                model.AppliedFilter.Player = model.Player;

                var battingTask = _summaryStatisticsDataSource.ReadBattingStatistics(model.AppliedFilter);
                var bowlingTask = _summaryStatisticsDataSource.ReadBowlingStatistics(model.AppliedFilter);
                var fieldingTask = _summaryStatisticsDataSource.ReadFieldingStatistics(model.AppliedFilter);
                Task.WaitAll(battingTask, bowlingTask, fieldingTask);

                model.BattingStatistics = battingTask.Result;
                model.BowlingStatistics = bowlingTask.Result;
                model.FieldingStatistics = fieldingTask.Result;

                if (model.AppliedFilter.Team != null)
                {
                    var teamWithName = model.Player.PlayerIdentities.FirstOrDefault(x => x.Team != null && x.Team.TeamRoute == model.AppliedFilter.Team.TeamRoute)?.Team;
                    if (teamWithName != null)
                    {
                        model.AppliedFilter.Team = teamWithName;
                    }
                }

                var filter = _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", filter);

                var teams = model.Player.PlayerIdentities.Where(x => !string.IsNullOrEmpty(x.Team?.TeamName)).Select(x => x.Team!.TeamName).Distinct().ToList();
                model.Metadata.PageTitle = $"{model.Player.PlayerName()}{filter}";
                model.Metadata.Description = $"{model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";
            }

            return model;
        }
    }
}
