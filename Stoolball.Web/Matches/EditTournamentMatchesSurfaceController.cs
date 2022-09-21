using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Matches
{
    public class EditTournamentMatchesSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingCacheClearer _cacheClearer;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IPostSaveRedirector _postSaveRedirector;

        public EditTournamentMatchesSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITournamentDataSource tournamentDataSource, ITournamentRepository tournamentRepository, IMatchListingCacheClearer cacheClearer,
            IAuthorizationPolicy<Tournament> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IPostSaveRedirector postSaveRedirector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _postSaveRedirector = postSaveRedirector ?? throw new ArgumentNullException(nameof(postSaveRedirector));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> UpdateMatches([Bind("Matches", Prefix = "Tournament")] Tournament postedTournament)
        {
            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.Path).ConfigureAwait(false);

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = beforeUpdate
            };
            model.Tournament.Matches = postedTournament?.Matches ?? new List<MatchInTournament>();

            // We're not interested in validating anything with a data annotation attribute
            foreach (var key in ModelState.Keys)
            {
                ModelState.Remove(key);
            }

            var teamsInTournament = model.Tournament.Teams.Select(x => x.TournamentTeamId!.Value);
            foreach (var added in model.Tournament.Matches.Where(x => !x.MatchId.HasValue))
            {
                if (added.Teams.Count < 2 || !added.Teams[0].TournamentTeamId.HasValue || !added.Teams[1].TournamentTeamId.HasValue || added.Teams[0].TournamentTeamId == added.Teams[1].TournamentTeamId)
                {
                    ModelState.AddModelError(string.Empty, $"Please remove '{added.MatchName}'. A match must be between two different teams.");
                }

                // Check for a TournamentTeamId that's not in the tournament. Pretty unlikely since it would require editing the request or a race where
                // the team is removed before this page is submitted, but since we can test we should.
                if (ModelState.IsValid)
                {
                    foreach (var team in added.Teams)
                    {
                        if (!teamsInTournament.Contains(team.TournamentTeamId!.Value))
                        {
                            ModelState.AddModelError(string.Empty, $"Please remove '{added.MatchName}'. We cannot add a match for a team that's not in the tournament.");
                        }
                    }
                }
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTournament] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedTournament = await _tournamentRepository.UpdateMatches(model.Tournament, currentMember.Key, currentMember.UserName, currentMember.Name).ConfigureAwait(false);
                _cacheClearer.ClearCacheForTournamentMatches(model.Tournament.TournamentId!.Value);

                // Use a regex to prevent part 4 of the journey Edit Matches > Edit Teams > Edit Matches > Edit Teams
                return _postSaveRedirector.WorkOutRedirect(model.Tournament.TournamentRoute, updatedTournament.TournamentRoute, "/edit", Request.Form["UrlReferrer"], $"^({updatedTournament.TournamentRoute}|{updatedTournament.TournamentRoute}/edit)$");
            }

            model.Metadata.PageTitle = "Matches in the " + model.Tournament.TournamentFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

            return View("EditTournamentMatches", model);
        }
    }
}