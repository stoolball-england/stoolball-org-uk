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
    public class HomeControllerTests : UmbracoBaseTest
    {
        private HomeController CreateController()
        {
            return new HomeController(
                Mock.Of<ILogger<HomeController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                ServiceContext)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public void Sets_page_title_from_content_name()
        {
            using var controller = CreateController();
            CurrentPage.Setup(x => x.Name).Returns("Home page");

            var result = controller.Index();

            var meta = ((IHasViewMetadata)((ViewResult)result).Model!).Metadata;
            Assert.Equal("Home page", meta.PageTitle);
        }

        [Fact]
        public void Returns_Home_ModelsBuilder_model()
        {
            using var controller = CreateController();

            var result = controller.Index();

            Assert.IsType<Home>(((ViewResult)result).Model);
        }

        [Fact]
        public void Has_default_content_security_policy()
        {
            var method = typeof(HomeController).GetMethod(nameof(HomeController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.False(attribute!.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GettyImages);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
        }

        [Fact]
        public void Supports_GET_and_HEAD_requests()
        {
            var method = typeof(HomeController).GetMethod(nameof(HomeController.Index))!;

            Assert.NotNull(method.GetCustomAttributes(typeof(HttpGetAttribute), false).SingleOrDefault());
            Assert.NotNull(method.GetCustomAttributes(typeof(HttpHeadAttribute), false).SingleOrDefault());
        }
    }
}
