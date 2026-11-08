using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
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
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly Tournament _tournament = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example",
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)
        };

        public DeleteTournamentControllerTests()
        {
            _dateFormatter.Setup(x => x.FormatDate(_tournament.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private DeleteTournamentController CreateController()
        {
            return new DeleteTournamentController(
                Mock.Of<ILogger<DeleteTournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _matchDataSource.Object,
                _commentsDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<DeleteTournamentViewModel> ReadModel()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_tournament);

            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<DeleteTournamentViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null!));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_tournament_returns_DeleteTournamentViewModel_with_tournament()
        {
            var model = await ReadModel();

            Assert.Equal(_tournament, model.Tournament);
        }

        [Fact]
        public async Task Total_comments_are_read_for_the_tournament()
        {
            _commentsDataSource.Setup(x => x.ReadTotalComments(_tournament.TournamentId!.Value)).ReturnsAsync(5);

            var model = await ReadModel();

            Assert.Equal(5, model.TotalComments);
        }

        [Fact]
        public async Task Matches_in_the_tournament_are_read_earliest_first()
        {
            var matches = new List<MatchListing> { new() { MatchName = "Match one" }, new() { MatchName = "Match two" } };
            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(matches);

            var model = await ReadModel();

            Assert.Equal(matches, model.Matches.Matches);
            _matchDataSource.Verify(x => x.ReadMatchListings(It.Is<MatchFilter>(f => f.TournamentId == _tournament.TournamentId && f.IncludeTournamentMatches && !f.IncludeTournaments), MatchSortOrder.MatchDateEarliestFirst), Times.Once);
        }

        [Fact]
        public async Task Confirmation_requires_the_tournament_name()
        {
            var model = await ReadModel();

            Assert.Equal("Example tournament", model.ConfirmDeleteRequest.RequiredText);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteTournament, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(_tournament)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var model = await ReadModel();

            Assert.Equal("Delete Example tournament (Mixed), 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_tournament.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
        }
    }
}
