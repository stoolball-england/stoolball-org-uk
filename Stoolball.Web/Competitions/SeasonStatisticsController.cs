using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class SeasonStatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public SeasonStatisticsController(ILogger<SeasonStatisticsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            ISeasonDataSource seasonDataSource,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
            IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _bestPlayerTotalDataSource = bestPlayerTotalDataSource ?? throw new ArgumentNullException(nameof(bestPlayerTotalDataSource));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsSummaryViewModel<Season>(CurrentPage)
            {
                Context = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false),
            };

            if (model.Context == null || model.Context.Competition == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { Season = model.Context, MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.AppliedFilter = model.DefaultFilter.Clone().Merge(await _statisticsFilterFactory.FromQueryString(Request.QueryString.Value));
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.AppliedFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Context.Competition.CompetitionName, Url = new Uri(model.Context.Competition.CompetitionRoute!, UriKind.Relative) });
                model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
                model.FilterViewModel.FilteredItemTypePlural = "Statistics";
                model.FilterViewModel.TeamRoute = model.AppliedFilter.Team?.TeamRoute;
                model.FilterViewModel.TeamName = model.AppliedFilter.Team?.TeamName;
                model.FilterViewModel.SupportsTeamFilter = true;

                model.Metadata.PageTitle = $"Statistics for {model.Context.SeasonFullNameAndPlayerType()}";
                model.Metadata.Description = $"Statistics for stoolball matches played in the {model.Context.SeasonFullName()}.";

                return CurrentTemplate(model);
            }
        }
    }
}