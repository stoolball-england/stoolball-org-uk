using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
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
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ISeasonEstimator _seasonEstimator;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;

        public MatchesForTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           ISeasonEstimator seasonEstimator,
           ICreateMatchSeasonSelector createMatchSeasonSelector,
           IAuthorizationPolicy<Team> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
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
                var model = new TeamViewModel(contentModel.Content, Services?.UserService)
                {
                    Team = team,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            TeamIds = new List<Guid> { team.TeamId.Value },
                            FromDate = _seasonEstimator.EstimateSeasonDates(DateTimeOffset.UtcNow).fromDate
                        }).ConfigureAwait(false),
                        DateTimeFormatter = _dateFormatter
                    },
                };

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Team);
                model.IsInACurrentLeague = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, MatchType.LeagueMatch).Any();
                model.IsInACurrentKnockoutCompetition = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, MatchType.KnockoutMatch).Any();

                model.Metadata.PageTitle = $"Matches for {model.Team.TeamName} stoolball team";

                return CurrentTemplate(model);
            }
        }
    }
}