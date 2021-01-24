using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Mvc;
using Stoolball.Data.SqlServer;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics
{
    public class EditStatisticsSurfaceController : SurfaceController
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IBackgroundTaskTracker _taskTracker;
        private readonly IMatchListingDataSource _matchListingDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;

        public EditStatisticsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IDatabaseConnectionFactory databaseConnectionFactory,
            IBackgroundTaskTracker taskTracker, IMatchListingDataSource matchListingDataSource, IMatchDataSource matchDataSource, IStatisticsRepository statisticsRepository,
            IBowlingFiguresCalculator bowlingFiguresCalculator)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _taskTracker = taskTracker ?? throw new ArgumentNullException(nameof(taskTracker));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public ActionResult UpdateStatistics()
        {
            var model = new StatisticsViewModel(CurrentPage, Services.UserService);
            model.IsAuthorized[AuthorizedAction.EditStatistics] = Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);

            if (model.IsAuthorized[AuthorizedAction.EditStatistics] && ModelState.IsValid)
            {
                model.BackgroundTaskId = Guid.NewGuid();
                var currentMember = Members.GetCurrentMember();
                HostingEnvironment.QueueBackgroundWorkItem(ct => UpdateStatistics(model.BackgroundTaskId.Value, currentMember.Key, currentMember.Name, ct));
            }

            model.Metadata.PageTitle = "Update statistics";

            return View("EditStatistics", model);
        }

        /// <remarks>
        /// https://devblogs.microsoft.com/aspnet/queuebackgroundworkitem-to-reliably-schedule-and-run-background-processes-in-asp-net/
        /// </remarks>
        private async Task<CancellationToken> UpdateStatistics(Guid taskId, Guid memberKey, string memberName, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Info(GetType(), "Updating match statistics for all matches in {Type:l}.{Method:l}.", GetType(), nameof(UpdateStatistics));

                var matchListings = (await _matchListingDataSource.ReadMatchListings(new MatchQuery
                {
                    IncludeMatches = true,
                    IncludeTournamentMatches = true,
                    IncludeTournaments = false,
                    UntilDate = DateTime.UtcNow
                }).ConfigureAwait(false));

                _taskTracker.SetTarget(taskId, matchListings.Sum(x => x.MatchInnings.Count));

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();

                    foreach (var matchListing in matchListings)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Logger.Warn(GetType(), "Background task cancellation requested in {Type:l}.{Method:l}.", GetType(), nameof(UpdateStatistics));
                        }

                        try
                        {

                            var match = await _matchDataSource.ReadMatchByRoute(matchListing.MatchRoute).ConfigureAwait(false);
                            if (match != null)
                            {
                                foreach (var innings in match.MatchInnings)
                                {
                                    using (var transaction = connection.BeginTransaction())
                                    {
                                        innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
                                        await _statisticsRepository.UpdateBowlingFigures(innings, memberKey, memberName, transaction).ConfigureAwait(false);
                                        transaction.Commit();
                                        _taskTracker.IncrementCompletedBy(taskId, 1);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(GetType(), "Error '{Error}' updating match statistics for '{MatchRoute}' in {Type:l}.{Method:l}.", ex.Message, matchListing.MatchRoute, GetType(), nameof(UpdateStatistics));
                            _taskTracker.IncrementErrorsBy(taskId, 1);
                        }
                    }
                    Logger.Info(GetType(), "Completed updating match statistics for all matches in {Type:l}.{Method:l}.", GetType(), nameof(UpdateStatistics));
                }
            }
            catch (TaskCanceledException tce)
            {
                Logger.Error(GetType(), "Caught TaskCanceledException '{Message}' in {Type:l}.{Method:l}.", tce.Message, GetType(), nameof(UpdateStatistics));
            }

            return cancellationToken;
        }
    }
}