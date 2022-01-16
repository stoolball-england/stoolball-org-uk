using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Email;
using Stoolball.Web.Routing;
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
using Umbraco.Web.PublishedModels;
using Xunit;
using UmbracoConstants = Umbraco.Core.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class LoginMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private class TestController<T> : LoginMemberSurfaceController where T : PublishedContentModel
        {
            private readonly T _contentModel;
            private readonly Func<LoginModel, ActionResult> _handleLogin;

            public TestController(ServiceContext services, UmbracoHelper umbracoHelper, T contentModel, Func<LoginModel, ActionResult> handleLogin, IStoolballRouterController stoolballRouterController)
            : base(Mock.Of<IUmbracoContextAccessor>(),
                Mock.Of<IUmbracoDatabaseFactory>(),
                services,
                AppCaches.NoCache,
                Mock.Of<ILogger>(),
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                Mock.Of<IEmailFormatter>(),
                Mock.Of<IEmailSender>(),
                Mock.Of<IVerificationToken>(),
                stoolballRouterController)
            {
                _contentModel = contentModel;
                _handleLogin = handleLogin;

                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var routeData = new RouteData();
                routeData.DataTokens.Add(UmbracoConstants.Web.UmbracoRouteDefinitionDataToken, new RouteDefinition());

                ControllerContext = new ControllerContext(context.Object, routeData, this);
            }

            protected override ActionResult TryUmbracoLogin(LoginModel model) => _handleLogin(model);

            protected override IPublishedContent CurrentPage => _contentModel;

            public bool BlockedLoginResultCalled { get; set; }

            protected override ActionResult BlockedLoginResult()
            {
                BlockedLoginResultCalled = true;
                return new UmbracoPageResult(Mock.Of<IProfilingLogger>());
            }
        }


        [Fact]
        public void Post_handler_has_content_security_policy_allows_forms()
        {
            var method = typeof(LoginMemberSurfaceController).GetMethod(nameof(LoginMemberSurfaceController.Login));
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
        public void Post_handler_has_form_post_attributes()
        {
            var method = typeof(LoginMemberSurfaceController).GetMethod(nameof(LoginMemberSurfaceController.Login));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Valid_login_returns_RedirectResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.GetValue<int>("totalLogins", null, null, false)).Returns(0);
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new RedirectResult("/", false),
                Mock.Of<IStoolballRouterController>()))
            {

                var result = controller.Login(model);

                Assert.IsType<RedirectResult>(result);
            }
        }

        [Fact]
        public void Invalid_password_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "invalid-password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
                var result = controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Invalid_username_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "invalid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns((IMember)null);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
                var result = controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Locked_out_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                var result = controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Locked_out_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                _ = controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }

        [Fact]
        public void Locked_out_calls_BlockedLoginResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                _ = controller.Login(model);

                Assert.True(controller.BlockedLoginResultCalled);
            }
        }

        [Fact]
        public void Not_approved_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                var result = controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Not_approved_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                _ = controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }

        [Fact]
        public void Not_approved_calls_BlockedLoginResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                _ = controller.Login(model);

                Assert.True(controller.BlockedLoginResultCalled);
            }
        }

        [Fact]
        public void Blocked_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                var result = controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Blocked_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                _ = controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }

        [Fact]
        public void Blocked_calls_BlockedLoginResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<LoginMember>(
                ServiceContext,
                UmbracoHelper,
                new LoginMember(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                Mock.Of<IStoolballRouterController>()))
            {
                _ = controller.Login(model);

                Assert.True(controller.BlockedLoginResultCalled);
            }
        }

        [Fact]
        public void Valid_login_via_StoolballRouter_returns_RedirectResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.GetValue<int>("totalLogins", null, null, false)).Returns(0);
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new RedirectResult("/stoolball-router/", false),
                Mock.Of<IStoolballRouterController>()))
            {

                var result = controller.Login(model); // ModelState.IsValid = true

                Assert.IsType<RedirectResult>(result);
            }

            var expected = new RedirectResult("https://localhost/some/page", false);
        }

        [Fact]
        public void Valid_login_via_StoolballRouter_returns_URL_from_model()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.GetValue<int>("totalLogins", null, null, false)).Returns(0);
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new RedirectResult("/stoolball-router/", false),
                Mock.Of<IStoolballRouterController>()))
            {

                var result = (RedirectResult)controller.Login(model); // ModelState.IsValid = true

                Assert.Equal(model.RedirectUrl, result.Url);
            }
        }


        [Fact]
        public void Invalid_password_via_StoolballRouter_returns_ViewResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "invalid-password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new ViewResult(),
                router.Object))
            {
                controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
                var result = controller.Login(model);

                Assert.IsType<ViewResult>(result);
            }
        }

        [Fact]
        public void Invalid_username_via_StoolballRouter_returns_ViewResult()
        {
            var model = new LoginModel { Username = "invalid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns((IMember)null);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
                var result = controller.Login(model);

                Assert.IsType<ViewResult>(result);
            }
        }


        [Fact]
        public void Locked_out_via_StoolballRouter_returns_ViewResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                var result = controller.Login(model);

                Assert.IsType<ViewResult>(result);
            }
        }


        [Fact]
        public void Locked_out_via_StoolballRouter_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                _ = controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }


        [Fact]
        public void Not_approved_via_StoolballRouter_returns_ViewResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                var result = controller.Login(model);

                Assert.IsType<ViewResult>(result);
            }
        }

        [Fact]
        public void Not_approved_via_StoolballRouter_addsModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                _ = controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }


        [Fact]
        public void Blocked_via_StoolballRouter_returns_ViewResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                var result = controller.Login(model);

                Assert.IsType<ViewResult>(result);
            }
        }

        [Fact]
        public void Blocked_via_StoolballRouter_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

            Setup();
            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);
            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            var router = new Mock<IStoolballRouterController>();
            router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
            router.SetupProperty(x => x.ControllerContext);
            router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

            using (var controller = new TestController<StoolballRouter>(
                ServiceContext,
                UmbracoHelper,
                new StoolballRouter(Mock.Of<IPublishedContent>()),
                x => new UmbracoPageResult(Mock.Of<IProfilingLogger>()),
                router.Object))
            {
                _ = controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }
    }
}
