using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TransientTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();

        public TransientTeamControllerTests() : base()
        {
        }

        private TransientTeamController CreateController()
        {
            return new TransientTeamController(
                Mock.Of<ILogger<TransientTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _matchDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Team>>(),
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IEmailProtector>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = "Example team",
                TeamRoute = "/teams/example"
            });

            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing> { new MatchListing { StartTime = DateTimeOffset.UtcNow, MatchRoute = "/tournaments/example" } });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
