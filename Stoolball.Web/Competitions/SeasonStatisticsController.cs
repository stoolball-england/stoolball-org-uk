using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
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

namespace Stoolball.Web.Competitions
{
    public class SeasonStatisticsController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IBestPlayerTotalStatisticsDataSource _bestPlayerTotalDataSource;

        public SeasonStatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
           IBestPlayerTotalStatisticsDataSource bestPlayerTotalDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _bestPlayerTotalDataSource = bestPlayerTotalDataSource ?? throw new ArgumentNullException(nameof(bestPlayerTotalDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsSummaryViewModel<Season>(contentModel.Content, Services?.UserService)
            {
                Context = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };

            if (model.Context == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.StatisticsFilter = new StatisticsFilter { MaxResultsAllowingExtraResultsIfValuesAreEqual = 10 };
                model.StatisticsFilter.Season = model.Context;
                model.PlayerInnings = (await _bestPerformanceDataSource.ReadPlayerInnings(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.BowlingFigures = (await _bestPerformanceDataSource.ReadBowlingFigures(model.StatisticsFilter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();
                model.MostRuns = (await _bestPlayerTotalDataSource.ReadMostRunsScored(model.StatisticsFilter).ConfigureAwait(false)).ToList();
                model.MostWickets = (await _bestPlayerTotalDataSource.ReadMostWickets(model.StatisticsFilter).ConfigureAwait(false)).ToList();
                model.MostCatches = (await _bestPlayerTotalDataSource.ReadMostCatches(model.StatisticsFilter).ConfigureAwait(false)).ToList();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Context.Competition.CompetitionName, Url = new Uri(model.Context.Competition.CompetitionRoute, UriKind.Relative) });

                model.Metadata.PageTitle = $"Statistics for {model.Context.SeasonFullNameAndPlayerType()}";
                model.Metadata.Description = $"Statistics for stoolball matches played in the {model.Context.SeasonFullName()}.";

                return CurrentTemplate(model);
            }
        }
    }
}