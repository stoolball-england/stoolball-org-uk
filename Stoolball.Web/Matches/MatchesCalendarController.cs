using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class MatchesCalendarController : RenderController, IRenderControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchListingDataSource;
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesCalendarController(ILogger<MatchesCalendarController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IClubDataSource clubDataSource,
            ITeamDataSource teamDataSource,
            ICompetitionDataSource competitionDataSource,
            IMatchLocationDataSource matchLocationDataSource,
            IMatchListingDataSource matchListingDataSource,
            ITournamentDataSource tournamentDataSource,
            IMatchDataSource matchDataSource,
            IDateTimeFormatter dateFormatter,
            IMatchFilterQueryStringParser matchFilterQueryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchListingViewModel(CurrentPage)
            {
                DefaultMatchFilter = new MatchFilter
                {
                    FromDate = DateTimeOffset.UtcNow.Date,
                    IncludeMatches = true,
                    IncludeTournaments = true,
                    IncludeTournamentMatches = false
                }
            };
            model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, Request.QueryString.Value);

            // Don't allow matches in the past - this is a calendar for planning future events
            if (model.AppliedMatchFilter.FromDate < model.DefaultMatchFilter.FromDate)
            {
                model.AppliedMatchFilter.FromDate = model.DefaultMatchFilter.FromDate;
            }

            var pageTitle = "Stoolball matches and tournaments";
            var legacyTournamentsCalendarUrl = Regex.Match(Request.Path, "/tournaments/(all|mixed|ladies|junior)/calendar.ics", RegexOptions.IgnoreCase);
            var path = Request.Path.HasValue ? Request.Path.Value!.ToString() : string.Empty;

            if (path.StartsWith("/matches/ics", StringComparison.OrdinalIgnoreCase))
            {
                model.AppliedMatchFilter.IncludeTournaments = false;
                pageTitle = "Stoolball matches";
            }
            else if (legacyTournamentsCalendarUrl.Success || path.StartsWith("/tournaments.ics", StringComparison.OrdinalIgnoreCase))
            {
                model.AppliedMatchFilter.IncludeMatches = false;
                pageTitle = "Stoolball tournaments";

                if (legacyTournamentsCalendarUrl.Success)
                {
                    switch (legacyTournamentsCalendarUrl.Groups[1].Value.ToUpperInvariant())
                    {
                        case "MIXED":
                            model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.Mixed);
                            break;
                        case "LADIES":
                            model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.Ladies);
                            break;
                        case "JUNIOR":
                            model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.JuniorMixed);
                            model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.JuniorGirls);
                            model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.JuniorBoys);
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (path.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase))
            {
                var tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Path);
                if (tournament == null) { return NotFound(); }
                pageTitle = tournament.TournamentFullName(x => tournament.StartTimeIsKnown ? _dateFormatter.FormatDateTime(tournament.StartTime, true, false) : _dateFormatter.FormatDate(tournament.StartTime, true, false));
                model.Matches.Add(tournament.ToMatchListing());
            }
            else if (path.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase))
            {
                var match = await _matchDataSource.ReadMatchByRoute(Request.Path);
                if (match == null) { return NotFound(); }
                pageTitle = match.MatchFullName(x => match.StartTimeIsKnown ? _dateFormatter.FormatDateTime(match.StartTime, true, false) : _dateFormatter.FormatDate(match.StartTime, true, false));
                model.Matches.Add(match.ToMatchListing());
            }
            else if (path.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                var club = await _clubDataSource.ReadClubByRoute(Request.Path);
                if (club == null)
                {
                    return NotFound();
                }
                pageTitle += " for " + club.ClubName;
                model.AppliedMatchFilter.TeamIds.AddRange(club.Teams.Select(x => x.TeamId!.Value));
            }
            else if (path.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                var team = await _teamDataSource.ReadTeamByRoute(Request.Path);
                if (team == null) { return NotFound(); }
                pageTitle += " for " + team.TeamName;
                model.AppliedMatchFilter.TeamIds.Add(team.TeamId!.Value);
            }
            else if (path.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path);
                if (competition == null) { return NotFound(); }
                pageTitle += " in the " + competition.CompetitionName;
                model.AppliedMatchFilter.CompetitionIds.Add(competition.CompetitionId!.Value);
            }
            else if (path.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path);
                if (location == null) { return NotFound(); }
                pageTitle += " at " + location.NameAndLocalityOrTown();
                model.AppliedMatchFilter.MatchLocationIds.Add(location.MatchLocationId!.Value);
            }

            // Remove from date from filter if it's the default, and describe the remainder in the feed title.
            var clonedFilter = model.AppliedMatchFilter.Clone();
            if (clonedFilter.FromDate == model.DefaultMatchFilter.FromDate)
            {
                clonedFilter.FromDate = null;
            }
            model.Metadata.PageTitle = pageTitle + _matchFilterHumanizer.MatchingFilter(clonedFilter);
            if (model.AppliedMatchFilter.PlayerTypes.Any())
            {
                model.Metadata.PageTitle = $"{model.AppliedMatchFilter.PlayerTypes.First().Humanize(LetterCasing.Sentence).Replace("Junior mixed", "Junior")} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
            }
            if (!model.Matches.Any())
            {
                model.Matches = await _matchListingDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.LatestUpdateFirst);
            }

            return CurrentTemplate(model);
        }
    }
}