using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class CreateTournamentController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;

        public CreateTournamentController(ILogger<CreateTournamentController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            ISeasonDataSource seasonDataSource)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditTournamentViewModel(CurrentPage)
            {
                Tournament = new Tournament
                {
                    QualificationType = TournamentQualificationType.OpenTournament,
                    PlayerType = PlayerType.Mixed,
                    PlayersPerTeam = 8,
                    DefaultOverSets = new List<OverSet> { new OverSet { Overs = 4, BallsPerOver = 8 } },
                    TournamentLocation = new MatchLocation()
                }
            };

            var path = Request.Path.HasValue ? Request.Path.Value : string.Empty;
            if (path!.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true);
                if (model.Team == null)
                {
                    return NotFound();
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
            else if (path.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, false);
                if (model.Season == null || !model.Season.EnableTournaments)
                {
                    return NotFound();
                }

                model.Tournament.TournamentName = model.Season.Competition.CompetitionName;
                if (model.Tournament.TournamentName.IndexOf("tournament", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    model.Tournament.TournamentName += " tournament";
                }

                model.Tournament.PlayerType = model.Season.Competition.PlayerType;

                model.Metadata.PageTitle = $"Add a tournament in the {model.Season.SeasonFullName()}";
            }

            model.IsAuthorized[AuthorizedAction.CreateTournament] = User.Identity?.IsAuthenticated ?? false;

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });

            return CurrentTemplate(model);
        }

    }
}