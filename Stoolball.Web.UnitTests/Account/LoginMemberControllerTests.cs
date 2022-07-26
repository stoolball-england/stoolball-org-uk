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
    public class LoginMemberControllerTests : UmbracoBaseTest
    {
        public LoginMemberControllerTests() : base()
        {
        }

        [Fact]
        public void Index_sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                CurrentPage.Setup(x => x.Name).Returns("Login member");
                SetupPropertyValue(CurrentPage, "description", "This is the description");

                var result = controller.Index();

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(CurrentPage.Object.Name, meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Index_has_content_security_policy_allows_forms()
        {
            var method = typeof(LoginMemberController).GetMethod(nameof(LoginMemberController.Index))!;
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
        public void Index_returns_LoginMember_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                SetupPropertyValue(CurrentPage, "description", string.Empty);

                var result = controller.Index();

                Assert.IsType<LoginMember>(((ViewResult)result).Model);
            }
        }

        private LoginMemberController CreateController()
        {
            return new LoginMemberController(Mock.Of<ILogger<LoginMemberController>>(),
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
