using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class MatchesCalendarController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchListingDataSource;
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;

        public MatchesCalendarController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           ITeamDataSource teamDataSource,
           ICompetitionDataSource competitionDataSource,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchListingDataSource matchListingDataSource,
           ITournamentDataSource tournamentDataSource,
           IMatchDataSource matchDataSource,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchListingViewModel(contentModel.Content, Services?.UserService)
            {
                MatchFilter = new MatchFilter
                {
                    IncludeMatches = true,
                    IncludeTournaments = true,
                    IncludeTournamentMatches = false,
                    FromDate = DateTimeOffset.UtcNow
                },
                DateTimeFormatter = _dateFormatter
            };

            var pageTitle = "Stoolball matches and tournaments";
            var legacyTournamentsCalendarUrl = Regex.Match(Request.RawUrl, "/tournaments/(all|mixed|ladies|junior)/calendar.ics", RegexOptions.IgnoreCase);

            if (Request.RawUrl.StartsWith("/matches.ics", StringComparison.OrdinalIgnoreCase))
            {
                model.MatchFilter.IncludeTournaments = false;
                pageTitle = "Stoolball matches";
            }
            else if (legacyTournamentsCalendarUrl.Success || Request.RawUrl.StartsWith("/tournaments.ics", StringComparison.OrdinalIgnoreCase))
            {
                model.MatchFilter.IncludeMatches = false;
                pageTitle = "Stoolball tournaments";

                if (legacyTournamentsCalendarUrl.Success)
                {
                    switch (legacyTournamentsCalendarUrl.Groups[1].Value.ToUpperInvariant())
                    {
                        case "MIXED":
                            model.MatchFilter.PlayerTypes.Add(PlayerType.Mixed);
                            break;
                        case "LADIES":
                            model.MatchFilter.PlayerTypes.Add(PlayerType.Ladies);
                            break;
                        case "JUNIOR":
                            model.MatchFilter.PlayerTypes.Add(PlayerType.JuniorMixed);
                            model.MatchFilter.PlayerTypes.Add(PlayerType.JuniorGirls);
                            model.MatchFilter.PlayerTypes.Add(PlayerType.JuniorBoys);
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (Request.RawUrl.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase))
            {
                var tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);
                if (tournament == null) { return new HttpNotFoundResult(); }
                pageTitle = tournament.TournamentFullName(x => tournament.StartTimeIsKnown ? _dateFormatter.FormatDateTime(tournament.StartTime.LocalDateTime, true, false) : _dateFormatter.FormatDate(tournament.StartTime.LocalDateTime, true, false));
                model.Matches.Add(tournament.ToMatchListing());
            }
            else if (Request.RawUrl.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase))
            {
                var match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false);
                if (match == null) { return new HttpNotFoundResult(); }
                pageTitle = match.MatchFullName(x => match.StartTimeIsKnown ? _dateFormatter.FormatDateTime(match.StartTime.LocalDateTime, true, false) : _dateFormatter.FormatDate(match.StartTime.LocalDateTime, true, false));
                model.Matches.Add(match.ToMatchListing());
            }
            else if (Request.RawUrl.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                var club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);
                if (club == null)
                {
                    return new HttpNotFoundResult();
                }
                pageTitle += " for " + club.ClubName;
                model.MatchFilter.TeamIds.AddRange(club.Teams.Select(x => x.TeamId.Value));
            }
            else if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                var team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl).ConfigureAwait(false);
                if (team == null) { return new HttpNotFoundResult(); }
                pageTitle += " for " + team.TeamName;
                model.MatchFilter.TeamIds.Add(team.TeamId.Value);
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);
                if (competition == null) { return new HttpNotFoundResult(); }
                pageTitle += " in the " + competition.CompetitionName;
                model.MatchFilter.CompetitionIds.Add(competition.CompetitionId.Value);
            }
            else if (Request.RawUrl.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl).ConfigureAwait(false);
                if (location == null) { return new HttpNotFoundResult(); }
                pageTitle += " at " + location.NameAndLocalityOrTown();
                model.MatchFilter.MatchLocationIds.Add(location.MatchLocationId.Value);
            }


            model.Metadata.PageTitle = pageTitle;
            if (model.MatchFilter.PlayerTypes.Any())
            {
                model.Metadata.PageTitle = $"{model.MatchFilter.PlayerTypes.First().Humanize(LetterCasing.Sentence).Replace("Junior mixed", "Junior")} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
            }
            if (!model.Matches.Any())
            {
                model.Matches = await _matchListingDataSource.ReadMatchListings(model.MatchFilter, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false);
            }

            return CurrentTemplate(model);
        }
    }
}