using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Security;
using Moq;
using Umbraco.Core.Cache;
using Umbraco.Core.Dictionary;
using Umbraco.Core.Logging;
using Umbraco.Core.Mapping;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using Umbraco.Web.Security;
using Umbraco.Web.Security.Providers;

namespace Stoolball.Web.UnitTests
{
    // Copied from https://our.umbraco.com/documentation/Implementation/Unit-Testing/
    public abstract class UmbracoBaseTest
    {
        public ServiceContext ServiceContext { get; set; }
        public MembershipHelper MembershipHelper { get; set; }
        public UmbracoHelper UmbracoHelper { get; set; }
        public UmbracoMapper UmbracoMapper { get; set; }
        public Mock<ICultureDictionary> CultureDictionary { get; set; }
        public Mock<ICultureDictionaryFactory> CultureDictionaryFactory { get; set; }
        public Mock<IPublishedContentQuery> PublishedContentQuery { get; set; }
        public Mock<HttpContextBase> HttpContext { get; set; }
        public Mock<HttpRequestBase> Request { get; set; }
        public Mock<IMemberService> MemberService { get; set; }
        public Mock<IPublishedMemberCache> MemberCache { get; set; }

        public virtual void Setup()
        {
            SetupHttpContext();
            SetupCultureDictionaries();
            SetupPublishedContentQuerying();
            SetupMembership();

            ServiceContext = ServiceContext.CreatePartial(memberService: MemberService.Object);
            UmbracoHelper = new UmbracoHelper(Mock.Of<IPublishedContent>(), Mock.Of<ITagQuery>(), CultureDictionaryFactory.Object, Mock.Of<IUmbracoComponentRenderer>(), PublishedContentQuery.Object, MembershipHelper);
            UmbracoMapper = new UmbracoMapper(new MapDefinitionCollection(new List<IMapDefinition>()));
        }

        public virtual void SetupHttpContext()
        {
            Request = new Mock<HttpRequestBase>();
            Request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(string.Empty));

            HttpContext = new Mock<HttpContextBase>();
            HttpContext.SetupGet(x => x.Request).Returns(Request.Object);
        }

        public virtual void SetupCultureDictionaries()
        {
            CultureDictionary = new Mock<ICultureDictionary>();
            CultureDictionaryFactory = new Mock<ICultureDictionaryFactory>();
            CultureDictionaryFactory.Setup(x => x.CreateDictionary()).Returns(CultureDictionary.Object);
        }

        public virtual void SetupPublishedContentQuerying()
        {
            PublishedContentQuery = new Mock<IPublishedContentQuery>();
        }

        public virtual void SetupMembership()
        {
            MemberService = new Mock<IMemberService>();
            var memberTypeService = Mock.Of<IMemberTypeService>();
            var membershipProvider = new MembersMembershipProvider(MemberService.Object, memberTypeService);

            MemberCache = new Mock<IPublishedMemberCache>();
            MembershipHelper = new MembershipHelper(HttpContext.Object, MemberCache.Object, membershipProvider, Mock.Of<RoleProvider>(), MemberService.Object, memberTypeService, Mock.Of<IUserService>(), Mock.Of<IPublicAccessService>(), AppCaches.NoCache, Mock.Of<ILogger>());
        }

        /// <summary>
        /// Setup <c>Members.GetCurrentMember()</c> to return the provided Umbraco member
        /// </summary>
        /// <param name="currentMember"></param>
        public void SetupCurrentMember(IMember currentMember)
        {
            if (currentMember is null)
            {
                throw new ArgumentNullException(nameof(currentMember));
            }

            // Configure the ASP.NET HttpContext.User property
            var identity = new Mock<IIdentity>();
            identity.Setup(x => x.IsAuthenticated).Returns(true);
            identity.Setup(x => x.Name).Returns(currentMember.Name);
            var user = new GenericPrincipal(identity.Object, Array.Empty<string>());
            HttpContext.Setup(x => x.User).Returns(user);

            // Configure Umbraco to get the currently logged-in member based on the same user
            // by mocking responses to the steps taken internally by MembershipHelper.GetCurrentUser()
            Thread.CurrentPrincipal = user;

            var memberContent = new Mock<IPublishedContent>();
            memberContent.Setup(x => x.Name).Returns(currentMember.Name);
            memberContent.Setup(x => x.Key).Returns(currentMember.Key);
            MemberService.Setup(x => x.GetByUsername(identity.Object.Name)).Returns(currentMember);
            MemberCache.Setup(x => x.GetByMember(currentMember)).Returns(memberContent.Object);
            memberContent.Setup(x => x.Id).Returns(currentMember.Id);
            MemberService.Setup(x => x.GetById(currentMember.Id)).Returns(currentMember);
        }

        public static void SetupPropertyValue(Mock<IPublishedContent> publishedContentMock, string alias, object value, string culture = null, string segment = null)
        {
            var property = new Mock<IPublishedProperty>();
            property.Setup(x => x.Alias).Returns(alias);
            property.Setup(x => x.GetValue(culture, segment)).Returns(value);
            property.Setup(x => x.HasValue(culture, segment)).Returns(value != null);
            publishedContentMock?.Setup(x => x.GetProperty(alias)).Returns(property.Object);
        }
    }
}