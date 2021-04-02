using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
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

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Competition.Seasons.Count - 1, result.Competition.Seasons.Count); // -1 because it's *other* seasons, not including this one
            foreach (var season in _databaseFixture.SeasonWithFullDetails.Competition.Seasons)
            {
                var resultSeason = result.Competition.Seasons.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                if (season.SeasonId == _databaseFixture.SeasonWithFullDetails.SeasonId)
                {
                    Assert.Null(resultSeason);
                }
                else
                {
                    Assert.NotNull(resultSeason);

                    Assert.Equal(season.FromYear, resultSeason.FromYear);
                    Assert.Equal(season.UntilYear, resultSeason.UntilYear);
                    Assert.Equal(season.SeasonRoute, resultSeason.SeasonRoute);
                }
            }

            int previousFromYear = int.MaxValue, previousUntilYear = int.MaxValue;
            foreach (var season in result.Competition.Seasons)
            {
                Assert.True(season.FromYear <= previousFromYear);
                Assert.True(season.UntilYear <= previousUntilYear);
                previousFromYear = season.FromYear;
                previousUntilYear = season.UntilYear;
            }
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.Teams.Count, result.Teams.Count);

            foreach (var teamInSeason in _databaseFixture.SeasonWithFullDetails.Teams)
            {
                var resultTeam = result.Teams.SingleOrDefault(x => x.Team.TeamId == teamInSeason.Team.TeamId);
                Assert.NotNull(resultTeam);

                Assert.Equal(teamInSeason.WithdrawnDate, resultTeam.WithdrawnDate);
                Assert.Equal(teamInSeason.Team.TeamName, resultTeam.Team.TeamName);
                Assert.Equal(teamInSeason.Team.TeamRoute, resultTeam.Team.TeamRoute);
            }
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_default_over_sets()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.SeasonWithFullDetails.SeasonRoute, "competitions", It.IsAny<string>())).Returns(_databaseFixture.SeasonWithFullDetails.SeasonRoute);
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.SeasonWithFullDetails.SeasonRoute, true).ConfigureAwait(false);

            for (var set = 0; set < _databaseFixture.SeasonWithFullDetails.DefaultOverSets.Count; set++)
            {
                Assert.Equal(_databaseFixture.SeasonWithFullDetails.DefaultOverSets[set].OverSetId, result.DefaultOverSets[set].OverSetId);
                Assert.Equal(_databaseFixture.SeasonWithFullDetails.DefaultOverSets[set].Overs, result.DefaultOverSets[set].Overs);
                Assert.Equal(_databaseFixture.SeasonWithFullDetails.DefaultOverSets[set].BallsPerOver, result.DefaultOverSets[set].BallsPerOver);
            }
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

            var results = await seasonDataSource.ReadPointsRules(_databaseFixture.SeasonWithFullDetails.SeasonId.Value).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.PointsRules.Count, results.Count());
            foreach (var adjustment in _databaseFixture.SeasonWithFullDetails.PointsRules)
            {
                var result = results.SingleOrDefault(x => x.PointsRuleId == adjustment.PointsRuleId);
                Assert.NotNull(result);

                Assert.Equal(adjustment.MatchResultType, result.MatchResultType);
                Assert.Equal(adjustment.HomePoints, result.HomePoints);
                Assert.Equal(adjustment.AwayPoints, result.AwayPoints);
            }
        }

        [Fact]
        public async Task Read_points_adjustments_returns_adjustment_and_team()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await seasonDataSource.ReadPointsAdjustments(_databaseFixture.SeasonWithFullDetails.SeasonId.Value).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.SeasonWithFullDetails.PointsAdjustments.Count, results.Count());
            foreach (var adjustment in _databaseFixture.SeasonWithFullDetails.PointsAdjustments)
            {
                var result = results.SingleOrDefault(x => x.PointsAdjustmentId == adjustment.PointsAdjustmentId);
                Assert.NotNull(result);

                Assert.Equal(adjustment.Team.TeamId, result.Team.TeamId);
                Assert.Equal(adjustment.Team.TeamName, result.Team.TeamName);
                Assert.Equal(adjustment.Points, result.Points);
                Assert.Equal(adjustment.Reason, result.Reason);
            }
        }

        [Fact]
        public async Task Read_seasons_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            foreach (var season in _databaseFixture.Seasons)
            {
                var result = results.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                Assert.NotNull(result);

                Assert.Equal(season.FromYear, result.FromYear);
                Assert.Equal(season.UntilYear, result.UntilYear);
            }
        }

        [Fact]
        public async Task Read_seasons_returns_competitions()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            foreach (var season in _databaseFixture.Seasons)
            {
                var result = results.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                Assert.NotNull(result);

                Assert.Equal(season.Competition.CompetitionName, result.Competition.CompetitionName);
            }
        }

        [Fact]
        public async Task Read_seasons_sorts_by_active_first_then_most_recent()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            var expectedActiveStatus = true;
            int previousFromYear = int.MaxValue, previousUntilYear = int.MaxValue;
            foreach (var season in results)
            {
                // The first time an inactive competition is seen, set a flag to say they must all be inactive.
                // Also reset the tracker that says season years must count down from the most recent.
                if (expectedActiveStatus && season.Competition.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                    previousFromYear = previousUntilYear = int.MaxValue;
                }
                Assert.Equal(expectedActiveStatus, !season.Competition.UntilYear.HasValue);
                Assert.True(season.FromYear <= previousFromYear);
                Assert.True(season.UntilYear <= previousUntilYear);
                previousFromYear = season.FromYear;
                previousUntilYear = season.UntilYear;
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact]
        public async Task Read_seasons_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Seasons.Count, results.Count);
            foreach (var season in _databaseFixture.Seasons)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_summer_season()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { Query = " 2021 season" }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.FromYear == 2021);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_winter_season()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { Query = "2020/21 season" }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.FromYear == 2020 && x.UntilYear == 2021);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_from_year()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { FromYear = DateTimeOffset.UtcNow.Year }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.FromYear == DateTimeOffset.UtcNow.Year);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_until_year()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { UntilYear = DateTimeOffset.UtcNow.Year }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.UntilYear == DateTimeOffset.UtcNow.Year);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_tournaments_enabled()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { EnableTournaments = true }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.EnableTournaments);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_player_type_query()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { Query = "LaDiEs" }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.Competition.PlayerType == PlayerType.Ladies);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_player_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { PlayerTypes = new List<PlayerType> { PlayerType.Mixed } }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.Competition.PlayerType == PlayerType.Mixed);
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_match_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = new SqlServerSeasonDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { MatchTypes = new List<MatchType> { MatchType.LeagueMatch } }).ConfigureAwait(false);

            var expected = _databaseFixture.Seasons.Where(x => x.MatchTypes.Contains(MatchType.LeagueMatch));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }
    }
}
