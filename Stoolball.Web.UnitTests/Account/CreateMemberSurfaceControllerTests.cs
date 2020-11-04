using System.Collections.Generic;
using System.Web.Mvc;
using Moq;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Email;
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
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Web.Tests.Account
{
    public class CreateMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IEmailFormatter> _emailFormatter;
        private readonly Mock<IEmailSender> _emailSender;

        private class TestCreateMemberSurfaceController : CreateMemberSurfaceController
        {
            private readonly Mock<IPublishedContent> _currentPage;

            public TestCreateMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IEmailFormatter emailFormatter, IEmailSender emailSender, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, emailFormatter, emailSender, verificationToken)
            {
                _currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(_currentPage, "approveMemberSubject", "Approve member");
                SetupPropertyValue(_currentPage, "approveMemberBody", "Approve member body");
                SetupPropertyValue(_currentPage, "memberExistsSubject", "Member exists");
                SetupPropertyValue(_currentPage, "memberExistsBody", "Member exists body");
            }

            protected override string GetRequestUrlAuthority()
            {
                return "localhost";
            }

            protected override IPublishedContent CurrentPage => _currentPage.Object;

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage()
            {
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }

            internal bool CreateMemberSucceeds { get; set; }

            internal string EmailFieldError { get; set; }

            protected override ActionResult CreateMemberInUmbraco(RegisterModel model)
            {
                TempData["FormSuccess"] = CreateMemberSucceeds;
                if (!string.IsNullOrEmpty(EmailFieldError))
                {
                    ModelState.AddModelError("registerModel.Email", EmailFieldError);
                }
                return null;
            }
        }

        public CreateMemberSurfaceControllerTests()
        {
            base.Setup();
            _emailFormatter = new Mock<IEmailFormatter>();
            _emailSender = new Mock<IEmailSender>();
        }

        private TestCreateMemberSurfaceController CreateController()
        {
            return new TestCreateMemberSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<ILogger>(),
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            _emailFormatter.Object,
                            _emailSender.Object,
                            Mock.Of<IVerificationToken>());
        }

        [Fact]
        public void New_member_returns_RedirectToUmbracoPageResult()
        {
            var model = RegisterModel.CreateModel();
            MemberService.Setup(x => x.GetByEmail(It.IsAny<string>())).Returns(Mock.Of<IMember>());
            using (var controller = CreateController())
            {
                controller.CreateMemberSucceeds = true;

                var result = controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public void New_member_is_assigned_to_All_Members()
        {
            var model = RegisterModel.CreateModel();
            MemberService.Setup(x => x.GetByEmail(It.IsAny<string>())).Returns(Mock.Of<IMember>());
            MemberService.Setup(x => x.AssignRole(It.IsAny<int>(), Groups.AllMembers));
            using (var controller = CreateController())
            {
                controller.CreateMemberSucceeds = true;

                controller.CreateMember(model);

                MemberService.VerifyAll();
            }
        }

        [Fact]
        public void New_member_sends_Approve_Member_email()
        {
            var model = RegisterModel.CreateModel();
            MemberService.Setup(x => x.GetByEmail(It.IsAny<string>())).Returns(Mock.Of<IMember>());
            _emailFormatter.Setup(x => x.FormatEmailContent("Approve member", "Approve member body", It.IsAny<Dictionary<string, string>>())).Returns(("Approve member", "Approve member body"));

            using (var controller = CreateController())
            {
                controller.CreateMemberSucceeds = true;

                controller.CreateMember(model);

                _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), "Approve member", "Approve member body"));
            }
        }

        [Fact]
        public void Duplicate_email_sends_Member_Exists_email()
        {
            var model = RegisterModel.CreateModel();
            _emailFormatter.Setup(x => x.FormatEmailContent("Member exists", "Member exists body", It.IsAny<Dictionary<string, string>>())).Returns(("Member exists", "Member exists body"));

            using (var controller = CreateController())
            {
                controller.CreateMemberSucceeds = false;
                controller.EmailFieldError = "A member with this username already exists.";

                controller.CreateMember(model);

                _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), "Member exists", "Member exists body"));
            }
        }

        [Fact]
        public void Duplicate_email_returns_RedirectToUmbracoPageResult()
        {
            var model = RegisterModel.CreateModel();
            using (var controller = CreateController())
            {
                controller.CreateMemberSucceeds = false;
                controller.EmailFieldError = "A member with this username already exists.";

                var result = controller.CreateMember(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Email_in_TempData_for_view()
        {
            var model = RegisterModel.CreateModel();
            model.Email = "test@example.org";
            using (var controller = CreateController())
            {
                controller.CreateMember(model);

                Assert.Equal(controller.TempData["Email"], model.Email);
            }
        }
    }
}