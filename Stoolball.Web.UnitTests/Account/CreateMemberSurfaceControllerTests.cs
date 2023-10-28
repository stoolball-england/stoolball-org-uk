using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Stoolball.Email;
using Stoolball.Logging;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.ActionResults;
using Umbraco.Cms.Web.Website.Models;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class CreateMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ILogger<CreateMemberSurfaceController>> _logger = new Mock<ILogger<CreateMemberSurfaceController>>();
        private readonly Mock<IEmailFormatter> _emailFormatter = new Mock<IEmailFormatter>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly Mock<ICreateMemberExecuter> _createMemberExecuter = new Mock<ICreateMemberExecuter>();
        private readonly Mock<IVerificationToken> _tokenReader = new Mock<IVerificationToken>();
        private readonly Mock<IMemberSignInManager> _memberSignInManager = new Mock<IMemberSignInManager>();
        private readonly IActionResult _createMemberSuccessResult = new StatusCodeResult(200);
        private const string APPROVE_MEMBER_EMAIL_SUBJECT = "Approve member";
        private const string APPROVE_MEMBER_EMAIL_BODY = "Approve member body";
        private const string MEMBER_EXISTS_EMAIL_SUBJECT = "Member exists";
        private const string MEMBER_EXISTS_EMAIL_BODY = "Member exists body";

        private CreateMemberSurfaceController CreateController(RegisterModel model, bool createMemberSucceeds = false, string? emailFieldError = null)
        {
            var controller = new CreateMemberSurfaceController(UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                _logger.Object,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                Mock.Of<IMemberManager>(),
                MemberService.Object,
                _memberSignInManager.Object,
                Mock.Of<IScopeProvider>(),
                _createMemberExecuter.Object,
                _emailFormatter.Object,
                _emailSender.Object,
                _tokenReader.Object)
            {
                ControllerContext = ControllerContext,
                TempData = new TempDataDictionary(HttpContext.Object, Mock.Of<ITempDataProvider>())
            };

            Request.SetupGet(x => x.Path).Returns(new PathString("/account/create"));

            SetupPropertyValue(CurrentPage, "approveMemberSubject", APPROVE_MEMBER_EMAIL_SUBJECT);
            SetupPropertyValue(CurrentPage, "approveMemberBody", APPROVE_MEMBER_EMAIL_BODY);
            SetupPropertyValue(CurrentPage, "memberExistsSubject", MEMBER_EXISTS_EMAIL_SUBJECT);
            SetupPropertyValue(CurrentPage, "memberExistsBody", MEMBER_EXISTS_EMAIL_BODY);

            _createMemberExecuter.Setup(x => x.CreateMember(controller.HandleRegisterMember, model))
                .Callback((Func<RegisterModel, Task<IActionResult>> executeFunction, RegisterModel modelToExecute) =>
                {
                    controller.TempData["FormSuccess"] = createMemberSucceeds;
                    if (!string.IsNullOrEmpty(emailFieldError))
                    {
                        controller.ModelState.AddModelError("registerModel.Email", emailFieldError);
                    }
                })
                .Returns(Task.FromResult(_createMemberSuccessResult));

            return controller;
        }

        [Fact]
        public void CreateMember_has_content_security_policy_allows_forms()
        {
            var method = typeof(CreateMemberSurfaceController).GetMethod(nameof(CreateMemberSurfaceController.CreateMember))!;
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
        public void CreateMember_has_form_post_attributes()
        {
            var method = typeof(CreateMemberSurfaceController).GetMethod(nameof(CreateMemberSurfaceController.CreateMember))!;

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_UmbracoPageResult_and_does_not_attempt_to_create_member()
        {
            var model = new CreateMemberFormData();
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = await controller.CreateMember(model);

                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Never);
                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async Task Null_model_returns_UmbracoPageResult_and_does_not_attempt_to_create_member()
        {
#nullable disable
            CreateMemberFormData model = null;
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                var result = await controller.CreateMember(model);

                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Never);
                Assert.IsType<UmbracoPageResult>(result);
            }
#nullable enable
        }

        [Fact]
        public async Task New_member_returns_RedirectToUmbracoPageResult()
        {
            var model = new CreateMemberFormData();
            model.Email = "test@example.org";
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(Mock.Of<IMember>());

            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                var result = await controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public async Task New_member_saves_email_and_token()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddDays(1);
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            _tokenReader.Setup(x => x.TokenFor(member.Object.Id)).Returns((token, tokenExpiry));
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                var result = await controller.CreateMember(model);

                member.Verify(x => x.SetValue("totalLogins", 0, null, null), Times.Once);
                member.Verify(x => x.SetValue("approvalToken", token, null, null), Times.Once);
                member.Verify(x => x.SetValue("approvalTokenExpires", tokenExpiry, null, null), Times.Once);
                base.MemberService.Verify(x => x.Save(member.Object), Times.Once);
            }
        }

        [Fact]
        public async Task New_member_is_assigned_to_All_Members()
        {
            var model = new CreateMemberFormData();
            model.Email = "test@example.org";
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);
            MemberService.Setup(x => x.AssignRole(member.Object.Id, Groups.AllMembers));

            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                await controller.CreateMember(model);

                MemberService.VerifyAll();
            }
        }

        [Fact]
        public async Task New_member_is_not_logged_in_automatically()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                await controller.CreateMember(model);

                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Once);
                _memberSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
            }
        }

        [Fact]
        public async Task New_member_sends_Approve_Member_email()
        {
            var model = new CreateMemberFormData();
            model.Name = "Member name";
            model.Email = "test@example.org";

            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            var token = Guid.NewGuid().ToString();
            _tokenReader.Setup(x => x.TokenFor(member.Object.Id)).Returns((token, DateTime.Now));

            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
                {
                    Assert.Equal(model.Name, tokens["name"]);
                    Assert.Equal(model.Email, tokens["email"]);
                    Assert.Equal(token, tokens["token"]);
                    Assert.Equal(Request.Object.Host.Value, tokens["domain"]);
                })
                .Returns((APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                await controller.CreateMember(model);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(model.Email, message.To[0]);
                        Assert.Equal(APPROVE_MEMBER_EMAIL_SUBJECT, message.Subject);
                        Assert.Equal(APPROVE_MEMBER_EMAIL_BODY, message.Body);
                    });
            }
        }

        [Fact]
        public async Task Creating_a_new_member_is_logged()
        {
            var model = new CreateMemberFormData();
            model.Email = "test@example.org";
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            member.SetupGet(x => x.Username).Returns(model.Email);
            member.SetupGet(x => x.Key).Returns(Guid.NewGuid());

            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            _emailFormatter.Setup(x => x.FormatEmailContent(APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((APPROVE_MEMBER_EMAIL_SUBJECT, APPROVE_MEMBER_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                await controller.CreateMember(model);

                _logger.Verify(x => x.Info(LoggingTemplates.CreateMember, member.Object.Username, member.Object.Key, typeof(CreateMemberSurfaceController), nameof(CreateMemberSurfaceController.CreateMember)));
            }
        }

        [Fact]
        public async Task Fail_to_create_member_does_not_save_additional_properties()
        {
            var model = new CreateMemberFormData();
            model.Email = "test@example.org";

            var member = new Mock<IMember>();
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            using (var controller = CreateController(model, createMemberSucceeds: false))
            {
                var result = await controller.CreateMember(model);

                base.MemberService.Verify(x => x.GetByEmail(model.Email), Times.Never);
                base.MemberService.Verify(x => x.Save(member.Object), Times.Never);
            }
        }

        [Fact]
        public async Task Duplicate_email_sends_Member_Exists_email()
        {
            var model = new CreateMemberFormData();
            model.Name = "Member name";
            model.Email = "test@example.org";

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
               .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
               {
                   Assert.Equal(model.Name, tokens["name"]);
                   Assert.Equal(model.Email, tokens["email"]);
                   Assert.Equal(Request.Object.Host.Value, tokens["domain"]);
               })
               .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "Username 'test@example.org' is already taken"))
            {
                await controller.CreateMember(model);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                {
                    Assert.Single(message.To);
                    Assert.Equal(model.Email, message.To[0]);
                    Assert.Equal(MEMBER_EXISTS_EMAIL_SUBJECT, message.Subject);
                    Assert.Equal(MEMBER_EXISTS_EMAIL_BODY, message.Body);
                });
            }
        }


        [Fact]
        public async Task Duplicate_email_returns_RedirectToUmbracoPageResult()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "Username 'test@example.org' is already taken"))
            {
                var result = await controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public async Task Duplicate_email_sets_ViewData_FormSuccess_to_true()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));

            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "Username 'test@example.org' is already taken"))
            {
                var result = await controller.CreateMember(model);

                Assert.True((bool)controller.TempData["FormSuccess"]);
            }
        }

        [Fact]
        public async Task Email_matching_requested_email_within_expiry_period_sends_Member_Exists_email()
        {
            var model = new CreateMemberFormData();
            model.Name = "Member name";
            model.Email = "test@example.org";

            var otherMember = new Mock<IMember>();
            var expiryDate = DateTime.Now.AddHours(12);
            otherMember.Setup(x => x.GetValue<DateTime>("requestedEmailTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);
            base.MemberService.Setup(x => x.GetMembersByPropertyValue("requestedEmail", model.Email, StringPropertyMatchType.Exact)).Returns(new[] { otherMember.Object });

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
               .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
               {
                   Assert.Equal(model.Name, tokens["name"]);
                   Assert.Equal(model.Email, tokens["email"]);
                   Assert.Equal(Request.Object.Host.Value, tokens["domain"]);
               })
               .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));

            using (var controller = CreateController(model))
            {
                await controller.CreateMember(model);

                _emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), null), Times.Once);
                _emailSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), null))
                    .Callback<EmailMessage, string>((message, emailType) =>
                    {
                        Assert.Single(message.To);
                        Assert.Equal(model.Email, message.To[0]);
                        Assert.Equal(MEMBER_EXISTS_EMAIL_SUBJECT, message.Subject);
                        Assert.Equal(MEMBER_EXISTS_EMAIL_BODY, message.Body);
                    });
            }
        }

        [Fact]
        public async Task Email_matching_requested_email_within_expiry_period_returns_RedirectToUmbracoPageResult()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };

            var otherMember = new Mock<IMember>();
            var expiryDate = DateTime.Now.AddHours(12);
            otherMember.Setup(x => x.GetValue<DateTime>("requestedEmailTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);
            base.MemberService.Setup(x => x.GetMembersByPropertyValue("requestedEmail", model.Email, StringPropertyMatchType.Exact)).Returns(new[] { otherMember.Object });

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));

            using (var controller = CreateController(model))
            {
                var result = await controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public async Task Email_matching_requested_email_within_expiry_period_sets_ViewData_FormSuccess_to_true()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };

            var otherMember = new Mock<IMember>();
            var expiryDate = DateTime.Now.AddHours(12);
            otherMember.Setup(x => x.GetValue<DateTime>("requestedEmailTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);
            base.MemberService.Setup(x => x.GetMembersByPropertyValue("requestedEmail", model.Email, StringPropertyMatchType.Exact)).Returns(new[] { otherMember.Object });

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));

            using (var controller = CreateController(model))
            {
                var result = await controller.CreateMember(model);

                Assert.True((bool)controller.TempData["FormSuccess"]);
            }
        }

        [Fact]
        public async Task Email_matching_requested_email_within_expiry_period_does_not_attempt_to_create_member()
        {
            var model = new CreateMemberFormData { Email = "test@example.org" };

            var otherMember = new Mock<IMember>();
            var expiryDate = DateTime.Now.AddHours(12);
            otherMember.Setup(x => x.GetValue<DateTime>("requestedEmailTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);
            base.MemberService.Setup(x => x.GetMembersByPropertyValue("requestedEmail", model.Email, StringPropertyMatchType.Exact)).Returns(new[] { otherMember.Object });

            _emailFormatter.Setup(x => x.FormatEmailContent(MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY, It.IsAny<Dictionary<string, string>>()))
                .Returns((MEMBER_EXISTS_EMAIL_SUBJECT, MEMBER_EXISTS_EMAIL_BODY));


            using (var controller = CreateController(model))
            {
                var result = await controller.CreateMember(model);

                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Never);
            }
        }

        [Fact]
        public async Task Email_matching_requested_email_past_expiry_period_attempts_to_create_member()
        {
            var model = new CreateMemberFormData();

            var otherMember = new Mock<IMember>();
            var expiryDate = DateTime.Now.AddHours(-12);
            otherMember.Setup(x => x.GetValue<DateTime>("requestedEmailTokenExpires", null, null, false)).Returns(expiryDate);
            _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);
            base.MemberService.Setup(x => x.GetMembersByPropertyValue("requestedEmail", model.Email, StringPropertyMatchType.Exact)).Returns(new[] { otherMember.Object });

            using (var controller = CreateController(model))
            {
                var result = await controller.CreateMember(model);

                _tokenReader.Verify(x => x.HasExpired(expiryDate), Times.Once);
                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Once);
            }
        }


        [Fact]
        public async Task Other_error_is_added_to_ModelState_and_returns_baseResult()
        {
            var model = new CreateMemberFormData();
            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "Some other error."))
            {
                var result = await controller.CreateMember(model);

                Assert.True(controller.ModelState.ContainsKey(string.Empty));
                Assert.Equal("Some other error.", controller.ModelState[string.Empty].Errors[0].ErrorMessage);
                Assert.Equal(_createMemberSuccessResult, result);
            }
        }

        [Fact]
        public async Task Email_in_TempData_for_view()
        {
            var model = new CreateMemberFormData();
            model.Email = "test@example.org";
            using (var controller = CreateController(model))
            {
                await controller.CreateMember(model);

                Assert.Equal(controller.TempData["Email"], model.Email);
            }
        }
    }
}