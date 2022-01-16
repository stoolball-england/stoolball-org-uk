using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Metadata;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.PublishedModels;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IVerificationToken> _tokenReader = new Mock<IVerificationToken>();
        private readonly Mock<IPublishedContent> _currentPage = new Mock<IPublishedContent>();
        private readonly Mock<IMember> _member = new Mock<IMember>();
        private readonly Mock<IProfilingLogger> _logger = new Mock<IProfilingLogger>();
        private string _token = Guid.NewGuid().ToString();

        public ResetPasswordControllerTests()
        {
            base.Setup();

            _currentPage.Setup(x => x.Name).Returns("Reset password");
            SetupPropertyValue(_currentPage, "description", "This is the description");

            _tokenReader.Setup(x => x.ExtractId(_token)).Returns(123);
            base.MemberService.Setup(x => x.GetById(123)).Returns(_member.Object);
        }

        [Fact]
        public void Has_content_security_policy_allows_forms()
        {
            var method = typeof(ResetPasswordController).GetMethod(nameof(ResetPasswordController.Index));
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        private ResetPasswordController CreateController()
        {
            var controller = new ResetPasswordController(Mock.Of<IGlobalSettings>(),
                                        Mock.Of<IUmbracoContextAccessor>(),
                                        ServiceContext,
                                        AppCaches.NoCache,
                                        _logger.Object,
                                        UmbracoHelper,
                                        _tokenReader.Object);

            controller.ControllerContext = new ControllerContext(base.HttpContext.Object, new RouteData(), controller);

            return controller;
        }

        [Fact]
        public void No_token_sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index(new ContentModel(_currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(_currentPage.Object.Name, meta.PageTitle);
                Assert.Equal(_currentPage.Object.Value("description"), meta.Description);
            }
        }

        [Fact]
        public void No_token_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void No_token_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Valid_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.True(((ResetPassword)((ViewResult)result).Model).PasswordResetTokenValid);
            }
        }

        [Fact]
        public void Valid_token_returns_ResetPassword_view()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).PasswordResetTokenValid);
            }
        }

        [Fact]
        public void Invalid_token_returns_ResetPassword_view()
        {
            using (var controller = CreateController())
            {
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_token_is_logged()
        {
            using (var controller = CreateController())
            {
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.Index(new ContentModel(_currentPage.Object));

                _logger.Verify(x => x.Info(typeof(ResetPasswordController), LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordController), nameof(ResetPasswordController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Mismatched_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token.Reverse());
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).PasswordResetTokenValid);
            }
        }

        [Fact]
        public void Mismatched_token_returns_ResetPassword_view()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token.Reverse());
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Mismatched_token_is_logged()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token.Reverse());
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                _logger.Verify(x => x.Info(typeof(ResetPasswordController), LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordController), nameof(ResetPasswordController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Expired_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddMinutes(-1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).PasswordResetTokenValid);
            }
        }

        [Fact]
        public void Expired_token_returns_ResetPassword_view()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddMinutes(-1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Expired_token_is_logged()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddMinutes(-1);
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}"));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index(new ContentModel(_currentPage.Object));

                _logger.Verify(x => x.Info(typeof(ResetPasswordController), LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordController), nameof(ResetPasswordController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Success_querystring_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}&successful=yes"));

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Success_querystring_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString($"?token={_token}&successful=yes"));

                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }
    }
}
