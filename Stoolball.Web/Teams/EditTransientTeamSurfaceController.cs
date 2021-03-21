using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Teams
{
    public class EditTransientTeamSurfaceController : SurfaceController
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTransientTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ITeamRepository teamRepository,
            IMatchListingDataSource matchDataSource, IAuthorizationPolicy<Team> authorizationPolicy, IDateTimeFormatter dateFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource;
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateTransientTeam([Bind(Prefix = "Team", Include = "TeamName,AgeRangeLower,AgeRangeUpper,PlayerType,Facebook,Twitter,Instagram,YouTube,Website")] Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var beforeUpdate = await _teamDataSource.ReadTeamByRoute(Request.RawUrl).ConfigureAwait(false);
            team.TeamId = beforeUpdate.TeamId;
            team.TeamRoute = beforeUpdate.TeamRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            team.Introduction = Request.Unvalidated.Form["Team.Introduction"];
            team.Cost = Request.Unvalidated.Form["Team.Cost"];
            team.PublicContactDetails = Request.Unvalidated.Form["Team.PublicContactDetails"];
            team.PrivateContactDetails = Request.Unvalidated.Form["Team.PrivateContactDetails"];

            if (team.AgeRangeLower < 11 && !team.AgeRangeUpper.HasValue)
            {
                ModelState.AddModelError("Team.AgeRangeUpper", "School teams and junior teams must specify a maximum age for players");
            }

            if (team.AgeRangeUpper.HasValue && team.AgeRangeUpper < team.AgeRangeLower)
            {
                ModelState.AddModelError("Team.AgeRangeUpper", "The maximum age for players cannot be lower than the minimum age");
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditTeam] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedTeam = await _teamRepository.UpdateTransientTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the team, as there is no actions page for a transient team
                return Redirect(updatedTeam.TeamRoute);
            }

            var model = new TeamViewModel(CurrentPage, Services.UserService)
            {
                Team = team,
            };
            model.IsAuthorized = isAuthorized;

            model.Matches = new MatchListingViewModel(CurrentPage, Services?.UserService)
            {
                Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                {
                    TeamIds = new List<Guid> { model.Team.TeamId.Value },
                    IncludeMatches = false
                }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            var match = model.Matches.Matches.First();
            model.Metadata.PageTitle = $"Edit {model.Team.TeamName}, {_dateFormatter.FormatDate(match.StartTime, false, false)}";

            model.Breadcrumbs.Add(new Breadcrumb { Name = match.MatchName, Url = new Uri(match.MatchRoute, UriKind.Relative) });

            return View("EditTransientTeam", model);
        }
    }
}