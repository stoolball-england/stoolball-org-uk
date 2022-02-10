using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
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
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordRequestSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new();
        private readonly Mock<ILogger<ResetPasswordRequestSurfaceController>> _logger = new();
        private readonly Mock<IEmailFormatter> _emailFormatter = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<IVerificationToken> _tokenReader = new();
        private readonly string _token = Guid.NewGuid().ToString();
        private readonly DateTime _expiryDate = DateTime.Now.AddDays(1);
        private const string CREATE_MEMBER_SUBJECT = "Create an account subject";
        private const string CREATE_MEMBER_BODY = "Create an account body";
        private const string RESET_PASSWORD_SUBJECT = "Reset password subject";
        private const string RESET_PASSWORD_BODY = "Reset password body";
        private const string APPROVE_MEMBER_SUBJECT = "Account not approved subject";
        private const string APPROVE_MEMBER_BODY = "Account not approved body";
        private const string REQUEST_URL_AUTHORITY = "www.stoolball.org.uk";

        public ResetPasswordRequestSurfaceControllerTests()
        {
            base.Setup();

            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");

            SetupCurrentMember(_currentMember.Object);

            CurrentPage.Setup(x => x.Name).Returns("Reset password");
            SetupPropertyValue(CurrentPage, "description", "This is the description");
            SetupPropertyValue(CurrentPage, "createMemberSubject", CREATE_MEMBER_SUBJECT);
            SetupPropertyValue(CurrentPage, "createMemberBody", CREATE_MEMBER_BODY);
            SetupPropertyValue(CurrentPage, "resetPasswordSubject", RESET_PASSWORD_SUBJECT);
            SetupPropertyValue(CurrentPage, "resetPasswordBody", RESET_PASSWORD_BODY);
            SetupPropertyValue(CurrentPage, "approveMemberSubject", APPROVE_MEMBER_SUBJECT);
            SetupPropertyValue(CurrentPage, "approveMemberBody", APPROVE_MEMBER_BODY);

            _emailFormatter.Setup(x => x.FormatEmailContent(CREATE_MEMBER_SUBJECT, CREATE_MEMBER_BODY, It.IsAny<Dictionary<string, string>>())).Returns((CREATE_MEMBER_SUBJECT, CREATE_MEMBER_BODY));
            _emailFormatter.Setup(x => x.FormatEmailContent(RESET_PASSWORD_SUBJECT, RESET_PASSWORD_BODY, It.IsAny<Dictionary<string, string>>())).Returns((RESET_PASSWORD_SUBJECT, RESET_PASSWORD_BODY));
            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_SUBJECT, APPROVE_MEMBER_BODY, It.IsAny<Dictionary<string, string>>())).Returns((APPROVE_MEMBER_SUBJECT, APPROVE_MEMBER_BODY));
        }

        private ResetPasswordRequestSurfaceController CreateController()
        {
            return new ResetPasswordRequestSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                Mock.Of<IUmbracoDatabaseFactory>(),
                base.ServiceContext,
                AppCaches.NoCache,
                _logger.Object,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _emailFormatter.Object,
                _emailSender.Object,
                _tokenReader.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = HttpContext.Object
                }
            };
        }

        [Fact]
        public void RequestPasswordReset_has_content_security_policy_allows_forms()
        {
            var method = typeof(ResetPasswordRequestSurfaceController).GetMethod(nameof(ResetPasswordRequestSurfaceController.RequestPasswordReset));
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
        public void RequestPasswordReset_has_form_post_attributes()
        {
            var method = typeof(ResetPasswordRequestSurfaceController).GetMethod(nameof(ResetPasswordRequestSurfaceController.RequestPasswordReset));

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
                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("Reset password", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public async void Invalid_ModelState_sets_email_from_form_data_and_redisplays_form()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.False((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public async void Invalid_ModelState_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async void Invalid_ModelState_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Invalid_ModelState_adds_email_error()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True(controller.ModelState.ContainsKey("Email"));
                Assert.Equal("Please enter a valid email address.", controller.ModelState["Email"].Errors.FirstOrDefault()?.ErrorMessage);
            }
        }

        [Fact]
        public async void Invalid_ModelState_does_not_save_or_email()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Never);
            }
        }

        [Fact]
        public async void Null_form_data_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var result = await controller.RequestPasswordReset(null);

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }


        [Fact]
        public async void Null_form_data_redisplays_form()
        {
            using (var controller = CreateController())
            {
                var result = await controller.RequestPasswordReset(null);

                Assert.False((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
            }
        }

        [Fact]
        public async void Null_form_data_adds_email_error()
        {
            using (var controller = CreateController())
            {
                await controller.RequestPasswordReset(null);

                Assert.True(controller.ModelState.ContainsKey("Email"));
                Assert.Equal("Please enter a valid email address.", controller.ModelState["Email"].Errors.FirstOrDefault()?.ErrorMessage);
            }
        }

        [Fact]
        public async void Null_form_data_does_not_save_or_email()
        {
            using (var controller = CreateController())
            {
                await controller.RequestPasswordReset(null);

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Never);
            }
        }

        [Fact]
        public async void Approved_member_saves_reset_token()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                await controller.RequestPasswordReset(formData);

                _currentMember.Verify(x => x.SetValue("passwordResetToken", _token, null, null), Times.Once);
                _currentMember.Verify(x => x.SetValue("passwordResetTokenExpires", _expiryDate, null, null), Times.Once);
                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Once);
            }
        }

        [Fact]
        public async void Approved_member_sends_reset_password_email()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));
                _emailFormatter.Setup(x => x.FormatEmailContent(RESET_PASSWORD_SUBJECT, RESET_PASSWORD_BODY, It.IsAny<Dictionary<string, string>>()))
                    .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
                    {
                        Assert.Equal(_currentMember.Object.Name, tokens["name"]);
                        Assert.Equal(formData.Email, tokens["email"]);
                        Assert.Equal(_token, tokens["token"]);
                        Assert.Equal(REQUEST_URL_AUTHORITY, tokens["domain"]);
                    })
                    .Returns((RESET_PASSWORD_SUBJECT, RESET_PASSWORD_BODY));

                await controller.RequestPasswordReset(formData);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(formData.Email, message.To[0]);
                        Assert.Equal(RESET_PASSWORD_SUBJECT, message.Subject);
                        Assert.Equal(RESET_PASSWORD_BODY, message.Body);
                    });
            }
        }

        [Fact]
        public async void Approved_member_request_is_logged()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);

                await controller.RequestPasswordReset(formData);

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetRequested, _currentMember.Object.Username, _currentMember.Object.Key, typeof(ResetPasswordRequestSurfaceController), nameof(ResetPasswordRequestSurfaceController.RequestPasswordReset)), Times.Once);
            }
        }

        [Fact]
        public async void Approved_member_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async void Approved_member_sets_email_from_form_data_and_displays_confirmation()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public async void Approved_member_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Unapproved_member_saves_approval_token()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                await controller.RequestPasswordReset(formData);

                _currentMember.Verify(x => x.SetValue("approvalToken", _token, null, null), Times.Once);
                _currentMember.Verify(x => x.SetValue("approvalTokenExpires", _expiryDate, null, null), Times.Once);
                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Once);
            }
        }

        [Fact]
        public async void Unapproved_member_sends_approve_member_email()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));
                _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_SUBJECT, APPROVE_MEMBER_BODY, It.IsAny<Dictionary<string, string>>()))
                    .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
                    {
                        Assert.Equal(_currentMember.Object.Name, tokens["name"]);
                        Assert.Equal(formData.Email, tokens["email"]);
                        Assert.Equal(_token, tokens["token"]);
                        Assert.Equal(REQUEST_URL_AUTHORITY, tokens["domain"]);
                    })
                    .Returns((APPROVE_MEMBER_SUBJECT, APPROVE_MEMBER_BODY));

                await controller.RequestPasswordReset(formData);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(formData.Email, message.To[0]);
                        Assert.Equal(APPROVE_MEMBER_SUBJECT, message.Subject);
                        Assert.Equal(APPROVE_MEMBER_BODY, message.Body);
                    });
            }
        }


        [Fact]
        public async void Unapproved_member_request_is_logged()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);

                await controller.RequestPasswordReset(formData);

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetRequested, _currentMember.Object.Username, _currentMember.Object.Key, typeof(ResetPasswordRequestSurfaceController), nameof(ResetPasswordRequestSurfaceController.RequestPasswordReset)), Times.Once);
            }
        }

        [Fact]
        public async void Unapproved_member_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async void Unapproved_member_sets_email_from_form_data_and_displays_confirmation()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public async void Unapproved_member_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Member_not_found_sends_create_member_email()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);
                _emailFormatter.Setup(x => x.FormatEmailContent(CREATE_MEMBER_SUBJECT, CREATE_MEMBER_BODY, It.IsAny<Dictionary<string, string>>()))
                    .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
                    {
                        Assert.Equal(formData.Email, tokens["email"]);
                        Assert.Equal(REQUEST_URL_AUTHORITY, tokens["domain"]);
                    })
                    .Returns((CREATE_MEMBER_SUBJECT, CREATE_MEMBER_BODY));

                await controller.RequestPasswordReset(formData);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(formData.Email, message.To[0]);
                        Assert.Equal(CREATE_MEMBER_SUBJECT, message.Subject);
                        Assert.Equal(CREATE_MEMBER_BODY, message.Body);
                    });
            }
        }

        [Fact]
        public async void Member_not_found_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async void Member_not_found_sets_email_from_form_data_and_displays_confirmation()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public async void Member_not_found_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public async void Member_not_found_does_not_save()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = await controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
            }
        }
    }
}
