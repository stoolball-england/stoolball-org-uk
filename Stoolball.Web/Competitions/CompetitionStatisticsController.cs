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
    public class CompetitionStatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public CompetitionStatisticsController(ILogger<CompetitionStatisticsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ICompetitionDataSource competitionDataSource,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
            IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
            IStatisticsFilterFactory statisticsFilterFactory,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _bestPlayerTotalDataSource = bestPlayerTotalDataSource ?? throw new ArgumentNullException(nameof(bestPlayerTotalDataSource));
            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsSummaryViewModel<Competition>(CurrentPage)
            {
                Context = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false),
            };

            if (model.Context == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { Competition = model.Context, MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.AppliedFilter = model.DefaultFilter.Clone().Merge(await _statisticsFilterFactory.FromQueryString(Request.QueryString.Value));
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.AppliedFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
                model.FilterViewModel.FilteredItemTypePlural = "Statistics";
                model.FilterViewModel.SupportsDateFilter = true;
                model.FilterViewModel.FromDate = model.AppliedFilter.FromDate;
                model.FilterViewModel.UntilDate = model.AppliedFilter.UntilDate;
                model.FilterViewModel.TeamRoute = model.AppliedFilter.Team?.TeamRoute;
                model.FilterViewModel.TeamName = model.AppliedFilter.Team?.TeamName;
                model.FilterViewModel.SupportsTeamFilter = true;
                model.Metadata.PageTitle = $"Statistics for {model.Context.CompetitionName}" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.Metadata.Description = $"Statistics for stoolball matches played in all the years of the {model.Context.CompetitionName}.";

                return CurrentTemplate(model);
            }
        }
    }
}