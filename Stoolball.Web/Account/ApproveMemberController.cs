using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Stoolball.Logging;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ApproveMemberController : RenderController
    {
        private readonly IVerificationToken _verificationToken;
        private readonly ILogger<ApproveMemberController> _logger;
        private readonly IPublishedValueFallback _publishedValueFallback;
        private readonly ServiceContext _serviceContext;

        public ApproveMemberController(
            ILogger<ApproveMemberController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IPublishedValueFallback publishedValueFallback,
            ServiceContext context,
            IVerificationToken verificationToken) :
            base(logger.Logger, compositeViewEngine, umbracoContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publishedValueFallback = publishedValueFallback ?? throw new System.ArgumentNullException(nameof(publishedValueFallback));
            _serviceContext = context ?? throw new System.ArgumentNullException(nameof(context));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public override IActionResult Index()
        {
            var model = new ApproveMember(CurrentPage, _publishedValueFallback);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                var approvalToken = Request.Query["token"];
                var memberId = _verificationToken.ExtractId(approvalToken);

                var memberService = _serviceContext.MemberService;
                var member = memberService.GetById(memberId);

                if (member != null && member.GetValue("approvalToken")?.ToString() == approvalToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("approvalTokenExpires")))
                {
                    // Approve the member and expire the token
                    member.IsApproved = true;
                    member.SetValue("approvalTokenExpires", _verificationToken.ResetExpiryTo());
                    memberService.Save(member);

                    model.ApprovalTokenValid = true;
                    model.MemberName = member.Name;

                    _logger.Info(LoggingTemplates.ApproveMember, member.Username, member.Key, typeof(ApproveMemberController), nameof(Index));
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