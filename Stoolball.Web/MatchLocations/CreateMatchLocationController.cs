using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.MatchLocations;
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
    public class CreateMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;

        public CreateMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IApiKeyProvider apiKeyProvider,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content, Services?.UserService)
            {
                MatchLocation = new MatchLocation(),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.MatchLocation, Members);

            model.Metadata.PageTitle = "Add a ground or sports centre";

            return Task.FromResult(CurrentTemplate(model));
        }
    }
}