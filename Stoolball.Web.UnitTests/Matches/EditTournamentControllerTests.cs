using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();

        public EditTournamentControllerTests()
        {
            Setup();
        }

        private EditTournamentController CreateController()
        {
            return new EditTournamentController(
                Mock.Of<ILogger<EditTournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                Mock.Of<IDateTimeFormatter>()
                )
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

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
