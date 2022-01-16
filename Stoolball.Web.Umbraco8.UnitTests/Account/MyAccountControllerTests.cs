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
    public class MyAccountControllerTests : UmbracoBaseTest
    {
        public MyAccountControllerTests()
        {
            base.Setup();
        }

        [Fact]
        public void Sets_name_and_description_from_content()
        {
            using (var controller = new MyAccountController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                currentPage.Setup(x => x.Name).Returns("My account");
                SetupPropertyValue(currentPage, "description", "This is the description");

                var result = controller.Index(new ContentModel(currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal("My account", meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(MyAccountController).GetMethod(nameof(MyAccountController.Index));
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
        public void Returns_MyAccount_ModelsBuilder_model()
        {
            using (var controller = new MyAccountController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(currentPage, "description", string.Empty);

                var result = controller.Index(new ContentModel(currentPage.Object));

                Assert.IsType<MyAccount>(((ViewResult)result).Model);
            }
        }
    }
}
