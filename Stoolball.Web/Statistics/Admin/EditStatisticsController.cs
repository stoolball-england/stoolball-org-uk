using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics.Admin
{
    public class EditStatisticsController : RenderController, IRenderControllerAsync
    {
        private readonly IMemberManager _memberManager;

        public EditStatisticsController(ILogger<EditStatisticsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMemberManager memberManager)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public new async Task<IActionResult> Index()
        {
            var model = new EditStatisticsViewModel(CurrentPage);
            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditStatistics] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);

            model.Metadata.PageTitle = "Update statistics";

            return CurrentTemplate(model);
        }
    }
}