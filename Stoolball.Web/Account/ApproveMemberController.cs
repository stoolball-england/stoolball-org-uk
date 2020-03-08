using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Account
{
    public class ApproveMemberController : RenderMvcController
    {
        // Gets the approval token from the querystring in a way that can be overridden for testing
        protected virtual string ReadApprovalToken() => Request.QueryString["token"];

        // Gets the time the approval token expiry date is evaluated against, in a way that can be overridden for testing
        protected virtual DateTime ApprovalTokenExpiryDeadline() => DateTime.UtcNow;

        [HttpGet]
        public override ActionResult Index(ContentModel model)
        {
            var contentModel = new ApproveMemberModel(model?.Content);

            try
            {
                var approvalToken = ReadApprovalToken();
                var memberId = ExtractMemberIdFromApprovalToken(approvalToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member.GetValue("approvalToken").ToString() == approvalToken && member.GetValue<DateTime>("approvalTokenExpiry") > ApprovalTokenExpiryDeadline())
                {
                    // Approve the member and expire the token
                    member.IsApproved = true;
                    member.SetValue("approvalTokenExpiry", ApprovalTokenExpiryDeadline());
                    memberService.Save(member);

                    contentModel.MemberWasApproved = true;
                    contentModel.MemberName = member.Name;
                }
                else
                {
                    contentModel.MemberWasApproved = false;
                }
            }
            catch (FormatException)
            {
                contentModel.MemberWasApproved = false;
            }
            return CurrentTemplate(contentModel);
        }

        private int ExtractMemberIdFromApprovalToken(string approvalToken)
        {
            if (string.IsNullOrEmpty(approvalToken)) { throw new FormatException(); }
         
            var match = Regex.Match(approvalToken, "^(?<MemberId>[0-9]+)-[0-9a-f]{32}$");
            if (!match.Success) { throw new FormatException("Invalid approval token"); }

            return Convert.ToInt32(match.Groups["MemberId"].Value, CultureInfo.InvariantCulture);
        }
    }
}