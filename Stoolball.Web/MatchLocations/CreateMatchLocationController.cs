using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class CreateMatchLocationController : RenderController, IRenderControllerAsync
    {
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;

        public CreateMatchLocationController(ILogger<CreateMatchLocationController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IApiKeyProvider apiKeyProvider,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchLocationViewModel(CurrentPage)
            {
                MatchLocation = new MatchLocation(),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.MatchLocation);

            model.Metadata.PageTitle = "Add a ground or sports centre";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

            return CurrentTemplate(model);
        }
    }
}