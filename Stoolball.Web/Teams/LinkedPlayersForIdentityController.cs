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
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class LinkedPlayersForIdentityController : RenderController, IRenderControllerAsync
    {
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public LinkedPlayersForIdentityController(ILogger<LinkedPlayersForIdentityController> logger,
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
            var model = new LinkedPlayersViewModel(CurrentPage)
            {
                ContextIdentity = await _playerDataSource.ReadPlayerIdentityByRoute(Request.Path)
            };

            if (model.ContextIdentity?.Team == null || model.ContextIdentity?.Player == null)
            {
                return NotFound();
            }
            else
            {
                model.Player = await _playerDataSource.ReadPlayerByRoute(model.ContextIdentity.Player!.PlayerRoute!);

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.ContextIdentity.Team);

                model.Metadata.PageTitle = "Players linked to the statistics for " + model.ContextIdentity.PlayerIdentityName;

                _breadcrumbBuilder.BuildBreadcrumbsForEditPlayers(model.Breadcrumbs, model.ContextIdentity.Team);

                return CurrentTemplate(model);
            }
        }
    }
}