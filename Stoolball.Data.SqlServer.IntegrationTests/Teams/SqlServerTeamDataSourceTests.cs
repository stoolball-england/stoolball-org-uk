using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerTeamDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerTeamDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_team_by_route_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithMinimalDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithMinimalDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TeamWithMinimalDetails.TeamRoute, false).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamWithMinimalDetails.TeamId, result.TeamId);
            Assert.Equal(_databaseFixture.TeamWithMinimalDetails.TeamName, result.TeamName);
            Assert.Equal(_databaseFixture.TeamWithMinimalDetails.TeamType, result.TeamType);
            Assert.Equal(_databaseFixture.TeamWithMinimalDetails.TeamRoute, result.TeamRoute);
            Assert.Equal(_databaseFixture.TeamWithMinimalDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TeamWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_team_by_route_with_related_data_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TeamWithFullDetails.TeamRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamWithFullDetails.TeamId, result.TeamId);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.TeamName, result.TeamName);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.TeamType, result.TeamType);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.TeamRoute, result.TeamRoute);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.PlayerType, result.PlayerType);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.Introduction, result.Introduction);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.AgeRangeLower, result.AgeRangeLower);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.AgeRangeUpper, result.AgeRangeUpper);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.ClubMark, result.ClubMark);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.Facebook, result.Facebook);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.Twitter, result.Twitter);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.Instagram, result.Instagram);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.YouTube, result.YouTube);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.Website, result.Website);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.PlayingTimes, result.PlayingTimes);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.Cost, result.Cost);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.PublicContactDetails, result.PublicContactDetails);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.PrivateContactDetails, result.PrivateContactDetails);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.MemberGroupName, result.MemberGroupName);
            Assert.Equal(_databaseFixture.TeamWithFullDetails.MemberGroupKey, result.MemberGroupKey);
        }

        [Fact]
        public async Task Read_team_by_route_returns_club()
        {
            var teamWithAClub = _databaseFixture.ClubWithTeams.Teams.First();
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(teamWithAClub.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(teamWithAClub.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeamByRoute(teamWithAClub.TeamRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.ClubWithTeams.ClubName, result.Club.ClubName);
            Assert.Equal(_databaseFixture.ClubWithTeams.ClubRoute, result.Club.ClubRoute);
        }

        [Fact]
        public async Task Read_team_by_route_returns_match_locations()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TeamWithFullDetails.TeamRoute, true).ConfigureAwait(false);

            foreach (var matchLocation in _databaseFixture.TeamWithFullDetails.MatchLocations)
            {
                var matchLocationResult = result.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);

                Assert.NotNull(matchLocationResult);
                Assert.Equal(matchLocation.MatchLocationRoute, matchLocation.MatchLocationRoute);
                Assert.Equal(matchLocation.SecondaryAddressableObjectName, matchLocation.SecondaryAddressableObjectName);
                Assert.Equal(matchLocation.PrimaryAddressableObjectName, matchLocation.PrimaryAddressableObjectName);
                Assert.Equal(matchLocation.Locality, matchLocation.Locality);
                Assert.Equal(matchLocation.Town, matchLocation.Town);
                Assert.Equal(matchLocation.AdministrativeArea, matchLocation.AdministrativeArea);
            }
        }

        [Fact]
        public async Task Read_team_by_route_returns_seasons_with_competitions_and_match_types()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TeamWithFullDetails.TeamRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamWithFullDetails.Seasons.Count, result.Seasons.Count);
            foreach (var season in _databaseFixture.TeamWithFullDetails.Seasons)
            {
                var seasonResult = result.Seasons.SingleOrDefault(x => x.Season.SeasonId == season.Season.SeasonId);

                Assert.NotNull(seasonResult);
                Assert.Equal(season.Season.FromYear, seasonResult.Season.FromYear);
                Assert.Equal(season.Season.UntilYear, seasonResult.Season.UntilYear);
                Assert.Equal(season.Season.SeasonRoute, seasonResult.Season.SeasonRoute);
                Assert.Equal(season.Season.Competition.CompetitionId, seasonResult.Season.Competition.CompetitionId);
                Assert.Equal(season.Season.Competition.CompetitionName, seasonResult.Season.Competition.CompetitionName);
                Assert.Equal(season.Season.MatchTypes.Count, seasonResult.Season.MatchTypes.Count);

                foreach (var expectedMatchType in season.Season.MatchTypes)
                {
                    Assert.Contains(expectedMatchType, season.Season.MatchTypes);
                }
            }
        }


        [Fact]
        public async Task Read_total_teams_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTotalTeams(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "DeTaIlS" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }


        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_locality()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "LoCaLiTy" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_town()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "ToWn" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_administrative_area()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "CoUnTy" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { CompetitionIds = new List<Guid> { _databaseFixture.TeamWithFullDetails.Seasons[0].Season.Competition.CompetitionId.Value } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.Seasons.Any(s => s.Season.Competition?.CompetitionId == _databaseFixture.TeamWithFullDetails.Seasons[0].Season.Competition.CompetitionId.Value)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_excluded_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { ExcludeTeamIds = new List<Guid> { _databaseFixture.TeamWithFullDetails.TeamId.Value } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count - 1, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_team_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { TeamTypes = new List<TeamType> { TeamType.Representative } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamType == TeamType.Representative), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_excluding_club_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { IncludeClubTeams = false };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.Club == null), result);
        }

        [Fact]
        public async Task Read_teams_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamId == team.TeamId);

                Assert.NotNull(result);
                Assert.Equal(team.TeamName, result.TeamName);
                Assert.Equal(team.TeamRoute, result.TeamRoute);
                Assert.Equal(team.PlayerType, result.PlayerType);
                Assert.Equal(team.UntilYear, result.UntilYear);
            }
        }

        [Fact]
        public async Task Read_teams_returns_match_locations()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamId == team.TeamId);
                Assert.NotNull(result);

                foreach (var matchLocation in team.MatchLocations)
                {
                    var resultMatchLocation = result.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                    Assert.NotNull(resultMatchLocation);
                    Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                    Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
                }
            }
        }

        [Fact]
        public async Task Read_teams_sorts_inactive_last()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            var expectedActiveStatus = true;
            foreach (var team in results)
            {
                // The first time an inactive team is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && team.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, !team.UntilYear.HasValue);
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact]
        public async Task Read_teams_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithFullDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithFullDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_team_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "DeTaIlS" };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_locality()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "LoCaLiTy" };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_town()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "ToWn" };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_administrative_area()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { Query = "CoUnTy" };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { CompetitionIds = new List<Guid> { _databaseFixture.TeamWithFullDetails.Seasons[0].Season.Competition.CompetitionId.Value } };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.Seasons.Any(s => s.Season.Competition?.CompetitionId == _databaseFixture.TeamWithFullDetails.Seasons[0].Season.Competition.CompetitionId.Value)), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.Seasons.Any(s => s.Season.Competition?.CompetitionId == _databaseFixture.TeamWithFullDetails.Seasons[0].Season.Competition.CompetitionId.Value)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_excluded_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { ExcludeTeamIds = new List<Guid> { _databaseFixture.TeamWithFullDetails.TeamId.Value } };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamId != _databaseFixture.TeamWithFullDetails.TeamId.Value), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamId != _databaseFixture.TeamWithFullDetails.TeamId.Value))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_team_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { TeamTypes = new List<TeamType> { TeamType.Representative } };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamType == TeamType.Representative), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamType == TeamType.Representative))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_excluding_club_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new TeamQuery { IncludeClubTeams = false };

            var result = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.Club == null), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.Club == null))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }
    }
}
