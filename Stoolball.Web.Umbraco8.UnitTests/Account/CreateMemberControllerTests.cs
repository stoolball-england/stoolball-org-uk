using System.Linq;
using System.Web.Mvc;
using Moq;
using Stoolball.Web.Account;
using Stoolball.Web.Metadata;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.PublishedModels;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class CreateMemberControllerTests : UmbracoBaseTest
    {
        public CreateMemberControllerTests()
        {
            base.Setup();
        }

        [Fact]
        public void Index_sets_name_and_description_from_content()
        {
            using (var controller = new CreateMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                currentPage.Setup(x => x.Name).Returns("Create member");
                SetupPropertyValue(currentPage, "description", "This is the description");

                var result = controller.Index(new ContentModel(currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("Create member", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Index_has_content_security_policy_allows_forms()
        {
            var method = typeof(CreateMemberController).GetMethod(nameof(CreateMemberController.Index));
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
        public void Index_returns_CreateMember_ModelsBuilder_model()
        {
            using (var controller = new CreateMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(currentPage, "description", string.Empty);

                var result = controller.Index(new ContentModel(currentPage.Object));

                Assert.IsType<CreateMember>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Post_sets_name_and_description_from_content()
        {
            using (var controller = new CreateMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                currentPage.Setup(x => x.Name).Returns("Email address");
                SetupPropertyValue(currentPage, "description", "This is the description");

                var result = controller.CreateMember(new ContentModel(currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("Email address", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Post_has_form_post_attributes()
        {
            var method = typeof(CreateMemberController).GetMethod(nameof(CreateMemberController.CreateMember));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);
        }

        [Fact]
        public void Post_returns_CreateMember_ModelsBuilder_model()
        {
            using (var controller = new CreateMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(currentPage, "description", string.Empty);

                var result = controller.CreateMember(new ContentModel(currentPage.Object));

                Assert.IsType<CreateMember>(((ViewResult)result).Model);
            }
        }
    }
}
