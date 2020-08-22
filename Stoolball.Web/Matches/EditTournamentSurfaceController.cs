using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Security;
using System;
using System.Globalization;
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
    public class EditTournamentSurfaceController : SurfaceController
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public EditTournamentSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
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
        public async Task<ActionResult> UpdateTournament([Bind(Prefix = "Tournament", Include = "TournamentName,QualificationType,PlayerType,PlayersPerTeam,OversPerInningsDefault")] Tournament postedTournament)
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

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            model.Tournament.TournamentNotes = Request.Unvalidated.Form["Tournament.TournamentNotes"];

            if (!string.IsNullOrEmpty(Request.Form["TournamentDate"]))
            {
                model.TournamentDate = DateTimeOffset.Parse(Request.Form["TournamentDate"], CultureInfo.CurrentCulture);
                model.Tournament.StartTime = model.TournamentDate.Value;
                if (!string.IsNullOrEmpty(Request.Form["StartTime"]))
                {
                    model.StartTime = DateTimeOffset.Parse(Request.Form["StartTime"], CultureInfo.CurrentCulture);
                    model.Tournament.StartTime = model.Tournament.StartTime.Add(model.StartTime.Value.TimeOfDay);
                    model.Tournament.StartTimeIsKnown = true;
                }
                else
                {
                    // If no start time specified, use a typical one but don't show it
                    model.Tournament.StartTime.AddHours(11);
                    model.Tournament.StartTimeIsKnown = false;
                }
            }
            if (!string.IsNullOrEmpty(Request.Form["TournamentLocationId"]))
            {
                model.TournamentLocationId = new Guid(Request.Form["TournamentLocationId"]);
                model.TournamentLocationName = Request.Form["TournamentLocationName"];
                model.Tournament.TournamentLocation = new MatchLocation
                {
                    MatchLocationId = model.TournamentLocationId
                };
            }

            model.IsAuthorized = IsAuthorized(beforeUpdate);

            if (model.IsAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _tournamentRepository.UpdateTournament(model.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the tournament
                return Redirect(model.Tournament.TournamentRoute);
            }

            model.Metadata.PageTitle = "Edit " + model.Tournament.TournamentFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            return View("EditTournament", model);
        }

        protected virtual bool IsAuthorized(Tournament tournament)
        {
            return _authorizationPolicy.CanEdit(tournament, Members);
        }
    }
}