﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Web.Security;
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
    public class LinkPlayerToMemberSurfaceController : SurfaceController
    {
        private readonly IPlayerSummaryViewModelFactory _viewModelFactory;
        private readonly IMemberManager _memberManager;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPlayerCacheInvalidator _playerCacheClearer;

        public LinkPlayerToMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IPlayerSummaryViewModelFactory viewModelFactory,
            IMemberManager memberManager,
            IPlayerRepository playerRepository,
            IPlayerCacheInvalidator playerCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _playerCacheClearer = playerCacheClearer ?? throw new ArgumentNullException(nameof(playerCacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public async Task<IActionResult> LinkPlayerToMemberAccount()
        {
            var model = await _viewModelFactory.CreateViewModel(CurrentPage, Request.Path, Request.QueryString.Value);

            if (model.Player == null || model.Player.MemberKey.HasValue)
            {
                return NotFound();
            }

            var currentMember = await _memberManager.GetCurrentMemberAsync();
            if (currentMember == null)
            {
                return Forbid();
            }

            var updatedPlayer = await _playerRepository.LinkPlayerToMemberAccount(model.Player, currentMember.Key, currentMember.Name);

            // Clear the cache for both the old player route and the new, so that the obsolete player route redirects rather than returning a cached result
            _playerCacheClearer.InvalidateCacheForPlayer(model.Player);
            _playerCacheClearer.InvalidateCacheForPlayer(updatedPlayer);

            model.Player = updatedPlayer;

            return Redirect($"{model.Player.PlayerRoute}?{Constants.QueryParameters.ConfirmPlayerLinkedToMember}");
        }
    }
}