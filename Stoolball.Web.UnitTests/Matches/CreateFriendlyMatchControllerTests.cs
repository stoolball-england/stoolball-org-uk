﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateFriendlyMatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<IStoolballEntityRouteParser> _routeParser = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ICreateMatchSeasonSelector> _createMatchSeasonSelector = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();

        private CreateFriendlyMatchController CreateController()
        {
            return new CreateFriendlyMatchController(
                Mock.Of<ILogger<CreateFriendlyMatchController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _routeParser.Object,
                _matchDataSource.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object,
                _createMatchSeasonSelector.Object,
                _editMatchHelper.Object)
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
        public async Task Route_matching_team_returns_EditFriendlyMatchViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(Request.Object.Path, true)).Returns(Task.FromResult(new Team()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditFriendlyMatchViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_friendly_season_returns_EditFriendlyMatchViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2020/"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(Request.Object.Path, false)).Returns(Task.FromResult<Season>(new Season
            {
                MatchTypes = new List<MatchType> { MatchType.FriendlyMatch }
            }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();
                Assert.IsType<EditFriendlyMatchViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
