using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Stoolball.Email;
using Stoolball.Logging;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.ActionResults;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class EmailAddressSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly MemberIdentityUser _currentMember;
        private readonly Mock<IMember> _member = new();
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ILogger<EmailAddressSurfaceController>> _logger = new();
        private readonly Mock<IEmailFormatter> _emailFormatter = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<IVerificationToken> _verificationToken = new();
        private const string VALID_PASSWORD = "validPa$$word";
        private const string EMAIL_TAKEN_SUBJECT = "Email address already in use subject";
        private const string EMAIL_TAKEN_BODY = "Email address already in use body";
        private const string CONFIRM_EMAIL_SUBJECT = "Please confirm your email address";
        private const string CONFIRM_EMAIL_BODY = "Confirm your email address body";
        private const string REQUEST_URL_AUTHORITY = "www.stoolball.org.uk";

        public EmailAddressSurfaceControllerTests() : base()
        {
            _member.Setup(x => x.Id).Returns(123);
            _member.Setup(x => x.Key).Returns(Guid.NewGuid());
            _member.Setup(x => x.Name).Returns("Current Member");
            SetupCurrentMember(_member.Object);

            _currentMember = new MemberIdentityUser
            {
                Id = _member.Object.Id.ToString(),
                Key = _member.Object.Key,
                Name = _member.Object.Name,
            };

            _memberManager.Setup(x => x.CheckPasswordAsync(_currentMember, VALID_PASSWORD)).Returns(Task.FromResult(true));
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(_currentMember));

            _emailFormatter.Setup(x => x.FormatEmailContent(EMAIL_TAKEN_SUBJECT, EMAIL_TAKEN_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((EMAIL_TAKEN_SUBJECT, EMAIL_TAKEN_BODY));
            _emailFormatter.Setup(x => x.FormatEmailContent(CONFIRM_EMAIL_SUBJECT, CONFIRM_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
               .Returns((CONFIRM_EMAIL_SUBJECT, CONFIRM_EMAIL_BODY));

            SetupPropertyValue(CurrentPage, "description", string.Empty);
            SetupPropertyValue(CurrentPage, "emailTakenSubject", EMAIL_TAKEN_SUBJECT);
            SetupPropertyValue(CurrentPage, "emailTakenBody", EMAIL_TAKEN_BODY);
            SetupPropertyValue(CurrentPage, "confirmEmailSubject", CONFIRM_EMAIL_SUBJECT);
            SetupPropertyValue(CurrentPage, "confirmEmailBody", CONFIRM_EMAIL_BODY);
        }

        private EmailAddressSurfaceController CreateController()
        {
            return new EmailAddressSurfaceController(UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                Mock.Of<IUmbracoDatabaseFactory>(),
                base.ServiceContext,
                AppCaches.NoCache,
                _logger.Object,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _emailFormatter.Object,
                _emailSender.Object,
                _verificationToken.Object)
            {
                ControllerContext = ControllerContext,
                TempData = new TempDataDictionary(HttpContext.Object, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public void UpdateEmailAddress_has_content_security_policy_allows_forms()
        {
            var method = typeof(EmailAddressSurfaceController).GetMethod(nameof(EmailAddressSurfaceController.UpdateEmailAddress))!;
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
        public void UpdateEmailAddress_has_form_post_attributes()
        {
            var method = typeof(EmailAddressSurfaceController).GetMethod(nameof(EmailAddressSurfaceController.UpdateEmailAddress))!;

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public async Task Request_for_email_already_in_use_sends_email()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = SetupAnotherAccountUsingThisEmail(model.Requested);

            Dictionary<string, string>? receivedTokens = null;
            _emailFormatter.Setup(x => x.FormatEmailContent(EMAIL_TAKEN_SUBJECT, EMAIL_TAKEN_BODY, It.IsAny<Dictionary<string, string>>()))
                .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) => receivedTokens = tokens)
                .Returns(("email subject", "email body"));

            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                Assert.Equal(otherMember.Object.Name, receivedTokens!["name"]);
                Assert.Equal(model.Requested, receivedTokens["email"]);
                Assert.Equal(REQUEST_URL_AUTHORITY, receivedTokens["domain"]);
                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(model.Requested, message.To[0]);
                        Assert.Equal("email subject", message.Subject);
                        Assert.Equal("email body", message.Body);
                    });
            }
        }

        private Mock<IMember> SetupAnotherAccountUsingThisEmail(string email)
        {
            var otherMember = new Mock<IMember>();
            otherMember.Setup(x => x.Name).Returns("Other member");
            otherMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            MemberService.Setup(x => x.GetByEmail(email)).Returns(otherMember.Object);
            return otherMember;
        }

        [Fact]
        public async Task Request_for_email_already_in_use_saves_token_anyway()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = SetupAnotherAccountUsingThisEmail(model.Requested);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                base.MemberService.Verify(x => x.Save(_member.Object), Times.Once);
            }
        }

        [Fact]
        public async Task Request_for_email_already_in_use_sets_TempData_for_view()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = SetupAnotherAccountUsingThisEmail(model.Requested);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public async Task Request_for_email_already_in_use_is_logged()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };

            var otherMember = SetupAnotherAccountUsingThisEmail(model.Requested);

            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                _logger.Verify(x => x.Info(LoggingTemplates.MemberRequestedEmailAddressAlreadyInUse, _member.Object.Name, _member.Object.Key, typeof(EmailAddressSurfaceController), nameof(EmailAddressSurfaceController.UpdateEmailAddress)), Times.Once);
            }
        }

        [Fact]
        public async Task Valid_request_saves_email_and_token()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddDays(1);
            _verificationToken.Setup(x => x.TokenFor(_member.Object.Id)).Returns((token, tokenExpiry));

            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                _member.Verify(x => x.SetValue("requestedEmail", model.Requested, null, null), Times.Once);
                _member.Verify(x => x.SetValue("requestedEmailToken", token, null, null), Times.Once);
                _member.Verify(x => x.SetValue("requestedEmailTokenExpires", tokenExpiry, null, null), Times.Once);
                base.MemberService.Verify(x => x.Save(_member.Object), Times.Once);
            }
        }


        [Fact]
        public async Task Valid_request_sends_email_with_token()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddDays(1);
            _verificationToken.Setup(x => x.TokenFor(_member.Object.Id)).Returns((token, tokenExpiry));

            Dictionary<string, string>? receivedTokens = null;
            _emailFormatter.Setup(x => x.FormatEmailContent(CONFIRM_EMAIL_SUBJECT, CONFIRM_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) => receivedTokens = tokens)
                .Returns((CONFIRM_EMAIL_SUBJECT, CONFIRM_EMAIL_BODY));

            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                Assert.Equal(_member.Object.Name, receivedTokens!["name"]);
                Assert.Equal(model.Requested, receivedTokens["email"]);
                Assert.Equal(REQUEST_URL_AUTHORITY, receivedTokens["domain"]);
                Assert.Equal(token, receivedTokens["token"]);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(model.Requested, message.To[0]);
                        Assert.Equal("confirm email subject", message.Subject);
                        Assert.Equal("confirm email body", message.Body);
                    });
            }
        }

        [Fact]
        public async Task Valid_request_is_logged()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                _logger.Verify(x => x.Info(LoggingTemplates.MemberRequestedEmailAddress, _member.Object.Name, _member.Object.Key, typeof(EmailAddressSurfaceController), nameof(EmailAddressSurfaceController.UpdateEmailAddress)), Times.Once);
            }
        }

        [Fact]
        public async Task Valid_request_sets_TempData_for_view()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public async Task Valid_request_returns_RedirectToUmbracoPageResult()
        {
            var model = new EmailAddressFormData { Requested = "new@example.org", Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        public async Task Invalid_password_sets_ModelState(string password)
        {
            var model = new EmailAddressFormData { Password = password };
            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(model);

                Assert.True(controller.ModelState.ContainsKey("formData." + nameof(model.Password)));
                Assert.Equal("Your password is incorrect or your account is locked.", controller.ModelState["formData." + nameof(model.Password)].Errors[0].ErrorMessage);
            }
        }

        [Fact]
        public async Task Invalid_model_does_not_save_or_set_TempData()
        {
            var model = new EmailAddressFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = await controller.UpdateEmailAddress(model);

                base.MemberService.Verify(x => x.Save(_member.Object), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public async Task Invalid_model_returns_EmailAddress_view()
        {
            var model = new EmailAddressFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = await controller.UpdateEmailAddress(model);

                Assert.Equal("EmailAddress", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async Task Invalid_model_returns_form_data_to_view()
        {
            var model = new EmailAddressFormData { Password = "pa$$word", Requested = "new@example.org" };
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = await controller.UpdateEmailAddress(model);

                var returnedFormData = ((EmailAddress)((ViewResult)result).Model).FormData;
                Assert.NotNull(returnedFormData);
                Assert.Equal(model.Password, returnedFormData.Password);
                Assert.Equal(model.Requested, returnedFormData.Requested);
            }
        }

        [Fact]
        public async Task Null_model_does_not_save_or_set_TempData()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(null);

                base.MemberService.Verify(x => x.Save(_member.Object), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public async Task Null_model_returns_400()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdateEmailAddress(null);

                Assert.IsType<StatusCodeResult>(result);
                Assert.Equal(400, ((StatusCodeResult)result).StatusCode);
            }
        }
    }
}
