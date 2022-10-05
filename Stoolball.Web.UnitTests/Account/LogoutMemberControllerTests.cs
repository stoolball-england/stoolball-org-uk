using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Account;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class LogoutMemberControllerTests : UmbracoBaseTest
    {
        [Fact]
        public void Index_sets_name_and_description_from_content()
        {
            var contextAccessor = new Mock<IUmbracoContextAccessor>();

            using (var controller = CreateController())
            {
                CurrentPage.Setup(x => x.Name).Returns("Logout member");
                SetupPropertyValue(CurrentPage, "description", "This is the description");

                var result = controller.Index();

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(CurrentPage.Object.Name, meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Index_has_content_security_policy()
        {
            var method = typeof(LogoutMemberController).GetMethod(nameof(LogoutMemberController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.False(attribute!.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        [Fact]
        public void Index_returns_LogoutMember_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                SetupPropertyValue(CurrentPage, "description", string.Empty);

                var result = controller.Index();

                Assert.IsType<LogoutMember>(((ViewResult)result).Model);
            }
        }

        private LogoutMemberController CreateController()
        {
            return new LogoutMemberController(Mock.Of<ILogger<LogoutMemberController>>(),
                            Mock.Of<ICompositeViewEngine>(),
                            UmbracoContextAccessor.Object,
                            Mock.Of<IVariationContextAccessor>(),
                            ServiceContext)
            {
                ControllerContext = ControllerContext
            };
        }
    }
}
