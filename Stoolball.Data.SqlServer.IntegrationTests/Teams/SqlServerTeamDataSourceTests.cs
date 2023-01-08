using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTeamDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IRouteNormaliser> routeNormaliser = new();

        public SqlServerTeamDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        private SqlServerTeamDataSource CreateDataSource()
        {
            return new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
        }

        private static Func<Team, bool> TeamsMatchingQuery(string query)
        {
            return x => ((!string.IsNullOrEmpty(x.TeamName) && x.TeamName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                          x.MatchLocations.Any(ml => (!string.IsNullOrEmpty(ml.Locality) && ml.Locality.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                                     (!string.IsNullOrEmpty(ml.Town) && ml.Town.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                                     (!string.IsNullOrEmpty(ml.AdministrativeArea) && ml.AdministrativeArea.Contains(query, StringComparison.OrdinalIgnoreCase))));
        }

        [Fact]
        public async Task Read_minimal_team_by_route_returns_basic_fields()
        {
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithMinimalDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithMinimalDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TestData.TeamWithMinimalDetails!.TeamRoute!, false).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.TeamWithMinimalDetails.TeamId, result!.TeamId);
            Assert.Equal(_databaseFixture.TestData.TeamWithMinimalDetails.TeamName, result.TeamName);
            Assert.Equal(_databaseFixture.TestData.TeamWithMinimalDetails.TeamType, result.TeamType);
            Assert.Equal(_databaseFixture.TestData.TeamWithMinimalDetails.TeamRoute, result.TeamRoute);
            Assert.Equal(_databaseFixture.TestData.TeamWithMinimalDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.TeamWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }


        [Fact]
        public async Task Read_team_by_route_with_related_data_returns_basic_fields()
        {
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.TeamId, result!.TeamId);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.TeamName, result.TeamName);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.TeamType, result.TeamType);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute, result.TeamRoute);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.PlayerType, result.PlayerType);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Introduction, result.Introduction);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.AgeRangeLower, result.AgeRangeLower);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.AgeRangeUpper, result.AgeRangeUpper);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.ClubMark, result.ClubMark);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Facebook, result.Facebook);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Twitter, result.Twitter);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Instagram, result.Instagram);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.YouTube, result.YouTube);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Website, result.Website);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.PlayingTimes, result.PlayingTimes);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Cost, result.Cost);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.PublicContactDetails, result.PublicContactDetails);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.PrivateContactDetails, result.PrivateContactDetails);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.UntilYear, result.UntilYear);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.MemberGroupName, result.MemberGroupName);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.MemberGroupKey, result.MemberGroupKey);
        }

        [Fact]
        public async Task Read_team_by_route_returns_club()
        {
            var teamWithAClub = _databaseFixture.TestData.ClubWithTeamsAndMatchLocation!.Teams.First();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(teamWithAClub.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(teamWithAClub.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTeamByRoute(teamWithAClub.TeamRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.ClubWithTeamsAndMatchLocation.ClubName, result!.Club?.ClubName);
            Assert.Equal(_databaseFixture.TestData.ClubWithTeamsAndMatchLocation.ClubRoute, result.Club?.ClubRoute);
        }

        [Fact]
        public async Task Read_team_by_route_returns_match_locations()
        {
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            foreach (var matchLocation in _databaseFixture.TestData.TeamWithFullDetails.MatchLocations)
            {
                var matchLocationResult = result!.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);

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
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.TeamWithFullDetails.Seasons.Count, result!.Seasons.Count);

            foreach (var season in _databaseFixture.TestData.TeamWithFullDetails.Seasons)
            {
                var seasonResult = result.Seasons.SingleOrDefault(x => x.Season?.SeasonId == season.Season?.SeasonId);

                Assert.NotNull(seasonResult?.Season);
                Assert.NotNull(season.Season);
                Assert.Equal(season.Season!.FromYear, seasonResult!.Season!.FromYear);
                Assert.Equal(season.Season.UntilYear, seasonResult.Season.UntilYear);
                Assert.Equal(season.Season.SeasonRoute, seasonResult.Season.SeasonRoute);
                Assert.Equal(season.Season.Competition?.CompetitionId, seasonResult.Season.Competition?.CompetitionId);
                Assert.Equal(season.Season.Competition?.CompetitionName, seasonResult.Season.Competition?.CompetitionName);
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
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTotalTeams(null).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = CreateDataSource();
            var uniqueTeamNames = _databaseFixture.TestData.Teams.Select(x => x.TeamName).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueTeamNames.Count; i++)
            {
                var filter = new TeamFilter { Query = uniqueTeamNames[i] };

                var result = await teamDataSource.ReadTotalTeams(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Teams.Count(TeamsMatchingQuery(filter.Query));

                Assert.NotEqual(0, result);
                Assert.Equal(expected, result);
            }
        }


        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_locality()
        {
            var teamDataSource = CreateDataSource();
            var uniqueLocalities = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Locality)).Select(x => x.Locality).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueLocalities.Count; i++)
            {
                var filter = new TeamFilter { Query = uniqueLocalities[i] };

                var result = await teamDataSource.ReadTotalTeams(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Teams.Count(TeamsMatchingQuery(filter.Query));

                Assert.NotEqual(0, result);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_town()
        {
            var teamDataSource = CreateDataSource();
            var uniqueTowns = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Town)).Select(x => x.Town).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueTowns.Count; i++)
            {
                var filter = new TeamFilter { Query = uniqueTowns[i] };

                var result = await teamDataSource.ReadTotalTeams(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Teams.Count(TeamsMatchingQuery(filter.Query));

                Assert.NotEqual(0, result);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_administrative_area()
        {
            var teamDataSource = CreateDataSource();
            var uniqueAdministrativeAreas = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.AdministrativeArea)).Select(x => x.AdministrativeArea).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueAdministrativeAreas.Count; i++)
            {
                var filter = new TeamFilter { Query = uniqueAdministrativeAreas[i] };

                var result = await teamDataSource.ReadTotalTeams(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Teams.Count(TeamsMatchingQuery(filter.Query));

                Assert.NotEqual(0, result);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_competition()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { CompetitionIds = new List<Guid> { _databaseFixture.TestData.TeamWithFullDetails!.Seasons[0].Season!.Competition!.CompetitionId!.Value } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.Seasons.Any(s => s.Season?.Competition?.CompetitionId == _databaseFixture.TestData.TeamWithFullDetails.Seasons[0].Season?.Competition?.CompetitionId)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_excluded_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { ExcludeTeamIds = new List<Guid> { _databaseFixture.TestData.TeamWithFullDetails!.TeamId!.Value } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count - 1, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_team_type()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { TeamTypes = new List<TeamType?> { TeamType.Representative } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.TeamType == TeamType.Representative), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_excluding_club_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { IncludeClubTeams = false };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.Club == null), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_active_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { ActiveTeams = true };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => !x.UntilYear.HasValue), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_inactive_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { ActiveTeams = false };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.NotEqual(0, result);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.UntilYear.HasValue), result);
        }

        [Fact]
        public async Task Read_teams_returns_basic_fields()
        {
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            Assert.NotEmpty(results);
            foreach (var team in _databaseFixture.TestData.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamId == team.TeamId);

                Assert.NotNull(result);
                Assert.Equal(team.TeamName, result!.TeamName);
                Assert.Equal(team.TeamRoute, result.TeamRoute);
                Assert.Equal(team.PlayerType, result.PlayerType);
                Assert.Equal(team.UntilYear, result.UntilYear);
                Assert.Equal(team.Introduction, result.Introduction);
                Assert.Equal(team.PlayingTimes, result.PlayingTimes);
                Assert.Equal(team.PublicContactDetails, result.PublicContactDetails);
                Assert.Equal(team.Website, result.Website);
            }
        }

        [Fact]
        public async Task Read_teams_returns_match_locations()
        {
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            Assert.NotEmpty(results);
            foreach (var team in _databaseFixture.TestData.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamId == team.TeamId);
                Assert.NotNull(result);

                foreach (var matchLocation in team.MatchLocations)
                {
                    var resultMatchLocation = result!.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                    Assert.NotNull(resultMatchLocation);
                    Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation!.SecondaryAddressableObjectName);
                    Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                    Assert.Equal(matchLocation.StreetDescription, resultMatchLocation.StreetDescription);
                    Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                    Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
                    Assert.Equal(matchLocation.AdministrativeArea, resultMatchLocation.AdministrativeArea);
                    Assert.Equal(matchLocation.Postcode, resultMatchLocation.Postcode);
                    Assert.Equal(matchLocation.Latitude, resultMatchLocation.Latitude);
                    Assert.Equal(matchLocation.Longitude, resultMatchLocation.Longitude);
                }
            }
        }

        [Fact]
        public async Task Read_teams_sorts_inactive_last()
        {
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            Assert.NotEmpty(results);
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
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!, It.IsAny<Dictionary<string, string?>>())).Returns(_databaseFixture.TestData.TeamWithFullDetails!.TeamRoute!);
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeams(null).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count, results.Count);
            foreach (var team in _databaseFixture.TestData.Teams)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = CreateDataSource();
            var uniqueTeamNames = _databaseFixture.TestData.Teams.Select(x => x.TeamName).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueTeamNames.Count; i++)
            {
                var filter = new TeamFilter { Query = uniqueTeamNames[i] };

                var results = await teamDataSource.ReadTeams(filter).ConfigureAwait(false);
                Assert.NotEmpty(results);

                var expected = _databaseFixture.TestData.Teams.Where(TeamsMatchingQuery(filter.Query)).ToList();
                Assert.Equal(expected.Count, results.Count);
                foreach (var team in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
                }
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_locality()
        {
            var teamDataSource = CreateDataSource();
            var uniqueLocalities = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Locality)).Select(x => x.Locality).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueLocalities.Count; i++)
            {
                var filter = new TeamFilter { Query = uniqueLocalities[i] };

                var results = await teamDataSource.ReadTeams(filter).ConfigureAwait(false);
                Assert.NotEmpty(results);

                var expected = _databaseFixture.TestData.Teams.Where(TeamsMatchingQuery(filter.Query)).ToList();
                Assert.Equal(expected.Count, results.Count);
                foreach (var team in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
                }
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_town()
        {
            var teamDataSource = CreateDataSource();
            var uniqueTowns = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Town)).Select(x => x.Town).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueTowns.Count; i++)
            {
                var query = new TeamFilter { Query = uniqueTowns[i] };

                var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);
                Assert.NotEmpty(results);

                var expected = _databaseFixture.TestData.Teams.Where(TeamsMatchingQuery(query.Query)).ToList();
                Assert.Equal(expected.Count, results.Count);
                foreach (var team in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
                }
            }
        }

        [Fact]
        public async Task Read_teams_supports_case_insensitive_filter_by_administrative_area()
        {
            var teamDataSource = CreateDataSource();
            var uniqueAdministrativeAreas = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.AdministrativeArea)).Select(x => x.AdministrativeArea).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            for (var i = 0; i < uniqueAdministrativeAreas.Count; i++)
            {
                var query = new TeamFilter { Query = uniqueAdministrativeAreas[i] };

                var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);
                Assert.NotEmpty(results);

                var expected = _databaseFixture.TestData.Teams.Where(TeamsMatchingQuery(query.Query)).ToList();
                Assert.Equal(expected.Count, results.Count);
                foreach (var team in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
                }
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_competition()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { CompetitionIds = new List<Guid> { _databaseFixture.TestData.TeamWithFullDetails!.Seasons[0].Season!.Competition!.CompetitionId!.Value } };

            var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.Seasons.Any(s => s.Season?.Competition?.CompetitionId == _databaseFixture.TestData.TeamWithFullDetails.Seasons[0].Season!.Competition!.CompetitionId)), results.Count);
            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.Seasons.Any(s => s.Season?.Competition?.CompetitionId == _databaseFixture.TestData.TeamWithFullDetails.Seasons[0].Season!.Competition!.CompetitionId)))
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_excluded_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { ExcludeTeamIds = new List<Guid> { _databaseFixture.TestData.TeamWithFullDetails!.TeamId!.Value } };

            var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.TeamId != _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value), results.Count);
            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.TeamId != _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_team_type()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { TeamTypes = new List<TeamType?> { TeamType.Representative } };

            var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.TeamType == TeamType.Representative), results.Count);
            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.TeamType == TeamType.Representative))
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_excluding_club_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { IncludeClubTeams = false };

            var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.Club == null), results.Count);
            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.Club == null))
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_active_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { ActiveTeams = true };

            var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => !x.UntilYear.HasValue), results.Count);
            foreach (var team in _databaseFixture.TestData.Teams.Where(x => !x.UntilYear.HasValue))
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_teams_supports_filter_by_inactive_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamFilter { ActiveTeams = false };

            var results = await teamDataSource.ReadTeams(query).ConfigureAwait(false);

            Assert.NotEmpty(results);
            Assert.Equal(_databaseFixture.TestData.Teams.Count(x => x.UntilYear.HasValue), results.Count);
            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.UntilYear.HasValue))
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }
    }
}
