using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class TournamentActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();

        public TournamentActionsControllerTests()
        {
            Setup();
        }

        private TournamentActionsController CreateController()
        {
            return new TournamentActionsController(
                Mock.Of<ILogger<TournamentActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                Mock.Of<IDateTimeFormatter>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_tournament_returns_TournamentViewModel()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentName = "Example tournament", TournamentRoute = "/tournaments/example" });

            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
