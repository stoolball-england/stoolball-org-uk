﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Clubs
{
    public class ClubStatisticsController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IInningsStatisticsDataSource _inningsStatisticsDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public ClubStatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
           IInningsStatisticsDataSource inningsStatisticsDataSource,
           IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _inningsStatisticsDataSource = inningsStatisticsDataSource ?? throw new ArgumentNullException(nameof(inningsStatisticsDataSource));
            _bestPlayerTotalDataSource = bestPlayerTotalDataSource ?? throw new ArgumentNullException(nameof(bestPlayerTotalDataSource));
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

            var model = new StatisticsSummaryViewModel<Club>(contentModel.Content, Services?.UserService)
            {
                Context = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false),
            };

            if (model.Context == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { Club = model.Context, MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, HttpUtility.ParseQueryString(Request.Url.Query));
                model.InningsStatistics = await _inningsStatisticsDataSource.ReadInningsStatistics(model.AppliedFilter).ConfigureAwait(false);

                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.AppliedFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

                model.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
                model.Metadata.PageTitle = $"Statistics for {model.Context.ClubName}" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.Metadata.Description = $"Statistics for matches played by all teams in {model.Context.ClubName}.";

                return CurrentTemplate(model);
            }
        }
    }
}