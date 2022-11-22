using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Listings;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>> _listingsBuilder = new();

        private CompetitionsController CreateController()
        {
            return new CompetitionsController(
              Mock.Of<ILogger<CompetitionsController>>(),
              CompositeViewEngine.Object,
              UmbracoContextAccessor.Object,
              _competitionDataSource.Object,
              _listingsBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }


        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(CompetitionsController).GetMethod(nameof(CompetitionsController.Index))!;
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
        public async Task Returns_CompetitionsViewModel_from_builder()
        {
            var url = new Uri(Request.Object.GetEncodedUrl());
            var model = new CompetitionsViewModel(Mock.Of<IPublishedContent>(), Mock.Of<IUserService>()) { Filter = new CompetitionFilter() };
            _listingsBuilder.Setup(x => x.BuildModel(
                It.IsAny<Func<CompetitionsViewModel>>(),
                _competitionDataSource.Object.ReadTotalCompetitions,
                _competitionDataSource.Object.ReadCompetitions,
                Constants.Pages.Competitions,
                url,
                Request.Object.QueryString.Value
                )).Returns(Task.FromResult(model));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _listingsBuilder.Verify(x => x.BuildModel(
                    It.IsAny<Func<CompetitionsViewModel>>(),
                    _competitionDataSource.Object.ReadTotalCompetitions,
                    _competitionDataSource.Object.ReadCompetitions,
                    Constants.Pages.Competitions,
                    url,
                    Request.Object.QueryString.Value
                    ), Times.Once);
                Assert.Equal(model, ((ViewResult)result).Model);
            }
        }

    }
}
