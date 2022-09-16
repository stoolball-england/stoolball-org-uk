using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.Statistics;
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

namespace Stoolball.Web.Statistics
{
    public class LinkedPlayersForMemberSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IPlayerRepository _playerRepository;
        private readonly ICacheClearer<Player> _playerCacheClearer;

        public LinkedPlayersForMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IPlayerDataSource playerDataSource,
            IPlayerRepository playerRepository,
            ICacheClearer<Player> playerCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
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
            var currentMember = await _memberManager.GetCurrentMemberAsync();
            if (currentMember == null)
            {
                return Forbid();
            }

            var player = await _playerDataSource.ReadPlayerByMemberKey(currentMember.Key);
            if (player != null)
            {
                player = await _playerDataSource.ReadPlayerByRoute(player.PlayerRoute);

                var identitiesToKeep = formData.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList();
                var identitiesToUnlink = player.PlayerIdentities.Where(x => !identitiesToKeep.Contains(x.PlayerIdentityId!.Value));

                foreach (var identity in identitiesToUnlink)
                {
                    await _playerRepository.UnlinkPlayerIdentityFromMemberAccount(identity, currentMember.Key, currentMember.Name);
                }

                if (identitiesToUnlink.Any())
                {
                    await _playerCacheClearer.ClearCacheFor(player);
                }
            }

            return Redirect(formData.PreferredNextRoute != null && formData.PreferredNextRoute.StartsWith("/players/") ? formData.PreferredNextRoute : Constants.Pages.AccountUrl);
        }
    }
}