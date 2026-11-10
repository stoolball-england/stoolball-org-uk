using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Configuration;
using Stoolball.Web.Models;
using Stoolball.Web.Pages;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Xunit;

namespace Stoolball.Web.UnitTests.Pages
{
    public class StyleGuideControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();

        private StyleGuideController CreateController()
        {
            return new StyleGuideController(
                Mock.Of<ILogger<StyleGuideController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                ServiceContext,
                _apiKeyProvider.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public void Sets_page_title_from_content_name()
        {
            using var controller = CreateController();
            CurrentPage.Setup(x => x.Name).Returns("Style guide");

            var result = controller.Index();

            var meta = ((IHasViewMetadata)((ViewResult)result).Model!).Metadata;
            Assert.Equal("Style guide", meta.PageTitle);
        }

        [Fact]
        public void Sets_GoogleMapsApiKey_from_provider()
        {
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("maps-key");
            using var controller = CreateController();

            var result = controller.Index();

            var model = Assert.IsType<StyleGuide>(((ViewResult)result).Model);
            Assert.Equal("maps-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(StyleGuideController).GetMethod(nameof(StyleGuideController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute!.GoogleMaps);
            Assert.True(attribute.TinyMCE);
            Assert.True(attribute.Forms);
            Assert.True(attribute.GettyImages);
            Assert.True(attribute.YouTube);
            Assert.False(attribute.GoogleGeocode);
        }
    }
}
