using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Stoolball.Email;
using Stoolball.Teams;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Stoolball.Web.Export
{
    public class SpogoCsvHostedService : RecurringHostedServiceBase
    {
        private readonly IRuntimeState _runtimeState;
        private readonly IContentService _contentService;
        private readonly IServerRoleAccessor _serverRoleAccessor;
        private readonly IProfilingLogger _profilingLogger;
        private readonly ILogger<OnlySportCsvHostedService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IContactDetailsParser _contactDetailsParser;

        private static TimeSpan HowOftenWeRepeat => TimeSpan.FromDays(1);
        private static TimeSpan DelayBeforeWeStart => TimeSpan.FromMinutes(1);

        public SpogoCsvHostedService(
            IRuntimeState runtimeState,
            IContentService contentService,
            IServerRoleAccessor serverRoleAccessor,
            IProfilingLogger profilingLogger,
            ILogger<OnlySportCsvHostedService> logger,
            IWebHostEnvironment webHostEnvironment,
            ITeamDataSource teamDataSource,
            IContactDetailsParser contactDetailsParser)
            : base(logger, HowOftenWeRepeat, DelayBeforeWeStart)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _serverRoleAccessor = serverRoleAccessor ?? throw new ArgumentNullException(nameof(serverRoleAccessor));
            _profilingLogger = profilingLogger ?? throw new ArgumentNullException(nameof(profilingLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _contactDetailsParser = contactDetailsParser ?? throw new ArgumentNullException(nameof(contactDetailsParser));
        }

        public async override Task PerformExecuteAsync(object state)
        {
            // Don't do anything if the site is not running.
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return;
            }

            _logger.LogInformation(nameof(SpogoCsvHostedService) + " running");

            var teams = (await _teamDataSource.ReadTeams(new TeamFilter
            {
                ActiveTeams = true,
                TeamTypes = new List<TeamType?> { TeamType.Regular }
            }).ConfigureAwait(false)).Select(x => new SpogoCsvRecord
            {
                TeamId = x.TeamId!.Value.GetHashCode() > 0 ? x.TeamId.Value.GetHashCode() : x.TeamId.Value.GetHashCode() * -1,
                TeamName = x.TeamName + " Stoolball Club",
                Description = FormatTeamDescription(x),
                PlayerType = x.PlayerType.Humanize(),
                HomeGroundName = x.MatchLocations.FirstOrDefault()?.Name(),
                StreetName = x.MatchLocations.FirstOrDefault()?.StreetDescription,
                Locality = x.MatchLocations.FirstOrDefault()?.Locality,
                Town = x.MatchLocations.FirstOrDefault()?.Town,
                AdministrativeArea = x.MatchLocations.FirstOrDefault()?.AdministrativeArea,
                Postcode = x.MatchLocations.FirstOrDefault()?.Postcode,
                Country = "England",
                Latitude = x.MatchLocations.FirstOrDefault()?.Latitude,
                Longitude = x.MatchLocations.FirstOrDefault()?.Longitude,
                Website = !string.IsNullOrWhiteSpace(x.Website) ? x.Website : "https://www.stoolball.org.uk" + x.TeamRoute,
                ContactEmail = _contactDetailsParser.ParseFirstEmailAddress(x.PublicContactDetails),
                ContactPhone = _contactDetailsParser.ParseFirstPhoneNumber(x.PublicContactDetails)
            }).Where(x => !string.IsNullOrEmpty(x.ContactEmail) || !string.IsNullOrEmpty(x.ContactPhone)).ToList();


            var path = Path.Combine(_webHostEnvironment.ContentRootPath, @"App_Data\csv");
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            using (var writer = new StreamWriter(Path.Combine(path, "spogo-protected.csv")))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(teams);
                }
            }
        }

        private static string FormatTeamDescription(Team team)
        {
            var html = team.Introduction + team.PlayingTimes;
            if (string.IsNullOrWhiteSpace(html)) { return string.Empty; }

            return Regex.Replace(Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ").Replace("&amp;", "&"), @"\s+", " ").Trim();
        }
    }
}
