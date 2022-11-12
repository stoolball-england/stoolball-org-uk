using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationStatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public MatchLocationStatisticsController(ILogger<MatchLocationStatisticsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchLocationDataSource matchLocationDataSource,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
            IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
            IStatisticsFilterFactory statisticsFilterFactory,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _bestPlayerTotalDataSource = bestPlayerTotalDataSource ?? throw new ArgumentNullException(nameof(bestPlayerTotalDataSource));
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsSummaryViewModel<MatchLocation>(CurrentPage)
            {
                Context = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path, false),
            };

            if (model.Context == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { MatchLocation = model.Context, MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.AppliedFilter = model.DefaultFilter.Clone().Merge(await _statisticsFilterFactory.FromQueryString(Request.QueryString.Value));
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.AppliedFilter)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.AppliedFilter)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.AppliedFilter)).ToList();
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

                model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
                model.FilterViewModel.FilteredItemTypePlural = "Statistics";
                model.FilterViewModel.FromDate = model.AppliedFilter.FromDate;
                model.FilterViewModel.UntilDate = model.AppliedFilter.UntilDate;
                model.FilterViewModel.TeamRoute = model.AppliedFilter.Team?.TeamRoute;
                model.FilterViewModel.TeamName = model.AppliedFilter.Team?.TeamName;
                model.FilterViewModel.SupportsTeamFilter = true;
                model.Metadata.PageTitle = $"Statistics for {model.Context.NameAndLocalityOrTown()}" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.Metadata.Description = $"Statistics for stoolball matches played at {model.Context.NameAndLocalityOrTown()}.";

                return CurrentTemplate(model);
            }
        }
    }
}