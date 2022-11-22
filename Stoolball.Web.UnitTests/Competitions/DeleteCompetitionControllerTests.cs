using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class DeleteCompetitionControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();

        private DeleteCompetitionController CreateController()
        {
            return new DeleteCompetitionController(
                Mock.Of<ILogger<DeleteCompetitionController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                _matchListingDataSource.Object,
                _teamDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_DeleteCompetitionViewModel()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = Guid.NewGuid(), CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<DeleteCompetitionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
