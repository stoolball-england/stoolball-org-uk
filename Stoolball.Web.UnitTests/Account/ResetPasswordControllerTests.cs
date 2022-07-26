using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Logging;
using Stoolball.Security;
using Stoolball.Web.Account;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IVerificationToken> _tokenReader = new();
        private readonly Mock<IMember> _member = new();
        private readonly Mock<ILogger<ResetPasswordController>> _logger = new();
        private string _token = Guid.NewGuid().ToString();

        public ResetPasswordControllerTests() : base()
        {
            CurrentPage.Setup(x => x.Name).Returns("Reset password");
            SetupPropertyValue(CurrentPage, "description", "This is the description");

            _tokenReader.Setup(x => x.ExtractId(_token)).Returns(123);
            base.MemberService.Setup(x => x.GetById(123)).Returns(_member.Object);
        }

        [Fact]
        public void Has_content_security_policy_allows_forms()
        {
            var method = typeof(ResetPasswordController).GetMethod(nameof(ResetPasswordController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute!.Forms);
            Assert.False(attribute.TinyMCE);
            Assert.False(attribute.YouTube);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
            Assert.False(attribute.GettyImages);
        }

        private ResetPasswordController CreateController()
        {
            var controller = new ResetPasswordController(_logger.Object,
                Mock.Of<ICompositeViewEngine>(),
                UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                ServiceContext,
                _tokenReader.Object)
            {
                ControllerContext = ControllerContext
            };

            return controller;
        }

        [Fact]
        public void No_token_sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index();

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(CurrentPage.Object.Name, meta.PageTitle);
                Assert.Equal("This is the description", meta.Description);
            }
        }

        [Fact]
        public void No_token_returns_ResetPasswordRequest_view()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index();

                Assert.Equal("ResetPasswordRequest", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void No_token_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index();

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public void Valid_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(false);

                var result = controller.Index();

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
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.Index();

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
                Assert.False(((ResetPassword)((ViewResult)result).Model).PasswordResetTokenValid);
            }
        }

        [Fact]
        public void Invalid_token_returns_ResetPassword_view()
        {
            using (var controller = CreateController())
            {
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.Index();

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Invalid_token_is_logged()
        {
            using (var controller = CreateController())
            {
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();

                var result = controller.Index();

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordController), nameof(ResetPasswordController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Mismatched_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token.Reverse());
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

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
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token.Reverse());
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Mismatched_token_is_logged()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddDays(1);
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token.Reverse());
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordController), nameof(ResetPasswordController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Expired_token_sets_PasswordResetTokenValid_on_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddMinutes(-1);
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

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
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

                Assert.Equal("ResetPassword", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Expired_token_is_logged()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now.AddMinutes(-1);
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token } }));
                _member.Setup(x => x.GetValue("passwordResetToken", null, null, false)).Returns(_token);
                _member.Setup(x => x.GetValue<DateTime>("passwordResetTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);

                var result = controller.Index();

                _logger.Verify(x => x.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, _token, typeof(ResetPasswordController), nameof(ResetPasswordController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Success_querystring_returns_ResetPasswordComplete_view()
        {
            using (var controller = CreateController())
            {
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token }, { "successful", "yes" } }));

                var result = controller.Index();

                Assert.Equal("ResetPasswordComplete", ((ViewResult)result).ViewName);
            }
        }

        [Fact]
        public void Success_querystring_returns_ResetPassword_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", _token }, { "successful", "yes" } }));

                var result = controller.Index();

                Assert.IsType<ResetPassword>(((ViewResult)result).Model);
            }
        }
    }
}
