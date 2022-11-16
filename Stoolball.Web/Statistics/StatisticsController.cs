using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics
{
    public class StatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly IMemberManager _memberManager;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceInAMatchStatisticsDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestTotalStatisticsDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public StatisticsController(ILogger<StatisticsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMemberManager memberManager,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceInAMatchStatisticsDataSource,
            IBestPlayerTotalStatisticsDataSource bestTotalStatisticsDataSource,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _bestPerformanceInAMatchStatisticsDataSource = bestPerformanceInAMatchStatisticsDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceInAMatchStatisticsDataSource));
            _bestTotalStatisticsDataSource = bestTotalStatisticsDataSource ?? throw new ArgumentNullException(nameof(bestTotalStatisticsDataSource));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsSummaryViewModel(CurrentPage);
            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditStatistics] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);

            model.DefaultFilter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
            model.AppliedFilter = model.DefaultFilter.Clone().Merge(_statisticsFilterQueryStringParser.ParseQueryString(Request.QueryString.Value));
            model.PlayerInnings = (await _bestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();
            model.MostRuns = (await _bestTotalStatisticsDataSource.ReadMostRunsScored(model.AppliedFilter)).ToList();
            model.MostWickets = (await _bestTotalStatisticsDataSource.ReadMostWickets(model.AppliedFilter)).ToList();
            model.BowlingFigures = (await _bestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();
            model.MostCatches = (await _bestTotalStatisticsDataSource.ReadMostCatches(model.AppliedFilter)).ToList();

            model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
            model.FilterViewModel.FilteredItemTypePlural = "Statistics";
            model.FilterViewModel.SupportsDateFilter = true;
            model.FilterViewModel.FromDate = model.AppliedFilter.FromDate;
            model.FilterViewModel.UntilDate = model.AppliedFilter.UntilDate;
            model.Metadata.PageTitle = "Statistics for all teams" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);

            return CurrentTemplate(model);
        }
    }
}