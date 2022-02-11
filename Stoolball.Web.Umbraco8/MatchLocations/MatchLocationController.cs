using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Email;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
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
    public class MatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IEmailProtector _emailProtector;

        public MatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy,
           IApiKeyProvider apiKeyProvider,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content, Services?.UserService)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            if (model.MatchLocation == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = model.MatchLocation.NameAndLocalityOrTown();
                model.Metadata.Description = model.MatchLocation.Description();

                model.MatchLocation.MatchLocationNotes = _emailProtector.ProtectEmailAddresses(model.MatchLocation.MatchLocationNotes, User.Identity.IsAuthenticated);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}