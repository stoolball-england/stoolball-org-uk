using System;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Routing;

namespace Stoolball.Web.UnitTests
{
    public abstract class UmbracoBaseTest
    {
        private Mock<IIdentity> _identity = new();

        protected Mock<HttpContext> HttpContext { get; private set; } = new();
        protected Mock<HttpRequest> Request { get; private set; } = new();
        protected ControllerContext ControllerContext { get; private set; }
        protected Mock<ICompositeViewEngine> CompositeViewEngine { get; private set; } = new();
        protected Mock<IUmbracoContextAccessor> UmbracoContextAccessor { get; private set; } = new();
        protected ServiceContext ServiceContext { get; private set; }
        protected Mock<IMemberService> MemberService { get; private set; } = new();
        protected Mock<IPublishedContent> CurrentPage { get; private set; } = new();

        protected UmbracoBaseTest()
        {
            SetupHttpContext();

            ControllerContext = new ControllerContext
            {
                HttpContext = HttpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
            };

            SetupCurrentPage();

            ServiceContext = ServiceContext.CreatePartial(
                memberService: MemberService.Object,
                userService: Mock.Of<IUserService>(),
                localizationService: Mock.Of<ILocalizationService>()
            );
        }

        public virtual void SetupHttpContext()
        {
            Request.SetupGet(x => x.Scheme).Returns("https");
            Request.SetupGet(x => x.Host).Returns(new HostString("www.stoolball.org.uk"));
            Request.SetupGet(x => x.Query).Returns(new QueryCollection());
            Request.SetupGet(x => x.Headers).Returns(new HeaderDictionary());

            HttpContext.SetupGet(x => x.Request).Returns(Request.Object);

            _identity.Setup(x => x.IsAuthenticated).Returns(false);
            var user = new GenericPrincipal(_identity.Object, Array.Empty<string>());
            HttpContext.Setup(x => x.User).Returns(user);

            // Configure Umbraco to get the currently logged-in member based on the same user
            // by mocking responses to the steps taken internally by MembershipHelper.GetCurrentUser()
            Thread.CurrentPrincipal = user;
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
            _identity.Setup(x => x.IsAuthenticated).Returns(true);
            _identity.Setup(x => x.Name).Returns(currentMember.Name);

            var memberContent = new Mock<IPublishedContent>();
            memberContent.Setup(x => x.Name).Returns(currentMember.Name);
            memberContent.Setup(x => x.Key).Returns(currentMember.Key);
            memberContent.Setup(x => x.Id).Returns(currentMember.Id);
            MemberService.Setup(x => x.GetByUsername(currentMember.Name)).Returns(currentMember);
            MemberService.Setup(x => x.GetById(currentMember.Id)).Returns(currentMember);
        }

        public void SetupPropertyValue(Mock<IPublishedContent> publishedContentMock, string alias, object value, string? culture = null, string? segment = null)
        {
            var property = new Mock<IPublishedProperty>();
            property.Setup(x => x.Alias).Returns(alias);
            property.Setup(x => x.GetValue(culture, segment)).Returns(value);
            property.Setup(x => x.HasValue(culture, segment)).Returns(value != null);
            publishedContentMock.Setup(x => x.GetProperty(alias)).Returns(property.Object);
        }

        public void SetupCurrentPage()
        {
            const string TEMPLATE_NAME = "Mock";

            var publishedRequest = new Mock<IPublishedRequest>();
            publishedRequest.Setup(request => request.PublishedContent).Returns(CurrentPage.Object);

            var features = new FeatureCollection();
            features.Set(new UmbracoRouteValues(publishedRequest.Object, new ControllerActionDescriptor(), TEMPLATE_NAME));
            HttpContext.SetupGet(x => x.Features).Returns(features);

            CompositeViewEngine.Setup(x => x.FindView(ControllerContext, TEMPLATE_NAME, false))
                .Returns(ViewEngineResult.Found(TEMPLATE_NAME, Mock.Of<IView>()));

            var umbracoContextMock = new Mock<IUmbracoContext>();
            umbracoContextMock.Setup(context => context.Content).Returns(Mock.Of<IPublishedContentCache>());
            umbracoContextMock.Setup(context => context.PublishedRequest).Returns(publishedRequest.Object);

            var umbracoContext = umbracoContextMock.Object;
            UmbracoContextAccessor.Setup(x => x.TryGetUmbracoContext(out umbracoContext)).Returns(true);
        }
    }
}
