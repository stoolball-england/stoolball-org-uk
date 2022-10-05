using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class DeleteTournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<ICommentsDataSource<Tournament>> _commentsDataSource = new();

        private DeleteTournamentController CreateController()
        {
            return new DeleteTournamentController(
                Mock.Of<ILogger<DeleteTournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _matchDataSource.Object,
                _commentsDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                Mock.Of<IDateTimeFormatter>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_tournament_returns_DeleteMatchViewModel()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentId = Guid.NewGuid(), TournamentName = "Example tournament", TournamentRoute = "/tournaments/example" });
            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<DeleteTournamentViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
