using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Configuration;
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
    public class EditMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IApiKeyProvider _apiKeyProvider;

        public EditMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy,
           IApiKeyProvider apiKeyProvider)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content, Services?.UserService)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl, false).ConfigureAwait(false),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };


            if (model.MatchLocation == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = "Edit " + model.MatchLocation.NameAndLocalityOrTown();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(model.MatchLocation.MatchLocationRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}