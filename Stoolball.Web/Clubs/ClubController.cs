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
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Clubs
{
    public class ClubController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;

        public ClubController(IGlobalSettings globalSettings,
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

            if (model.Club == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, model.Club.MemberGroupName }, null);

                model.Metadata.PageTitle = model.Club.ClubName;
                model.Metadata.Description = model.Club.Description();

                return CurrentTemplate(model);
            }
        }
    }
}