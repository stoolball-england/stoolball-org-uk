using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class EditMatchLocationController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IApiKeyProvider _apiKeyProvider;

        public EditMatchLocationController(ILogger<EditMatchLocationController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchLocationDataSource matchLocationDataSource,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy,
            IApiKeyProvider apiKeyProvider)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchLocationViewModel(CurrentPage)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path, false),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };


            if (model.MatchLocation == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = "Edit " + model.MatchLocation.NameAndLocalityOrTown();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(model.MatchLocation.MatchLocationRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}