using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerSeasonDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IRouteNormaliser> _routeNormaliser = new();

        public SqlServerSeasonDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }
        private SqlServerSeasonDataSource CreateDataSource()
        {
            return new SqlServerSeasonDataSource(
                _databaseFixture.ConnectionFactory,
                _routeNormaliser.Object
                );
        }

        [Fact]
        public async Task Read_season_by_id_returns_basic_fields()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonId!.Value).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonId, result!.SeasonId);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.FromYear, result.FromYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Results, result.Results);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonRoute, result.SeasonRoute);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.EnableTournaments, result.EnableTournaments);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.PlayersPerTeam, result.PlayersPerTeam);
        }

        [Fact]
        public async Task Read_season_by_id_returns_competition()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonId!.Value).ConfigureAwait(false);

            Assert.NotNull(result?.Competition);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition!.CompetitionName, result!.Competition!.CompetitionName);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.PlayerType, result.Competition.PlayerType);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.UntilYear, result.Competition.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.CompetitionRoute, result.Competition.CompetitionRoute);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.MemberGroupName, result.Competition.MemberGroupName);
        }


        [Fact]
        public async Task Read_season_by_id_returns_match_types()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonId!.Value).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.MatchTypes.Count, result!.MatchTypes.Count);
            foreach (var matchType in _databaseFixture.TestData.SeasonWithMinimalDetails.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }
        }

        [Fact]
        public async Task Read_season_by_id_with_related_data_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.SeasonId, result!.SeasonId);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.FromYear, result.FromYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Introduction, result.Introduction);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableTournaments, result.EnableTournaments);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.ResultsTableType, result.ResultsTableType);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableLastPlayerBatsOn, result.EnableLastPlayerBatsOn);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableBonusOrPenaltyRuns, result.EnableBonusOrPenaltyRuns);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableRunsScored, result.EnableRunsScored);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableRunsConceded, result.EnableRunsConceded);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Results, result.Results);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute, result.SeasonRoute);
        }

        [Fact]
        public async Task Read_season_by_id_with_related_data_returns_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value, true).ConfigureAwait(false);

            Assert.NotNull(result?.Competition);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition!.CompetitionName, result!.Competition!.CompetitionName);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.PlayerType, result.Competition.PlayerType);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.UntilYear, result.Competition.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Introduction, result.Competition.Introduction);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.PublicContactDetails, result.Competition.PublicContactDetails);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Website, result.Competition.Website);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Facebook, result.Competition.Facebook);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Twitter, result.Competition.Twitter);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Instagram, result.Competition.Instagram);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.YouTube, result.Competition.YouTube);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.CompetitionRoute, result.Competition.CompetitionRoute);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.MemberGroupName, result.Competition.MemberGroupName);
        }

        [Fact]
        public async Task Read_season_by_id_with_related_data_returns_other_seasons_latest_first()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value, true).ConfigureAwait(false);

            Assert.NotNull(result?.Competition);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails!.Competition!.Seasons.Count - 1, result!.Competition!.Seasons.Count); // -1 because it's *other* seasons, not including this one
            foreach (var season in _databaseFixture.TestData.SeasonWithFullDetails.Competition.Seasons)
            {
                var resultSeason = result.Competition.Seasons.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                if (season.SeasonId == _databaseFixture.TestData.SeasonWithFullDetails.SeasonId)
                {
                    Assert.Null(resultSeason);
                }
                else
                {
                    Assert.NotNull(resultSeason);

                    Assert.Equal(season.FromYear, resultSeason!.FromYear);
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
        public async Task Read_season_by_id_with_related_data_returns_teams()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Teams.Count, result!.Teams.Count);

            foreach (var teamInSeason in _databaseFixture.TestData.SeasonWithFullDetails.Teams)
            {
                var resultTeam = result.Teams.SingleOrDefault(x => x.Team?.TeamId == teamInSeason.Team?.TeamId);
                Assert.NotNull(resultTeam);

                Assert.Equal(teamInSeason.WithdrawnDate, resultTeam!.WithdrawnDate);
                Assert.Equal(teamInSeason.Team?.TeamName, resultTeam.Team?.TeamName);
                Assert.Equal(teamInSeason.Team?.TeamRoute, resultTeam.Team?.TeamRoute);
            }
        }

        [Fact]
        public async Task Read_season_by_id_with_related_data_returns_default_over_sets()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value, true).ConfigureAwait(false);

            Assert.NotNull(result);
            for (var set = 0; set < _databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets.Count; set++)
            {
                Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets[set].OverSetId, result!.DefaultOverSets[set].OverSetId);
                Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets[set].Overs, result.DefaultOverSets[set].Overs);
                Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets[set].BallsPerOver, result.DefaultOverSets[set].BallsPerOver);
            }
        }

        [Fact]
        public async Task Read_season_by_id_with_related_data_returns_match_types()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonById(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.MatchTypes.Count, result!.MatchTypes.Count);
            foreach (var matchType in _databaseFixture.TestData.SeasonWithFullDetails.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }
        }

        [Fact]
        public async Task Read_season_by_route_returns_basic_fields()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonRoute!, false).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonId, result!.SeasonId);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.FromYear, result.FromYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Results, result.Results);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonRoute, result.SeasonRoute);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.EnableTournaments, result.EnableTournaments);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.PlayersPerTeam, result.PlayersPerTeam);
        }

        [Fact]
        public async Task Read_season_by_route_returns_competition()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonRoute!, false).ConfigureAwait(false);

            Assert.NotNull(result?.Competition);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition!.CompetitionName, result!.Competition!.CompetitionName);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.PlayerType, result.Competition.PlayerType);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.UntilYear, result.Competition.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.CompetitionRoute, result.Competition.CompetitionRoute);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.Competition.MemberGroupName, result.Competition.MemberGroupName);
        }


        [Fact]
        public async Task Read_season_by_route_returns_match_types()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithMinimalDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithMinimalDetails.SeasonRoute!, false).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithMinimalDetails.MatchTypes.Count, result!.MatchTypes.Count);
            foreach (var matchType in _databaseFixture.TestData.SeasonWithMinimalDetails.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_basic_fields()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.SeasonId, result!.SeasonId);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.FromYear, result.FromYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Introduction, result.Introduction);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableTournaments, result.EnableTournaments);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.ResultsTableType, result.ResultsTableType);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableLastPlayerBatsOn, result.EnableLastPlayerBatsOn);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableBonusOrPenaltyRuns, result.EnableBonusOrPenaltyRuns);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableRunsScored, result.EnableRunsScored);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.EnableRunsConceded, result.EnableRunsConceded);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Results, result.Results);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute, result.SeasonRoute);
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_competition()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result?.Competition);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition!.CompetitionName, result!.Competition!.CompetitionName);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.PlayerType, result.Competition.PlayerType);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.UntilYear, result.Competition.UntilYear);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Introduction, result.Competition.Introduction);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.PublicContactDetails, result.Competition.PublicContactDetails);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Website, result.Competition.Website);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Facebook, result.Competition.Facebook);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Twitter, result.Competition.Twitter);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.Instagram, result.Competition.Instagram);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.YouTube, result.Competition.YouTube);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.CompetitionRoute, result.Competition.CompetitionRoute);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition.MemberGroupName, result.Competition.MemberGroupName);
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_other_seasons_latest_first()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result?.Competition);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Competition!.Seasons.Count - 1, result!.Competition!.Seasons.Count); // -1 because it's *other* seasons, not including this one
            foreach (var season in _databaseFixture.TestData.SeasonWithFullDetails.Competition.Seasons)
            {
                var resultSeason = result.Competition.Seasons.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                if (season.SeasonId == _databaseFixture.TestData.SeasonWithFullDetails.SeasonId)
                {
                    Assert.Null(resultSeason);
                }
                else
                {
                    Assert.NotNull(resultSeason);

                    Assert.Equal(season.FromYear, resultSeason!.FromYear);
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
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.Teams.Count, result!.Teams.Count);

            foreach (var teamInSeason in _databaseFixture.TestData.SeasonWithFullDetails.Teams)
            {
                var resultTeam = result.Teams.SingleOrDefault(x => x.Team?.TeamId == teamInSeason.Team?.TeamId);
                Assert.NotNull(resultTeam);

                Assert.Equal(teamInSeason.WithdrawnDate, resultTeam!.WithdrawnDate);
                Assert.Equal(teamInSeason.Team?.TeamName, resultTeam.Team?.TeamName);
                Assert.Equal(teamInSeason.Team?.TeamRoute, resultTeam.Team?.TeamRoute);
            }
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_default_over_sets()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            for (var set = 0; set < _databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets.Count; set++)
            {
                Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets[set].OverSetId, result!.DefaultOverSets[set].OverSetId);
                Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets[set].Overs, result.DefaultOverSets[set].Overs);
                Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.DefaultOverSets[set].BallsPerOver, result.DefaultOverSets[set].BallsPerOver);
            }
        }

        [Fact]
        public async Task Read_season_by_route_with_related_data_returns_match_types()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!, "competitions", It.IsAny<string>())).Returns(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonRoute!);
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasonByRoute(_databaseFixture.TestData.SeasonWithFullDetails.SeasonRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.MatchTypes.Count, result!.MatchTypes.Count);
            foreach (var matchType in _databaseFixture.TestData.SeasonWithFullDetails.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }
        }

        [Fact]
        public async Task Read_points_rules_returns_basic_fields()
        {
            var seasonDataSource = CreateDataSource();

            var results = await seasonDataSource.ReadPointsRules(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.True(results.Any());
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.PointsRules.Count, results.Count());
            foreach (var adjustment in _databaseFixture.TestData.SeasonWithFullDetails.PointsRules)
            {
                var result = results.SingleOrDefault(x => x.PointsRuleId == adjustment.PointsRuleId);
                Assert.NotNull(result);

                Assert.Equal(adjustment.MatchResultType, result!.MatchResultType);
                Assert.Equal(adjustment.HomePoints, result.HomePoints);
                Assert.Equal(adjustment.AwayPoints, result.AwayPoints);
            }
        }

        [Fact]
        public async Task Read_points_adjustments_returns_adjustment_and_team()
        {
            var seasonDataSource = CreateDataSource();

            var results = await seasonDataSource.ReadPointsAdjustments(_databaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value).ConfigureAwait(false);

            Assert.True(results.Any());
            Assert.Equal(_databaseFixture.TestData.SeasonWithFullDetails.PointsAdjustments.Count, results.Count());
            foreach (var adjustment in _databaseFixture.TestData.SeasonWithFullDetails.PointsAdjustments)
            {
                var result = results.SingleOrDefault(x => x.PointsAdjustmentId == adjustment.PointsAdjustmentId);
                Assert.NotNull(result);

                Assert.Equal(adjustment.Team?.TeamId, result!.Team?.TeamId);
                Assert.Equal(adjustment.Team?.TeamName, result.Team?.TeamName);
                Assert.Equal(adjustment.Points, result.Points);
                Assert.Equal(adjustment.Reason, result.Reason);
            }
        }

        [Fact]
        public async Task Read_seasons_returns_basic_fields()
        {
            var seasonDataSource = CreateDataSource();

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            Assert.True(results.Any());
            foreach (var season in _databaseFixture.TestData.Seasons)
            {
                var result = results.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                Assert.NotNull(result);

                Assert.Equal(season.FromYear, result!.FromYear);
                Assert.Equal(season.UntilYear, result.UntilYear);
            }
        }

        [Fact]
        public async Task Read_seasons_returns_competitions()
        {
            var seasonDataSource = CreateDataSource();

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            Assert.True(results.Any());
            foreach (var season in _databaseFixture.TestData.Seasons)
            {
                var result = results.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                Assert.NotNull(result);

                Assert.Equal(season.Competition?.CompetitionName, result!.Competition?.CompetitionName);
            }
        }

        [Fact]
        public async Task Read_seasons_sorts_by_active_first_then_most_recent()
        {
            var seasonDataSource = CreateDataSource();

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            var expectedActiveStatus = true;
            int previousFromYear = int.MaxValue, previousUntilYear = int.MaxValue;
            Assert.True(results.Any());
            Assert.NotNull(results);
            foreach (var season in results)
            {
                // The first time an inactive competition is seen, set a flag to say they must all be inactive.
                // Also reset the tracker that says season years must count down from the most recent.
                Assert.NotNull(season.Competition);
                if (expectedActiveStatus && season.Competition!.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                    previousFromYear = previousUntilYear = int.MaxValue;
                }
                Assert.Equal(expectedActiveStatus, !season.Competition!.UntilYear.HasValue);
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
            var seasonDataSource = CreateDataSource();

            var results = await seasonDataSource.ReadSeasons(null).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.True(results.Any());
            Assert.Equal(_databaseFixture.TestData.Seasons.Count, results.Count);
            foreach (var season in _databaseFixture.TestData.Seasons)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_summer_season()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { Query = " 2021 season" }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.FromYear == 2021);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_winter_season()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { Query = "2020/21 season" }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.FromYear == 2020 && x.UntilYear == 2021);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_from_year()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { FromYear = DateTimeOffset.UtcNow.Year }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.FromYear == DateTimeOffset.UtcNow.Year);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_until_year()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { UntilYear = DateTimeOffset.UtcNow.Year }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.UntilYear == DateTimeOffset.UtcNow.Year);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_tournaments_enabled()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { EnableTournaments = true }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.EnableTournaments);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_case_insensitive_filter_by_player_type_query()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { Query = "LaDiEs" }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.Competition?.PlayerType == PlayerType.Ladies);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_player_type()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { PlayerTypes = new List<PlayerType> { PlayerType.Mixed } }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.Competition?.PlayerType == PlayerType.Mixed);
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }

        [Fact]
        public async Task Read_seasons_supports_filter_by_match_type()
        {
            var seasonDataSource = CreateDataSource();

            var result = await seasonDataSource.ReadSeasons(new CompetitionFilter { MatchTypes = new List<MatchType> { MatchType.LeagueMatch } }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Seasons.Where(x => x.MatchTypes.Contains(MatchType.LeagueMatch));
            Assert.NotNull(result);
            Assert.True(result.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var season in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SeasonId == season.SeasonId));
            }
        }
    }
}
