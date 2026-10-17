using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Listings;
using Stoolball.Navigation;
using Stoolball.Schools;
using Stoolball.Web.Models;
using Stoolball.Web.Schools;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Stoolball.Web.UnitTests.Schools
{
    public class SchoolsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISchoolDataSource> _schoolDataSource = new();
        private readonly Mock<IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>> _listingsBuilder = new();

        private SchoolsController CreateController()
        {
            return new SchoolsController(
                Mock.Of<ILogger<SchoolsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _schoolDataSource.Object,
                _listingsBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }


        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(SchoolsController).GetMethod(nameof(SchoolsController.Index))!;
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
        public async Task Returns_SchoolsViewModel_from_builder()
        {
            var model = new SchoolsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>());
            _listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<SchoolsViewModel>>(),
                _schoolDataSource.Object.ReadTotalSchools,
                _schoolDataSource.Object.ReadSchools,
                Constants.Pages.Schools,
                new Uri(Request.Object.GetEncodedUrl()),
                Request.Object.QueryString.Value
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _listingsBuilder.Verify(x => x.BuildModel(
                    It.IsAny<Func<SchoolsViewModel>>(),
                    _schoolDataSource.Object.ReadTotalSchools,
                    _schoolDataSource.Object.ReadSchools,
                    Constants.Pages.Schools,
                    new Uri(Request.Object.GetEncodedUrl()),
                    Request.Object.QueryString.Value
                    ), Times.Once);
                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Sets_schools_breadcrumb()
        {
            var model = new SchoolsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>());
            _listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<SchoolsViewModel>>(),
                _schoolDataSource.Object.ReadTotalSchools,
                _schoolDataSource.Object.ReadSchools,
                Constants.Pages.Schools,
                new Uri(Request.Object.GetEncodedUrl()),
                Request.Object.QueryString.Value
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var breadcrumbs = (((ViewResult)result).Model as BaseViewModel)?.Breadcrumbs;
                Assert.Equal(2, breadcrumbs!.Count);
                Assert.Equal(Constants.Pages.Home, breadcrumbs[0].Name);
                Assert.Equal(Constants.Pages.Schools, breadcrumbs[1].Name);
            }
        }

        [Theory]
        [InlineData(51, false)]
        [InlineData(50, true)]
        public async Task Page_beyond_last_page_returns_NotFound(int totalListings, bool expectNotFound)
        {
            var model = new SchoolsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>());
            model.Filter.Paging = new Paging { PageNumber = 2, PageSize = 50, Total = totalListings };
            _listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<SchoolsViewModel>>(),
                It.IsAny<Func<SchoolFilter, Task<int>>>(),
                It.IsAny<Func<SchoolFilter, Task<List<School>>>>(),
                It.IsAny<string>(),
                It.IsAny<Uri>(),
                It.IsAny<string>()
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                if (expectNotFound)
                {
                    Assert.IsType<NotFoundResult>(result);
                }
                else
                {
                    Assert.Equal(model, ((ViewResult)result).Model);
                }
            }
        }
    }
}
