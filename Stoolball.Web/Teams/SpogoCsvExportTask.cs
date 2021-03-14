using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using CsvHelper;
using Humanizer;
using Stoolball.Email;
using Stoolball.Teams;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;

namespace Stoolball.Web.Teams
{
    internal class SpogoCsvExportTask : RecurringTaskBase
    {
        private readonly IProfilingLogger _logger;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IContactDetailsParser _contactDetailsParser;

        public SpogoCsvExportTask(IBackgroundTaskRunner<RecurringTaskBase> runner, int delayBeforeWeStart, int howOftenWeRepeat, IProfilingLogger logger, ITeamDataSource teamDataSource,
            IContactDetailsParser contactDetailsParser)
            : base(runner, delayBeforeWeStart, howOftenWeRepeat)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
            _contactDetailsParser = contactDetailsParser ?? throw new System.ArgumentNullException(nameof(contactDetailsParser));
        }

        public async override Task<bool> PerformRunAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Warn(GetType(), "Background task cancellation requested in {Type:l}.", GetType());
                return false;
            }

            _logger.Info<SpogoCsvExportTask>(nameof(SpogoCsvExportTask) + " running");

            var teams = (await _teamDataSource.ReadTeams(new TeamFilter
            {
                ActiveTeams = true,
                TeamTypes = new List<TeamType> { TeamType.Regular }
            }).ConfigureAwait(false)).Select(x => new SpogoCsvRecord
            {
                TeamId = x.TeamId.Value.GetHashCode() > 0 ? x.TeamId.Value.GetHashCode() : x.TeamId.Value.GetHashCode() * -1,
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

            var path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\csv");
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            using (var writer = new StreamWriter(Path.Combine(path, "spogo-protected.csv")))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(teams);
                }
            }

            foreach (var team in teams) { team.ContactEmail = team.ContactPhone = string.Empty; }
            using (var writer = new StreamWriter(Path.Combine(path, "spogo.csv")))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(teams);
                }
            }

            // Keep repeating
            return true;
        }

        private static string FormatTeamDescription(Team team)
        {
            var html = team.Introduction + team.PlayingTimes;
            if (string.IsNullOrWhiteSpace(html)) { return string.Empty; }

            return Regex.Replace(Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ").Replace("&amp;", "&"), @"\s+", " ").Trim();
        }

        public override bool IsAsync => true;
    }
}