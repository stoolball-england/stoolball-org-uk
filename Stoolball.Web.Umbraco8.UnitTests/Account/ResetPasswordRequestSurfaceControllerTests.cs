﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Email;
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
    public class ResetPasswordRequestSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new Mock<IMember>();
        private readonly Mock<ILogger> _logger = new Mock<ILogger>();
        private readonly Mock<IEmailFormatter> _emailFormatter = new Mock<IEmailFormatter>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly Mock<IVerificationToken> _tokenReader = new Mock<IVerificationToken>();
        private readonly string _token = Guid.NewGuid().ToString();
        private readonly DateTime _expiryDate = DateTime.Now.AddDays(1);
        private const string CREATE_MEMBER_SUBJECT = "Create an account subject";
        private const string CREATE_MEMBER_BODY = "Create an account body";
        private const string RESET_PASSWORD_SUBJECT = "Reset password subject";
        private const string RESET_PASSWORD_BODY = "Reset password body";
        private const string APPROVE_MEMBER_SUBJECT = "Account not approved subject";
        private const string APPROVE_MEMBER_BODY = "Account not approved body";
        private const string REQUEST_URL_AUTHORITY = "localhost";

        private class TestResetPasswordRequestSurfaceController : ResetPasswordRequestSurfaceController
        {
            private readonly Mock<IPublishedContent> _currentPage;

            public TestResetPasswordRequestSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                HttpContextBase httpContext,
                IEmailFormatter emailFormatter,
                IEmailSender emailSender,
                IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, emailFormatter, emailSender, verificationToken)
            {
                _currentPage = new Mock<IPublishedContent>();
                _currentPage.Setup(x => x.Name).Returns("Reset password");
                SetupPropertyValue(_currentPage, "description", "This is the description");
                SetupPropertyValue(_currentPage, "createMemberSubject", CREATE_MEMBER_SUBJECT);
                SetupPropertyValue(_currentPage, "createMemberBody", CREATE_MEMBER_BODY);
                SetupPropertyValue(_currentPage, "resetPasswordSubject", RESET_PASSWORD_SUBJECT);
                SetupPropertyValue(_currentPage, "resetPasswordBody", RESET_PASSWORD_BODY);
                SetupPropertyValue(_currentPage, "approveMemberSubject", APPROVE_MEMBER_SUBJECT);
                SetupPropertyValue(_currentPage, "approveMemberBody", APPROVE_MEMBER_BODY);

                ControllerContext = new ControllerContext(httpContext, new RouteData(), this);
            }
            protected override IPublishedContent CurrentPage => _currentPage.Object;
        }

        public ResetPasswordRequestSurfaceControllerTests()
        {
            base.Setup();

            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");

            SetupCurrentMember(_currentMember.Object);
        }

        private TestResetPasswordRequestSurfaceController CreateController()
        {
            var controller = new TestResetPasswordRequestSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            _logger.Object,
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            base.HttpContext.Object,
                            _emailFormatter.Object,
                            _emailSender.Object,
                            _tokenReader.Object);

            base.Request.SetupGet(x => x.Url).Returns(new Uri("https://localhost/account/reset-password"));

            return controller;
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
        public void Sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("Reset password", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Invalid_ModelState_sets_email_from_form_data_and_redisplays_form()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.False((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public void Invalid_ModelState_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Invalid_ModelState_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_ModelState_adds_email_error()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True(controller.ModelState.ContainsKey("Email"));
                Assert.Equal("Please enter a valid email address.", controller.ModelState["Email"].Errors.FirstOrDefault()?.ErrorMessage);
            }
        }

        [Fact]
        public void Invalid_ModelState_does_not_save_or_email()
        {
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public void Null_form_data_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var result = controller.RequestPasswordReset(null);

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }


        [Fact]
        public void Null_form_data_redisplays_form()
        {
            using (var controller = CreateController())
            {
                var result = controller.RequestPasswordReset(null);

                Assert.False((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
            }
        }

        [Fact]
        public void Null_form_data_adds_email_error()
        {
            using (var controller = CreateController())
            {
                controller.RequestPasswordReset(null);

                Assert.True(controller.ModelState.ContainsKey("Email"));
                Assert.Equal("Please enter a valid email address.", controller.ModelState["Email"].Errors.FirstOrDefault()?.ErrorMessage);
            }
        }

        [Fact]
        public void Null_form_data_does_not_save_or_email()
        {
            using (var controller = CreateController())
            {
                controller.RequestPasswordReset(null);

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public void Approved_member_saves_reset_token()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                controller.RequestPasswordReset(formData);

                _currentMember.Verify(x => x.SetValue("passwordResetToken", _token, null, null), Times.Once);
                _currentMember.Verify(x => x.SetValue("passwordResetTokenExpires", _expiryDate, null, null), Times.Once);
                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Approved_member_sends_reset_password_email()
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

                controller.RequestPasswordReset(formData);

                _emailSender.Verify(x => x.SendEmail(formData.Email, RESET_PASSWORD_SUBJECT, RESET_PASSWORD_BODY), Times.Once);
            }
        }

        [Fact]
        public void Approved_member_request_is_logged()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);

                controller.RequestPasswordReset(formData);

                _logger.Verify(x => x.Info(typeof(ResetPasswordRequestSurfaceController), LoggingTemplates.MemberPasswordResetRequested, _currentMember.Object.Username, _currentMember.Object.Key, typeof(ResetPasswordRequestSurfaceController), nameof(ResetPasswordRequestSurfaceController.RequestPasswordReset)), Times.Once);
            }
        }

        [Fact]
        public void Approved_member_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Approved_member_sets_email_from_form_data_and_displays_confirmation()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public void Approved_member_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(true);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Unapproved_member_saves_approval_token()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                controller.RequestPasswordReset(formData);

                _currentMember.Verify(x => x.SetValue("approvalToken", _token, null, null), Times.Once);
                _currentMember.Verify(x => x.SetValue("approvalTokenExpires", _expiryDate, null, null), Times.Once);
                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Unapproved_member_sends_approve_member_email()
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

                controller.RequestPasswordReset(formData);

                _emailSender.Verify(x => x.SendEmail(formData.Email, APPROVE_MEMBER_SUBJECT, APPROVE_MEMBER_BODY), Times.Once);
            }
        }


        [Fact]
        public void Unapproved_member_request_is_logged()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);

                controller.RequestPasswordReset(formData);

                _logger.Verify(x => x.Info(typeof(ResetPasswordRequestSurfaceController), LoggingTemplates.MemberPasswordResetRequested, _currentMember.Object.Username, _currentMember.Object.Key, typeof(ResetPasswordRequestSurfaceController), nameof(ResetPasswordRequestSurfaceController.RequestPasswordReset)), Times.Once);
            }
        }

        [Fact]
        public void Unapproved_member_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Unapproved_member_sets_email_from_form_data_and_displays_confirmation()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public void Unapproved_member_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns(_currentMember.Object);
                _currentMember.SetupGet(x => x.IsApproved).Returns(false);
                _tokenReader.Setup(x => x.TokenFor(_currentMember.Object.Id)).Returns((_token, _expiryDate));

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Member_not_found_sends_create_member_email()
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

                controller.RequestPasswordReset(formData);

                _emailSender.Verify(x => x.SendEmail(formData.Email, CREATE_MEMBER_SUBJECT, CREATE_MEMBER_BODY), Times.Once);
            }
        }

        [Fact]
        public void Member_not_found_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Member_not_found_sets_email_from_form_data_and_displays_confirmation()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.True((((ViewResult)result).Model as ResetPassword)?.ShowPasswordResetRequested);
                Assert.Equal("email@example.org", (((ViewResult)result).Model as ResetPassword)?.Email);
            }
        }

        [Fact]
        public void Member_not_found_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Member_not_found_does_not_save()
        {
            using (var controller = CreateController())
            {
                var formData = new ResetPasswordRequestFormData { Email = "email@example.org" };
                MemberService.Setup(x => x.GetByEmail(formData.Email)).Returns((IMember)null);

                var result = controller.RequestPasswordReset(new ResetPasswordRequestFormData { Email = "email@example.org" });

                MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
            }
        }
    }
}