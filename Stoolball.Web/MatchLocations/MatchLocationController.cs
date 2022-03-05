using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Email;
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
    public class MatchLocationController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IEmailProtector _emailProtector;

        public MatchLocationController(ILogger<MatchLocationController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchLocationDataSource matchLocationDataSource,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy,
            IApiKeyProvider apiKeyProvider,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchLocationViewModel(CurrentPage)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path, true).ConfigureAwait(false),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            if (model.MatchLocation == null)
            {
                return NotFound();
            }
            else
            {
                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = model.MatchLocation.NameAndLocalityOrTown();
                model.Metadata.Description = model.MatchLocation.Description();

                model.MatchLocation.MatchLocationNotes = _emailProtector.ProtectEmailAddresses(model.MatchLocation.MatchLocationNotes, User.Identity?.IsAuthenticated ?? false);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}