using Stoolball.Metadata;
using Stoolball.Security;
using System;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class ApproveMemberController : RenderMvcController
    {
        private readonly IVerificationToken _verificationToken;

        public ApproveMemberController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ServiceContext services, AppCaches appCaches, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IVerificationToken verificationToken) :
            base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper)
        {
            _verificationToken = verificationToken;
        }

        // Gets the approval token from the querystring in a way that can be overridden for testing
        protected virtual string ReadApprovalToken() => Request.QueryString["token"];

        [HttpGet]
        public override ActionResult Index(ContentModel model)
        {
            var contentModel = new ApproveMember(model?.Content)
            {
                Metadata = new ViewMetadata
                {
                    PageTitle = model.Content.Name
                }
            };

            try
            {
                var approvalToken = ReadApprovalToken();
                var memberId = _verificationToken.ExtractId(approvalToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member.GetValue("approvalToken").ToString() == approvalToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("approvalTokenExpires")))
                {
                    // Approve the member and expire the token
                    member.IsApproved = true;
                    member.SetValue("approvalTokenExpires", _verificationToken.ResetExpiryTo());
                    memberService.Save(member);

                    contentModel.ApprovalTokenValid = true;
                    contentModel.MemberName = member.Name;
                }
                else
                {
                    contentModel.ApprovalTokenValid = false;
                }
            }
            catch (FormatException)
            {
                contentModel.ApprovalTokenValid = false;
            }
            return CurrentTemplate(contentModel);
        }
    }
}