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
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchesRssQueryStringParser _queryStringParser;

        public MatchesRssController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           ITeamDataSource teamDataSource,
           ICompetitionDataSource competitionDataSource,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IMatchesRssQueryStringParser queryStringParser)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _queryStringParser = queryStringParser ?? throw new ArgumentNullException(nameof(queryStringParser));
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
                MatchFilter = _queryStringParser.ParseFilterFromQueryString(Request.QueryString),
                DateTimeFormatter = _dateFormatter
            };

            if (Request.RawUrl.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                var club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);
                if (club == null) { return new HttpNotFoundResult(); }
                model.MatchFilter.TeamIds.AddRange(club.Teams.Select(x => x.TeamId.Value));
            }
            else if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                var team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl).ConfigureAwait(false);
                if (team == null) { return new HttpNotFoundResult(); }
                model.MatchFilter.TeamIds.Add(team.TeamId.Value);
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);
                if (competition == null) { return new HttpNotFoundResult(); }
                model.MatchFilter.CompetitionIds.Add(competition.CompetitionId.Value);
            }


            model.Metadata.PageTitle = "Stoolball matches";
            model.Metadata.Description = $"New or updated stoolball matches on the Stoolball England website";
            if (model.MatchFilter.PlayerTypes.Any())
            {
                model.Metadata.PageTitle = $"{model.MatchFilter.PlayerTypes.First().Humanize(LetterCasing.Sentence)} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
                model.Metadata.Description = $"New or updated {model.MatchFilter.PlayerTypes.First()} stoolball matches on the Stoolball England website";
            }
            model.Matches = await _matchDataSource.ReadMatchListings(model.MatchFilter, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false);

            return View(Request.QueryString["format"] == "tweet" ? "MatchTweets" : "MatchesRss", model);
        }
    }
}