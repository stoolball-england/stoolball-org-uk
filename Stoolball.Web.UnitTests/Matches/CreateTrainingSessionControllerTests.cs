using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateTrainingSessionControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();

        public CreateTrainingSessionControllerTests()
        {
            base.Setup();
        }

        private CreateTrainingSessionController CreateController()
        {
            return new CreateTrainingSessionController(
                Mock.Of<ILogger<CreateTrainingSessionController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object,
                Mock.Of<ICreateMatchSeasonSelector>(),
                Mock.Of<IEditMatchHelper>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2022"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_EditTrainingSessionViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditTrainingSessionViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_training_season_returns_EditTrainingSessionViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2022"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season>(new Season
            {
                MatchTypes = new List<MatchType> { MatchType.TrainingSession }
            }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditTrainingSessionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
