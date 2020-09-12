using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class CreateTournamentController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;

        public CreateTournamentController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           ISeasonDataSource seasonDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new EditTournamentViewModel(contentModel.Content, Services?.UserService)
            {
                Tournament = new Tournament
                {
                    QualificationType = TournamentQualificationType.OpenTournament,
                    PlayerType = PlayerType.Mixed,
                    PlayersPerTeam = 8,
                    OversPerInningsDefault = 4,
                    TournamentLocation = new MatchLocation()
                }
            };
            if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                if (model.Team == null)
                {
                    return new HttpNotFoundResult();
                }

                model.Tournament.TournamentName = model.Team.TeamName;
                if (model.Tournament.TournamentName.IndexOf("tournament", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    model.Tournament.TournamentName += " tournament";
                }

                model.TournamentLocationId = model.Team.MatchLocations.FirstOrDefault()?.MatchLocationId;
                model.TournamentLocationName = model.Team.MatchLocations.FirstOrDefault()?.NameAndLocalityOrTownIfDifferent();

                model.Tournament.PlayerType = model.Team.PlayerType;

                model.Metadata.PageTitle = $"Add a tournament for {model.Team.TeamName}";
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, false).ConfigureAwait(false);
                if (model.Season == null || !model.Season.EnableTournaments)
                {
                    return new HttpNotFoundResult();
                }

                model.Tournament.TournamentName = model.Season.Competition.CompetitionName;
                if (model.Tournament.TournamentName.IndexOf("tournament", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    model.Tournament.TournamentName += " tournament";
                }

                model.Tournament.PlayerType = model.Season.Competition.PlayerType;

                model.Metadata.PageTitle = $"Add a tournament in the {model.Season.SeasonFullName()}";
            }

            model.IsAuthorized[AuthorizedAction.CreateTournament] = User.Identity.IsAuthenticated;

            return CurrentTemplate(model);
        }

    }
}