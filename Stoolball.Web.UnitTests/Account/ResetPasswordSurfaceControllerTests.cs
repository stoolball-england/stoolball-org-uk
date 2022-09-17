using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Logging;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.ActionResults;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new();
        private readonly Mock<ILogger<ResetPasswordSurfaceController>> _logger = new();
        private readonly Mock<IVerificationToken> _tokenReader = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<IMemberSignInManager> _memberSignInManager = new();
        private readonly string _token = Guid.NewGuid().ToString();

        private class TestResetPasswordSurfaceController : ResetPasswordSurfaceController
        {
            public TestResetPasswordSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IVariationContextAccessor variationContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger<ResetPasswordSurfaceController> logger,
                IProfilingLogger profilingLogger,
                IPublishedUrlProvider publishedUrlProvider,
                IMemberSignInManager memberSignInManager,
                IVerificationToken verificationToken,
                IPasswordHasher passwordHasher)
            : base(umbracoContextAccessor, variationContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, publishedUrlProvider, memberSignInManager, verificationToken, passwordHasher)
            {
            }

            public QueryString QuerystringPassedToRedirectToCurrentUmbracoPage { get; private set; }

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage(QueryString queryString)
            {
                QuerystringPassedToRedirectToCurrentUmbracoPage = queryString;
                return base.RedirectToCurrentUmbracoPage(queryString);
            }
        }

        public ResetPasswordSurfaceControllerTests() : base()
        {
            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");
            _currentMember.Setup(x => x.Username).Returns("member@example.org");
            _currentMember.Setup(x => x.GetValue<string>("passwordResetToken", null, null, false)).Returns(_token);
            _currentMember.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            SetupCurrentMember(_currentMember.Object);
        }

        private TestResetPasswordSurfaceController CreateController()
        {
            var controller = new TestResetPasswordSurfaceController(UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                Mock.Of<IUmbracoDatabaseFactory>(),
                base.ServiceContext,
                AppCaches.NoCache,
                _logger.Object,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberSignInManager.Object,
                _tokenReader.Object,
                _passwordHasher.Object)
            {
                ControllerContext = ControllerContext
            };

            Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));

            CurrentPage.Setup(x => x.Name).Returns("Reset password");
            SetupPropertyValue(CurrentPage, "description", "This is the description");

            return controller;
        }

        [Fact]
        public void UpdatePassword_has_content_security_policy_allows_forms()
        {
            var method = typeof(ResetPasswordSurfaceController).GetMethod(nameof(ResetPasswordSurfaceController.UpdatePassword))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute!.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        [Fact]
        public void UpdatePassword_has_form_post_attributes()
        {
            var method = typeof(ResetPasswordSurfaceController).GetMethod(nameof(ResetPasswordSurfaceController.UpdatePassword))!;

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public async void Sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("Reset password", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public async void Invalid_ModelState_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async void Invalid_ModelState_returns_ResetPassword_view_and_redisplays_form()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
                Assert.True((((ViewResult)result).Model as ResetPassword)?.PasswordResetTokenValid);
            }
        }

        [Fact]
        public async void Invalid_ModelState_does_not_save()
        {
            using (var controller = CreateController())
            {
                var model = new ResetPasswordFormData { NewPassword = "pa$$word" };
                controller.ModelState.AddModelError(string.Empty, "Any error");

                await controller.UpdatePassword(model);

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }

        [Fact]
        public async void Null_form_data_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePassword(null);

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public async void Null_form_data_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePassword(null);

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Null_form_data_does_not_save()
        {
            using (var controller = CreateController())
            {
                await controller.UpdatePassword(null);

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }

        [Fact]
        public async void Invalid_token_is_logged()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public async void Invalid_token_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Invalid_token_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public async void Invalid_token_does_not_save()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = await controller.UpdatePassword(new ResetPasswordFormData { NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }

        [Fact]
        public async void Member_not_found_is_logged()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);

#nullable disable
                MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);
#nullable enable

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public async void Member_not_found_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);

#nullable disable
                MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);
#nullable enable


                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Member_not_found_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);

#nullable disable
                MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);
#nullable enable

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public async void Member_not_found_does_not_save()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);

#nullable disable
                MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);
#nullable enable

                var result = await controller.UpdatePassword(new ResetPasswordFormData { NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }

        [Fact]
        public async void Mismatched_token_is_logged()
        {
            using (var controller = CreateController())
            {
                var wrongToken = string.Join("", _token.Reverse());
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", wrongToken } }));
                _tokenReader.Setup(x => x.ExtractId(wrongToken)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, wrongToken, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public async void Mismatched_token_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                var wrongToken = string.Join("", _token.Reverse());
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", wrongToken } }));
                _tokenReader.Setup(x => x.ExtractId(wrongToken)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Mismatched_token_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                var wrongToken = string.Join("", _token.Reverse());
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", wrongToken } }));
                _tokenReader.Setup(x => x.ExtractId(wrongToken)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public async void Mismatched_token_does_not_save()
        {
            using (var controller = CreateController())
            {
                var wrongToken = string.Join("", _token.Reverse());
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", wrongToken } }));
                _tokenReader.Setup(x => x.ExtractId(wrongToken)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = await controller.UpdatePassword(new ResetPasswordFormData { NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }

        [Fact]
        public async void Expired_token_is_logged()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public async void Expired_token_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Expired_token_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public async void Expired_token_does_not_save()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = await controller.UpdatePassword(new ResetPasswordFormData { NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }

        private void SetupExpiredToken()
        {
            _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
            base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);
            var expiryDate = DateTime.Now.AddDays(-1);
            _currentMember.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);
        }

        [Fact]
        public async void Valid_token_returns_RedirectToUmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<RedirectToUmbracoPageResult>(result);
                Assert.Equal($"?token={_token}&successful=yes", controller.QuerystringPassedToRedirectToCurrentUmbracoPage.Value);
            }
        }

        [Fact]
        public async void Valid_token_resets_LockedOut_status()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                _currentMember.VerifySet(x => x.IsLockedOut = false, Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object), Times.Once);
            }
        }

        [Fact]
        public async void Valid_token_resets_expiry()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();
                var expiryResetTo = DateTime.Now.AddHours(1);
                _tokenReader.Setup(x => x.ResetExpiryTo()).Returns(expiryResetTo);

                var result = await controller.UpdatePassword(new ResetPasswordFormData());

                _currentMember.Verify(x => x.SetValue("passwordResetTokenExpires", expiryResetTo, null, null), Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object), Times.Once);
            }
        }

        [Fact]
        public async void Valid_token_resets_password()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var model = new ResetPasswordFormData { NewPassword = "pa$$word" };
                var result = await controller.UpdatePassword(model);

                _passwordHasher.Verify(x => x.HashPassword(model.NewPassword), Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object), Times.Once);
            }
        }

        [Fact]
        public async void Valid_token_is_logged()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var model = new ResetPasswordFormData { NewPassword = "pa$$word" };
                var result = await controller.UpdatePassword(model);

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordReset, _currentMember.Object.Username, _currentMember.Object.Key, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public async void Valid_token_attempts_login()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var model = new ResetPasswordFormData { NewPassword = "pa$$word" };
                var result = await controller.UpdatePassword(model);

                _memberSignInManager.Verify(x => x.PasswordSignInAsync(_currentMember.Object.Username, model.NewPassword, false, false), Times.Once);
            }
        }

        [Fact]
        public async void Valid_token_does_not_attempt_login_if_member_blocked()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();
                _currentMember.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

                var model = new ResetPasswordFormData { NewPassword = "pa$$word" };
                var result = await controller.UpdatePassword(model);

                _memberSignInManager.Verify(x => x.PasswordSignInAsync(_currentMember.Object.Username, model.NewPassword, false, false), Times.Never);
            }
        }

        private void SetupValidTokenScenario()
        {
            _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
            base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);
            var expiryDate = DateTime.Now.AddDays(1);
            _currentMember.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);
        }

    }
}
