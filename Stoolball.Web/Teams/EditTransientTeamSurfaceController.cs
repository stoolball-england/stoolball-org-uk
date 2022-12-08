using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Teams
{
    public class EditTransientTeamSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTransientTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITeamDataSource teamDataSource, ITeamRepository teamRepository, IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy, IDateTimeFormatter dateFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<IActionResult> UpdateTransientTeam([Bind("TeamName", "AgeRangeLower", "AgeRangeUpper", "PlayerType", "Facebook", "Twitter", "Instagram", "YouTube", "Website", Prefix = "Team")] Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var beforeUpdate = await _teamDataSource.ReadTeamByRoute(Request.Path).ConfigureAwait(false);
            team.TeamId = beforeUpdate.TeamId;
            team.TeamRoute = beforeUpdate.TeamRoute;

            // get this from the form instead of via modelbinding so that HTML can be allowed
            team.Introduction = Request.Form["Team.Introduction"];
            team.Cost = Request.Form["Team.Cost"];
            team.PublicContactDetails = Request.Form["Team.PublicContactDetails"];
            team.PrivateContactDetails = Request.Form["Team.PrivateContactDetails"];

            if (team.AgeRangeLower < 11 && !team.AgeRangeUpper.HasValue)
            {
                ModelState.AddModelError("Team.AgeRangeUpper", "School teams and junior teams must specify a maximum age for players");
            }

            if (team.AgeRangeUpper.HasValue && team.AgeRangeUpper < team.AgeRangeLower)
            {
                ModelState.AddModelError("Team.AgeRangeUpper", "The maximum age for players cannot be lower than the minimum age");
            }

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditTeam] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedTeam = await _teamRepository.UpdateTransientTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the team, as there is no actions page for a transient team
                return Redirect(updatedTeam.TeamRoute);
            }

            var model = new TeamViewModel(CurrentPage, Services.UserService)
            {
                Team = team,
            };
            model.Authorization.CurrentMemberIsAuthorized = isAuthorized;

            model.Matches = new MatchListingViewModel(CurrentPage, Services?.UserService)
            {
                Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                {
                    TeamIds = new List<Guid> { model.Team.TeamId!.Value },
                    IncludeMatches = false
                }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false)
            };

            var match = model.Matches.Matches.First();
            model.Metadata.PageTitle = $"Edit {model.Team.TeamName}, {_dateFormatter.FormatDate(match.StartTime, false, false)}";

            model.Breadcrumbs.Add(new Breadcrumb { Name = match.MatchName, Url = new Uri(match.MatchRoute, UriKind.Relative) });

            return View("EditTransientTeam", model);
        }
    }
}