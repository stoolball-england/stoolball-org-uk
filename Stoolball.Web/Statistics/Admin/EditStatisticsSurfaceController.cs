using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics.Admin
{
    public class EditStatisticsSurfaceController : SurfaceController
    {
        private readonly ILogger<EditStatisticsSurfaceController> _logger;
        private readonly IMemberManager _memberManager;
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IBackgroundTaskTracker _taskTracker;
        private readonly IMatchListingDataSource _matchListingDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerInMatchStatisticsBuilder _playerInMatchStatisticsBuilder;
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private readonly IBackgroundTaskQueue _taskQueue;

        public EditStatisticsSurfaceController(ILogger<EditStatisticsSurfaceController> logger, IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IDatabaseConnectionFactory databaseConnectionFactory, IMatchListingDataSource matchListingDataSource, IMatchDataSource matchDataSource, IStatisticsRepository statisticsRepository,
            IBowlingFiguresCalculator bowlingFiguresCalculator, IPlayerInMatchStatisticsBuilder playerInMatchStatisticsBuilder, IPlayerIdentityFinder playerIdentityFinder,
            IBackgroundTaskQueue taskQueue, IBackgroundTaskTracker taskTracker)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _taskTracker = taskTracker ?? throw new ArgumentNullException(nameof(taskTracker));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerInMatchStatisticsBuilder = playerInMatchStatisticsBuilder ?? throw new ArgumentNullException(nameof(playerInMatchStatisticsBuilder));
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public async Task<IActionResult> UpdateStatistics()
        {
            var model = new EditStatisticsViewModel(CurrentPage, Services.UserService);
            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditStatistics] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditStatistics] && ModelState.IsValid)
            {
                model.BackgroundTaskId = Guid.NewGuid();
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                _ = _taskQueue.QueueBackgroundWorkItemAsync(async ct => await UpdateStatistics(model.BackgroundTaskId.Value, currentMember.Key, currentMember.Name, ct));
            }

            model.Metadata.PageTitle = "Update statistics";

            return View("EditStatistics", model);
        }

        /// <remarks>
        /// .NET Framework code was based on 
        /// https://devblogs.microsoft.com/aspnet/queuebackgroundworkitem-to-reliably-schedule-and-run-background-processes-in-asp-net/
        /// Now called unaltered from .NET Core based on https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0&tabs=visual-studio
        /// </remarks>
        private async Task<CancellationToken> UpdateStatistics(Guid taskId, Guid memberKey, string memberName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info("Updating match statistics for all matches in {Type:l}.{Method:l}.", GetType(), nameof(UpdateStatistics));

                var matchListings = (await _matchListingDataSource.ReadMatchListings(new MatchFilter
                {
                    IncludeMatches = true,
                    IncludeTournamentMatches = true,
                    IncludeTournaments = false,
                    UntilDate = DateTime.UtcNow
                }, MatchSortOrder.MatchDateEarliestFirst));

                _taskTracker.SetTarget(taskId, matchListings.Sum(x => x.MatchInnings.Count) + matchListings.Count);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();

                    foreach (var matchListing in matchListings)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.Warn("Background task cancellation requested in {Type:l}.{Method:l}.", GetType(), nameof(UpdateStatistics));
                        }

                        try
                        {
                            var match = await _matchDataSource.ReadMatchByRoute(matchListing.MatchRoute);
                            if (match != null)
                            {
                                using (var transaction = connection.BeginTransaction())
                                {
                                    await _statisticsRepository.DeletePlayerStatistics(match.MatchId!.Value, transaction);
                                    foreach (var innings in match.MatchInnings)
                                    {
                                        await _statisticsRepository.DeleteBowlingFigures(innings.MatchInningsId!.Value, transaction);
                                        innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
                                        await _statisticsRepository.UpdateBowlingFigures(innings, memberKey, memberName, transaction);
                                        _taskTracker.IncrementCompletedBy(taskId, 1);
                                    }

                                    var hasPlayerData = _playerIdentityFinder.PlayerIdentitiesInMatch(match).Any();
                                    if (hasPlayerData)
                                    {
                                        var statisticsData = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(match);
                                        await _statisticsRepository.UpdatePlayerStatistics(statisticsData, transaction);
                                    }
                                    _taskTracker.IncrementCompletedBy(taskId, 1);
                                    transaction.Commit();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Error '{Error}' updating match statistics for '{MatchRoute}' in {Type:l}.{Method:l}.", ex.Message, matchListing.MatchRoute, GetType(), nameof(UpdateStatistics));
                            _taskTracker.IncrementErrorsBy(taskId, 1);
                        }
                    }
                    _logger.Info("Completed updating match statistics for all matches in {Type:l}.{Method:l}.", GetType(), nameof(UpdateStatistics));
                }
            }
            catch (TaskCanceledException tce)
            {
                _logger.Error("Caught TaskCanceledException '{Message}' in {Type:l}.{Method:l}.", tce.Message, GetType(), nameof(UpdateStatistics));
            }

            return cancellationToken;
        }
    }
}