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
    public class ContentControllerTests : UmbracoBaseTest
    {
        private ContentController CreateController()
        {
            return new ContentController(
                Mock.Of<ILogger<ContentController>>(),
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
        public void Returns_Content_ModelsBuilder_model()
        {
            using var controller = CreateController();
            SetupPropertyValue(CurrentPage, "description", string.Empty);

            var result = controller.Index();

            Assert.IsType<Content>(((ViewResult)result).Model);
        }

        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(ContentController).GetMethod(nameof(ContentController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute!.Forms);
            Assert.True(attribute.TinyMCE);
            Assert.True(attribute.YouTube);
            Assert.True(attribute.GettyImages);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
        }
    }
}
