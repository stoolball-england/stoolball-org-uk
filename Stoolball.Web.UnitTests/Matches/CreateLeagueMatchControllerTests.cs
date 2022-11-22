using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateLeagueMatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IStoolballEntityRouteParser> _routeParser = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ICreateMatchSeasonSelector> _createMatchSeasonSelector = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();

        private CreateLeagueMatchController CreateController()
        {
            return new CreateLeagueMatchController(
                Mock.Of<ILogger<CreateLeagueMatchController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _routeParser.Object,
                _matchDataSource.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object,
                _createMatchSeasonSelector.Object,
                _editMatchHelper.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _routeParser.Setup(x => x.ParseRoute(Request.Object.Path)).Returns(StoolballEntityType.Team);
            _teamDataSource.Setup(x => x.ReadTeamByRoute(Request.Object.Path, true)).Returns(Task.FromResult<Team?>(null));

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
            _routeParser.Setup(x => x.ParseRoute(Request.Object.Path)).Returns(StoolballEntityType.Season);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(Request.Object.Path, true)).Returns(Task.FromResult<Season?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_without_a_league_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(Request.Object.Path, true)).Returns(Task.FromResult(new Team()));
            _routeParser.Setup(x => x.ParseRoute(Request.Object.Path)).Returns(StoolballEntityType.Team);
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.LeagueMatch)).Returns(new List<Season>());
            _editMatchHelper.Setup(x => x.PossibleSeasonsAsListItems(It.IsAny<IEnumerable<Season>>())).Returns(new List<SelectListItem>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_non_league_season_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/"));
            _routeParser.Setup(x => x.ParseRoute(Request.Object.Path)).Returns(StoolballEntityType.Season);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(Request.Object.Path, true)).Returns(Task.FromResult(new Season()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_league_team_returns_EditLeagueMatchViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(Request.Object.Path, true)).Returns(Task.FromResult(new Team()));
            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.LeagueMatch)).Returns(new List<Season> { new Season() });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditLeagueMatchViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_league_season_returns_EditLeagueMatchViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2020/"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(Request.Object.Path, true)).Returns(Task.FromResult(new Season
            {
                MatchTypes = new List<MatchType> { MatchType.LeagueMatch }
            }));
            _editMatchHelper.Setup(x => x.PossibleTeamsAsListItems(It.IsAny<IEnumerable<TeamInSeason>>())).Returns(new List<SelectListItem>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditLeagueMatchViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
