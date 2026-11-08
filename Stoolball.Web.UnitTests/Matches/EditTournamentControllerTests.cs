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
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly Tournament _tournament = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example",
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 30, 0, TimeSpan.Zero),
            StartTimeIsKnown = true
        };

        public EditTournamentControllerTests()
        {
            _dateFormatter.Setup(x => x.FormatDate(_tournament.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private EditTournamentController CreateController()
        {
            return new EditTournamentController(
                Mock.Of<ILogger<EditTournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<EditTournamentViewModel> ReadModel()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_tournament);

            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
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
        public async Task Route_matching_tournament_returns_EditTournamentViewModel_with_tournament()
        {
            var model = await ReadModel();

            Assert.Equal(_tournament, model.Tournament);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(_tournament)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Known_start_time_sets_date_and_time()
        {
            var model = await ReadModel();

            Assert.Equal(_tournament.StartTime, model.TournamentDate);
            Assert.Equal(_tournament.StartTime, model.StartTime);
        }

        [Fact]
        public async Task Unknown_start_time_sets_date_only()
        {
            _tournament.StartTimeIsKnown = false;

            var model = await ReadModel();

            Assert.Equal(_tournament.StartTime, model.TournamentDate);
            Assert.Null(model.StartTime);
        }

        [Fact]
        public async Task Tournament_location_sets_location_id_and_name()
        {
            _tournament.TournamentLocation = new MatchLocation { MatchLocationId = Guid.NewGuid(), PrimaryAddressableObjectName = "Example ground", Town = "Exampletown" };

            var model = await ReadModel();

            Assert.Equal(_tournament.TournamentLocation.MatchLocationId, model.TournamentLocationId);
            Assert.Equal("Example ground, Exampletown", model.TournamentLocationName);
        }

        [Fact]
        public async Task No_tournament_location_leaves_location_empty()
        {
            var model = await ReadModel();

            Assert.Null(model.TournamentLocationId);
            Assert.Null(model.TournamentLocationName);
        }

        [Fact]
        public async Task No_default_over_sets_adds_an_empty_over_set()
        {
            var model = await ReadModel();

            var overSet = Assert.Single(model.Tournament!.DefaultOverSets);
            Assert.Null(overSet.Overs);
        }

        [Fact]
        public async Task Existing_default_over_sets_are_unchanged()
        {
            var overSet = new OverSet { Overs = 6, BallsPerOver = 8 };
            _tournament.DefaultOverSets.Add(overSet);

            var model = await ReadModel();

            Assert.Equal(overSet, Assert.Single(model.Tournament!.DefaultOverSets));
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var model = await ReadModel();

            Assert.Equal("Edit Example tournament, 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_tournament.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
        }
    }
}
