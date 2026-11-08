using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateTournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();

        private CreateTournamentController CreateController()
        {
            return new CreateTournamentController(
                Mock.Of<ILogger<CreateTournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<EditTournamentViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
        }

        private Team SetupTeam(string teamName = "Example team")
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            var team = new Team
            {
                TeamName = teamName,
                PlayerType = PlayerType.Ladies,
                MatchLocations = [
                    new MatchLocation { MatchLocationId = Guid.NewGuid(), PrimaryAddressableObjectName = "Example ground", Town = "Exampletown" },
                    new MatchLocation { MatchLocationId = Guid.NewGuid(), PrimaryAddressableObjectName = "Other ground", Town = "Othertown" }
                ]
            };
            _teamDataSource.Setup(x => x.ReadTeamByRoute("/teams/example/", true)).ReturnsAsync(team);
            return team;
        }

        private Season SetupSeason(string competitionName = "Example league")
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2026/"));
            var season = new Season
            {
                FromYear = 2026,
                UntilYear = 2026,
                EnableTournaments = true,
                Competition = new Competition { CompetitionName = competitionName, PlayerType = PlayerType.JuniorMixed }
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute("/competitions/example/2026/", false)).ReturnsAsync(season);
            return season;
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example/"));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example/2020/"));
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_season_without_tournaments_returns_404()
        {
            var season = SetupSeason();
            season.EnableTournaments = false;

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task New_tournament_has_default_values()
        {
            SetupTeam();

            var model = await ReadModel();

            var tournament = model.Tournament!;
            Assert.Equal(TournamentQualificationType.OpenTournament, tournament.QualificationType);
            Assert.Equal(8, tournament.PlayersPerTeam);
            var overSet = Assert.Single(tournament.DefaultOverSets);
            Assert.Equal(4, overSet.Overs);
            Assert.Equal(8, overSet.BallsPerOver);
            Assert.NotNull(tournament.TournamentLocation);
        }

        [Fact]
        public async Task Route_matching_team_returns_EditTournamentViewModel_with_team()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team, model.Team);
            Assert.Null(model.Season);
            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Route_matching_team_names_tournament_after_team_with_tournament_suffix()
        {
            SetupTeam("Example team");

            var model = await ReadModel();

            Assert.Equal("Example team tournament", model.Tournament!.TournamentName);
        }

        [Fact]
        public async Task Route_matching_team_does_not_repeat_tournament_in_name()
        {
            SetupTeam("Example Tournament team");

            var model = await ReadModel();

            Assert.Equal("Example Tournament team", model.Tournament!.TournamentName);
        }

        [Fact]
        public async Task Route_matching_team_uses_first_team_location_and_player_type()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team.MatchLocations[0].MatchLocationId, model.TournamentLocationId);
            Assert.Equal("Example ground, Exampletown", model.TournamentLocationName);
            Assert.Equal(PlayerType.Ladies, model.Tournament!.PlayerType);
        }

        [Fact]
        public async Task Route_matching_team_without_locations_has_no_location()
        {
            var team = SetupTeam();
            team.MatchLocations.Clear();

            var model = await ReadModel();

            Assert.Null(model.TournamentLocationId);
            Assert.Null(model.TournamentLocationName);
        }

        [Fact]
        public async Task Route_matching_team_sets_page_title()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Add a tournament for Example team", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Route_matching_season_with_tournaments_returns_EditTournamentViewModel_with_season()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(season, model.Season);
            Assert.Null(model.Team);
            _teamDataSource.Verify(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Route_matching_season_names_tournament_after_competition_with_tournament_suffix()
        {
            SetupSeason("Example league");

            var model = await ReadModel();

            Assert.Equal("Example league tournament", model.Tournament!.TournamentName);
        }

        [Fact]
        public async Task Route_matching_season_does_not_repeat_tournament_in_name()
        {
            SetupSeason("Example tournament series");

            var model = await ReadModel();

            Assert.Equal("Example tournament series", model.Tournament!.TournamentName);
        }

        [Fact]
        public async Task Route_matching_season_uses_competition_player_type_and_sets_page_title()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(PlayerType.JuniorMixed, model.Tournament!.PlayerType);
            Assert.Equal($"Add a tournament in the {season.SeasonFullName()}", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Signed_out_member_is_not_authorized_to_create_tournament()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTournament]);
        }

        [Fact]
        public async Task Signed_in_member_is_authorized_to_create_tournament()
        {
            SetupTeam();
            SetupCurrentMember(Mock.Of<IMember>(x => x.Name == "Jo Bloggs"));

            var model = await ReadModel();

            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTournament]);
        }

        [Fact]
        public async Task Breadcrumbs_end_with_tournaments()
        {
            SetupTeam();

            var model = await ReadModel();

            var breadcrumb = model.Breadcrumbs.Last();
            Assert.Equal(Constants.Pages.Tournaments, breadcrumb.Name);
            Assert.Equal(Constants.Pages.TournamentsUrl, breadcrumb.Url?.ToString());
        }
    }
}
