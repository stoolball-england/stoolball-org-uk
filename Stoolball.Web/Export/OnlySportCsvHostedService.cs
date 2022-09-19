using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Stoolball.Matches;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Stoolball.Web.Export
{
    public class OnlySportCsvHostedService : RecurringHostedServiceBase
    {
        private readonly IRuntimeState _runtimeState;
        private readonly ILogger<OnlySportCsvHostedService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMatchListingDataSource _matchListingDataSource;

        private static TimeSpan HowOftenWeRepeat => TimeSpan.FromDays(1);
        private static TimeSpan DelayBeforeWeStart => TimeSpan.FromMinutes(1);

        public OnlySportCsvHostedService(
            IRuntimeState runtimeState,
            IContentService contentService,
            IServerRoleAccessor serverRoleAccessor,
            IProfilingLogger profilingLogger,
            ILogger<OnlySportCsvHostedService> logger,
            IWebHostEnvironment webHostEnvironment,
            IMatchListingDataSource matchListingDataSource)
            : base(logger, HowOftenWeRepeat, DelayBeforeWeStart)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
        }

        public async override Task PerformExecuteAsync(object state)
        {
            // Don't do anything if the site is not running.
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return;
            }

            _logger.LogInformation(nameof(OnlySportCsvHostedService) + " running");

            var matches = (await _matchListingDataSource.ReadMatchListings(new MatchFilter
            {
                FromDate = DateTimeOffset.UtcNow
            }, MatchSortOrder.MatchDateEarliestFirst)).Select(x => new OnlySportCsvRecord
            {
                MatchId = x.MatchId.GetHashCode() > 0 ? x.MatchId.GetHashCode() : x.MatchId.GetHashCode() * -1,
                Title = x.MatchName,
                StartTime = x.StartTime.ToUnixTimeSeconds(),
                Latitude = x.MatchLocation?.Latitude,
                Longitude = x.MatchLocation?.Longitude,
                Website = "https://www.stoolball.org.uk" + x.MatchRoute,
                Description = x.Description()
            }).ToList();

            var path = Path.Combine(_webHostEnvironment.ContentRootPath, @"App_Data\csv");
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            using (var writer = new StreamWriter(Path.Combine(path, "onlysport.csv")))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(matches);
                }
            }
        }
    }
}
