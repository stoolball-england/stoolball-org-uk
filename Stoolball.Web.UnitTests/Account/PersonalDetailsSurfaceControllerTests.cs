using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Stoolball.Logging;
using Stoolball.Web.Account;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.ActionResults;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class PersonalDetailsSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMember> _currentMember = new();
        private readonly Mock<ILogger<PersonalDetailsSurfaceController>> _logger = new();

        public PersonalDetailsSurfaceControllerTests() : base()
        {
            _currentMember.Setup(x => x.Id).Returns(123);
            _currentMember.Setup(x => x.Key).Returns(Guid.NewGuid());
            _currentMember.Setup(x => x.Name).Returns("Current Member");

            SetupCurrentMember(_currentMember.Object);
        }


        private PersonalDetailsSurfaceController CreateController()
        {
            var memberManager = new Mock<IMemberManager>();
            memberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult(new MemberIdentityUser { Key = _currentMember.Object.Key }));

            MemberService.Setup(x => x.GetByKey(_currentMember.Object.Key)).Returns(_currentMember.Object);

            return new PersonalDetailsSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                base.ServiceContext,
                AppCaches.NoCache,
                _logger.Object,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                memberManager.Object)
            {
                ControllerContext = ControllerContext,
                TempData = new TempDataDictionary(HttpContext.Object, Mock.Of<ITempDataProvider>())
            };
        }


        [Fact]
        public void UpdatePersonalDetails_has_content_security_policy_allows_forms()
        {
            var method = typeof(PersonalDetailsSurfaceController).GetMethod(nameof(PersonalDetailsSurfaceController.UpdatePersonalDetails))!;
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
        public void UpdatePersonalDetails_has_form_post_attributes()
        {
            var method = typeof(PersonalDetailsSurfaceController).GetMethod(nameof(PersonalDetailsSurfaceController.UpdatePersonalDetails))!;

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);

            var umbracoRouteAttribute = method.GetCustomAttributes(typeof(ValidateUmbracoFormRouteStringAttribute), false).SingleOrDefault();
            Assert.NotNull(umbracoRouteAttribute);
        }

        [Fact]
        public async void Valid_request_updates_name_from_model_and_saves()
        {
            var model = new PersonalDetailsFormData { Name = "Requested name" };
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePersonalDetails(model);

                _currentMember.VerifySet(x => x.Name = model.Name, Times.Once);
                base.MemberService.Verify(x => x.Save(_currentMember.Object), Times.Once);
            }
        }

        [Fact]
        public async void Valid_request_logs_with_original_member_name()
        {
            var model = new PersonalDetailsFormData { Name = "Requested name" };
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePersonalDetails(model);

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPersonalDetailsUpdated, _currentMember.Object.Name, _currentMember.Object.Key, typeof(PersonalDetailsSurfaceController), nameof(PersonalDetailsSurfaceController.UpdatePersonalDetails)), Times.Once);
            }
        }

        [Fact]
        public async void Valid_request_sets_TempData_for_view()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePersonalDetails(model);

                Assert.Equal(true, controller.TempData["Success"]);
            }
        }

        [Fact]
        public async void Valid_request_returns_RedirectToUmbracoPageResult()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePersonalDetails(model);

                Assert.IsType<RedirectToUmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Invalid_model_does_not_save_or_set_TempData()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError("Name", "Name is required");
                var result = await controller.UpdatePersonalDetails(model);

                base.MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public async void Invalid_model_returns_UmbracoPageResult()
        {
            var model = new PersonalDetailsFormData();
            using (var controller = CreateController())
            {
                controller.ModelState.AddModelError("Name", "Name is required");
                var result = await controller.UpdatePersonalDetails(model);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }

        [Fact]
        public async void Null_model_does_not_save_or_set_TempData()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePersonalDetails(null);

                base.MemberService.Verify(x => x.Save(_currentMember.Object), Times.Never);
                Assert.False(controller.TempData.ContainsKey("Success"));
            }
        }

        [Fact]
        public async void Null_model_returns_UmbracoPageResult()
        {
            using (var controller = CreateController())
            {
                var result = await controller.UpdatePersonalDetails(null);

                Assert.IsType<UmbracoPageResult>(result);
            }
        }
    }
}