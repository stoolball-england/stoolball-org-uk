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
    public class ApproveMemberControllerTests : UmbracoBaseTest
    {
        private const string CURRENT_PAGE_NAME = "Confirm email address";
        private const string CURRENT_PAGE_DESCRIPTION = "This is the description";
        private readonly string _token;
        private Mock<IVerificationToken> _tokenReader = new();
        private Mock<IMember> MEMBER = new();
        private Mock<ILogger<ApproveMemberController>> _logger = new();

        public ApproveMemberControllerTests()
        {
            base.Setup();

            CurrentPage.Setup(x => x.Name).Returns(CURRENT_PAGE_NAME);
            SetupPropertyValue(CurrentPage, "description", CURRENT_PAGE_DESCRIPTION);

            MEMBER.SetupGet(x => x.Id).Returns(123);
            MEMBER.SetupGet(x => x.Username).Returns("old@example.org");
            MEMBER.SetupGet(x => x.Key).Returns(Guid.NewGuid());
            MEMBER.SetupGet(x => x.Name).Returns("Example name");
            MemberService.Setup(x => x.GetById(MEMBER.Object.Id)).Returns(MEMBER.Object);

            _token = Guid.NewGuid().ToString();
            base.Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "token", new StringValues(_token) } }));
            _tokenReader.Setup(x => x.ExtractId(_token)).Returns(MEMBER.Object.Id);
        }
        private ApproveMemberController CreateController()
        {
            return new ApproveMemberController(_logger.Object,
                                        Mock.Of<ICompositeViewEngine>(),
                                        UmbracoContextAccessor.Object,
                                        Mock.Of<IVariationContextAccessor>(),
                                        ServiceContext,
                                        _tokenReader.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public void Index_sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index();

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(CURRENT_PAGE_NAME, meta.PageTitle);
                Assert.Equal(CURRENT_PAGE_DESCRIPTION, meta.Description);
            }
        }


        [Fact]
        public void Index_has_content_security_policy_allows_forms()
        {
            var method = typeof(ApproveMemberController).GetMethod(nameof(ApproveMemberController.Index));
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
        public void Member_is_looked_up_based_on_the_id_in_the_token_in_the_querystring()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index();

                _tokenReader.Verify(x => x.ExtractId(_token), Times.Once);
                MemberService.Verify(x => x.GetById(MEMBER.Object.Id), Times.Once);
            }
        }

        [Fact]
        public void Invalid_token_format_returns_invalid_and_does_not_save()
        {
            using (var controller = CreateController())
            {
                _tokenReader.Setup(x => x.ExtractId(_token)).Throws<FormatException>();
                var result = controller.Index();

                Assert.False(((ApproveMember)((ViewResult)result).Model).ApprovalTokenValid);
                MemberService.Verify(x => x.Save(MEMBER.Object), Times.Never);
            }
        }


        [Fact]
        public void Mismatched_token_returns_invalid_and_does_not_save()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("approvalToken", null, null, false)).Returns(_token.Reverse());
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index();

                Assert.False(((ApproveMember)((ViewResult)result).Model).ApprovalTokenValid);
                MemberService.Verify(x => x.Save(MEMBER.Object), Times.Never);
            }
        }

        [Fact]
        public void Expired_token_returns_invalid_and_does_not_save()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now;
                MEMBER.Setup(x => x.GetValue("approvalToken", null, null, false)).Returns(_token);
                MEMBER.Setup(x => x.GetValue<DateTime>("approvalTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);
                var result = controller.Index();

                _tokenReader.Verify(x => x.HasExpired(expiryDate), Times.Once);
                Assert.False(((ApproveMember)((ViewResult)result).Model).ApprovalTokenValid);
                MemberService.Verify(x => x.Save(MEMBER.Object), Times.Never);
            }
        }

        [Fact]
        public void Valid_token_updates_member_approval_status_and_saves()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("approvalToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index();

                MEMBER.VerifySet(x => x.IsApproved = true, Times.Once);
                MemberService.Verify(x => x.Save(MEMBER.Object), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_resets_token_expiry_and_saves()
        {
            using (var controller = CreateController())
            {
                var resetExpiry = DateTime.Now;
                MEMBER.Setup(x => x.GetValue("approvalToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                _tokenReader.Setup(x => x.ResetExpiryTo()).Returns(resetExpiry);
                var result = controller.Index();

                MEMBER.Verify(x => x.SetValue("approvalTokenExpires", resetExpiry, null, null), Times.Once);
                MemberService.Verify(x => x.Save(MEMBER.Object), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_logs()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("approvalToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index();

                _logger.Verify(x => x.Info(LoggingTemplates.ApproveMember, MEMBER.Object.Username, MEMBER.Object.Key, typeof(ApproveMemberController), nameof(ApproveMemberController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_returns_valid_with_member_name()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("approvalToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index();

                var model = (ApproveMember)((ViewResult)result).Model;
                Assert.True(model.ApprovalTokenValid);
                Assert.Equal(MEMBER.Object.Name, model.MemberName);
            }
        }

        [Fact]
        public void Index_returns_ApproveMember_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index();

                Assert.IsType<ApproveMember>(((ViewResult)result).Model);
            }
        }
    }
}
