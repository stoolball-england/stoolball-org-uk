using Stoolball.MatchLocations;
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

namespace Stoolball.Web.MatchLocations
{
    public class CreateMatchLocationController : RenderMvcControllerAsync
    {
        public CreateMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
        }

        [HttpGet]
        public override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content)
            {
                MatchLocation = new MatchLocation()
            };

            model.IsAuthorized = IsAuthorized();

            model.Metadata.PageTitle = "Add a ground or sports hall";

            return Task.FromResult(CurrentTemplate(model));
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to add a match location
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized()
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, Groups.AllMembers }, null);
        }
    }
}