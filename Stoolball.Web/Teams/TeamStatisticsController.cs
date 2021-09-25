using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class TeamStatisticsController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IInningsStatisticsDataSource _inningsStatisticsDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public TeamStatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
           IInningsStatisticsDataSource inningsStatisticsDataSource,
           IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
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
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsSummaryViewModel<Team>(contentModel.Content, Services?.UserService)
            {
                Context = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };

            if (model.Context == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter = _statisticsFilterQueryStringParser.ParseQueryString(new StatisticsFilter { Team = model.Context }, HttpUtility.ParseQueryString(Request.Url.Query));
                model.InningsStatistics = await _inningsStatisticsDataSource.ReadInningsStatistics(model.StatisticsFilter).ConfigureAwait(false);

                model.StatisticsFilter.MaxResultsAllowingExtraResultsIfValuesAreEqual = 10;
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.StatisticsFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.StatisticsFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.StatisticsFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (model.Context.Club != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Context.Club.ClubName, Url = new Uri(model.Context.Club.ClubRoute, UriKind.Relative) });
                }

                model.FilterDescription = _statisticsFilterHumanizer.StatisticsMatchingUserFilter(model.StatisticsFilter);
                model.Metadata.PageTitle = $"Statistics for {model.Context.TeamName} stoolball team" + _statisticsFilterHumanizer.MatchingUserFilter(model.StatisticsFilter);
                model.Metadata.Description = $"Statistics for {model.Context.TeamName}, a {model.Context.Description().Substring(2)}";

                return CurrentTemplate(model);
            }
        }
    }
}