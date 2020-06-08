using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
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
    public class CreateTeamController : RenderMvcControllerAsync
    {
        public CreateTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamViewModel(contentModel.Content)
            {
                Team = new Team
                {
                    TeamType = TeamType.Regular,
                    PlayerType = PlayerType.Mixed,
                    FromYear = DateTime.UtcNow.Year,
                    AgeRangeLower = 11
                }
            };

            model.IsAuthorized = IsAuthorized();

            model.Metadata.PageTitle = "Add a team";

            return Task.FromResult(CurrentTemplate(model));
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to add a team
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized()
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, Groups.AllMembers }, null);
        }
    }
}