using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Teams;
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
    public class CreateLeagueMatchController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;
        private readonly IAuthorizationPolicy<Competition> _competitionAuthorizationPolicy;

        public CreateLeagueMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           ISeasonDataSource seasonDataSource,
           ICreateMatchSeasonSelector createMatchSeasonSelector,
           IEditMatchHelper editMatchHelper,
           IAuthorizationPolicy<Competition> competitionAuthorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
            _competitionAuthorizationPolicy = competitionAuthorizationPolicy ?? throw new ArgumentNullException(nameof(competitionAuthorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new EditLeagueMatchViewModel(contentModel.Content, Services?.UserService)
            {
                Match = new Match
                {
                    MatchType = MatchType.LeagueMatch,
                    MatchLocation = new MatchLocation()
                }
            };
            if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                if (model.Team == null)
                {
                    return new HttpNotFoundResult();
                }

                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType).ToList();

                if (possibleSeasons.Count == 0)
                {
                    return new HttpNotFoundResult();
                }

                if (possibleSeasons.Count == 1)
                {
                    model.Match.Season = possibleSeasons.First();
                }

                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);

                await _editMatchHelper.ConfigureModelPossibleTeams(model, possibleSeasons).ConfigureAwait(false);

                model.HomeTeamId = model.Team.TeamId;
                model.MatchLocationId = model.Team.MatchLocations.FirstOrDefault()?.MatchLocationId;
                model.MatchLocationName = model.Team.MatchLocations.FirstOrDefault()?.NameAndLocalityOrTownIfDifferent();

                if (model.PossibleAwayTeams.Count > 1)
                {
                    model.AwayTeamId = new Guid(model.PossibleAwayTeams[1].Value);
                }
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false);
                if (model.Season == null || !model.Season.MatchTypes.Contains(MatchType.LeagueMatch))
                {
                    return new HttpNotFoundResult();
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

            model.IsAuthorized[Stoolball.Security.AuthorizedAction.CreateMatch] = User.Identity.IsAuthenticated;
            if (model.Season != null && model.Season.Teams.Count <= 1 && model.Season.Competition != null)
            {
                _competitionAuthorizationPolicy.IsAuthorized(model.Season.Competition).TryGetValue(Stoolball.Security.AuthorizedAction.EditCompetition, out var canEditCompetition);
                model.IsAuthorized[Stoolball.Security.AuthorizedAction.EditCompetition] = canEditCompetition;
            }

            _editMatchHelper.ConfigureAddMatchModelMetadata(model);

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
            return CurrentTemplate(model);
        }

    }
}