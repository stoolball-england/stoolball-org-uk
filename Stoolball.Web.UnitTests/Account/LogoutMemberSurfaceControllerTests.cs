using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Web.Account;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.ActionResults;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class LogoutMemberSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberSignInManager> _memberSignInManager = new Mock<IMemberSignInManager>();

        public LogoutMemberSurfaceControllerTests()
        {
            base.Setup();
        }

        private LogoutMemberSurfaceController CreateController()
        {
            return new LogoutMemberSurfaceController(UmbracoContextAccessor.Object,
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            Mock.Of<IPublishedUrlProvider>(),
                            _memberSignInManager.Object)
            {
                ControllerContext = ControllerContext
            };
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
        public async void Logged_in_member_is_logged_out()
        {
            SetupCurrentMember(Mock.Of<IMember>());
            using (var controller = CreateController())
            {
                var result = await controller.HandleLogout();

                _memberSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
            }
        }

        [Fact]
        public async void Logged_in_member_returns_RedirectToUmbracoPageResult()
        {
            SetupCurrentMember(Mock.Of<IMember>());
            using (var controller = CreateController())
            {
                var result = await controller.HandleLogout();

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Logged_out_member_does_not_attempt_logout()
        {
            using (var controller = CreateController())
            {
                var result = await controller.HandleLogout();

                _memberSignInManager.Verify(x => x.SignOutAsync(), Times.Never);
            }
        }

        [Fact]
        public async void Logged_out_member_returns_RedirectToUmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                var result = await controller.HandleLogout();

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }
    }
}