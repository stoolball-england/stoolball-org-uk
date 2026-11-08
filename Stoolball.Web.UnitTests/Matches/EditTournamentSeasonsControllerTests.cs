using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentSeasonsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<ISeasonEstimator> _seasonEstimator = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly Tournament _tournament = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example",
            PlayerType = PlayerType.Ladies,
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)
        };

        public EditTournamentSeasonsControllerTests()
        {
            _dateFormatter.Setup(x => x.FormatDate(_tournament.StartTime, false, false, false)).Returns("1 June 2026");
            _seasonEstimator.Setup(x => x.EstimateSeasonDates(_tournament.StartTime)).Returns((new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero)));
            _seasonDataSource.Setup(x => x.ReadSeasons(It.IsAny<CompetitionFilter>())).ReturnsAsync(new List<Season>());
        }

        private EditTournamentSeasonsController CreateController()
        {
            return new EditTournamentSeasonsController(
                Mock.Of<ILogger<EditTournamentSeasonsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _seasonDataSource.Object,
                _authorizationPolicy.Object,
                _seasonEstimator.Object,
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

        private static Season CreateSeason(string competitionName) => new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            Competition = new Competition { CompetitionName = competitionName }
        };

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null!));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_tournament_returns_EditTournamentViewModel_with_tournament_and_date()
        {
            var model = await ReadModel();

            Assert.Equal(_tournament, model.Tournament);
            Assert.Equal(_tournament.StartTime, model.TournamentDate);
        }

        [Fact]
        public async Task UrlReferrer_is_read_from_Referer_header()
        {
            Request.Object.Headers.Referer = "https://www.stoolball.org.uk/tournaments/example/edit";

            var model = await ReadModel();

            Assert.Equal(new Uri("https://www.stoolball.org.uk/tournaments/example/edit"), model.UrlReferrer);
        }

        [Fact]
        public async Task Possible_seasons_are_filtered_by_estimated_season_player_type_match_types_and_tournaments_enabled()
        {
            await ReadModel();

            _seasonDataSource.Verify(x => x.ReadSeasons(It.Is<CompetitionFilter>(f =>
                f.FromYear == 2025 &&
                f.UntilYear == 2026 &&
                f.PlayerTypes.SequenceEqual(new[] { PlayerType.Ladies }) &&
                f.MatchTypes.Count == 3 &&
                f.MatchTypes.Contains(MatchType.FriendlyMatch) &&
                f.MatchTypes.Contains(MatchType.KnockoutMatch) &&
                f.MatchTypes.Contains(MatchType.LeagueMatch) &&
                f.EnableTournaments == true
                )), Times.Once);
        }

        [Fact]
        public async Task Current_seasons_are_added_to_possible_seasons_without_duplicates_and_sorted_by_name()
        {
            var alreadyPossible = CreateSeason("B league");
            var possibleOnly = CreateSeason("C league");
            var currentOnly = CreateSeason("A league");
            _seasonDataSource.Setup(x => x.ReadSeasons(It.IsAny<CompetitionFilter>())).ReturnsAsync([possibleOnly, alreadyPossible]);
            _tournament.Seasons.Add(new Season { SeasonId = alreadyPossible.SeasonId, Competition = new Competition { CompetitionName = "B league" } });
            _tournament.Seasons.Add(currentOnly);

            var model = await ReadModel();

            Assert.Equal([currentOnly, alreadyPossible, possibleOnly], model.PossibleSeasons);
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
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var model = await ReadModel();

            Assert.Equal("Where to list Example tournament, 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_tournament.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
        }
    }
}
