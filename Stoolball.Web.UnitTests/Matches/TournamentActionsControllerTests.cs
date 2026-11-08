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
    public class TournamentActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly Tournament _tournament = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example",
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)
        };

        public TournamentActionsControllerTests()
        {
            _dateFormatter.Setup(x => x.FormatDate(_tournament.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private TournamentActionsController CreateController()
        {
            return new TournamentActionsController(
                Mock.Of<ILogger<TournamentActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<TournamentViewModel> ReadModel()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_tournament);

            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<TournamentViewModel>(((ViewResult)result).Model);
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
        public async Task Route_matching_tournament_returns_TournamentViewModel_with_tournament()
        {
            var model = await ReadModel();

            Assert.Equal(_tournament, model.Tournament);
        }

        [Fact]
        public async Task Model_has_authorization_details()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } };
            var memberNames = new List<string> { "Jo Bloggs" };
            _authorizationPolicy.Setup(x => x.IsAuthorized(_tournament)).ReturnsAsync(authorization);
            _authorizationPolicy.Setup(x => x.AuthorizedMemberNames(_tournament)).ReturnsAsync(memberNames);

            var model = await ReadModel();

            Assert.Equal("edit or delete", model.Authorization.AuthorizedAction);
            Assert.Equal("Example tournament, 1 June 2026", model.Authorization.AuthorizationFor);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(memberNames, model.Authorization.AuthorizedMemberNames);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var model = await ReadModel();

            Assert.Equal("Example tournament (Mixed), 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_tournament.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
        }
    }
}
