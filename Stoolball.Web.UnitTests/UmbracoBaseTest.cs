using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Dictionary;
using Umbraco.Core.Mapping;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using Umbraco.Web.Security;
using Umbraco.Web.Security.Providers;

namespace Stoolball.Web.Tests
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
        public Mock<IMemberService> MemberService { get; set; }
        public Mock<IPublishedMemberCache> MemberCache { get; set; }

        public virtual void Setup()
        {
            this.SetupHttpContext();
            this.SetupCultureDictionaries();
            this.SetupPublishedContentQuerying();
            this.SetupMembership();

            this.ServiceContext = ServiceContext.CreatePartial(memberService: MemberService.Object);
            this.UmbracoHelper = new UmbracoHelper(Mock.Of<IPublishedContent>(), Mock.Of<ITagQuery>(), this.CultureDictionaryFactory.Object, Mock.Of<IUmbracoComponentRenderer>(), this.PublishedContentQuery.Object, this.MembershipHelper);
            this.UmbracoMapper = new UmbracoMapper(new MapDefinitionCollection(new List<IMapDefinition>()));
        }

        public virtual void SetupHttpContext()
        {
            this.HttpContext = new Mock<HttpContextBase>();
        }

        public virtual void SetupCultureDictionaries()
        {
            this.CultureDictionary = new Mock<ICultureDictionary>();
            this.CultureDictionaryFactory = new Mock<ICultureDictionaryFactory>();
            this.CultureDictionaryFactory.Setup(x => x.CreateDictionary()).Returns(this.CultureDictionary.Object);
        }

        public virtual void SetupPublishedContentQuerying()
        {
            this.PublishedContentQuery = new Mock<IPublishedContentQuery>();
        }

        public virtual void SetupMembership()
        {
            this.MemberService = new Mock<IMemberService>();
            var memberTypeService = Mock.Of<IMemberTypeService>();
            var membershipProvider = new MembersMembershipProvider(MemberService.Object, memberTypeService);

            this.MemberCache = new Mock<IPublishedMemberCache>();
            this.MembershipHelper = new MembershipHelper(this.HttpContext.Object, this.MemberCache.Object, membershipProvider, Mock.Of<RoleProvider>(), MemberService.Object, memberTypeService, Mock.Of<IUserService>(), Mock.Of<IPublicAccessService>(), AppCaches.NoCache, Mock.Of<ILogger>());
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