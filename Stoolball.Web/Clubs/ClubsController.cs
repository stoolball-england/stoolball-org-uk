﻿using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Clubs
{
    public class ClubsController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;

        public ClubsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new System.ArgumentNullException(nameof(clubDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new ClubsViewModel(contentModel.Content)
            {
                ClubQuery = new ClubQuery
                {
                    Query = Request.QueryString["q"]?.Trim()
                }
            };

            if (!string.IsNullOrEmpty(model.ClubQuery.Query))
            {
                model.Clubs = await _clubDataSource.ReadClubListings(model.ClubQuery).ConfigureAwait(false);
            }

            model.Metadata.PageTitle = "Stoolball clubs";
            if (!string.IsNullOrEmpty(model.ClubQuery.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.ClubQuery.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}