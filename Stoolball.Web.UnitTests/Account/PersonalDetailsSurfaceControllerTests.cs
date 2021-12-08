using System;
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
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class PersonalDetailsSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new Mock<IMember>();
        private readonly Mock<ILogger> _logger = new Mock<ILogger>();

        private class TestPersonalDetailsSurfaceController : PersonalDetailsSurfaceController
        {
            public TestPersonalDetailsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
                IUmbracoDatabaseFactory databaseFactory,
                ServiceContext services,
                AppCaches appCaches,
                ILogger logger,
                IProfilingLogger profilingLogger,
                UmbracoHelper umbracoHelper,
                HttpContextBase httpContext)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
            {
                ControllerContext = new ControllerContext(httpContext, new RouteData(), this);
            }

            protected override RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage()
            {
                return new RedirectToUmbracoPageResult(0, UmbracoContextAccessor);
            }
        }

        public PersonalDetailsSurfaceControllerTests()
        {
            base.Setup();

            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");

            SetupCurrentMember(_currentMember.Object);
        }


        private TestPersonalDetailsSurfaceController CreateController()
        {
            return new TestPersonalDetailsSurfaceController(Mock.Of<IUmbracoContextAccessor>(),
                            Mock.Of<IUmbracoDatabaseFactory>(),
                            base.ServiceContext,
                            AppCaches.NoCache,
                            _logger.Object,
                            Mock.Of<IProfilingLogger>(),
                            base.UmbracoHelper,
                            base.HttpContext.Object);
        }


        [Fact]
        public void UpdatePersonalDetails_has_content_security_policy_allows_forms()
        {
            var method = typeof(PersonalDetailsSurfaceController).GetMethod(nameof(PersonalDetailsSurfaceController.UpdatePersonalDetails));
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
        public void UpdatePersonalDetails_has_form_post_attributes()
        {
            var method = typeof(PersonalDetailsSurfaceController).GetMethod(nameof(PersonalDetailsSurfaceController.UpdatePersonalDetails));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public void Valid_request_updates_name_from_model_and_saves()
        {
            var model = new PersonalDetailsFormData { Name = "Requested name" };
            using (var controller = CreateController())
            {
                var result = controller.UpdatePersonalDetails(model);

                _currentMember.VerifySet(x => x.Name = model.Name, Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_logs_with_original_member_name()
        {
            var model = new PersonalDetailsFormData { Name = "Requested name" };
            using (var controller = CreateController())
            {
                var result = controller.UpdatePersonalDetails(model);

                _logger.Verify(x => x.Info(typeof(PersonalDetailsSurfaceController), LoggingTemplates.MemberPersonalDetailsUpdated, _currentMember.Object.Name, _currentMember.Object.Key, typeof(PersonalDetailsSurfaceController), nameof(PersonalDetailsSurfaceController.UpdatePersonalDetails)), Times.Once);
            }
        }

        [Fact]
        public void Valid_request_sets_TempData_for_view()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                var result = controller.UpdatePersonalDetails(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public void Valid_request_returns_RedirectToUmbracoPageResult()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                var result = controller.UpdatePersonalDetails(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Invalid_model_does_not_save_or_set_TempData()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError("Name", "Name is required");
                var result = controller.UpdatePersonalDetails(model);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public void Invalid_model_returns_UmbracoPageResult()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError("Name", "Name is required");
                var result = controller.UpdatePersonalDetails(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public void Null_model_does_not_save_or_set_TempData()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdatePersonalDetails(null);

                base.MemberService.Verify(x => x.Save(_currentMember.Object, true), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public void Null_model_returns_UmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                var result = controller.UpdatePersonalDetails(null);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }
    }
}