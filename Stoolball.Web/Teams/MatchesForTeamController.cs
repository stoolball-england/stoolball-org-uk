using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class MatchesForTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesForTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IMatchFilterFactory matchFilterFactory,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           ICreateMatchSeasonSelector createMatchSeasonSelector,
           IAuthorizationPolicy<Team> authorizationPolicy,
           IMatchFilterQueryStringParser matchFilterQueryStringParser,
           IMatchFilterHumanizer matchFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
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

            var team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);

            if (team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var filter = _matchFilterFactory.MatchesForTeams(new List<Guid> { team.TeamId.Value });
                var model = new TeamViewModel(contentModel.Content, Services?.UserService)
                {
                    Team = team,
                    DefaultMatchFilter = filter.filter,
                    Matches = new MatchListingViewModel(contentModel.Content, Services?.UserService)
                    {
                        DateTimeFormatter = _dateFormatter
                    },
                };
                model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, HttpUtility.ParseQueryString(Request.Url.Query));
                model.Matches.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, filter.sortOrder).ConfigureAwait(false);

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Team);
                model.IsInACurrentLeague = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, MatchType.LeagueMatch).Any();
                model.IsInACurrentKnockoutCompetition = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, MatchType.KnockoutMatch).Any();

                var userFilter = _matchFilterHumanizer.MatchingFilter(model.AppliedMatchFilter);
                if (!string.IsNullOrWhiteSpace(userFilter))
                {
                    model.FilterDescription = _matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter) + userFilter;
                }
                model.Metadata.PageTitle = $"{_matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter)} for {model.Team.TeamName} stoolball team{userFilter}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (model.Team.Club != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Team.Club.ClubName, Url = new Uri(model.Team.Club.ClubRoute, UriKind.Relative) });
                }

                return CurrentTemplate(model);
            }
        }
    }
}