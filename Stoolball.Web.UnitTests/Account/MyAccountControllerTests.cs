using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Account;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class MyAccountControllerTests : UmbracoBaseTest
    {
        public MyAccountControllerTests()
        {
            base.Setup();
        }
        private MyAccountController CreateController()
        {
            return new MyAccountController(
                            Mock.Of<ILogger<MyAccountController>>(),
                            Mock.Of<ICompositeViewEngine>(),
                            UmbracoContextAccessor.Object,
                            Mock.Of<IVariationContextAccessor>(),
                            ServiceContext)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public void Sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                CurrentPage.Setup(x => x.Name).Returns("My account");
                SetupPropertyValue(CurrentPage, "description", "This is the description");

                var result = controller.Index();

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
            using (var controller = CreateController())
            {
                SetupPropertyValue(CurrentPage, "description", string.Empty);

                var result = controller.Index();

                Assert.IsType<MyAccount>(((ViewResult)result).Model);
            }
        }
    }
}
