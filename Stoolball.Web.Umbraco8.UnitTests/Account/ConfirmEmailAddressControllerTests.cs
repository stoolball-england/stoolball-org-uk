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
    public class ConfirmEmailAddressControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPublishedContent> _currentPage;
        private const string CURRENT_PAGE_NAME = "Confirm email address";
        private const string CURRENT_PAGE_DESCRIPTION = "This is the description";
        private readonly string _token;
        private Mock<IVerificationToken> _tokenReader = new Mock<IVerificationToken>();
        private Mock<IMember> MEMBER = new Mock<IMember>();
        private Mock<IProfilingLogger> _logger = new Mock<IProfilingLogger>();
        private const string REQUESTED_EMAIL = "new@example.org";

        public ConfirmEmailAddressControllerTests()
        {
            base.Setup();

            _currentPage = new Mock<IPublishedContent>();
            _currentPage.Setup(x => x.Name).Returns(CURRENT_PAGE_NAME);
            SetupPropertyValue(_currentPage, "description", CURRENT_PAGE_DESCRIPTION);

            MEMBER.SetupGet(x => x.Id).Returns(123);
            MEMBER.SetupGet(x => x.Username).Returns("old@example.org");
            MEMBER.SetupGet(x => x.Email).Returns(MEMBER.Object.Username);
            MEMBER.SetupGet(x => x.Key).Returns(Guid.NewGuid());
            MEMBER.SetupGet(x => x.Name).Returns("Example name");
            MEMBER.Setup(x => x.GetValue("requestedEmail", null, null, false)).Returns(REQUESTED_EMAIL);
            MemberService.Setup(x => x.GetById(MEMBER.Object.Id)).Returns(MEMBER.Object);

            _token = Guid.NewGuid().ToString();
            base.Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString("?token=" + _token));
            _tokenReader.Setup(x => x.ExtractId(_token)).Returns(MEMBER.Object.Id);
        }

        private ConfirmEmailAddressController CreateController()
        {
            var controller = new ConfirmEmailAddressController(Mock.Of<IGlobalSettings>(),
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
        public void Index_sets_name_and_description_from_content()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index(new ContentModel(_currentPage.Object));

                var meta = ((IHasViewMetadata)((ViewResult)result).Model).Metadata;
                Assert.Equal(CURRENT_PAGE_NAME, meta.PageTitle);
                Assert.Equal(CURRENT_PAGE_DESCRIPTION, meta.Description);
            }
        }

        [Fact]
        public void Index_has_content_security_policy_allows_forms()
        {
            var method = typeof(ConfirmEmailAddressController).GetMethod(nameof(ConfirmEmailAddressController.Index));
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
                var result = controller.Index(new ContentModel(_currentPage.Object));

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
                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.False(((ConfirmEmailAddress)((ViewResult)result).Model).TokenValid);
                MemberService.Verify(x => x.Save(MEMBER.Object, true), Times.Never);
            }
        }


        [Fact]
        public void Mismatched_token_returns_invalid_and_does_not_save()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token.Reverse());
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.False(((ConfirmEmailAddress)((ViewResult)result).Model).TokenValid);
                MemberService.Verify(x => x.Save(MEMBER.Object, true), Times.Never);
            }
        }

        [Fact]
        public void Expired_token_returns_invalid_and_does_not_save()
        {
            using (var controller = CreateController())
            {
                var expiryDate = DateTime.Now;
                MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token);
                MEMBER.Setup(x => x.GetValue<DateTime>("requestedEmailTokenExpires", null, null, false)).Returns(expiryDate);
                _tokenReader.Setup(x => x.HasExpired(expiryDate)).Returns(true);
                var result = controller.Index(new ContentModel(_currentPage.Object));

                _tokenReader.Verify(x => x.HasExpired(expiryDate), Times.Once);
                Assert.False(((ConfirmEmailAddress)((ViewResult)result).Model).TokenValid);
                MemberService.Verify(x => x.Save(MEMBER.Object, true), Times.Never);
            }
        }

        [Fact]
        public void Valid_token_does_not_save_if_another_member_exists_with_that_email()
        {
            MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token);
            _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
            base.MemberService.Setup(x => x.GetByEmail(REQUESTED_EMAIL)).Returns(Mock.Of<IMember>());

            using (var controller = CreateController())
            {
                var result = controller.Index(new ContentModel(_currentPage.Object));

                base.MemberService.Verify(x => x.GetByEmail(REQUESTED_EMAIL), Times.Once);
                base.MemberService.Verify(x => x.Save(MEMBER.Object, true), Times.Never);
                Assert.False(((ConfirmEmailAddress)((ViewResult)result).Model).TokenValid);
            }
        }

        [Fact]
        public void Valid_token_updates_member_username_and_email_and_saves()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index(new ContentModel(_currentPage.Object));

                MEMBER.VerifySet(x => x.Username = REQUESTED_EMAIL, Times.Once);
                MEMBER.VerifySet(x => x.Email = REQUESTED_EMAIL, Times.Once);
                MemberService.Verify(x => x.Save(MEMBER.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_resets_token_expiry_and_saves()
        {
            using (var controller = CreateController())
            {
                var resetExpiry = DateTime.Now;
                MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                _tokenReader.Setup(x => x.ResetExpiryTo()).Returns(resetExpiry);
                var result = controller.Index(new ContentModel(_currentPage.Object));

                MEMBER.Verify(x => x.SetValue("requestedEmailTokenExpires", resetExpiry, null, null), Times.Once);
                MemberService.Verify(x => x.Save(MEMBER.Object, true), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_logs()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index(new ContentModel(_currentPage.Object));

                _logger.Verify(x => x.Info(typeof(ConfirmEmailAddressController), LoggingTemplates.ConfirmEmailAddress, MEMBER.Object.Username, MEMBER.Object.Key, typeof(ConfirmEmailAddressController), nameof(ConfirmEmailAddressController.Index)), Times.Once);
            }
        }

        [Fact]
        public void Valid_token_returns_valid_with_member_name_and_email()
        {
            using (var controller = CreateController())
            {
                MEMBER.Setup(x => x.GetValue("requestedEmailToken", null, null, false)).Returns(_token);
                _tokenReader.Setup(x => x.HasExpired(It.IsAny<DateTime>())).Returns(false);
                var result = controller.Index(new ContentModel(_currentPage.Object));

                var model = (ConfirmEmailAddress)((ViewResult)result).Model;
                Assert.True(model.TokenValid);
                Assert.Equal(MEMBER.Object.Name, model.MemberName);
                Assert.Equal(MEMBER.Object.Email, model.EmailAddress);
            }
        }

        [Fact]
        public void Index_returns_ConfirmEmailAddress_ModelsBuilder_model()
        {
            using (var controller = CreateController())
            {
                var result = controller.Index(new ContentModel(_currentPage.Object));

                Assert.IsType<ConfirmEmailAddress>(((ViewResult)result).Model);
            }
        }
    }
}
