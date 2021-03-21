using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using CsvHelper;
using Humanizer;
using Stoolball.Matches;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;

namespace Stoolball.Web.Matches
{
    internal class WomensSportsNetworkCsvExportTask : RecurringTaskBase
    {
        private readonly IProfilingLogger _logger;
        private readonly IMatchListingDataSource _matchDataSource;

        public WomensSportsNetworkCsvExportTask(IBackgroundTaskRunner<RecurringTaskBase> runner, int delayBeforeWeStart, int howOftenWeRepeat, IProfilingLogger logger, IMatchListingDataSource matchDataSource)
            : base(runner, delayBeforeWeStart, howOftenWeRepeat)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
        }

        public async override Task<bool> PerformRunAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Warn(GetType(), "Background task cancellation requested in {Type:l}.", GetType());
                return false;
            }

            _logger.Info<WomensSportsNetworkCsvExportTask>(nameof(WomensSportsNetworkCsvExportTask) + " running");

            var matches = (await _matchDataSource.ReadMatchListings(new MatchFilter
            {
                FromDate = DateTimeOffset.UtcNow
            }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false)).Select(x => new WomensSportsNetworkCsvRecord
            {
                MatchId = x.MatchId.GetHashCode() > 0 ? x.MatchId.GetHashCode() : x.MatchId.GetHashCode() * -1,
                Title = x.MatchName,
                StartTime = x.StartTime.ToString("o"), // ISO8601 (RFC3339) http://www.faqs.org/rfcs/rfc3339.html
                MatchType = x.MatchType?.Humanize() ?? "Tournament",
                Overs = x.Overs,
                PlayerType = x.PlayerType.Humanize(),
                PlayersPerTeam = x.PlayersPerTeam,
                Latitude = x.MatchLocation?.Latitude,
                Longitude = x.MatchLocation?.Longitude,
                SecondaryAddressableObjectName = x.MatchLocation?.SecondaryAddressableObjectName,
                PrimaryAddressableObjectName = x.MatchLocation?.PrimaryAddressableObjectName,
                Town = x.MatchLocation?.Town,
                Website = "https://www.stoolball.org.uk" + x.MatchRoute,
                Description = x.Description()
            }).ToList();

            var path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\csv");
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            using (var writer = new StreamWriter(Path.Combine(path, "wsnet.csv")))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(matches);
                }
            }

            // Keep repeating
            return true;
        }

        public override bool IsAsync => true;
    }
}