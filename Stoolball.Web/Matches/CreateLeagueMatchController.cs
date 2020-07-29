using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Security;
using System;
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
    public class CreateLeagueMatchController : BaseCreateMatchController
    {
        public CreateLeagueMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           ISeasonDataSource seasonDataSource,
           ICreateMatchSeasonSelector createMatchSeasonSelector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper, teamDataSource, seasonDataSource, createMatchSeasonSelector)
        {
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new CreateLeagueMatchViewModel(contentModel.Content) { Match = new Match { MatchLocation = new MatchLocation() } };
            if (Request.Url.AbsolutePath.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                await ConfigureModelForContextTeam(model, MatchType.LeagueMatch, true).ConfigureAwait(false);

                if (model.Team == null || model.PossibleSeasons.Count == 0)
                {
                    return new HttpNotFoundResult();
                }

                if (model.PossibleSeasons.Count == 1)
                {
                    model.Match.Season = new Season { SeasonId = new Guid(model.PossibleSeasons[0].Value) };
                }
            }
            else if (Request.Url.AbsolutePath.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var actionResult = await ConfigureModelForContextSeason(model, MatchType.LeagueMatch).ConfigureAwait(false);
                if (actionResult != null) return actionResult;

                if (model.PossibleTeams.Count > 0)
                {
                    model.HomeTeamId = new Guid(model.PossibleTeams[0].Value);
                }
                if (model.PossibleTeams.Count > 1)
                {
                    model.AwayTeamId = new Guid(model.PossibleTeams[1].Value);
                }
            }

            model.IsAuthorized = User.Identity.IsAuthenticated;

            ConfigureModelMetadata(model, MatchType.LeagueMatch);

            return CurrentTemplate(model);
        }

    }
}