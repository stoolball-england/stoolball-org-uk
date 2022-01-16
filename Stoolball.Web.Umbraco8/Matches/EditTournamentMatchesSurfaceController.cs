using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class EditTournamentMatchesSurfaceController : SurfaceController
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly ICacheClearer<Tournament> _cacheClearer;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IPostSaveRedirector _postSaveRedirector;

        public EditTournamentMatchesSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITournamentDataSource tournamentDataSource, ICacheClearer<Tournament> cacheClearer,
            ITournamentRepository tournamentRepository, IAuthorizationPolicy<Tournament> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IPostSaveRedirector postSaveRedirector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
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
        public async Task<ActionResult> UpdateMatches([Bind(Prefix = "Tournament", Include = "Matches")] Tournament postedTournament)
        {
            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = beforeUpdate,
                DateFormatter = _dateTimeFormatter
            };
            model.Tournament.Matches = postedTournament?.Matches ?? new List<MatchInTournament>();

            // We're not interested in validating anything with a data annotation attribute
            foreach (var key in ModelState.Keys)
            {
                ModelState[key].Errors.Clear();
            }

            var teamsInTournament = model.Tournament.Teams.Select(x => x.TournamentTeamId.Value);
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
                        if (!teamsInTournament.Contains(team.TournamentTeamId.Value))
                        {
                            ModelState.AddModelError(string.Empty, $"Please remove '{added.MatchName}'. We cannot add a match for a team that's not in the tournament.");
                        }
                    }
                }
            }

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditTournament] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedTournament = await _tournamentRepository.UpdateMatches(model.Tournament, currentMember.Key, Members.CurrentUserName, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.ClearCacheFor(updatedTournament).ConfigureAwait(false);

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