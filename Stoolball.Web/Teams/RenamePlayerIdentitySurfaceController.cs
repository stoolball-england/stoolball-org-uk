using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Teams
{
    public class RenamePlayerIdentitySurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerRepository _playerRepository;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public RenamePlayerIdentitySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IPlayerDataSource playerDataSource, IPlayerRepository playerRepository, IAuthorizationPolicy<Team> authorizationPolicy, ITeamBreadcrumbBuilder breadcrumbBuilder)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> RenamePlayerIdentity([Bind(Prefix = "FormData")] PlayerIdentityFormData formData)
        {
            if (formData is null)
            {
                throw new ArgumentNullException(nameof(formData));
            }

            var model = new PlayerIdentityViewModel(CurrentPage, Services.UserService)
            {
                FormData = formData,
                PlayerIdentity = await _playerDataSource.ReadPlayerIdentityByRoute(Request.Path)
            };

            if (model.PlayerIdentity?.Team == null)
            {
                return NotFound();
            }

            var redirectToUrl = model.PlayerIdentity.Team.TeamRoute + "/edit/players";
            if (model.PlayerIdentity.PlayerIdentityName == formData.PlayerSearch)
            {
                return Redirect(redirectToUrl); // unchanged
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.PlayerIdentity.Team);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();

                var playerIdentity = new PlayerIdentity
                {
                    PlayerIdentityId = model.PlayerIdentity.PlayerIdentityId,
                    PlayerIdentityName = formData.PlayerSearch,
                    Team = model.PlayerIdentity.Team
                };

                var result = await _playerRepository.UpdatePlayerIdentity(playerIdentity, currentMember.Key, currentMember.UserName).ConfigureAwait(false);

                if (result.Status == PlayerIdentityUpdateResult.NotUnique)
                {
                    ModelState.AddModelError(string.Join(".", nameof(model.FormData), nameof(formData.PlayerSearch)), $"There is already a player called '{formData.PlayerSearch}'");
                }
                else
                {
                    return Redirect(redirectToUrl);
                }
            }

            model.Metadata.PageTitle = "Rename " + model.PlayerIdentity.PlayerIdentityName;

            _breadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.PlayerIdentity.Team, true);

            return View("RenamePlayerIdentity", model);
        }
    }
}