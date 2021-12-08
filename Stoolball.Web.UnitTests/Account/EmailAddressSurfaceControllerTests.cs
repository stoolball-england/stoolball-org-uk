using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Moq;
using Stoolball.Web.Account;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class EmailAddressSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new Mock<IMember>();
        private readonly Mock<ILogger> _logger = new Mock<ILogger>();
        private const string VALID_PASSWORD = "validPa$$word";

        private class TestEmailAddressSurfaceController : EmailAddressSurfaceController
        {
            public TestEmailAddressSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                HttpContextBase httpContext,
                MembershipProvider membershipProvider)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper, membershipProvider)
            {
                ControllerContext = new ControllerContext(httpContext, new RouteData(), this);
            }

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage()
            {
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }
        }

        public EmailAddressSurfaceControllerTests()
        {
            base.Setup();

            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");

            SetupCurrentMember(_currentMember.Object);
        }

        private TestEmailAddressSurfaceController CreateController()
        {
            var membershipProvider = new Mock<MembershipProvider>();
            membershipProvider.Setup(x => x.ValidateUser(_currentMember.Object.Name, VALID_PASSWORD)).Returns(true);

            return new TestEmailAddressSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            _logger.Object,
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            base.HttpContext.Object,
                            membershipProvider.Object);
        }

        [Fact]
        public void UpdateEmailAddress_has_content_security_policy_allows_forms()
        {
            var method = typeof(EmailAddressSurfaceController).GetMethod(nameof(EmailAddressSurfaceController.UpdateEmailAddress));
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
        public void UpdateEmailAddress_has_form_post_attributes()
        {
            var method = typeof(EmailAddressSurfaceController).GetMethod(nameof(EmailAddressSurfaceController.UpdateEmailAddress));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Valid_request_saves()
        {
            var model = new EmailAddressFormData { RequestedEmail = "new@example.org", Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_logs()
        {
            var model = new EmailAddressFormData { Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                _logger.Verify(x => x.Info(typeof(EmailAddressSurfaceController), LoggingTemplates.MemberEmailAddressRequested, _currentMember.Object.Name, _currentMember.Object.Key, typeof(EmailAddressSurfaceController), nameof(EmailAddressSurfaceController.UpdateEmailAddress)), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_sets_TempData_for_view()
        {
            var model = new EmailAddressFormData { Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public void Valid_request_returns_RedirectToUmbracoPageResult()
        {
            var model = new EmailAddressFormData { Password = VALID_PASSWORD };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        public void Invalid_password_sets_ModelState(string password)
        {
            var model = new EmailAddressFormData { Password = password };
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(model);

                Assert.True(controller.ModelState.ContainsKey("formData." + nameof(model.Password)));
                Assert.Equal("Your password is incorrect. Enter your current password.", controller.ModelState["formData." + nameof(model.Password)].Errors[0].ErrorMessage);
            }
        }

        [Fact]
        public void Invalid_model_does_not_save_or_set_TempData()
        {
            var model = new EmailAddressFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = controller.UpdateEmailAddress(model);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public void Invalid_model_returns_UmbracoPageResult()
        {
            var model = new EmailAddressFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError(string.Empty, "Something is invalid");
                var result = controller.UpdateEmailAddress(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Null_model_does_not_save_or_set_TempData()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(null);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public void Null_model_returns_UmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdateEmailAddress(null);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }
    }
}
