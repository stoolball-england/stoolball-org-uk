using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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
        public async Task<ActionResult> CreateTournament([Bind(Prefix = "Tournament", Include = "TournamentName,QualificationType,PlayerType,PlayersPerTeam,OversPerInningsDefault")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService) { Tournament = postedTournament };
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
                await _tournamentRepository.CreateTournament(model.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the tournament
                return Redirect(model.Tournament.TournamentRoute);
            }

            return View("CreateTournament", model);
        }

    }
}