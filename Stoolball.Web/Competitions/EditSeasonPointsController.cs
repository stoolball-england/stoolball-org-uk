using Stoolball.Umbraco.Data.Competitions;
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

namespace Stoolball.Web.Competitions
{
    public class EditSeasonPointsController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;

        public EditSeasonPointsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new System.ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new SeasonViewModel(contentModel.Content)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false),
            };


            if (model.Season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Season.PointsRules.AddRange(await _seasonDataSource.ReadPointsRules(model.Season.SeasonId.Value).ConfigureAwait(false));

                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Edit points for " + model.Season.SeasonFullNameAndPlayerType();

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to edit this competition
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(SeasonViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, model?.Season.Competition.MemberGroupName }, null);
        }
    }
}