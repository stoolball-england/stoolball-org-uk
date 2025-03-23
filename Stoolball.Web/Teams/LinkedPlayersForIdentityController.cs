using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class LinkedPlayersForIdentityController : RenderController, IRenderControllerAsync
    {
        private readonly IMemberManager _memberManager;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public LinkedPlayersForIdentityController(ILogger<LinkedPlayersForIdentityController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMemberManager memberManager,
            IAuthorizationPolicy<Team> authorizationPolicy,
            ITeamDataSource teamDataSource,
            IPlayerDataSource playerDataSource,
            ITeamBreadcrumbBuilder breadcrumbBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new LinkedPlayersViewModel(CurrentPage)
            {
                ContextIdentity = await _playerDataSource.ReadPlayerIdentityByRoute(Request.Path),
                CurrentMemberKey = (await _memberManager.GetCurrentMemberAsync())?.Key
            };

            if (model.ContextIdentity?.Team?.TeamRoute == null || model.ContextIdentity?.Player?.PlayerRoute == null)
            {
                return NotFound();
            }
            else
            {
                model.Player = await _playerDataSource.ReadPlayerByRoute(model.ContextIdentity.Player.PlayerRoute);
                model.ContextIdentity.Team = await _teamDataSource.ReadTeamByRoute(model.ContextIdentity.Team.TeamRoute);
                if (model.ContextIdentity.Team is null) { return NotFound(); }

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.ContextIdentity.Team);
                var isTeamOwner = await _memberManager.IsMemberAuthorizedAsync(null, [model.ContextIdentity.Team.MemberGroupName!], null);
                model.CurrentMemberRole = isTeamOwner ? PlayerIdentityLinkedBy.Team : PlayerIdentityLinkedBy.StoolballEngland;

                model.Metadata.PageTitle = $"Link {model.ContextIdentity.PlayerIdentityName} to the same player listed under another name";

                _breadcrumbBuilder.BuildBreadcrumbsForEditPlayers(model.Breadcrumbs, model.ContextIdentity.Team);

                return CurrentTemplate(model);
            }
        }
    }
}