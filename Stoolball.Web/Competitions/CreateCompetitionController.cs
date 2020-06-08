using Stoolball.Competitions;
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
    public class CreateCompetitionController : RenderMvcControllerAsync
    {
        public CreateCompetitionController(IGlobalSettings globalSettings,
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

            var model = new CompetitionViewModel(contentModel.Content)
            {
                Competition = new Competition
                {
                    PlayersPerTeam = 11
                }
            };

            model.IsAuthorized = IsAuthorized();

            model.Metadata.PageTitle = "Add a competition";

            return Task.FromResult(CurrentTemplate(model));
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to add a competition
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized()
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, Groups.AllMembers }, null);
        }
    }
}