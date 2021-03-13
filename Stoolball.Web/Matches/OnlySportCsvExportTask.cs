using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using CsvHelper;
using Stoolball.Matches;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;

namespace Stoolball.Web.Matches
{
    internal class OnlySportCsvExportTask : RecurringTaskBase
    {
        private readonly IProfilingLogger _logger;
        private readonly IMatchListingDataSource _matchDataSource;

        public OnlySportCsvExportTask(IBackgroundTaskRunner<RecurringTaskBase> runner, int delayBeforeWeStart, int howOftenWeRepeat, IProfilingLogger logger, IMatchListingDataSource matchDataSource)
            : base(runner, delayBeforeWeStart, howOftenWeRepeat)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
        }

        public async override Task<bool> PerformRunAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Warn(GetType(), "Background task cancellation requested in {Type:l}.", GetType());
                return false;
            }

            _logger.Info<OnlySportCsvExportTask>(nameof(OnlySportCsvExportTask) + " running");

            var matches = (await _matchDataSource.ReadMatchListings(new MatchFilter
            {
                FromDate = DateTimeOffset.UtcNow.AddYears(-2)
            }).ConfigureAwait(false)).Select(x => new OnlySportCsvRecord
            {
                MatchId = x.MatchId.GetHashCode() > 0 ? x.MatchId.GetHashCode() : x.MatchId.GetHashCode() * -1,
                Title = x.MatchName,
                StartTime = x.StartTime.ToUnixTimeSeconds(),
                Latitude = x.MatchLocation?.Latitude,
                Longitude = x.MatchLocation?.Longitude,
                Website = "https://www.stoolball.org.uk" + x.MatchRoute,
                Description = x.Description()
            }).ToList();

            var path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\csv", "onlysport.csv");
            using (var writer = new StreamWriter(path))
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