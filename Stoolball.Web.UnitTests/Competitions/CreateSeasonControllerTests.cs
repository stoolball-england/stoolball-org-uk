using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CreateSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();

        private CreateSeasonController CreateController()
        {
            return new CreateSeasonController(
                Mock.Of<ILogger<CreateSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_SeasonViewModel()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task FromYear_defaults_to_current_year()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(DateTime.Today.Year, ((SeasonViewModel)((ViewResult)result).Model).Season?.FromYear);
            }
        }

        [Fact]
        public async Task UntilYear_defaults_to_zero()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(0, ((SeasonViewModel)((ViewResult)result).Model).Season?.UntilYear);
            }
        }

        [Fact]
        public async Task PlayersPerTeam_defaults_to_11()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(11, ((SeasonViewModel)((ViewResult)result).Model).Season?.PlayersPerTeam);
            }
        }
    }
}
