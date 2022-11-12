using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Clubs
{
    public class ClubStatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IInningsStatisticsDataSource _inningsStatisticsDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public ClubStatisticsController(ILogger<ClubStatisticsController> logger,
           ICompositeViewEngine compositeViewEngine,
           IUmbracoContextAccessor umbracoContextAccessor,
           IClubDataSource clubDataSource,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
           IInningsStatisticsDataSource inningsStatisticsDataSource,
           IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
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
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsSummaryViewModel<Club>(CurrentPage)
            {
                Context = await _clubDataSource.ReadClubByRoute(Request.Path)
            };

            if (model.Context == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { Club = model.Context, MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.AppliedFilter = model.DefaultFilter.Clone().Merge(_statisticsFilterQueryStringParser.ParseQueryString(Request.QueryString.Value));
                if (model.AppliedFilter.Team != null)
                {
                    model.AppliedFilter.Team = model.Context.Teams.FirstOrDefault(x => x.TeamRoute == model.AppliedFilter.Team.TeamRoute);
                }

                model.InningsStatistics = await _inningsStatisticsDataSource.ReadInningsStatistics(model.AppliedFilter).ConfigureAwait(false);

                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.AppliedFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.AppliedFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

                model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter));
                model.FilterViewModel.FilteredItemTypePlural = "Statistics";
                model.FilterViewModel.from = model.AppliedFilter.FromDate;
                model.FilterViewModel.to = model.AppliedFilter.UntilDate;
                model.FilterViewModel.team = model.AppliedFilter.Team?.TeamRoute;
                model.FilterViewModel.SupportsTeamFilter = model.Context.Teams.Any();
                model.FilterViewModel.Teams = model.Context.Teams;
                model.Metadata.PageTitle = $"Statistics for {model.Context.ClubName}" + _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
                model.Metadata.Description = $"Statistics for matches played by all teams in {model.Context.ClubName}.";

                return CurrentTemplate(model);
            }
        }
    }
}