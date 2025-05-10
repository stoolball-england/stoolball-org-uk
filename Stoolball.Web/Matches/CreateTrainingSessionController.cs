using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class CreateTrainingSessionController : RenderController, IRenderControllerAsync
    {
        private readonly IStoolballEntityRouteParser _routeParser;
        private readonly IMatchDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;

        public CreateTrainingSessionController(ILogger<CreateTrainingSessionController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStoolballEntityRouteParser routeParser,
            IMatchDataSource matchDataSource,
            ITeamDataSource teamDataSource,
            ISeasonDataSource seasonDataSource,
            ICreateMatchSeasonSelector createMatchSeasonSelector,
            IEditMatchHelper editMatchHelper)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _routeParser = routeParser ?? throw new ArgumentNullException(nameof(routeParser));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditTrainingSessionViewModel(CurrentPage)
            {
                Match = new Match
                {
                    MatchType = MatchType.TrainingSession,
                    MatchLocation = new MatchLocation()
                }
            };

            var context = Request.Path.HasValue ? _routeParser.ParseRoute(Request.Path.Value) : null;
            if (context == StoolballEntityType.Team)
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true);
                if (model.Team == null) return NotFound();

                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType);
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);

                model.Match.Teams.Add(new TeamInMatch { Team = model.Team, PlayingAsTeamName = model.Team.TeamName });
                model.MatchLocationId = model.Team.MatchLocations.FirstOrDefault()?.MatchLocationId;
                model.MatchLocationName = model.Team.MatchLocations.FirstOrDefault()?.NameAndLocalityOrTownIfDifferent();
            }
            else if (context == StoolballEntityType.Season)
            {
                model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, false);
                if (model.Season == null || !model.Season.MatchTypes.Contains(MatchType.TrainingSession))
                {
                    return NotFound();
                }
            }

            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch] = User.Identity?.IsAuthenticated ?? false;

            _editMatchHelper.ConfigureAddMatchModelMetadata(model);

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });

            if (Request.Query.TryGetValue("confirm", out var createdMatchRoute) && _routeParser.ParseRoute(createdMatchRoute.First()) == StoolballEntityType.Match)
            {
                model.CreatedMatch = await _matchDataSource.ReadMatchByRoute(createdMatchRoute.First());
            }

            return CurrentTemplate(model);
        }

    }
}