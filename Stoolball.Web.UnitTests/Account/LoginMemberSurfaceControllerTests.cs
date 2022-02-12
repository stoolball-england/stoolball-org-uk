using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Models;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.ActionResults;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class LoginMemberSurfaceControllerTests : UmbracoBaseTest
    {
        public LoginMemberSurfaceControllerTests()
        {
            base.Setup();
        }

        private class TestController<T> : LoginMemberSurfaceController where T : PublishedContentModel
        {
            private readonly T _contentModel;
            private readonly Func<LoginModel, Task<IActionResult>> _handleLogin;

            public TestController(IUmbracoContextAccessor umbracoContextAccessor, HttpContext httpContext, ServiceContext services, T contentModel, Func<LoginModel, Task<IActionResult>> handleLogin)//, IStoolballRouterController stoolballRouterController)
            : base(umbracoContextAccessor,
                Mock.Of<IUmbracoDatabaseFactory>(),
                services,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                Mock.Of<IMemberSignInManager>(),
                Mock.Of<IEmailFormatter>(),
                Mock.Of<IEmailSender>(),
                Mock.Of<IVerificationToken>())
            {
                _contentModel = contentModel;
                _handleLogin = handleLogin;


                ControllerContext = ControllerContext;
                //ControllerContext.RouteData.DataTokens.Add(UmbracoConstants.Web.UmbracoRouteDefinitionDataToken, new RouteDefinition());
            }

            protected override async Task<IActionResult> TryUmbracoLogin(LoginModel model) => await _handleLogin(model);

            protected override IPublishedContent CurrentPage => _contentModel;

            public bool BlockedLoginResultCalled { get; set; }

            protected override IActionResult BlockedLoginResult()
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

        private TestController<LoginMember> CreateController(IActionResult expectedResult)
        {
            return new TestController<LoginMember>(
                UmbracoContextAccessor.Object,
                HttpContext.Object,
                ServiceContext,
                new LoginMember(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
                x => Task.FromResult(expectedResult))
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async void Valid_login_returns_RedirectResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.GetValue<int>("totalLogins", null, null, false)).Returns(0);
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new RedirectResult("/", false)))
            {

                var result = await controller.Login(model);

                Assert.IsType<RedirectResult>(result);
            }
        }


        [Fact]
        public async void Invalid_password_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "invalid-password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
                var result = await controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Invalid_username_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "invalid@example.org", Password = "password", RedirectUrl = "/" };

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns((IMember)null);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
                var result = await controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Locked_out_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                var result = await controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Locked_out_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                _ = await controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }

        [Fact]
        public async void Locked_out_calls_BlockedLoginResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(true);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                _ = await controller.Login(model);

                Assert.True(controller.BlockedLoginResultCalled);
            }
        }

        [Fact]
        public async void Not_approved_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                var result = await controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Not_approved_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                _ = await controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }

        [Fact]
        public async void Not_approved_calls_BlockedLoginResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(false);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                _ = await controller.Login(model);

                Assert.True(controller.BlockedLoginResultCalled);
            }
        }

        [Fact]
        public async void Blocked_returns_UmbracoPageResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                var result = await controller.Login(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Blocked_adds_ModelError()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                _ = await controller.Login(model);

                Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
            }
        }

        [Fact]
        public async void Blocked_calls_BlockedLoginResult()
        {
            var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "/" };

            var member = new Mock<IMember>();
            member.Setup(x => x.IsApproved).Returns(true);
            member.Setup(x => x.IsLockedOut).Returns(false);
            member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

            MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

            using (var controller = CreateController(new UmbracoPageResult(Mock.Of<IProfilingLogger>())))
            {
                _ = await controller.Login(model);

                Assert.True(controller.BlockedLoginResultCalled);
            }
        }

        //[Fact]
        //public async void Valid_login_via_StoolballRouter_returns_RedirectResult()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.GetValue<int>("totalLogins", null, null, false)).Returns(0);
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new RedirectResult("/stoolball-router/", false) as IActionResult)))
        //    //Mock.Of<IStoolballRouterController>()))
        //    {

        //        var result = await controller.Login(model); // ModelState.IsValid = true

        //        Assert.IsType<RedirectResult>(result);
        //    }

        //    var expected = new RedirectResult("https://localhost/some/page", false);
        //}

        //[Fact]
        //public async void Valid_login_via_StoolballRouter_returns_URL_from_model()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.GetValue<int>("totalLogins", null, null, false)).Returns(0);
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new RedirectResult("/stoolball-router/", false) as IActionResult)))
        //    //Mock.Of<IStoolballRouterController>()))
        //    {

        //        var result = (RedirectResult)(await controller.Login(model)); // ModelState.IsValid = true

        //        Assert.Equal(model.RedirectUrl, result.Url);
        //    }
        //}


        //[Fact]
        //public async void Invalid_password_via_StoolballRouter_returns_ViewResult()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "invalid-password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new ViewResult() as IActionResult),
        //        router.Object))
        //    {
        //        controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
        //        var result = await controller.Login(model);

        //        Assert.IsType<ViewResult>(result);
        //    }
        //}

        //[Fact]
        //public async void Invalid_username_via_StoolballRouter_returns_ViewResult()
        //{
        //    var model = new LoginModel { Username = "invalid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns((IMember)null);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        controller.ModelState.AddModelError("loginModel", "Invalid username or password"); // This happens during a real attempt to login
        //        var result = await controller.Login(model);

        //        Assert.IsType<ViewResult>(result);
        //    }
        //}


        //[Fact]
        //public async void Locked_out_via_StoolballRouter_returns_ViewResult()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(true);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        var result = await controller.Login(model);

        //        Assert.IsType<ViewResult>(result);
        //    }
        //}


        //[Fact]
        //public async void Locked_out_via_StoolballRouter_adds_ModelError()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(true);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        _ = await controller.Login(model);

        //        Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
        //    }
        //}


        //[Fact]
        //public async void Not_approved_via_StoolballRouter_returns_ViewResult()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(false);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        var result = await controller.Login(model);

        //        Assert.IsType<ViewResult>(result);
        //    }
        //}

        //[Fact]
        //public async void Not_approved_via_StoolballRouter_addsModelError()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(false);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(false);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        _ = await controller.Login(model);

        //        Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
        //    }
        //}


        //[Fact]
        //public async void Blocked_via_StoolballRouter_returns_ViewResult()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    var router = new Mock<IStoolballRouterController>();
        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        var result = await controller.Login(model);

        //        Assert.IsType<ViewResult>(result);
        //    }
        //}

        //[Fact]
        //public async void Blocked_via_StoolballRouter_adds_ModelError()
        //{
        //    var model = new LoginModel { Username = "valid@example.org", Password = "password", RedirectUrl = "https://localhost/some/page" };

        //    var member = new Mock<IMember>();
        //    member.Setup(x => x.IsApproved).Returns(true);
        //    member.Setup(x => x.IsLockedOut).Returns(false);
        //    member.Setup(x => x.GetValue<bool>("blockLogin", null, null, false)).Returns(true);

        //    MemberService.Setup(x => x.GetByEmail(model.Username)).Returns(member.Object);

        //    router.Setup(x => x.ModelState).Returns(new ModelStateDictionary());
        //    router.SetupProperty(x => x.ControllerContext);
        //    router.Setup(x => x.Index(It.IsAny<ContentModel>())).Returns(Task.FromResult(new ViewResult() as ActionResult));

        //    using (var controller = new TestController<StoolballRouter>(
        //        HttpContext.Object,
        //        ServiceContext,
        //        new StoolballRouter(Mock.Of<IPublishedContent>(), Mock.Of<IPublishedValueFallback>()),
        //        x => Task.FromResult(new UmbracoPageResult(Mock.Of<IProfilingLogger>()) as IActionResult),
        //        router.Object))
        //    {
        //        _ = await controller.Login(model);

        //        Assert.True(controller.ModelState["loginModel"].Errors[0].ErrorMessage == "Invalid username or password");
        //    }
        //}
    }
}
