using System;
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
    public class CreateFriendlyMatchController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;

        public CreateFriendlyMatchController(ILogger<CreateFriendlyMatchController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            ISeasonDataSource seasonDataSource,
            ICreateMatchSeasonSelector createMatchSeasonSelector,
            IEditMatchHelper editMatchHelper)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditFriendlyMatchViewModel(CurrentPage)
            {
                Match = new Match
                {
                    MatchType = MatchType.FriendlyMatch,
                    MatchLocation = new MatchLocation()
                }
            };

            var path = Request.Path.HasValue ? Request.Path.Value : string.Empty;
            if (path!.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true);
                if (model.Team == null) return NotFound();

                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType);
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);

                model.HomeTeamId = model.Team.TeamId;
                model.HomeTeamName = model.Team.TeamName;
                model.MatchLocationId = model.Team.MatchLocations.FirstOrDefault()?.MatchLocationId;
                model.MatchLocationName = model.Team.MatchLocations.FirstOrDefault()?.NameAndLocalityOrTownIfDifferent();
            }
            else if (path.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path.Value, false);
                if (model.Season == null || !model.Season.MatchTypes.Contains(MatchType.FriendlyMatch))
                {
                    return NotFound();
                }
            }

            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch] = User.Identity?.IsAuthenticated ?? false;

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });

            _editMatchHelper.ConfigureAddMatchModelMetadata(model);

            return CurrentTemplate(model);
        }

    }
}