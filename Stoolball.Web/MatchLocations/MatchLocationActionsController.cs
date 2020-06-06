using Stoolball.Umbraco.Data.MatchLocations;
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
    public class MatchLocationActionsController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;

        public MatchLocationActionsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationViewModel(contentModel.Content)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Url.AbsolutePath).ConfigureAwait(false)
            };

            if (model.MatchLocation == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = IsAuthorized(model);
                model.IsAdministrator = IsAdministrator();

                model.Metadata.PageTitle = "Edit " + model.MatchLocation.NameAndLocalityOrTown();

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to edit this match location
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(MatchLocationViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, model?.MatchLocation.MemberGroupName }, null);
        }
    }
}