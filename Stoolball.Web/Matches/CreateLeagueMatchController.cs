using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
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
    public class CreateLeagueMatchController : RenderController, IRenderControllerAsync
    {
        private readonly IStoolballEntityRouteParser _routeParser;
        private readonly IMatchDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;
        private readonly IAuthorizationPolicy<Competition> _competitionAuthorizationPolicy;

        public CreateLeagueMatchController(ILogger<CreateLeagueMatchController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStoolballEntityRouteParser routeParser,
            IMatchDataSource matchDataSource,
            ITeamDataSource teamDataSource,
            ISeasonDataSource seasonDataSource,
            ICreateMatchSeasonSelector createMatchSeasonSelector,
            IEditMatchHelper editMatchHelper,
            IAuthorizationPolicy<Competition> competitionAuthorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _routeParser = routeParser ?? throw new ArgumentNullException(nameof(routeParser));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
            _competitionAuthorizationPolicy = competitionAuthorizationPolicy ?? throw new ArgumentNullException(nameof(competitionAuthorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditLeagueMatchViewModel(CurrentPage)
            {
                Match = new Match
                {
                    MatchType = MatchType.LeagueMatch,
                    MatchLocation = new MatchLocation()
                }
            };

            var context = Request.Path.HasValue ? _routeParser.ParseRoute(Request.Path.Value) : null;
            if (context == StoolballEntityType.Team)
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true);
                if (model.Team == null)
                {
                    return NotFound();
                }

                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType).ToList();

                if (possibleSeasons.Count == 0)
                {
                    return NotFound();
                }

                if (possibleSeasons.Count == 1)
                {
                    model.Match.Season = possibleSeasons.First();
                }

                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);

                await _editMatchHelper.ConfigureModelPossibleTeams(model, possibleSeasons);

                model.HomeTeamId = model.Team.TeamId;
                model.MatchLocationId = model.Team.MatchLocations.FirstOrDefault()?.MatchLocationId;
                model.MatchLocationName = model.Team.MatchLocations.FirstOrDefault()?.NameAndLocalityOrTownIfDifferent();

                if (model.PossibleAwayTeams.Count > 1)
                {
                    model.AwayTeamId = new Guid(model.PossibleAwayTeams[1].Value);
                }
            }
            else if (context == StoolballEntityType.Season)
            {
                model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true);
                if (model.Season == null || !model.Season.MatchTypes.Contains(MatchType.LeagueMatch))
                {
                    return NotFound();
                }
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(new[] { model.Match.Season });

                model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season.Teams);
                if (model.PossibleHomeTeams.Count > 0)
                {
                    model.HomeTeamId = new Guid(model.PossibleHomeTeams[0].Value);
                }

                model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season.Teams);
                if (model.PossibleAwayTeams.Count > 1)
                {
                    model.AwayTeamId = new Guid(model.PossibleAwayTeams[1].Value);
                }
            }

            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch] = User.Identity?.IsAuthenticated ?? false;
            if (model.Season != null && model.Season.Teams.Count <= 1 && model.Season.Competition != null)
            {
                (await _competitionAuthorizationPolicy.IsAuthorized(model.Season.Competition)).TryGetValue(AuthorizedAction.EditCompetition, out var canEditCompetition);
                model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditCompetition] = canEditCompetition;
            }

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