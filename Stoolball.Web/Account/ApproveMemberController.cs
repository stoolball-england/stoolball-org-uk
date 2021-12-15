using System;
using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ApproveMemberController : RenderMvcController
    {
        private readonly IVerificationToken _verificationToken;

        public ApproveMemberController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            UmbracoHelper umbracoHelper,
            IVerificationToken verificationToken) :
            base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper)
        {
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new ApproveMember(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                var approvalToken = Request.QueryString["token"];
                var memberId = _verificationToken.ExtractId(approvalToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member != null && member.GetValue("approvalToken")?.ToString() == approvalToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("approvalTokenExpires")))
                {
                    // Approve the member and expire the token
                    member.IsApproved = true;
                    member.SetValue("approvalTokenExpires", _verificationToken.ResetExpiryTo());
                    memberService.Save(member);

                    model.ApprovalTokenValid = true;
                    model.MemberName = member.Name;

                    Logger.Info(typeof(ApproveMemberController), LoggingTemplates.ApproveMember, member.Username, member.Key, typeof(ApproveMemberController), nameof(Index));
                }
                else
                {
                    model.ApprovalTokenValid = false;
                }
            }
            catch (FormatException)
            {
                model.ApprovalTokenValid = false;
            }
            return View("ApproveMember", model);
        }
    }
}