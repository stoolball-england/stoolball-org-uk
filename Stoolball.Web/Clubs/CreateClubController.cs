using Stoolball.Clubs;
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
    public class CreateClubController : RenderMvcControllerAsync
    {
        public CreateClubController(IGlobalSettings globalSettings,
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

            var model = new ClubViewModel(contentModel.Content)
            {
                Club = new Club()
            };

            model.IsAuthorized = IsAuthorized();

            model.Metadata.PageTitle = "Add a club";

            return Task.FromResult(CurrentTemplate(model));
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to add a club
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized()
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors }, null);
        }
    }
}