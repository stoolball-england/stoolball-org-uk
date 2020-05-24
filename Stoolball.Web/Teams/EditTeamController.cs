using Stoolball.Umbraco.Data.Teams;
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

namespace Stoolball.Web.Teams
{
    public class EditTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;

        public EditTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamViewModel(contentModel.Content)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false)
            };


            if (model.Team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Edit " + model.Team.TeamName;

                return CurrentTemplate(model);
            }
        }


        /// <summary>
        /// Checks whether the currently signed-in member is authorized to edit this team
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(TeamViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, model?.Team.MemberGroupName }, null);
        }
    }
}