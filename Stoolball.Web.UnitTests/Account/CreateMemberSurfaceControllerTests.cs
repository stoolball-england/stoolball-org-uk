using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
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
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class CreateMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ILogger> _logger = new Mock<ILogger>();
        private readonly Mock<IEmailFormatter> _emailFormatter = new Mock<IEmailFormatter>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();
        private readonly Mock<ICreateMemberExecuter> _createMemberExecuter = new Mock<ICreateMemberExecuter>();
        private readonly Mock<IVerificationToken> _tokenReader = new Mock<IVerificationToken>();
        private readonly ActionResult _createMemberSuccessResult = new HttpStatusCodeResult(200);

        private class TestCreateMemberSurfaceController : CreateMemberSurfaceController
        {
            private readonly Mock<IPublishedContent> _currentPage;

            public TestCreateMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                ICreateMemberExecuter createMemberExecuter,
                IEmailFormatter emailFormatter,
                IEmailSender emailSender,
                IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, createMemberExecuter, emailFormatter, emailSender, verificationToken)
            {
                _currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(_currentPage, "approveMemberSubject", "Approve member");
                SetupPropertyValue(_currentPage, "approveMemberBody", "Approve member body");
                SetupPropertyValue(_currentPage, "memberExistsSubject", "Member exists");
                SetupPropertyValue(_currentPage, "memberExistsBody", "Member exists body");
            }

            protected override IPublishedContent CurrentPage => _currentPage.Object;

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage()
            {
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }
        }

        public CreateMemberSurfaceControllerTests()
        {
            base.Setup();
        }

        private TestCreateMemberSurfaceController CreateController(RegisterModel model, bool createMemberSucceeds = false, string emailFieldError = null)
        {
            var profilingLogger = new Mock<IProfilingLogger>();
            var controller = new TestCreateMemberSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            _logger.Object,
                            profilingLogger.Object,
                            base.UmbracoHelper,
                            _createMemberExecuter.Object,
                            _emailFormatter.Object,
                            _emailSender.Object,
                            _tokenReader.Object);

            Request.SetupGet(x => x.Url).Returns(new Uri("https://localhost/account/create"));
            controller.ControllerContext = new ControllerContext(HttpContext.Object, new RouteData(), controller);

            _createMemberExecuter.Setup(x => x.CreateMember(controller.HandleRegisterMember, model))
                .Callback((Func<RegisterModel, ActionResult> executeFunction, RegisterModel modelToExecute) =>
                {
                    controller.TempData["FormSuccess"] = createMemberSucceeds;
                    if (!string.IsNullOrEmpty(emailFieldError))
                    {
                        controller.ModelState.AddModelError("registerModel.Email", emailFieldError);
                    }
                })
                .Returns(_createMemberSuccessResult);

            return controller;
        }

        [Fact]
        public void CreateMember_has_content_security_policy_allows_forms()
        {
            var method = typeof(CreateMemberSurfaceController).GetMethod(nameof(CreateMemberSurfaceController.CreateMember));
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
        public void CreateMember_has_form_post_attributes()
        {
            var method = typeof(CreateMemberSurfaceController).GetMethod(nameof(CreateMemberSurfaceController.CreateMember));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Invalid_ModelState_returns_UmbracoPageResult_and_does_not_attempt_to_create_member()
        {
            var model = RegisterModel.CreateModel();
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                controller.ModelState.AddModelError(string.Empty, "Any error");

                var result = controller.CreateMember(model);

                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Never);
                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Null_model_returns_UmbracoPageResult_and_does_not_attempt_to_create_member()
        {
            RegisterModel model = null;
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                var result = controller.CreateMember(model);

                _createMemberExecuter.Verify(x => x.CreateMember(controller.HandleRegisterMember, model), Times.Never);
                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void New_member_returns_RedirectToUmbracoPageResult()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(Mock.Of<IMember>());
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                var result = controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public void New_member_saves_email_and_token()
        {
            var model = RegisterModel.CreateModel();
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddDays(1);
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            _tokenReader.Setup(x => x.TokenFor(member.Object.Id)).Returns((token, tokenExpiry));
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                var result = controller.CreateMember(model);

                member.Verify(x => x.SetValue("totalLogins", 0, null, null), Times.Once);
                member.Verify(x => x.SetValue("approvalToken", token, null, null), Times.Once);
                member.Verify(x => x.SetValue("approvalTokenExpires", tokenExpiry, null, null), Times.Once);
                base.MemberService.Verify(x => x.Save(member.Object, true), Times.Once);
            }
        }

        [Fact]
        public void New_member_is_assigned_to_All_Members()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);
            MemberService.Setup(x => x.AssignRole(member.Object.Id, Groups.AllMembers));
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                controller.CreateMember(model);

                MemberService.VerifyAll();
            }
        }

        [Fact]
        public void New_member_is_not_logged_in_automatically()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                controller.CreateMember(model);

                _createMemberExecuter.Setup(x => x.CreateMember(controller.HandleRegisterMember, model))
                    .Callback((Func<RegisterModel, ActionResult> executeFunction, RegisterModel modelToExecute) =>
                    {
                        Assert.False(modelToExecute.LoginOnSuccess);
                    });
            }
        }

        [Fact]
        public void New_member_sends_Approve_Member_email()
        {
            var model = RegisterModel.CreateModel();
            model.Name = "Member name";
            model.Email = "test@example.org";

            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            var token = Guid.NewGuid().ToString();
            _tokenReader.Setup(x => x.TokenFor(member.Object.Id)).Returns((token, DateTime.Now));

            _emailFormatter.Setup(x => x.FormatEmailContent("Approve member", "Approve member body", It.IsAny<Dictionary<string, string>>()))
                .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
                {
                    Assert.Equal(model.Name, tokens["name"]);
                    Assert.Equal(model.Email, tokens["email"]);
                    Assert.Equal(token, tokens["token"]);
                    Assert.Equal(Request.Object.Url.Authority, tokens["domain"]);
                })
                .Returns(("Approve member", "Approve member body"));

            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                controller.CreateMember(model);

                _emailSender.Verify(x => x.SendEmail(model.Email, "Approve member", "Approve member body"));
            }
        }

        [Fact]
        public void Creating_a_new_member_is_logged()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";
            var member = new Mock<IMember>();
            member.SetupGet(x => x.Id).Returns(123);
            member.SetupGet(x => x.Username).Returns(model.Email);
            member.SetupGet(x => x.Key).Returns(Guid.NewGuid());

            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);
            using (var controller = CreateController(model, createMemberSucceeds: true))
            {
                controller.CreateMember(model);

                _logger.Verify(x => x.Info(typeof(CreateMemberSurfaceController), LoggingTemplates.CreateMember, member.Object.Username, member.Object.Key, typeof(CreateMemberSurfaceController), nameof(CreateMemberSurfaceController.CreateMember)));
            }
        }

        [Fact]
        public void Duplicate_email_sends_Member_Exists_email()
        {
            var model = RegisterModel.CreateModel();
            model.Name = "Member name";
            model.Email = "test@example.org";

            _emailFormatter.Setup(x => x.FormatEmailContent("Member exists", "Member exists body", It.IsAny<Dictionary<string, string>>()))
               .Callback<string, string, Dictionary<string, string>>((subject, body, tokens) =>
               {
                   Assert.Equal(model.Name, tokens["name"]);
                   Assert.Equal(model.Email, tokens["email"]);
                   Assert.Equal(Request.Object.Url.Authority, tokens["domain"]);
               })
               .Returns(("Member exists", "Member exists body"));

            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "A member with this username already exists."))
            {
                controller.CreateMember(model);

                _emailSender.Verify(x => x.SendEmail(model.Email, "Member exists", "Member exists body"));
            }
        }

        [Fact]
        public void Fail_to_create_member_does_not_save_additional_properties()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";

            var member = new Mock<IMember>();
            MemberService.Setup(x => x.GetByEmail(model.Email)).Returns(member.Object);

            using (var controller = CreateController(model, createMemberSucceeds: false))
            {
                var result = controller.CreateMember(model);

                base.MemberService.Verify(x => x.GetByEmail(model.Email), Times.Never);
                base.MemberService.Verify(x => x.Save(member.Object, true), Times.Never);
            }
        }

        [Fact]
        public void Duplicate_email_returns_RedirectToUmbracoPageResult()
        {
            var model = RegisterModel.CreateModel();
            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "A member with this username already exists."))
            {
                var result = controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Duplicate_email_sets_ViewData_FormSuccess_to_true()
        {
            var model = RegisterModel.CreateModel();
            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "A member with this username already exists."))
            {
                var result = controller.CreateMember(model);

                Assert.True((bool)controller.TempData["FormSuccess"]);
            }
        }

        [Fact]
        public void Other_error_is_added_to_ModelState_and_returns_baseResult()
        {
            var model = RegisterModel.CreateModel();
            using (var controller = CreateController(model, createMemberSucceeds: false, emailFieldError: "Some other error."))
            {
                var result = controller.CreateMember(model);

                Assert.True(controller.ModelState.ContainsKey(string.Empty));
                Assert.Equal("Some other error.", controller.ModelState[string.Empty].Errors[0].ErrorMessage);
                Assert.Equal(_createMemberSuccessResult, result);
            }
        }

        [Fact]
        public void Email_in_TempData_for_view()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";
            using (var controller = CreateController(model))
            {
                controller.CreateMember(model);

                Assert.Equal(controller.TempData["Email"], model.Email);
            }
        }
    }
}