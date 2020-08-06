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
    public class CreateFriendlyMatchController : BaseCreateMatchController
    {
        public CreateFriendlyMatchController(IGlobalSettings globalSettings,
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

            var model = new CreateFriendlyMatchViewModel(contentModel.Content) { Match = new Match { MatchLocation = new MatchLocation() } };
            if (Request.Url.AbsolutePath.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                await ConfigureModelForContextTeam(model, MatchType.FriendlyMatch, false).ConfigureAwait(false);
                if (model.Team == null) return new HttpNotFoundResult();

                model.HomeTeamName = model.Team.TeamName;
            }
            else if (Request.Url.AbsolutePath.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                var actionResult = await ConfigureModelForContextSeason(model, MatchType.FriendlyMatch).ConfigureAwait(false);
                if (actionResult != null) return actionResult;
            }

            model.IsAuthorized = User.Identity.IsAuthenticated;

            ConfigureModelMetadata(model, MatchType.FriendlyMatch);

            return CurrentTemplate(model);
        }

    }
}