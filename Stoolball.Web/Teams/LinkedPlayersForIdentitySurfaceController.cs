using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
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
    public class LinkedPlayersForIdentitySurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPlayerCacheInvalidator _playerCacheClearer;

        public LinkedPlayersForIdentitySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IPlayerDataSource playerDataSource,
            IPlayerRepository playerRepository,
            IPlayerCacheInvalidator playerCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _playerCacheClearer = playerCacheClearer ?? throw new ArgumentNullException(nameof(playerCacheClearer));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public async Task<IActionResult> UpdateLinkedPlayers(LinkedPlayersFormData formData)
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
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.ContextIdentity.Team);
                if (!model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam])
                {
                    return Forbid();
                }

                model.Player = await _playerDataSource.ReadPlayerByRoute(model.ContextIdentity.Player!.PlayerRoute!);

                var previousIdentities = model.Player!.PlayerIdentities.Select(id => id.PlayerIdentityId!.Value).ToList();
                var submittedIdentities = formData.PlayerIdentities.Select(id => id.PlayerIdentityId!.Value).ToList();
                var identitiesToLink = submittedIdentities.Where(id => !previousIdentities.Contains(id));
                var identitiesToKeep = submittedIdentities.Where(id => previousIdentities.Contains(id));
                var identitiesToUnlink = model.Player!.PlayerIdentities.Where(id => id.LinkedBy == PlayerIdentityLinkedBy.ClubOrTeam && !identitiesToKeep.Contains(id.PlayerIdentityId!.Value));

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                foreach (var identity in identitiesToLink)
                {
                    await _playerRepository.LinkPlayerIdentity(model.Player, formData.PlayerIdentities.Single(id => id.PlayerIdentityId == identity), PlayerIdentityLinkedBy.ClubOrTeam, currentMember.Key, currentMember.Name);
                }

                foreach (var identity in identitiesToUnlink)
                {
                    await _playerRepository.UnlinkPlayerIdentity(identity, currentMember.Key, currentMember.Name);
                }

                if (identitiesToUnlink.Any())
                {
                    _playerCacheClearer.InvalidateCacheForPlayer(model.Player);
                }

                var redirectToUrl = model.ContextIdentity.Team.TeamRoute + "/edit/players";
                return Redirect(redirectToUrl);
            }
        }
    }
}