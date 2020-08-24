using Stoolball.MatchLocations;
using Stoolball.Web.Configuration;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class CreateMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IApiKeyProvider _apiKeyProvider;

        public CreateMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IApiKeyProvider apiKeyProvider)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content)
            {
                MatchLocation = new MatchLocation(),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            model.IsAuthorized = IsAuthorized();

            model.Metadata.PageTitle = "Add a ground or sports centre";

            return Task.FromResult(CurrentTemplate(model));
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to add a match location
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized()
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
        }
    }
}