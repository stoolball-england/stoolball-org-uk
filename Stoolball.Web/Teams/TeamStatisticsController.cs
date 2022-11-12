using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class TeamStatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IInningsStatisticsDataSource _inningsStatisticsDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public TeamStatisticsController(ILogger<TeamStatisticsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
            IInningsStatisticsDataSource inningsStatisticsDataSource,
            IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
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
            var model = new StatisticsSummaryViewModel<Team>(CurrentPage)
            {
                Context = await _teamDataSource.ReadTeamByRoute(Request.Path, true)
            };

            if (model.Context == null)
            {
                return NotFound();
            }
            else
            {
                model.DefaultFilter = new StatisticsFilter { Team = model.Context, MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.AppliedFilter = model.DefaultFilter.Clone().Merge(_statisticsFilterQueryStringParser.ParseQueryString(Request.QueryString.Value));
                model.InningsStatistics = await _inningsStatisticsDataSource.ReadInningsStatistics(model.AppliedFilter);

                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.AppliedFilter, StatisticsSortOrder.BestFirst)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.AppliedFilter)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.AppliedFilter)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.AppliedFilter)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (model.Context.Club != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Context.Club.ClubName, Url = new Uri(model.Context.Club.ClubRoute!, UriKind.Relative) });
                }

                var appliedFilterWithoutDefaultFilter = model.AppliedFilter.Clone();
                appliedFilterWithoutDefaultFilter.Team = null;
                model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter("Statistics", _statisticsFilterHumanizer.MatchingUserFilter(appliedFilterWithoutDefaultFilter));
                model.FilterViewModel.FilteredItemTypePlural = "Statistics";
                model.FilterViewModel.from = model.AppliedFilter.FromDate;
                model.FilterViewModel.to = model.AppliedFilter.UntilDate;
                model.Metadata.PageTitle = $"Statistics for {model.Context.TeamName} stoolball team" + _statisticsFilterHumanizer.MatchingUserFilter(appliedFilterWithoutDefaultFilter);
                model.Metadata.Description = $"Statistics for {model.Context.TeamName}, a {model.Context.Description().Substring(2)}";

                return CurrentTemplate(model);
            }
        }
    }
}