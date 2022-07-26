using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class DeleteSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        public DeleteSeasonControllerTests() : base()
        {
        }

        private DeleteSeasonController CreateController()
        {
            return new DeleteSeasonController(
                Mock.Of<ILogger<DeleteSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _matchListingDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_returns_DeleteSeasonViewModel()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/1234"
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<DeleteSeasonViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
