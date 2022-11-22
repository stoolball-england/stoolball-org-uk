using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class MatchesRssController : RenderController, IRenderControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IMatchesRssQueryStringParser _queryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesRssController(ILogger<MatchesRssController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IClubDataSource clubDataSource,
            ITeamDataSource teamDataSource,
            ICompetitionDataSource competitionDataSource,
            IMatchLocationDataSource matchLocationDataSource,
            IMatchListingDataSource matchDataSource,
            IMatchesRssQueryStringParser queryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _queryStringParser = queryStringParser ?? throw new ArgumentNullException(nameof(queryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchListingViewModel(CurrentPage)
            {
                AppliedMatchFilter = _queryStringParser.ParseFilterFromQueryString(Request.QueryString.Value),
            };

            var pageTitle = "Stoolball matches";
            var path = Request.Path.HasValue ? Request.Path.Value!.ToString() : string.Empty;

            if (path.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                var club = await _clubDataSource.ReadClubByRoute(Request.Path);
                if (club == null) { return NotFound(); }
                pageTitle += " for " + club.ClubName;
                model.AppliedMatchFilter.TeamIds.AddRange(club.Teams.Select(x => x.TeamId!.Value));
            }
            else if (path.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                var team = await _teamDataSource.ReadTeamByRoute(Request.Path).ConfigureAwait(false);
                if (team == null) { return NotFound(); }
                pageTitle += " for " + team.TeamName;
                model.AppliedMatchFilter.TeamIds.Add(team.TeamId!.Value);
            }
            else if (path.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false);
                if (competition == null) { return NotFound(); }
                pageTitle += " in the " + competition.CompetitionName;
                model.AppliedMatchFilter.CompetitionIds.Add(competition.CompetitionId!.Value);
            }
            else if (path.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path).ConfigureAwait(false);
                if (location == null) { return NotFound(); }
                pageTitle += " at " + location.NameAndLocalityOrTown();
                model.AppliedMatchFilter.MatchLocationIds.Add(location.MatchLocationId!.Value);
            }

            // Remove from date from filter if it's the default, and describe the remainder in the feed title.
            var clonedFilter = model.AppliedMatchFilter.Clone();
            if (clonedFilter.FromDate == DateTimeOffset.UtcNow.Date)
            {
                clonedFilter.FromDate = null;
            }
            // Remove to date filter if it's a rolling date
            // (if user has set a specific end date a exactly year in the future unfortunately we'll miss it, but this is only for the description)
            if (clonedFilter.UntilDate == DateTimeOffset.UtcNow.Date.AddDays(365).AddDays(1).AddSeconds(-1))
            {
                clonedFilter.UntilDate = null;
            }
            model.Metadata.PageTitle = pageTitle + _matchFilterHumanizer.MatchingFilter(clonedFilter);
            model.Metadata.Description = $"New or updated stoolball matches on the Stoolball England website";
            if (model.AppliedMatchFilter.PlayerTypes.Any())
            {
                model.Metadata.PageTitle = $"{model.AppliedMatchFilter.PlayerTypes.First().Humanize(LetterCasing.Sentence)} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
                model.Metadata.Description = $"New or updated {model.AppliedMatchFilter.PlayerTypes.First()} stoolball matches on the Stoolball England website";
            }


            // TEMP:
            model.AppliedMatchFilter.FromDate = DateTime.Now.AddYears(-2);

            model.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.LatestUpdateFirst);

            return View(Request.Query["format"] == "tweet" ? "MatchTweets" : "MatchesRss", model);
        }
    }
}