using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Clubs
{
    public class EditClubController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;

        public EditClubController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new System.ArgumentNullException(nameof(clubDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new ClubViewModel(contentModel.Content)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.Url.AbsolutePath).ConfigureAwait(false)
            };


            model.IsAuthorized = Members.IsMemberAuthorized(null, new[] { "Administrators" }, null);

            if (model.Club == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Metadata.PageTitle = "Edit " + model.Club.ClubName;

                return CurrentTemplate(model);
            }
        }
    }
}