using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class RenamePlayerIdentityController : RenderController, IRenderControllerAsync
    {
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public RenamePlayerIdentityController(ILogger<RenamePlayerIdentityController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IPlayerDataSource playerDataSource,
            ITeamBreadcrumbBuilder breadcrumbBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new PlayerIdentityViewModel(CurrentPage)
            {
                PlayerIdentity = await _playerDataSource.ReadPlayerIdentityByRoute(Request.Path)
            };

            if (model.PlayerIdentity?.Team == null)
            {
                return NotFound();
            }
            else
            {

                model.FormData.PlayerSearch = model.PlayerIdentity.PlayerIdentityName;

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.PlayerIdentity.Team);

                model.Metadata.PageTitle = "Rename " + model.PlayerIdentity.PlayerIdentityName;

                _breadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.PlayerIdentity.Team, true);

                return CurrentTemplate(model);
            }
        }
    }
}