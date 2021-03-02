using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Xunit;

namespace Stoolball.Web.Tests.Statistics
{
    public class StatisticsFilterUrlParserTests
    {
        private Mock<IRouteNormaliser> _routeNormaliser;
        private Mock<IPlayerDataSource> _playerDataSource;
        private Mock<IClubDataSource> _clubDataSource;
        private Mock<ITeamDataSource> _teamDataSource;
        private Mock<IMatchLocationDataSource> _matchLocationDataSource;
        private Mock<ICompetitionDataSource> _competitionDataSource;
        private Mock<ISeasonDataSource> _seasonDataSource;

        public StatisticsFilterUrlParserTests()
        {
            _playerDataSource = new Mock<IPlayerDataSource>();
            _clubDataSource = new Mock<IClubDataSource>();
            _teamDataSource = new Mock<ITeamDataSource>();
            _matchLocationDataSource = new Mock<IMatchLocationDataSource>();
            _competitionDataSource = new Mock<ICompetitionDataSource>();
            _seasonDataSource = new Mock<ISeasonDataSource>();
            _routeNormaliser = new Mock<IRouteNormaliser>();
        }

        [Fact]
        public void Null_url_throws_ArgumentNullException()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            Assert.ThrowsAsync<ArgumentNullException>(async () => await parser.ParseUrl(null).ConfigureAwait(false));
        }

        [Fact]
        public void Relative_url_throws_ArgumentException()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            Assert.ThrowsAsync<ArgumentException>(async () => await parser.ParseUrl(new Uri("/somewhere", UriKind.Relative)).ConfigureAwait(false));
        }

        [Fact]
        public async Task Page_number_defaults_to_1()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org")).ConfigureAwait(false);

            Assert.Equal(1, result.PageNumber);
        }

        [Fact]
        public async Task Page_number_is_parsed()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org?page=5")).ConfigureAwait(false);

            Assert.Equal(5, result.PageNumber);
        }

        [Fact]
        public async Task Player_route_is_normalised()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org/players/example")).ConfigureAwait(false);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity("/players/example", "players"), Times.Once);
        }

        [Fact]
        public async Task Player_route_populates_player_from_playerDataSource()
        {
            var player = new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/example" };
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(player.PlayerRoute, "players")).Returns(player.PlayerRoute);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(player.PlayerRoute)).Returns(Task.FromResult(player));

            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{player.PlayerRoute}")).ConfigureAwait(false);

            _playerDataSource.Verify(x => x.ReadPlayerByRoute(player.PlayerRoute), Times.Once);
            Assert.Equal(player, result.Player);
        }

        [Fact]
        public async Task Club_route_is_normalised()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org/clubs/example")).ConfigureAwait(false);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity("/clubs/example", "clubs"), Times.Once);
        }

        [Fact]
        public async Task Club_route_populates_club_from_clubDataSource()
        {
            var club = new Club { ClubId = Guid.NewGuid(), ClubRoute = "/clubs/example" };
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(club.ClubRoute, "clubs")).Returns(club.ClubRoute);
            _clubDataSource.Setup(x => x.ReadClubByRoute(club.ClubRoute)).Returns(Task.FromResult(club));

            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{club.ClubRoute}")).ConfigureAwait(false);

            _clubDataSource.Verify(x => x.ReadClubByRoute(club.ClubRoute), Times.Once);
            Assert.Equal(club, result.Club);
        }

        [Fact]
        public async Task Team_route_is_normalised()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org/teams/example")).ConfigureAwait(false);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity("/teams/example", "teams"), Times.Once);
        }

        [Fact]
        public async Task Team_route_populates_team_from_teamDataSource()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamRoute = "/teams/example" };
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(team.TeamRoute, "teams")).Returns(team.TeamRoute);
            _teamDataSource.Setup(x => x.ReadTeamByRoute(team.TeamRoute, false)).Returns(Task.FromResult(team));

            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{team.TeamRoute}")).ConfigureAwait(false);

            _teamDataSource.Verify(x => x.ReadTeamByRoute(team.TeamRoute, false), Times.Once);
            Assert.Equal(team, result.Team);
        }

        [Fact]
        public async Task MatchLocation_route_is_normalised()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org/locations/example")).ConfigureAwait(false);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity("/locations/example", "locations"), Times.Once);
        }

        [Fact]
        public async Task MatchLocation_route_populates_location_from_matchLocationDataSource()
        {
            var matchLocation = new MatchLocation { MatchLocationId = Guid.NewGuid(), MatchLocationRoute = "/locations/example" };
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(matchLocation.MatchLocationRoute, "locations")).Returns(matchLocation.MatchLocationRoute);
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(matchLocation.MatchLocationRoute, false)).Returns(Task.FromResult(matchLocation));

            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{matchLocation.MatchLocationRoute}")).ConfigureAwait(false);

            _matchLocationDataSource.Verify(x => x.ReadMatchLocationByRoute(matchLocation.MatchLocationRoute, false), Times.Once);
            Assert.Equal(matchLocation, result.MatchLocation);
        }

        [Fact]
        public async Task Competition_route_is_normalised()
        {
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri("https://example.org/competitions/example")).ConfigureAwait(false);

            _routeNormaliser.Verify(x => x.NormaliseRouteToEntity("/competitions/example", "competitions"), Times.Once);
        }

        [Fact]
        public async Task Competition_route_populates_competition_from_competitionDataSource()
        {
            var competition = new Competition { CompetitionId = Guid.NewGuid(), CompetitionRoute = "/competitions/example" };
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(competition.CompetitionRoute, "competitions")).Returns(competition.CompetitionRoute);
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(competition.CompetitionRoute)).Returns(Task.FromResult(competition));

            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{competition.CompetitionRoute}")).ConfigureAwait(false);

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
            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{seasonRoute}")).ConfigureAwait(false);

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
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(season.SeasonRoute, "competitions", Constants.Pages.SeasonUrlRegEx)).Returns(season.SeasonRoute);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(season.SeasonRoute, false)).Returns(Task.FromResult(season));

            var parser = new StatisticsFilterUrlParser(_playerDataSource.Object, _clubDataSource.Object, _teamDataSource.Object, _matchLocationDataSource.Object,
                _competitionDataSource.Object, _seasonDataSource.Object, _routeNormaliser.Object);

            var result = await parser.ParseUrl(new Uri($"https://example.org{season.SeasonRoute}")).ConfigureAwait(false);

            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(season.SeasonRoute, false), Times.Once);
            Assert.Equal(season, result.Season);
        }
    }
}
