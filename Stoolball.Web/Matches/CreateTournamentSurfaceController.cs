using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class CreateTournamentSurfaceController : SurfaceController
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;

        public CreateTournamentSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITournamentRepository tournamentRepository,
            ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<ActionResult> CreateTournament([Bind(Prefix = "Tournament", Include = "TournamentName,QualificationType,PlayerType,PlayersPerTeam,DefaultOverSets")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            postedTournament.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);
            var model = new EditTournamentViewModel(CurrentPage, Services.UserService) { Tournament = postedTournament };
            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            model.Tournament.TournamentNotes = Request.Unvalidated.Form["Tournament.TournamentNotes"];

            if (!string.IsNullOrEmpty(Request.Form["TournamentDate"]) && DateTimeOffset.TryParse(Request.Form["TournamentDate"], out var parsedDate))
            {
                model.TournamentDate = parsedDate;
                model.Tournament.StartTime = model.TournamentDate.Value;

                if (model.TournamentDate < SqlDateTime.MinValue.Value.Date || model.TournamentDate > SqlDateTime.MaxValue.Value.Date)
                {
                    ModelState.AddModelError("TournamentDate", $"The tournament date must be between {SqlDateTime.MinValue.Value.Date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)} and {SqlDateTime.MaxValue.Value.Date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}.");
                }

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
                // the field label as it could confuse the majority, so only reveal it here.
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

            if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                model.Tournament.Teams.Add(new TeamInTournament { Team = model.Team, TeamRole = TournamentTeamRole.Organiser });
                model.Metadata.PageTitle = $"Add a tournament for {model.Team.TeamName}";
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, false).ConfigureAwait(false);
                model.Tournament.Seasons.Add(model.Season);
                model.Metadata.PageTitle = $"Add a tournament in the {model.Season.SeasonFullName()}";
            }

            model.IsAuthorized[AuthorizedAction.CreateTournament] = User.Identity.IsAuthenticated;

            if (model.IsAuthorized[AuthorizedAction.CreateTournament] && ModelState.IsValid &&
                (model.Team != null ||
                (model.Season != null && model.Season.EnableTournaments)))
            {
                var currentMember = Members.GetCurrentMember();
                var createdTournament = await _tournamentRepository.CreateTournament(model.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the tournament
                return Redirect(createdTournament.TournamentRoute);
            }

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });

            return View("CreateTournament", model);
        }

    }
}