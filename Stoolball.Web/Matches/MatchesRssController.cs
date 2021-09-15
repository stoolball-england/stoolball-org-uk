using System;
using System.Globalization;
using System.Linq;
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
    public class MatchesRssController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchesRssQueryStringParser _queryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesRssController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           ITeamDataSource teamDataSource,
           ICompetitionDataSource competitionDataSource,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IMatchesRssQueryStringParser queryStringParser,
           IMatchFilterHumanizer matchFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _queryStringParser = queryStringParser ?? throw new ArgumentNullException(nameof(queryStringParser));
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
                AppliedMatchFilter = _queryStringParser.ParseFilterFromQueryString(Request.QueryString),
                DateTimeFormatter = _dateFormatter
            };

            string pageTitle = "Stoolball matches";
            if (Request.RawUrl.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                var club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);
                if (club == null) { return new HttpNotFoundResult(); }
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
            model.Metadata.Description = $"New or updated stoolball matches on the Stoolball England website";
            if (model.AppliedMatchFilter.PlayerTypes.Any())
            {
                model.Metadata.PageTitle = $"{model.AppliedMatchFilter.PlayerTypes.First().Humanize(LetterCasing.Sentence)} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
                model.Metadata.Description = $"New or updated {model.AppliedMatchFilter.PlayerTypes.First()} stoolball matches on the Stoolball England website";
            }
            model.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false);

            return View(Request.QueryString["format"] == "tweet" ? "MatchTweets" : "MatchesRss", model);
        }
    }
}