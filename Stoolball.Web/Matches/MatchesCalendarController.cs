using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

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
           IDateTimeFormatter dateFormatter,
           IMatchFilterQueryStringParser matchFilterQueryStringParser,
           IMatchFilterHumanizer matchFilterHumanizer)
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
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
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
                AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString(Request.Url.Query)),
                DateTimeFormatter = _dateFormatter
            };

            if (Request.QueryString["from"] == null)
            {
                model.AppliedMatchFilter.FromDate = DateTimeOffset.UtcNow.Date;
            }
            model.AppliedMatchFilter.IncludeMatches = true;
            model.AppliedMatchFilter.IncludeTournaments = true;
            model.AppliedMatchFilter.IncludeTournamentMatches = false;

            var pageTitle = "Stoolball matches and tournaments";
            var legacyTournamentsCalendarUrl = Regex.Match(Request.RawUrl, "/tournaments/(all|mixed|ladies|junior)/calendar.ics", RegexOptions.IgnoreCase);

            if (Request.RawUrl.StartsWith("/matches.ics", StringComparison.OrdinalIgnoreCase))
            {
                model.AppliedMatchFilter.IncludeTournaments = false;
                pageTitle = "Stoolball matches";
            }
            else if (legacyTournamentsCalendarUrl.Success || Request.RawUrl.StartsWith("/tournaments.ics", StringComparison.OrdinalIgnoreCase))
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
            else if (Request.RawUrl.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase))
            {
                var tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);
                if (tournament == null) { return new HttpNotFoundResult(); }
                pageTitle = tournament.TournamentFullName(x => tournament.StartTimeIsKnown ? _dateFormatter.FormatDateTime(tournament.StartTime, true, false) : _dateFormatter.FormatDate(tournament.StartTime, true, false));
                model.Matches.Add(tournament.ToMatchListing());
            }
            else if (Request.RawUrl.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase))
            {
                var match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false);
                if (match == null) { return new HttpNotFoundResult(); }
                pageTitle = match.MatchFullName(x => match.StartTimeIsKnown ? _dateFormatter.FormatDateTime(match.StartTime, true, false) : _dateFormatter.FormatDate(match.StartTime, true, false));
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
                model.AppliedMatchFilter.TeamIds.AddRange(club.Teams.Select(x => x.TeamId.Value));
            }
            else if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                var team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl).ConfigureAwait(false);
                if (team == null) { return new HttpNotFoundResult(); }
                pageTitle += " for " + team.TeamName;
                model.AppliedMatchFilter.TeamIds.Add(team.TeamId.Value);
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);
                if (competition == null) { return new HttpNotFoundResult(); }
                pageTitle += " in the " + competition.CompetitionName;
                model.AppliedMatchFilter.CompetitionIds.Add(competition.CompetitionId.Value);
            }
            else if (Request.RawUrl.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl).ConfigureAwait(false);
                if (location == null) { return new HttpNotFoundResult(); }
                pageTitle += " at " + location.NameAndLocalityOrTown();
                model.AppliedMatchFilter.MatchLocationIds.Add(location.MatchLocationId.Value);
            }


            // Remove date from filter and describe the remainder in the feed title, because the date range is not the subject of the feed,
            // it's just what we're including in the feed right now to return only currently relevant data.
            var clonedFilter = model.AppliedMatchFilter.Clone();
            clonedFilter.FromDate = clonedFilter.UntilDate = null;
            model.Metadata.PageTitle = pageTitle + _matchFilterHumanizer.MatchingFilter(clonedFilter);
            if (model.AppliedMatchFilter.PlayerTypes.Any())
            {
                model.Metadata.PageTitle = $"{model.AppliedMatchFilter.PlayerTypes.First().Humanize(LetterCasing.Sentence).Replace("Junior mixed", "Junior")} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
            }
            if (!model.Matches.Any())
            {
                model.Matches = await _matchListingDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false);
            }

            return CurrentTemplate(model);
        }
    }
}