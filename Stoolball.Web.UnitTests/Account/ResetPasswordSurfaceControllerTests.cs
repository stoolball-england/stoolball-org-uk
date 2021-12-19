using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Metadata;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new Mock<IMember>();
        private readonly Mock<ILogger> _logger = new Mock<ILogger>();
        private readonly Mock<IVerificationToken> _tokenReader = new Mock<IVerificationToken>();
        private readonly Mock<ILoginMemberWrapper> _loginMemberWrapper = new Mock<ILoginMemberWrapper>();
        private readonly string _token = Guid.NewGuid().ToString();

        private class TestResetPasswordSurfaceController : ResetPasswordSurfaceController
        {
            private readonly Mock<IPublishedContent> _currentPage;

            public TestResetPasswordSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                HttpContextBase httpContext,
                ILoginMemberWrapper loginMemberWrapper,
                IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, loginMemberWrapper, verificationToken)
            {
                _currentPage = new Mock<IPublishedContent>();
                _currentPage.Setup(x => x.Name).Returns("Reset password");
                SetupPropertyValue(_currentPage, "description", "This is the description");

                ControllerContext = new ControllerContext(httpContext, new RouteData(), this);
            }

            protected override IPublishedContent CurrentPage => _currentPage.Object;

            public string QuerystringPassedToRedirectToCurrentUmbracoPage { get; private set; }

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage(string queryString)
            {
                QuerystringPassedToRedirectToCurrentUmbracoPage = queryString;
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }
        }

        public ResetPasswordSurfaceControllerTests()
        {
            base.Setup();

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
            var controller = new TestResetPasswordSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            _logger.Object,
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            base.HttpContext.Object,
                            _loginMemberWrapper.Object,
                            _tokenReader.Object);

            base.Request.SetupGet(x => x.Url).Returns(new Uri("https://localhost/account/reset-password"));
            base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));

            return controller;
        }

        [Fact]
        public void UpdatePassword_has_content_security_policy_allows_forms()
        {
            var method = typeof(ResetPasswordSurfaceController).GetMethod(nameof(ResetPasswordSurfaceController.UpdatePassword));
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        [Fact]
        public void UpdatePassword_has_form_post_attributes()
        {
            var method = typeof(ResetPasswordSurfaceController).GetMethod(nameof(ResetPasswordSurfaceController.UpdatePassword));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdatePassword(new ResetPasswordFormData());

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("Reset password", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Invalid_ModelState_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Invalid_ModelState_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_ModelState_does_not_save()
        {
            using (var controller = CreateController())
            {
                var model = new ResetPasswordFormData { NewPassword = "pa$$word" };
                controller.ModelState.AddModelError(string.Empty, "Any error");

                controller.UpdatePassword(model);

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                MemberService.Verify(x => x.SavePassword(_currentMember.Object, model.NewPassword), Times.Never);
            }
        }

        [Fact]
        public void Null_form_data_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdatePassword(null);

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Null_form_data_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdatePassword(null);

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Null_form_data_does_not_save()
        {
            using (var controller = CreateController())
            {
                controller.UpdatePassword(null);

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                MemberService.Verify(x => x.SavePassword(_currentMember.Object, It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public void Invalid_token_is_logged()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                _logger.Verify(x => x.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public void Invalid_token_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_token_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public void Invalid_token_does_not_save()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.UpdatePassword(new ResetPasswordFormData { NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                MemberService.Verify(x => x.SavePassword(_currentMember.Object, It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public void Member_not_found_is_logged()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                _logger.Verify(x => x.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public void Member_not_found_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);

                var result = controller.UpdatePassword(new ResetPasswordFormData());

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Member_not_found_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);

                var result = controller.UpdatePassword(new ResetPasswordFormData());

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public void Member_not_found_does_not_save()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns((IMember)null);

                var result = controller.UpdatePassword(new ResetPasswordFormData { NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                MemberService.Verify(x => x.SavePassword(_currentMember.Object, It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public void Mismatched_token_is_logged()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);
                var model = new ResetPasswordFormData { PasswordResetToken = _token.Reverse().ToString() };

                var result = controller.UpdatePassword(model);

                _logger.Verify(x => x.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public void Mismatched_token_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token.Reverse().ToString() });

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Mismatched_token_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token.Reverse().ToString() });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public void Mismatched_token_does_not_save()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Returns(_currentMember.Object.Id);
                base.MemberService.Setup(x => x.GetById(_currentMember.Object.Id)).Returns(_currentMember.Object);

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token.Reverse().ToString(), NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                MemberService.Verify(x => x.SavePassword(_currentMember.Object, It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public void Expired_token_is_logged()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var model = new ResetPasswordFormData { PasswordResetToken = _token };
                var result = controller.UpdatePassword(model);

                _logger.Verify(x => x.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public void Expired_token_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Expired_token_returns_ResetPassword_ModelsBuilder_model_showing_failure_message()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).ShowPasswordResetSuccessful);
            }
        }

        [Fact]
        public void Expired_token_does_not_save()
        {
            using (var controller = CreateController())
            {
                SetupExpiredToken();

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token, NewPassword = "pa$$word" });

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                MemberService.Verify(x => x.SavePassword(_currentMember.Object, It.IsAny<string>()), Times.Never);
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
        public void Valid_token_returns_RedirectToUmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                Assert.IsType<RedirectToUmbracoPageResult>(result);
                Assert.Equal($"token={_token}&successful=yes", controller.QuerystringPassedToRedirectToCurrentUmbracoPage);
            }
        }

        [Fact]
        public void Valid_token_resets_LockedOut_status()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                _currentMember.VerifySet(x => x.IsLockedOut = false, Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_resets_expiry()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();
                var expiryResetTo = DateTime.Now.AddHours(1);
                _tokenReader.Setup(x => x.ResetExpiryTo()).Returns(expiryResetTo);

                var result = controller.UpdatePassword(new ResetPasswordFormData { PasswordResetToken = _token });

                _currentMember.Verify(x => x.SetValue("passwordResetTokenExpires", expiryResetTo, null, null), Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_resets_password()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var model = new ResetPasswordFormData { PasswordResetToken = _token, NewPassword = "pa$$word" };
                var result = controller.UpdatePassword(model);

                base.MemberService.Verify(x => x.SavePassword(_currentMember.Object, model.NewPassword), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_is_logged()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var model = new ResetPasswordFormData { PasswordResetToken = _token, NewPassword = "pa$$word" };
                var result = controller.UpdatePassword(model);

                _logger.Verify(x => x.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordReset, _currentMember.Object.Username, _currentMember.Object.Key, typeof(ResetPasswordSurfaceController), nameof(ResetPasswordSurfaceController.UpdatePassword)), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_attempts_login()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();

                var model = new ResetPasswordFormData { PasswordResetToken = _token, NewPassword = "pa$$word" };
                var result = controller.UpdatePassword(model);

                _loginMemberWrapper.Verify(x => x.LoginMember(_currentMember.Object.Username, model.NewPassword), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_does_not_attempt_login_if_member_blocked()
        {
            using (var controller = CreateController())
            {
                SetupValidTokenScenario();
                _currentMember.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

                var model = new ResetPasswordFormData { PasswordResetToken = _token, NewPassword = "pa$$word" };
                var result = controller.UpdatePassword(model);

                _loginMemberWrapper.Verify(x => x.LoginMember(_currentMember.Object.Username, model.NewPassword), Times.Never);
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
