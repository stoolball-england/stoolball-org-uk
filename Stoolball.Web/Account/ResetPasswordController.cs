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
    public class ResetPasswordController : RenderController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ServiceContext _serviceContext;
        private readonly ILogger<ResetPasswordController> _logger;
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordController(
            ILogger<ResetPasswordController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            ServiceContext context,
            IVerificationToken verificationToken) :
            base(logger.Logger, compositeViewEngine, umbracoContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _variationContextAccessor = variationContextAccessor ?? throw new ArgumentNullException(nameof(variationContextAccessor));
            _serviceContext = context ?? throw new ArgumentNullException(nameof(context));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public override IActionResult Index()
        {
            var model = new ResetPassword(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor));
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                model.PasswordResetToken = Request.Query["token"];

                // If there's no token, show the form to request a password reset
                if (string.IsNullOrEmpty(model.PasswordResetToken))
                {
                    return View("ResetPasswordRequest", model);
                }

                // Show a message saying the reset was successful
                if (Request.Query["successful"] == "yes")
                {
                    model.ShowPasswordResetSuccessful = true;
                    return View("ResetPasswordComplete", model);
                }

                var memberId = _verificationToken.ExtractId(model.PasswordResetToken);

                var member = _serviceContext.MemberService.GetById(memberId);

                if (member.GetValue("passwordResetToken").ToString() == model.PasswordResetToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires")))
                {
                    // Show the set a new password form
                    model.PasswordResetTokenValid = true;
                }
                else
                {
                    // Show a message saying the token was not valid
                    _logger.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordController), nameof(ResetPasswordController.Index));
                    model.PasswordResetTokenValid = false;
                }
            }
            catch (FormatException)
            {
                // Show a message saying the token was not valid
                _logger.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordController), nameof(ResetPasswordController.Index));
                model.PasswordResetTokenValid = false;
            }
            return View("ResetPassword", model);
        }
    }
}