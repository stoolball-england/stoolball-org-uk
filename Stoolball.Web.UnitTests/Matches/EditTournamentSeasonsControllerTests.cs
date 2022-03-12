using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentSeasonsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ISeasonEstimator> _seasonEstimator = new();

        public EditTournamentSeasonsControllerTests()
        {
            Setup();
        }

        private EditTournamentSeasonsController CreateController()
        {
            return new EditTournamentSeasonsController(
                Mock.Of<ILogger<EditTournamentSeasonsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _seasonDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                _seasonEstimator.Object,
                Mock.Of<IDateTimeFormatter>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/not-a-match"));
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_tournament_returns_EditTournamentViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/tournaments/example-tournament"));
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentName = "Example tournament", TournamentRoute = "/tournaments/example" });
            _seasonDataSource.Setup(x => x.ReadSeasons(It.IsAny<CompetitionFilter>())).Returns(Task.FromResult(new List<Season>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
