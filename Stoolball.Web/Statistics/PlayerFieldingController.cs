﻿using System;
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
    public class PlayerFieldingController : RenderMvcControllerAsync
    {
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerSummaryStatisticsDataSource _summaryStatisticsDataSource;
        private readonly IPlayerPerformanceStatisticsDataSource _playerPerformanceStatisticsDataSource;

        public PlayerFieldingController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IPlayerDataSource playerDataSource,
           IPlayerSummaryStatisticsDataSource summaryStatisticsDataSource,
           IPlayerPerformanceStatisticsDataSource playerPerformanceStatisticsDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _summaryStatisticsDataSource = summaryStatisticsDataSource ?? throw new ArgumentNullException(nameof(summaryStatisticsDataSource));
            _playerPerformanceStatisticsDataSource = playerPerformanceStatisticsDataSource ?? throw new ArgumentNullException(nameof(playerPerformanceStatisticsDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new PlayerFieldingViewModel(contentModel.Content, Services?.UserService)
            {
                Player = await _playerDataSource.ReadPlayerByRoute(Request.RawUrl).ConfigureAwait(false),
            };

            if (model.Player == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter = new StatisticsFilter();
                model.StatisticsFilter.Player = model.Player;
                model.FieldingStatistics = await _summaryStatisticsDataSource.ReadFieldingStatistics(model.StatisticsFilter).ConfigureAwait(false);

                var catchesFilter = new StatisticsFilter
                {
                    CaughtByPlayerIdentityIds = model.StatisticsFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList(),
                    Paging = new Paging { PageSize = 5 }
                };
                model.Catches = (await _playerPerformanceStatisticsDataSource.ReadPlayerInnings(catchesFilter).ConfigureAwait(false)).ToList();

                var runOutsFilter = new StatisticsFilter
                {
                    RunOutByPlayerIdentityIds = model.StatisticsFilter.Player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList(),
                    Paging = new Paging { PageSize = 5 }
                };
                model.RunOuts = (await _playerPerformanceStatisticsDataSource.ReadPlayerInnings(runOutsFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });

                var teams = model.Player.PlayerIdentities.Select(x => x.Team.TeamName).Distinct().ToList();
                model.Metadata.PageTitle = $"{model.Player.PlayerName()}, a player for {teams.Humanize()} stoolball {(teams.Count > 1 ? "teams" : "team")}";
                //model.Metadata.Description = model.Player.Description();

                return CurrentTemplate(model);
            }
        }
    }
}