﻿using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Clubs
{
    public class EditMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IApiKeyProvider _apiKeyProvider;

        public EditMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IApiKeyProvider apiKeyProvider)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Url.AbsolutePath, false).ConfigureAwait(false),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };


            if (model.MatchLocation == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Edit " + model.MatchLocation.NameAndLocalityOrTown();

                return CurrentTemplate(model);
            }
        }


        /// <summary>
        /// Checks whether the currently signed-in member is authorized to edit this match location
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(MatchLocationViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, model?.MatchLocation.MemberGroupName }, null);
        }
    }
}