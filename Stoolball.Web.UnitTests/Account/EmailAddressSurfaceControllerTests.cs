using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Moq;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Email;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class EmailAddressSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new Mock<IMember>();
        private readonly Mock<ILogger> _logger = new Mock<ILogger>();
        private readonly Mock<IEmailFormatter> _emailFormatter = new Mock<IEmailFormatter>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly Mock<IVerificationToken> _verificationToken = new Mock<IVerificationToken>();
        private const string VALID_PASSWORD = "validPa$$word";
        private const string EMAIL_TAKEN_SUBJECT = "Email address already in use subject";
        private const string EMAIL_TAKEN_BODY = "Email address already in use body";
        private const string CONFIRM_EMAIL_SUBJECT = "Please confirm your email address";
        private const string CONFIRM_EMAIL_BODY = "Confirm your email address body";
        private const string REQUEST_URL_AUTHORITY = "localhost";

        private class TestEmailAddressSurfaceController : EmailAddressSurfaceController
        {
            private readonly Mock<IPublishedContent> _currentPage;

            public TestEmailAddressSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                HttpContextBase httpContext,
                MembershipProvider membershipProvider,
                IEmailFormatter emailFormatter,
                IEmailSender emailSender,
                IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, membershipProvider, emailFormatter, emailSender, verificationToken)
            {
                _currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(_currentPage, "emailTakenSubject", EMAIL_TAKEN_SUBJECT);
                SetupPropertyValue(_currentPage, "emailTakenBody", EMAIL_TAKEN_BODY);
                SetupPropertyValue(_currentPage, "confirmEmailSubject", CONFIRM_EMAIL_SUBJECT);
                SetupPropertyValue(_currentPage, "confirmEmailBody", CONFIRM_EMAIL_BODY);

                ControllerContext = new ControllerContext(httpContext, new RouteData(), this);
            }

            protected override IPublishedContent CurrentPage => _currentPage.Object;

            protected override string GetRequestUrlAuthority()
            {
                return REQUEST_URL_AUTHORITY;
            }

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage()
            {
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }
        }

        public EmailAddressSurfaceControllerTests()
        {
            base.Setup();

            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");

            SetupCurrentMember(_currentMember.Object);
        }

        private TestEmailAddressSurfaceController CreateController()
        {
            var membershipProvider = new Mock<MembershipProvider>();
            membershipProvider.Setup(x => x.ValidateUser(_currentMember.Object.Name, VALID_PASSWORD)).Returns(true);


            return new TestEmailAddressSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            _logger.Object,
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            base.HttpContext.Object,
                            membershipProvider.Object,
                            _emailFormatter.Object,
                            _emailSender.Object,
                            _verificationToken.Object);
        }

        [Fact]
        public void UpdateEmailAddress_has_content_security_policy_allows_forms()
        {
            var method = typeof(EmailAddressSurfaceController).GetMethod(nameof(EmailAddressSurfaceController.UpdateEmailAddress));
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
        public void UpdateEmailAddress_has_form_post_attributes()
        {
            var method = typeof(EmailAddressSurfaceController).GetMethod(nameof(EmailAddressSurfaceController.UpdateEmailAddress));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Request_for_email_already_in_use_sends_email()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = new Mock<IPublishedContent>();
            otherMember.Setup(x => x.Name).Returns("Other member");
            otherMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            MemberCache.Setup(x => x.GetByEmail(model.RequestedEmail)).Returns(otherMember.Object);

            Dictionary<string, string> receivedTokens = null;
            _emailFormatter.Setup(x => x.FormatEmailContent(EMAIL_TAKEN_SUBJECT, EMAIL_TAKEN_BODY, It.IsAny<Dictionary<string, string>>()))
                .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) => receivedTokens = tokens)
                .Returns(("email subject", "email body"));

            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.Equal(otherMember.Object.Name, receivedTokens["name"]);
                Assert.Equal(model.RequestedEmail, receivedTokens["email"]);
                Assert.Equal(REQUEST_URL_AUTHORITY, receivedTokens["domain"]);
                _emailSender.Verify(x => x.SendEmail(model.RequestedEmail, "email subject", "email body"), Times.Once);
            }
        }

        [Fact]
        public void Request_for_email_already_in_use_does_not_save()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = new Mock<IPublishedContent>();
            otherMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            MemberCache.Setup(x => x.GetByEmail(model.RequestedEmail)).Returns(otherMember.Object);

            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
            }
        }

        [Fact]
        public void Request_for_email_already_in_use_sets_TempData_for_view()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = new Mock<IPublishedContent>();
            otherMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            MemberCache.Setup(x => x.GetByEmail(model.RequestedEmail)).Returns(otherMember.Object);

            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public void Request_for_email_already_in_use_is_logged()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = new Mock<IPublishedContent>();
            otherMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            MemberCache.Setup(x => x.GetByEmail(model.RequestedEmail)).Returns(otherMember.Object);

            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                _logger.Verify(x => x.Info(typeof(EmailAddressSurfaceController), LoggingTemplates.MemberRequestedEmailAddressAlreadyInUse, _currentMember.Object.Name, _currentMember.Object.Key, typeof(EmailAddressSurfaceController), nameof(EmailAddressSurfaceController.UpdateEmailAddress)), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_saves_email_and_token()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddDays(1);
            _verificationToken.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((token, tokenExpiry));

            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                _currentMember.Verify(x => x.SetValue("requestedEmail", model.RequestedEmail, null, null), Times.Once);
                _currentMember.Verify(x => x.SetValue("requestedEmailToken", token, null, null), Times.Once);
                _currentMember.Verify(x => x.SetValue("requestedEmailTokenExpires", tokenExpiry, null, null), Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }


        [Fact]
        public void Valid_request_sends_email_with_token()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddDays(1);
            _verificationToken.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((token, tokenExpiry));

            Dictionary<string, string> receivedTokens = null;
            _emailFormatter.Setup(x => x.FormatEmailContent(CONFIRM_EMAIL_SUBJECT, CONFIRM_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) => receivedTokens = tokens)
                .Returns(("confirm email subject", "confirm email body"));

            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.Equal(_currentMember.Object.Name, receivedTokens["name"]);
                Assert.Equal(model.RequestedEmail, receivedTokens["email"]);
                Assert.Equal(REQUEST_URL_AUTHORITY, receivedTokens["domain"]);
                Assert.Equal(token, receivedTokens["token"]);
                _emailSender.Verify(x => x.SendEmail(model.RequestedEmail, "confirm email subject", "confirm email body"), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_is_logged()
        {
            var model = new EmailAddressFormData { Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                _logger.Verify(x => x.Info(typeof(EmailAddressSurfaceController), LoggingTemplates.MemberRequestedEmailAddress, _currentMember.Object.Name, _currentMember.Object.Key, typeof(EmailAddressSurfaceController), nameof(EmailAddressSurfaceController.UpdateEmailAddress)), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_sets_TempData_for_view()
        {
            var model = new EmailAddressFormData { Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public void Valid_request_returns_RedirectToUmbracoPageResult()
        {
            var model = new EmailAddressFormData { Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        public void Invalid_password_sets_ModelState(string password)
        {
            var model = new EmailAddressFormData { Password = password };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.True(controller.ModelState.ContainsKey("formData." + nameof(model.Password)));
                Assert.Equal("Your password is incorrect or your account is locked.", controller.ModelState["formData." + nameof(model.Password)].Errors[0].ErrorMessage);
            }
        }

        [Fact]
        public void Invalid_model_does_not_save_or_set_TempData()
        {
            var model = new EmailAddressFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = controller.UpdateEmailAddress(model);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public void Invalid_model_returns_UmbracoPageResult()
        {
            var model = new EmailAddressFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = controller.UpdateEmailAddress(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Null_model_does_not_save_or_set_TempData()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(null);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public void Null_model_returns_UmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(null);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }
    }
}
