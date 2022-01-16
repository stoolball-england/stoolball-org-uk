using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Web.Account;
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

namespace Stoolball.Web.UnitTests.Account
{
    public class LogoutMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ILogoutMemberWrapper> _logoutMemberWrapper = new Mock<ILogoutMemberWrapper>();

        private class TestLogoutMemberSurfaceController : LogoutMemberSurfaceController
        {
            private readonly Mock<IPublishedContent> _currentPage = new Mock<IPublishedContent>();

            public TestLogoutMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                HttpContextBase httpContext,
                ILogoutMemberWrapper logoutMemberWrapper)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, logoutMemberWrapper)
            {
                ControllerContext = new ControllerContext(httpContext, new RouteData(), this);
            }

            protected override IPublishedContent CurrentPage => _currentPage.Object;

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage()
            {
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }
        }

        public LogoutMemberSurfaceControllerTests()
        {
            base.Setup();
        }


        private TestLogoutMemberSurfaceController CreateController()
        {
            return new TestLogoutMemberSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<ILogger>(),
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            base.HttpContext.Object,
                            _logoutMemberWrapper.Object);
        }


        [Fact]
        public void HandleLogout_has_content_security_policy()
        {
            var method = typeof(LogoutMemberSurfaceController).GetMethod(nameof(LogoutMemberSurfaceController.HandleLogout));
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.False(attribute.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        [Fact]
        public void HandleLogout_has_form_post_attributes()
        {
            var method = typeof(LogoutMemberSurfaceController).GetMethod(nameof(LogoutMemberSurfaceController.HandleLogout));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Logged_in_member_is_logged_out()
        {
            SetupCurrentMember(Mock.Of<IMember>());
            using (var controller = CreateController())
            {
                var result = controller.HandleLogout();

                _logoutMemberWrapper.Verify(x => x.LogoutMember(), Times.Once);
            }
        }

        [Fact]
        public void Logged_in_member_returns_RedirectToUmbracoPageResult()
        {
            SetupCurrentMember(Mock.Of<IMember>());
            using (var controller = CreateController())
            {
                var result = controller.HandleLogout();

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Logged_out_member_does_not_attempt_logout()
        {
            using (var controller = CreateController())
            {
                var result = controller.HandleLogout();

                _logoutMemberWrapper.Verify(x => x.LogoutMember(), Times.Never);
            }
        }

        [Fact]
        public void Logged_out_member_returns_RedirectToUmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                var result = controller.HandleLogout();

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }
    }
}