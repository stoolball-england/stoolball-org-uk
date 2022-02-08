using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Stoolball.Logging;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ConfirmEmailAddressController : RenderController
    {
        private readonly ILogger<ConfirmEmailAddressController> _logger;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ServiceContext _serviceContext;
        private readonly IVerificationToken _verificationToken;

        public ConfirmEmailAddressController(
            ILogger<ConfirmEmailAddressController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            ServiceContext context,
            IVerificationToken verificationToken) :
            base(logger.Logger, compositeViewEngine, umbracoContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _variationContextAccessor = variationContextAccessor ?? throw new System.ArgumentNullException(nameof(variationContextAccessor));
            _serviceContext = context ?? throw new System.ArgumentNullException(nameof(context));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public override IActionResult Index()
        {
            var model = new ConfirmEmailAddress(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor));
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                var token = Request.Query["token"];
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
            _serviceContext.MemberService.Save(member);

            _logger.Info(LoggingTemplates.ConfirmEmailAddress, member.Username, member.Key, typeof(ConfirmEmailAddressController), nameof(Index));
        }

        private bool RequestedEmailIsNotUsedByAnotherMember(IMember member)
        {
            return _serviceContext.MemberService.GetByEmail(member.GetValue("requestedEmail").ToString()) == null;
        }

        private bool MemberHasValidEmailToken(IMember member, string token)
        {
            return member != null && member.GetValue("requestedEmailToken")?.ToString() == token && !_verificationToken.HasExpired(member.GetValue<DateTime>("requestedEmailTokenExpires"));
        }

        private IMember FindMemberMatchingThisToken(string token)
        {
            var memberId = _verificationToken.ExtractId(token);
            return _serviceContext.MemberService.GetById(memberId);
        }
    }
}