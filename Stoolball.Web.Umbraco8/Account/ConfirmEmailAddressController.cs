using System;
using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ConfirmEmailAddressController : RenderMvcController
    {
        private readonly IVerificationToken _verificationToken;

        public ConfirmEmailAddressController(IGlobalSettings globalSettings,
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
            var model = new ConfirmEmailAddress(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                var token = Request.QueryString["token"];
                var member = FindMemberMatchingThisToken(token);

                if (MemberHasValidEmailToken(member, token))
                {
                    if (RequestedEmailIsNotUsedByAnotherMember(member))
                    {
                        ConfirmNewEmailAddress(member);

                        AddMemberInfoToViewModel(model, member);
                        model.TokenValid = true;
                    }
                    else
                    {
                        model.TokenValid = false;
                    }
                }
                else
                {
                    model.TokenValid = false;
                }
            }
            catch (FormatException)
            {
                model.TokenValid = false;
            }
            return View("ConfirmEmailAddress", model);
        }

        private static void AddMemberInfoToViewModel(ConfirmEmailAddress model, IMember member)
        {
            model.MemberName = member.Name;
            model.EmailAddress = member.Email;
        }

        private void ConfirmNewEmailAddress(IMember member)
        {
            // Update the email address and expire the token
            member.Username = member.Email = member.GetValue("requestedEmail").ToString();
            member.SetValue("requestedEmailTokenExpires", _verificationToken.ResetExpiryTo());
            Services.MemberService.Save(member);

            Logger.Info(typeof(ConfirmEmailAddressController), LoggingTemplates.ConfirmEmailAddress, member.Username, member.Key, typeof(ConfirmEmailAddressController), nameof(Index));
        }

        private bool RequestedEmailIsNotUsedByAnotherMember(IMember member)
        {
            return Services.MemberService.GetByEmail(member.GetValue("requestedEmail").ToString()) == null;
        }

        private bool MemberHasValidEmailToken(IMember member, string token)
        {
            return member != null && member.GetValue("requestedEmailToken")?.ToString() == token && !_verificationToken.HasExpired(member.GetValue<DateTime>("requestedEmailTokenExpires"));
        }

        private IMember FindMemberMatchingThisToken(string token)
        {
            var memberId = _verificationToken.ExtractId(token);
            return Services.MemberService.GetById(memberId);
        }
    }
}