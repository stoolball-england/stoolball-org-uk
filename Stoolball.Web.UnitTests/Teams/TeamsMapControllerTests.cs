using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Configuration;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamsMapControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();

        private TeamsMapController CreateController()
        {
            return new TeamsMapController(
                Mock.Of<ILogger<TeamsMapController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _apiKeyProvider.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<TeamsViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<TeamsViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public void Null_api_key_provider_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TeamsMapController(
                Mock.Of<ILogger<TeamsMapController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                null!));
        }

        [Fact]
        public async Task Returns_TeamsViewModel_with_Google_Maps_API_key()
        {
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("example-key");

            var model = await ReadModel();

            Assert.Equal("example-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public async Task Model_has_page_title()
        {
            var model = await ReadModel();

            Assert.Equal("Map of clubs and teams", model.Metadata.PageTitle);
        }
    }
}
