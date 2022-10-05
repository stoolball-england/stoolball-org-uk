using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateTournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();

        private CreateTournamentController CreateController()
        {
            return new CreateTournamentController(
                Mock.Of<ILogger<CreateTournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2020/"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_without_tournaments_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season { EnableTournaments = false }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_team_returns_EditTournamentViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team { TeamName = "Example team" }));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_season_with_tournaments_returns_EditTournamentViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2020/"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult(new Season
            {
                EnableTournaments = true,
                Competition = new Competition { CompetitionName = "Example competition" }
            }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
