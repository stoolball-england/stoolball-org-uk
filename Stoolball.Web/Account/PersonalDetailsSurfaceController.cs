using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Logging;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class PersonalDetailsSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ILogger<PersonalDetailsSurfaceController> _logger;

        public PersonalDetailsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger<PersonalDetailsSurfaceController> logger, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager) :
            base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new System.ArgumentNullException(nameof(memberManager));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> UpdatePersonalDetails([Bind(Prefix = "formData")] PersonalDetailsFormData model)
        {
            if (ModelState.IsValid && model != null)
            {
                var member = await _memberManager.GetCurrentMemberAsync();

                var editableMember = Services.MemberService.GetByKey(member.Key);
                editableMember.Name = model.Name;

                Services.MemberService.Save(editableMember);

                _logger.Info(LoggingTemplates.MemberPersonalDetailsUpdated, editableMember.Name, editableMember.Key, typeof(PersonalDetailsSurfaceController), nameof(UpdatePersonalDetails));

                TempData["Success"] = true;
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                return CurrentUmbracoPage();
            }
        }
    }
}
