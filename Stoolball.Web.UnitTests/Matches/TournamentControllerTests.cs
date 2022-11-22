using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Comments;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class TournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<ICommentsDataSource<Tournament>> _commentsDataSource = new();

        private TournamentController CreateController()
        {
            return new TournamentController(
                Mock.Of<ILogger<TournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _matchDataSource.Object,
                Mock.Of<IMatchFilterFactory>(),
                _commentsDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Tournament>>(),
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IApiKeyProvider>(),
                Mock.Of<IEmailProtector>(),
                Mock.Of<IBadLanguageFilter>())
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
        public async Task Route_matching_tournament_returns_TournamentViewModel()
        {
            var tournamentId = Guid.NewGuid();
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentId = tournamentId, TournamentName = "Example tournament" });

            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            _commentsDataSource.Setup(x => x.ReadComments(tournamentId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TournamentViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_tournament_reads_comments()
        {
            var tournamentId = Guid.NewGuid();
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(new Tournament { TournamentId = tournamentId, TournamentName = "Example tournament" });

            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            _commentsDataSource.Setup(x => x.ReadComments(tournamentId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _commentsDataSource.Verify(x => x.ReadComments(tournamentId), Times.Once);
            }
        }
    }
}
