using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Security;
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
        public async Task<ActionResult> UpdateTournament([Bind(Prefix = "Tournament", Include = "TournamentName,QualificationType,PlayerType,PlayersPerTeam,DefaultOverSets")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);

            postedTournament.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);
            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = postedTournament,
                DateFormatter = _dateTimeFormatter
            };
            model.Tournament.TournamentId = beforeUpdate.TournamentId;
            model.Tournament.TournamentRoute = beforeUpdate.TournamentRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            model.Tournament.TournamentNotes = Request.Unvalidated.Form["Tournament.TournamentNotes"];

            if (!string.IsNullOrEmpty(Request.Form["TournamentDate"]) && DateTimeOffset.TryParse(Request.Form["TournamentDate"], out var parsedDate))
            {
                model.TournamentDate = parsedDate;
                model.Tournament.StartTime = model.TournamentDate.Value;

                if (!string.IsNullOrEmpty(Request.Form["StartTime"]))
                {
                    if (DateTimeOffset.TryParse(Request.Form["StartTime"], out var parsedTime))
                    {
                        model.StartTime = parsedTime;
                        model.Tournament.StartTime = model.Tournament.StartTime.Add(model.StartTime.Value.TimeOfDay);
                        model.Tournament.StartTimeIsKnown = true;
                    }
                    else
                    {
                        // This may be seen in browsers that don't support <input type="time" />, mainly Safari.
                        // Each browser that supports <input type="time" /> may have a very different interface so don't advertise
                        // this format up-front as it could confuse the majority. Instead, only reveal it here.
                        ModelState.AddModelError("StartTime", "Enter a time in 24-hour HH:MM format.");
                    }
                }
                else
                {
                    // If no start time specified, use a typical one but don't show it
                    model.Tournament.StartTime.AddHours(11);
                    model.Tournament.StartTimeIsKnown = false;
                }
            }
            else
            {
                // This may be seen in browsers that don't support <input type="date" />, mainly Safari. 
                // This is the format <input type="date" /> expects and posts, so we have to repopulate the field in this format,
                // so although this code _can_ parse other formats we don't advertise that. We also don't want YYYY-MM-DD in 
                // the field label as it would confuse the majority, so only reveal it here.
                ModelState.AddModelError("TournamentDate", "Enter a date in YYYY-MM-DD format.");
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

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditTournament] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedTournament = await _tournamentRepository.UpdateTournament(model.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the tournament
                return Redirect(updatedTournament.TournamentRoute);
            }

            model.Metadata.PageTitle = "Edit " + model.Tournament.TournamentFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

            return View("EditTournament", model);
        }
    }
}