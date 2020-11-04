using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class TeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public TeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IAuthorizationPolicy<Team> authorizationPolicy,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamViewModel(contentModel.Content, Services?.UserService)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false)
            };

            if (model.Team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Team);

                model.Metadata.PageTitle = model.Team.TeamName + " stoolball team";
                model.Metadata.Description = model.Team.Description();

                model.Team.Cost = _emailProtector.ProtectEmailAddresses(model.Team.Cost, User.Identity.IsAuthenticated);
                model.Team.Introduction = _emailProtector.ProtectEmailAddresses(model.Team.Introduction, User.Identity.IsAuthenticated);
                model.Team.PlayingTimes = _emailProtector.ProtectEmailAddresses(model.Team.PlayingTimes, User.Identity.IsAuthenticated);
                model.Team.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Team.PublicContactDetails, User.Identity.IsAuthenticated);

                return CurrentTemplate(model);
            }
        }
    }
}