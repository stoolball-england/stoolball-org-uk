using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Security;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
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
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public EditTournamentTeamsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITournamentDataSource tournamentDataSource,
            ITournamentRepository tournamentRepository, IAuthorizationPolicy<Tournament> authorizationPolicy, IDateTimeFormatter dateTimeFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<ActionResult> UpdateTeams([Bind(Prefix = "Tournament", Include = "TournamentName,QualificationType,MaximumTeamsInTournament,Teams")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);

            var model = new EditTournamentViewModel(CurrentPage)
            {
                Tournament = postedTournament,
                DateFormatter = _dateTimeFormatter
            };
            model.Tournament.TournamentId = beforeUpdate.TournamentId;
            model.Tournament.TournamentRoute = beforeUpdate.TournamentRoute;
            model.Tournament.TournamentName = beforeUpdate.TournamentName;
            model.Tournament.StartTime = beforeUpdate.StartTime;

            // Populate properties that will be inherited by any new transient teams
            model.Tournament.TournamentLocation = beforeUpdate.TournamentLocation;
            model.Tournament.PlayerType = beforeUpdate.PlayerType;

            model.IsAuthorized = IsAuthorized(beforeUpdate);

            if (model.IsAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _tournamentRepository.UpdateTeams(model.Tournament, currentMember.Key, Members.CurrentUserName, currentMember.Name).ConfigureAwait(false);

                // Redirect to the tournament
                return Redirect(model.Tournament.TournamentRoute);
            }

            model.Metadata.PageTitle = "Teams in the " + model.Tournament.TournamentFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            return View("EditTournamentTeams", model);
        }

        protected virtual bool IsAuthorized(Tournament tournament)
        {
            return _authorizationPolicy.CanEdit(tournament, Members);
        }
    }
}