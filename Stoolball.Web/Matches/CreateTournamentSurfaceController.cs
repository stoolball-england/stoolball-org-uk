using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
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
    public class CreateTournamentSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchValidator _matchValidator;
        private readonly IMatchListingCacheInvalidator _cacheClearer;

        public CreateTournamentSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITournamentRepository tournamentRepository, ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource,
            IMatchValidator matchValidator, IMatchListingCacheInvalidator cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchValidator = matchValidator ?? throw new ArgumentNullException(nameof(matchValidator));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<ActionResult> CreateTournament([Bind("TournamentName", "QualificationType", "PlayerType", "PlayersPerTeam", "DefaultOverSets", Prefix = "Tournament")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            postedTournament.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);
            var model = new EditTournamentViewModel(CurrentPage, Services.UserService) { Tournament = postedTournament };
            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            model.Tournament.TournamentNotes = Request.Form["Tournament.TournamentNotes"];

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
                // the field label as it could confuse the majority, so only reveal it here.
                ModelState.AddModelError("TournamentDate", "Enter a date in YYYY-MM-DD format.");
            }

            _matchValidator.DateIsValidForSqlServer(model.TournamentDate, ModelState, "TournamentDate", "tournament");

            var path = Request.Path.HasValue ? Request.Path.Value : string.Empty;
            if (path!.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true).ConfigureAwait(false);
                model.Tournament.Teams.Add(new TeamInTournament { Team = model.Team, TeamRole = TournamentTeamRole.Organiser });
                model.Metadata.PageTitle = $"Add a tournament for {model.Team.TeamName}";
            }
            else if (path.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, false).ConfigureAwait(false);
                model.Tournament.Seasons.Add(model.Season);
                model.Metadata.PageTitle = $"Add a tournament in the {model.Season.SeasonFullName()}";

                _matchValidator.DateIsWithinTheSeason(model.TournamentDate, model.Season, ModelState, "TournamentDate", "tournament");
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

            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTournament] = User.Identity?.IsAuthenticated ?? false;

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTournament] && ModelState.IsValid &&
                (model.Team != null ||
                (model.Season != null && model.Season.EnableTournaments)))
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var createdTournament = await _tournamentRepository.CreateTournament(model.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.InvalidateCacheForTournament(createdTournament).ConfigureAwait(false);

                return Redirect(createdTournament.TournamentRoute);
            }

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });

            return View("CreateTournament", model);
        }

    }
}