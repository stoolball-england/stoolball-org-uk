﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;

        public MatchLocationsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            var model = new MatchLocationsViewModel(contentModel.Content, Services?.UserService)
            {
                MatchLocationFilter = new MatchLocationFilter
                {
                    Query = Request.QueryString["q"]?.Trim(),
                    TeamTypes = new List<TeamType> { TeamType.LimitedMembership, TeamType.Occasional, TeamType.Regular, TeamType.Representative, TeamType.SchoolAgeGroup, TeamType.SchoolAgeGroup, TeamType.SchoolClub, TeamType.SchoolOther }
                }
            };

            model.MatchLocationFilter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
            model.MatchLocationFilter.Paging.PageSize = Constants.Defaults.PageSize;
            model.MatchLocationFilter.Paging.PageUrl = Request.Url;
            model.MatchLocationFilter.Paging.Total = await _matchLocationDataSource.ReadTotalMatchLocations(model.MatchLocationFilter).ConfigureAwait(false);
            model.MatchLocations = await _matchLocationDataSource.ReadMatchLocations(model.MatchLocationFilter).ConfigureAwait(false);

            model.Metadata.PageTitle = Constants.Pages.MatchLocations;
            if (!string.IsNullOrEmpty(model.MatchLocationFilter.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.MatchLocationFilter.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}