using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Models;
using Stoolball.Web.Pages;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Xunit;

namespace Stoolball.Web.UnitTests.Pages
{
    public class ProductListControllerTests : UmbracoBaseTest
    {
        private ProductListController CreateController()
        {
            return new ProductListController(
                Mock.Of<ILogger<ProductListController>>(),
                CompositeViewEngine.Object,
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
            using var controller = CreateController();
            CurrentPage.Setup(x => x.Name).Returns("Page name");
            SetupPropertyValue(CurrentPage, "description", "This is the description");

            var result = controller.Index();

            var meta = ((IHasViewMetadata)((ViewResult)result).Model!).Metadata;
            Assert.Equal("Page name", meta.PageTitle);
            Assert.Equal("This is the description", meta.Description);
        }

        [Fact]
        public void Returns_ProductList_ModelsBuilder_model()
        {
            using var controller = CreateController();
            SetupPropertyValue(CurrentPage, "description", string.Empty);

            var result = controller.Index();

            Assert.IsType<ProductList>(((ViewResult)result).Model);
        }

        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(ProductListController).GetMethod(nameof(ProductListController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.False(attribute!.Forms);
            Assert.True(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GettyImages);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
        }
    }
}
