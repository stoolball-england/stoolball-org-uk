using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class StatisticsFilterFactoryTests
    {
        private Mock<IRouteNormaliser> _routeNormaliser = new();
        private Mock<IPlayerDataSource> _playerDataSource = new();
        private Mock<IClubDataSource> _clubDataSource = new();
        private Mock<ITeamDataSource> _teamDataSource = new();
        private Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private Mock<ICompetitionDataSource> _competitionDataSource = new();
        private Mock<ISeasonDataSource> _seasonDataSource = new();

        [Fact]
        public void Null_route_throws_ArgumentException()
        {
            var filterFactory = new StatisticsFilterFactory(Mock.Of<IStoolballEntityRouteParser>(), _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            Assert.ThrowsAsync<ArgumentException>(async () => await filterFactory.FromRoute(null));
        }

        [Fact]
        public void Empty_string_route_throws_ArgumentException()
        {
            var filterFactory = new StatisticsFilterFactory(Mock.Of<IStoolballEntityRouteParser>(), _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            Assert.ThrowsAsync<ArgumentException>(async () => await filterFactory.FromRoute(string.Empty));
        }

        [Fact]
        public async Task Player_route_is_normalised()
        {
            var route = "/players/example";
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(route)).Returns(StoolballEntityType.Player);

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(route);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity(route, "players"), Times.Once);
        }

        [Fact]
        public async Task Player_route_populates_player_from_playerDataSource()
        {
            var player = new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/example" };
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(player.PlayerRoute)).Returns(StoolballEntityType.Player);
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(player.PlayerRoute, "players")).Returns(player.PlayerRoute);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute)).Returns(Task.FromResult(player));

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(player.PlayerRoute);

            _playerDataSource.Verify(x => x.ReadPlayerByRoute(player.PlayerRoute), Times.Once);
            Assert.Equal(player, result.Player);
        }

        [Fact]
        public async Task Club_route_is_normalised()
        {
            var route = "/clubs/example";
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(route)).Returns(StoolballEntityType.Club);
            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(route);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity(route, "clubs"), Times.Once);
        }

        [Fact]
        public async Task Club_route_populates_club_from_clubDataSource()
        {
            var club = new Club { ClubId = Guid.NewGuid(), ClubRoute = "/clubs/example" };
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(club.ClubRoute)).Returns(StoolballEntityType.Club);
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(club.ClubRoute, "clubs")).Returns(club.ClubRoute);
            _clubDataSource.Setup(x => x.ReadClubByRoute(club.ClubRoute)).Returns(Task.FromResult(club));

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(club.ClubRoute);

            _clubDataSource.Verify(x => x.ReadClubByRoute(club.ClubRoute), Times.Once);
            Assert.Equal(club, result.Club);
        }

        [Fact]
        public async Task Team_route_is_normalised()
        {
            var route = "/teams/example";
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(route)).Returns(StoolballEntityType.Team);
            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(route);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity(route, "teams"), Times.Once);
        }

        [Fact]
        public async Task Team_route_populates_team_from_teamDataSource()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamRoute = "/teams/example" };
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(team.TeamRoute)).Returns(StoolballEntityType.Team);
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(team.TeamRoute, "teams")).Returns(team.TeamRoute);
            _teamDataSource.Setup(x => x.ReadTeamByRoute(team.TeamRoute, true)).Returns(Task.FromResult(team));

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(team.TeamRoute);

            _teamDataSource.Verify(x => x.ReadTeamByRoute(team.TeamRoute, true), Times.Once);
            Assert.Equal(team, result.Team);
        }

        [Fact]
        public async Task MatchLocation_route_is_normalised()
        {
            var route = "/locations/example";
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(route)).Returns(StoolballEntityType.MatchLocation);
            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(route);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity(route, "locations"), Times.Once);
        }

        [Fact]
        public async Task MatchLocation_route_populates_location_from_matchLocationDataSource()
        {
            var matchLocation = new MatchLocation { MatchLocationId = Guid.NewGuid(), MatchLocationRoute = "/locations/example" };
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(matchLocation.MatchLocationRoute)).Returns(StoolballEntityType.MatchLocation);
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(matchLocation.MatchLocationRoute, "locations")).Returns(matchLocation.MatchLocationRoute);
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(matchLocation.MatchLocationRoute, false)).Returns(Task.FromResult(matchLocation));

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(matchLocation.MatchLocationRoute);

            _matchLocationDataSource.Verify(x => x.ReadMatchLocationByRoute(matchLocation.MatchLocationRoute, false), Times.Once);
            Assert.Equal(matchLocation, result.MatchLocation);
        }

        [Fact]
        public async Task Competition_route_is_normalised()
        {
            var route = "/competitions/example";
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(route)).Returns(StoolballEntityType.Competition);
            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(route);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity(route, "competitions"), Times.Once);
        }

        [Fact]
        public async Task Competition_route_populates_competition_from_competitionDataSource()
        {
            var competition = new Competition { CompetitionId = Guid.NewGuid(), CompetitionRoute = "/competitions/example" };
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(competition.CompetitionRoute)).Returns(StoolballEntityType.Competition);
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(competition.CompetitionRoute, "competitions")).Returns(competition.CompetitionRoute);
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(competition.CompetitionRoute)).Returns(Task.FromResult(competition));

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(competition.CompetitionRoute);

            _competitionDataSource.Verify(x => x.ReadCompetitionByRoute(competition.CompetitionRoute), Times.Once);
            Assert.Equal(competition, result.Competition);
        }

        [Theory]
        [InlineData("/competitions/example/2021")]
        [InlineData("/competitions/example/2021/sub-page")]
        [InlineData("/competitions/example/2021-22")]
        [InlineData("/competitions/example/2021-22/sub-page")]
        public async Task Season_route_is_normalised(string seasonRoute)
        {
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(seasonRoute)).Returns(StoolballEntityType.Season);
            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(seasonRoute);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity(seasonRoute, "competitions", Constants.Pages.SeasonUrlRegEx), Times.Once);
        }

        [Theory]
        [InlineData("/competitions/example/2021")]
        [InlineData("/competitions/example/2021/sub-page")]
        [InlineData("/competitions/example/2021-22")]
        [InlineData("/competitions/example/2021-22/sub-page")]
        public async Task Season_route_populates_season_from_seasonDataSource(string seasonRoute)
        {
            var season = new Season { SeasonId = Guid.NewGuid(), SeasonRoute = seasonRoute };
            var routeParser = new Mock<IStoolballEntityRouteParser>();
            routeParser.Setup(x => x.ParseRoute(seasonRoute)).Returns(StoolballEntityType.Season);
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(season.SeasonRoute, "competitions", Constants.Pages.SeasonUrlRegEx)).Returns(season.SeasonRoute);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(season.SeasonRoute, false)).Returns(Task.FromResult(season));

            var filterFactory = new StatisticsFilterFactory(routeParser.Object, _playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await filterFactory.FromRoute(season.SeasonRoute);

            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(season.SeasonRoute, false), Times.Once);
            Assert.Equal(season, result.Season);
        }
    }
}
