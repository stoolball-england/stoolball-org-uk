using System;
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
    public class EditTournamentTeamsSurfaceController : SurfaceController
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly ICacheClearer<Tournament> _cacheClearer;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IPostSaveRedirector _postSaveRedirector;

        public EditTournamentTeamsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
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
        public async Task<ActionResult> UpdateTeams([Bind(Prefix = "Tournament", Include = "MaximumTeamsInTournament,Teams")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = beforeUpdate,
                DateFormatter = _dateTimeFormatter
            };
            model.Tournament.MaximumTeamsInTournament = postedTournament.MaximumTeamsInTournament;
            model.Tournament.Teams = postedTournament.Teams;

            // We're not interested in validating other details of the tournament or the selected teams
            foreach (var key in ModelState.Keys.Where(x => x != "Tournament.MaximumTeamsInTournament"))
            {
                ModelState[key].Errors.Clear();
            }

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditTournament] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedTournament = await _tournamentRepository.UpdateTeams(model.Tournament, currentMember.Key, Members.CurrentUserName, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.ClearCacheFor(updatedTournament).ConfigureAwait(false);

                return _postSaveRedirector.WorkOutRedirect(model.Tournament.TournamentRoute, updatedTournament.TournamentRoute, "/edit", Request.Form["UrlReferrer"], null);
            }

            model.Metadata.PageTitle = "Teams in the " + model.Tournament.TournamentFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

            return View("EditTournamentTeams", model);
        }
    }
}