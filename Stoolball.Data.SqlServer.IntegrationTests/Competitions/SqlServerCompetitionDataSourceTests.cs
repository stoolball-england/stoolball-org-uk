using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Navigation;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerCompetitionDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerCompetitionDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_competition_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.CompetitionWithMinimalDetails.CompetitionRoute, "competitions")).Returns(_databaseFixture.TestData.CompetitionWithMinimalDetails.CompetitionRoute);
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitionByRoute(_databaseFixture.TestData.CompetitionWithMinimalDetails.CompetitionRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_competition_by_route_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute, "competitions")).Returns(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute);
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitionByRoute(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionId, result!.CompetitionId);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionName, result.CompetitionName);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.PlayerType, result.PlayerType);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Introduction, result.Introduction);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.PublicContactDetails, result.PublicContactDetails);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.PrivateContactDetails, result.PrivateContactDetails);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Facebook, result.Facebook);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Twitter, result.Twitter);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Instagram, result.Instagram);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.YouTube, result.YouTube);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Website, result.Website);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute, result.CompetitionRoute);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.MemberGroupKey, result.MemberGroupKey);
            Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.MemberGroupName, result.MemberGroupName);
        }


        [Fact]
        public async Task Read_competition_by_route_returns_seasons_most_recent_first()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute, "competitions")).Returns(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute);
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitionByRoute(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute).ConfigureAwait(false);

            Assert.NotNull(result);
            for (var season = 0; season < _databaseFixture.TestData.CompetitionWithFullDetails.Seasons.Count; season++)
            {
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].FromYear, result!.Seasons[season].FromYear);
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].UntilYear, result.Seasons[season].UntilYear);
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].SeasonRoute, result.Seasons[season].SeasonRoute);
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].PlayersPerTeam, result.Seasons[season].PlayersPerTeam);
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].EnableTournaments, result.Seasons[season].EnableTournaments);
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].EnableLastPlayerBatsOn, result.Seasons[season].EnableLastPlayerBatsOn);
                Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].EnableBonusOrPenaltyRuns, result.Seasons[season].EnableBonusOrPenaltyRuns);
            }
        }

        [Fact]
        public async Task Read_competition_by_route_returns_default_over_sets()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute, "competitions")).Returns(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute);
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitionByRoute(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute).ConfigureAwait(false);

            Assert.NotNull(result);
            for (var season = 0; season < _databaseFixture.TestData.CompetitionWithFullDetails.Seasons.Count; season++)
            {
                for (var set = 0; set < _databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].DefaultOverSets.Count; set++)
                {
                    Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].DefaultOverSets[set].OverSetId, result!.Seasons[season].DefaultOverSets[set].OverSetId);
                    Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].DefaultOverSets[set].OverSetNumber, result.Seasons[season].DefaultOverSets[set].OverSetNumber);
                    Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].DefaultOverSets[set].Overs, result.Seasons[season].DefaultOverSets[set].Overs);
                    Assert.Equal(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].DefaultOverSets[set].BallsPerOver, result.Seasons[season].DefaultOverSets[set].BallsPerOver);
                }
            }
        }

        [Fact]
        public async Task Read_competition_by_route_returns_match_types()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute, "competitions")).Returns(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute);
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitionByRoute(_databaseFixture.TestData.CompetitionWithFullDetails.CompetitionRoute).ConfigureAwait(false);

            Assert.NotNull(result);
            for (var season = 0; season < _databaseFixture.TestData.CompetitionWithFullDetails.Seasons.Count; season++)
            {
                for (var matchType = 0; matchType < _databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].MatchTypes.Count; matchType++)
                {
                    Assert.Contains(_databaseFixture.TestData.CompetitionWithFullDetails.Seasons[season].MatchTypes[matchType], result!.Seasons[season].MatchTypes);
                }
            }
        }

        [Fact]
        public async Task Read_total_competitions_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadTotalCompetitions(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.Competitions.Count, result);
        }


        [Fact]
        public async Task Read_total_competitions_supports_case_insensitive_filter_by_partial_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new CompetitionFilter { Query = "MiNiMaL" };

            var result = await competitionDataSource.ReadTotalCompetitions(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.Competitions.Count(x => x.CompetitionName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }


        [Fact]
        public async Task Read_total_competitions_supports_case_insensitive_filter_by_partial_player_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new CompetitionFilter { Query = "JuNioR" };

            var result = await competitionDataSource.ReadTotalCompetitions(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.Competitions.Count(x => x.PlayerType.ToString().Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_competitions_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await competitionDataSource.ReadCompetitions(new CompetitionFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Competitions.Count } }).ConfigureAwait(false);

            foreach (var competition in _databaseFixture.TestData.Competitions)
            {
                var result = results.SingleOrDefault(x => x.CompetitionId == competition.CompetitionId);

                Assert.NotNull(result);
                Assert.Equal(competition.CompetitionName, result!.CompetitionName);
                Assert.Equal(competition.CompetitionRoute, result.CompetitionRoute);
                Assert.Equal(competition.UntilYear, result.UntilYear);
                Assert.Equal(competition.PlayerType, result.PlayerType);
            }
        }


        [Fact]
        public async Task Read_competitions_returns_latest_season_with_route_and_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await competitionDataSource.ReadCompetitions(new CompetitionFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Competitions.Count } }).ConfigureAwait(false);

            foreach (var competition in _databaseFixture.TestData.Competitions)
            {
                var result = results.SingleOrDefault(x => x.CompetitionId == competition.CompetitionId);
                Assert.NotNull(result);

                var expectedSeason = competition.Seasons.OrderByDescending(x => x.FromYear).FirstOrDefault();
                if (expectedSeason != null)
                {
                    var resultSeason = result!.Seasons.SingleOrDefault(x => x.SeasonId == expectedSeason.SeasonId);
                    Assert.NotNull(resultSeason);
                    Assert.Equal(expectedSeason.SeasonRoute, resultSeason!.SeasonRoute);

                    foreach (var team in expectedSeason.Teams)
                    {
                        var resultTeam = resultSeason.Teams.SingleOrDefault(x => x.Team.TeamId == team.Team.TeamId);

                        Assert.NotNull(resultTeam);
                    }
                }
                else
                {
                    Assert.Empty(result!.Seasons);
                }

            }
        }

        [Fact]
        public async Task Read_competitions_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitions(new CompetitionFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Competitions.Count } }).ConfigureAwait(false);

            foreach (var competition in _databaseFixture.TestData.Competitions)
            {
                Assert.NotNull(result.Single(x => x.CompetitionId == competition.CompetitionId));
            }
        }


        [Fact]
        public async Task Read_competitions_supports_case_insensitive_filter_by_partial_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new CompetitionFilter { Query = "MiNiMaL", Paging = new Paging { PageSize = _databaseFixture.TestData.Competitions.Count } };

            var result = await competitionDataSource.ReadCompetitions(query).ConfigureAwait(false);

            foreach (var competition in _databaseFixture.TestData.Competitions.Where(x => x.CompetitionName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.Single(x => x.CompetitionId == competition.CompetitionId));
            }
        }


        [Fact]
        public async Task Read_competitions_supports_case_insensitive_filter_by_partial_player_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new CompetitionFilter { Query = "JuNioR", Paging = new Paging { PageSize = _databaseFixture.TestData.Competitions.Count } };

            var result = await competitionDataSource.ReadCompetitions(query).ConfigureAwait(false);

            foreach (var competition in _databaseFixture.TestData.Competitions.Where(x => x.PlayerType.ToString().Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.Single(x => x.CompetitionId == competition.CompetitionId));
            }
        }

        [Fact]
        public async Task Read_competitions_sorts_by_most_recently_active_then_no_seasons_then_inactive()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitions(new CompetitionFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Competitions.Count } }).ConfigureAwait(false);

            var expectedActiveStatus = true;
            var hasSeasonsIfActive = true;
            foreach (var competition in result)
            {
                // The first time an active competition with no seasons is seen, set a flag to say they must all have no seasons if active
                if (hasSeasonsIfActive && !competition.UntilYear.HasValue && competition.Seasons.Count == 0)
                {
                    hasSeasonsIfActive = false;
                }
                if (!competition.UntilYear.HasValue)
                {
                    Assert.Equal(hasSeasonsIfActive, competition.Seasons.Any());
                }

                // The first time an inactive competition is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && competition.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, !competition.UntilYear.HasValue);
            }
        }


        [Fact]
        public async Task Read_competitions_pages_results()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.Competitions.Count;
            while (remaining > 0)
            {
                var result = await competitionDataSource.ReadCompetitions(new CompetitionFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } }).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, result.Count);

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
