﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Data.Abstractions.Models;
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
        private readonly ITeamDataSource _teamDataSource;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPlayerCacheInvalidator _playerCacheClearer;

        public LinkedPlayersForIdentitySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IAuthorizationPolicy<Team> authorizationPolicy,
            ITeamDataSource teamDataSource,
            IPlayerDataSource playerDataSource,
            IPlayerRepository playerRepository,
            IPlayerCacheInvalidator playerCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
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

            if (model.ContextIdentity?.Team == null || model.ContextIdentity.PlayerIdentityId == null || model.ContextIdentity?.Player == null)
            {
                return NotFound();
            }
            else
            {
                model.ContextIdentity.Team = await _teamDataSource.ReadTeamByRoute(model.ContextIdentity.Team.TeamRoute!).ConfigureAwait(false);
                if (model.ContextIdentity?.Team == null) { return NotFound(); }

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.ContextIdentity.Team);
                if (!model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam])
                {
                    return Forbid();
                }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                model.CurrentMemberKey = currentMember?.Key;
                var isTeamOwner = await _memberManager.IsMemberAuthorizedAsync(null, [model.ContextIdentity.Team.MemberGroupName!], null);
                var roleGrantingPermission = isTeamOwner ? PlayerIdentityLinkedBy.Team : PlayerIdentityLinkedBy.StoolballEngland;

                model.Player = await _playerDataSource.ReadPlayerByRoute(model.ContextIdentity.Player!.PlayerRoute!);

                if (model.Player!.MemberKey.HasValue && model.Player.MemberKey != currentMember!.Key)
                {
                    return Forbid();
                }

                var previousIdentities = model.Player!.PlayerIdentities.Select(id => id.PlayerIdentityId!.Value).ToList();
                var submittedIdentities = formData.PlayerIdentities.Select(id => id.PlayerIdentityId!.Value).ToList();
                var identitiesToLink = submittedIdentities.Where(id => !previousIdentities.Contains(id));
                var identitiesToKeep = submittedIdentities.Where(id => previousIdentities.Contains(id)).Union([model.ContextIdentity.PlayerIdentityId.Value]);
                var identitiesToUnlink = model.Player!.PlayerIdentities.Where(id => id.LinkedBy == PlayerIdentityLinkedBy.Team && !identitiesToKeep.Contains(id.PlayerIdentityId!.Value));

                var movedPlayerResults = new List<LinkPlayersResult>();

                var playersToLink = formData.PlayerIdentities.Where(pi => identitiesToLink.Contains(pi.PlayerIdentityId!.Value)).Select(pi => pi.Player!.PlayerId!.Value).Distinct();
                foreach (var playerId in playersToLink)
                {
                    movedPlayerResults.Add(await _playerRepository.LinkPlayers(model.Player.PlayerId!.Value, playerId, roleGrantingPermission, currentMember!.Key, currentMember.Name!).ConfigureAwait(false));
                }

                foreach (var identity in identitiesToUnlink)
                {
                    await _playerRepository.UnlinkPlayerIdentity(identity.PlayerIdentityId!.Value, currentMember!.Key, currentMember.Name!);
                }

                if (identitiesToLink.Any() || identitiesToUnlink.Any())
                {
                    // The target player is always the same, and needs its cache cleared for link or unlink.
                    _playerCacheClearer.InvalidateCacheForPlayer(model.Player);

                    // The source identity needs its cache cleared for a link.
                    var sourcePlayersCleared = new List<Guid>();
                    foreach (var movedPlayer in movedPlayerResults)
                    {
                        if (sourcePlayersCleared.Contains(movedPlayer.PlayerIdForSourcePlayer)) { continue; }
                        sourcePlayersCleared.Add(movedPlayer.PlayerIdForSourcePlayer);

                        var sourcePlayer = new Player
                        {
                            PlayerId = movedPlayer.PlayerIdForSourcePlayer,
                            MemberKey = movedPlayer.PreviousMemberKeyForSourcePlayer,
                            PlayerRoute = movedPlayer.PreviousRouteForSourcePlayer
                        };
                        _playerCacheClearer.InvalidateCacheForPlayer(sourcePlayer);
                    }

                    // Clear cache for listings of all players in this target player's team (the source player is on the same team).
                    var teams = model.Player.PlayerIdentities.Select(pi => pi.Team).OfType<Team>().ToArray();
                    _playerCacheClearer.InvalidateCacheForTeams(teams);
                }

                var redirectToUrl = model.ContextIdentity.Team.TeamRoute + "/edit/players";
                return Redirect(redirectToUrl);
            }
        }
    }
}