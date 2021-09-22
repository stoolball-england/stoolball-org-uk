using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Stoolball.MatchLocations;
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

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationStatisticsController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;

        public MatchLocationStatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
           IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource,
           IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
           IStatisticsFilterHumanizer statisticsFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
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

            var model = new StatisticsSummaryViewModel<MatchLocation>(contentModel.Content, Services?.UserService)
            {
                Context = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl, false).ConfigureAwait(false),
            };

            if (model.Context == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter = _statisticsFilterQueryStringParser.ParseQueryString(new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 }, HttpUtility.ParseQueryString(Request.Url.Query));
                model.StatisticsFilter.MatchLocation = model.Context;
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.StatisticsFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.StatisticsFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.StatisticsFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

                model.FilterDescription = _statisticsFilterHumanizer.StatisticsMatchingFilter(model.StatisticsFilter);
                model.Metadata.PageTitle = $"Statistics for {model.Context.NameAndLocalityOrTown()}" + _statisticsFilterHumanizer.MatchingFilter(model.StatisticsFilter);
                model.Metadata.Description = $"Statistics for stoolball matches played at {model.Context.NameAndLocalityOrTown()}.";

                return CurrentTemplate(model);
            }
        }
    }
}