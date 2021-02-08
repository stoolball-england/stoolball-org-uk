using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerSeasonDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerSeasonDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_season_by_route_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, false).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.SeasonId, result.SeasonId);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.FromYear, result.FromYear);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.Results, result.Results);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, result.SeasonRoute);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.EnableTournaments, result.EnableTournaments);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.PlayersPerTeam, result.PlayersPerTeam);
        }

        [Fact]
        public async Task Read_season_by_route_returns_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, false).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.Competition.CompetitionName, result.Competition.CompetitionName);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.Competition.PlayerType, result.Competition.PlayerType);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.Competition.UntilYear, result.Competition.UntilYear);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.Competition.CompetitionRoute, result.Competition.CompetitionRoute);
            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.Competition.MemberGroupName, result.Competition.MemberGroupName);
        }


        [Fact]
        public async Task Read_season_by_route_returns_match_types()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithMinimalDetails.SeasonRoute, false).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithMinimalDetails.MatchTypes.Count, result.MatchTypes.Count);
            foreach (var matchType in _databaseFixture.SeasonWithMinimalDetails.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.SeasonId, result.SeasonId);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.FromYear, result.FromYear);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Introduction, result.Introduction);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.EnableTournaments, result.EnableTournaments);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.ResultsTableType, result.ResultsTableType);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.EnableLastPlayerBatsOn, result.EnableLastPlayerBatsOn);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.EnableBonusOrPenaltyRuns, result.EnableBonusOrPenaltyRuns);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.EnableRunsScored, result.EnableRunsScored);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.EnableRunsConceded, result.EnableRunsConceded);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Results, result.Results);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.SeasonRoute, result.SeasonRoute);
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.CompetitionName, result.Competition.CompetitionName);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.PlayerType, result.Competition.PlayerType);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.UntilYear, result.Competition.UntilYear);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.Introduction, result.Competition.Introduction);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.PublicContactDetails, result.Competition.PublicContactDetails);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.Website, result.Competition.Website);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.Facebook, result.Competition.Facebook);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.Twitter, result.Competition.Twitter);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.Instagram, result.Competition.Instagram);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.YouTube, result.Competition.YouTube);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.CompetitionRoute, result.Competition.CompetitionRoute);
            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.MemberGroupName, result.Competition.MemberGroupName);
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_other_seasons_latest_first()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_default_over_sets()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_match_types()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.MatchTypes.Count, result.MatchTypes.Count);
            foreach (var matchType in _databaseFixture.SeasonWithFullDetails.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }
        }

        [Fact]
        public async Task Read_points_rules_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadPointsRules(_databaseFixture.SeasonWithFullDetails.SeasonId.Value).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_points_adjustments_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadPointsAdjustments(_databaseFixture.SeasonWithFullDetails.SeasonId.Value).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_returns_competitions()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_sorts_by_active_first_then_most_recent()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_summer_season()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_winter_season()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_player_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_match_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            throw new NotImplementedException();
        }
    }
}
