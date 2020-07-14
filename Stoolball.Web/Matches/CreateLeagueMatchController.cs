using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
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
        private readonly ICreateLeagueMatchEligibleSeasons _createLeagueMatchEligibleSeasons;

        public CreateLeagueMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           ISeasonDataSource seasonDataSource,
           ICreateLeagueMatchEligibleSeasons createLeagueMatchEligibleSeasons)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createLeagueMatchEligibleSeasons = createLeagueMatchEligibleSeasons ?? throw new ArgumentNullException(nameof(createLeagueMatchEligibleSeasons));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new CreateMatchViewModel(contentModel.Content) { Match = new Match() };
            if (Request.Url.AbsolutePath.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false);
                model.PossibleSeasons = _createLeagueMatchEligibleSeasons.SelectEligibleSeasons(model.Team?.Seasons)
                    .Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId.Value.ToString() })
                    .ToList();
            }
            else if (Request.Url.AbsolutePath.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath).ConfigureAwait(false);
            }

            if ((model.Team == null || model.PossibleSeasons == null || !model.PossibleSeasons.Any()) &&
                (model.Season == null || !model.Season.MatchTypes.Contains(MatchType.LeagueMatch)))
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = User.Identity.IsAuthenticated;

                if (model.Team != null)
                {
                    model.Metadata.PageTitle = $"Add a league match for {model.Team.TeamName}";
                }
                else if (model.Season != null)
                {
                    model.Metadata.PageTitle = $"Add a league match in the {model.Season.SeasonFullName()}";
                }

                return CurrentTemplate(model);
            }
        }
    }
}