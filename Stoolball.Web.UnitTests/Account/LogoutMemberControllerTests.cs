﻿using System.Linq;
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
    public class LogoutMemberControllerTests : UmbracoBaseTest
    {
        public LogoutMemberControllerTests()
        {
            base.Setup();
        }

        [Fact]
        public void Index_sets_name_and_description_from_content()
        {
            using (var controller = new LogoutMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                currentPage.Setup(x => x.Name).Returns("Logout member");
                SetupPropertyValue(currentPage, "description", "This is the description");

                var result = controller.Index(new ContentModel(currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(currentPage.Object.Name, meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Index_has_content_security_policy()
        {
            var method = typeof(LogoutMemberController).GetMethod(nameof(LogoutMemberController.Index));
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
        public void Index_returns_LogoutMember_ModelsBuilder_model()
        {
            using (var controller = new LogoutMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(currentPage, "description", string.Empty);

                var result = controller.Index(new ContentModel(currentPage.Object));

                Assert.IsType<LogoutMember>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Post_sets_name_and_description_from_content()
        {
            using (var controller = new LogoutMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                currentPage.Setup(x => x.Name).Returns("Logout member");
                SetupPropertyValue(currentPage, "description", "This is the description");

                var result = controller.LogoutMember(new ContentModel(currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(currentPage.Object.Name, meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void Post_has_form_post_attributes()
        {
            var method = typeof(LogoutMemberController).GetMethod(nameof(LogoutMemberController.LogoutMember));

            var httpPostAttribute = method.GetCustomAttributes(typeof(HttpPostAttribute), false).SingleOrDefault();
            Assert.NotNull(httpPostAttribute);

            var antiForgeryTokenAttribute = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).SingleOrDefault();
            Assert.NotNull(antiForgeryTokenAttribute);
        }

        [Fact]
        public void Post_returns_LogoutMember_ModelsBuilder_model()
        {
            using (var controller = new LogoutMemberController(Mock.Of<IGlobalSettings>(),
                            Mock.Of<IUmbracoContextAccessor>(),
                            ServiceContext,
                            AppCaches.NoCache,
                            Mock.Of<IProfilingLogger>(),
                            UmbracoHelper))
            {
                var currentPage = new Mock<IPublishedContent>();
                SetupPropertyValue(currentPage, "description", string.Empty);

                var result = controller.LogoutMember(new ContentModel(currentPage.Object));

                Assert.IsType<LogoutMember>(((ViewResult)result).Model);
            }
        }
    }
}