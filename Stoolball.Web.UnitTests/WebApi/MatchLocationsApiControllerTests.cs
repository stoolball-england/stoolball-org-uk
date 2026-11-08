using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.WebApi;
using Xunit;

namespace Stoolball.Web.UnitTests.WebApi
{
    public class MatchLocationsApiControllerTests
    {
        private readonly Mock<IMatchLocationDataSource> _locationDataSource = new();
        private MatchLocationFilter? _filterUsed;

        public MatchLocationsApiControllerTests()
        {
            _locationDataSource.Setup(x => x.ReadMatchLocations(It.IsAny<MatchLocationFilter>()))
                .Callback<MatchLocationFilter>(f => _filterUsed = f)
                .ReturnsAsync(new List<MatchLocation>());
        }

        private MatchLocationsApiController CreateController() => new(_locationDataSource.Object);

        private static MatchLocation CreateLocation() => new()
        {
            MatchLocationId = Guid.NewGuid(),
            PrimaryAddressableObjectName = "Example ground",
            Town = "Exampleton",
            Latitude = 51.5,
            Longitude = -0.1
        };

        private void SetupLocations(params MatchLocation[] locations)
        {
            _locationDataSource.Setup(x => x.ReadMatchLocations(It.IsAny<MatchLocationFilter>())).ReturnsAsync(locations.ToList());
        }

        [Fact]
        public void Null_location_data_source_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchLocationsApiController(null!));
        }

        public static TheoryData<string> Endpoints { get; } = new() { "autocomplete", "map" };

        private Task CallEndpoint(string endpoint, string? query = null, string[]? not = null, string[]? teamType = null, bool? hasActiveTeams = null, Guid? season = null)
        {
            var controller = CreateController();
            return endpoint == "autocomplete"
                ? controller.Autocomplete(query, not, teamType, hasActiveTeams, season)
                : controller.Map(query, not, teamType, hasActiveTeams, season);
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task Query_and_active_teams_filter_are_passed_to_data_source(string endpoint)
        {
            await CallEndpoint(endpoint, query: "example", hasActiveTeams: true);

            Assert.Equal("example", _filterUsed!.Query);
            Assert.True(_filterUsed.HasActiveTeams);
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task No_parameters_gives_unfiltered_query(string endpoint)
        {
            await CallEndpoint(endpoint);

            Assert.Null(_filterUsed!.Query);
            Assert.Null(_filterUsed.HasActiveTeams);
            Assert.Empty(_filterUsed.ExcludeMatchLocationIds);
            Assert.Empty(_filterUsed.TeamTypes);
            Assert.Empty(_filterUsed.SeasonIds);
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task Valid_not_ids_are_excluded_and_invalid_or_null_ids_are_ignored(string endpoint)
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            await CallEndpoint(endpoint, not: [first.ToString(), "not-a-guid", null!, second.ToString()]);

            Assert.Equal([first, second], _filterUsed!.ExcludeMatchLocationIds);
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task Valid_team_types_are_filtered_ignoring_case_and_invalid_types_are_ignored(string endpoint)
        {
            await CallEndpoint(endpoint, teamType: ["regular", "NotATeamType", "SchoolClub"]);

            Assert.Equal([TeamType.Regular, TeamType.SchoolClub], _filterUsed!.TeamTypes);
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task Season_is_added_to_filter(string endpoint)
        {
            var season = Guid.NewGuid();

            await CallEndpoint(endpoint, season: season);

            Assert.Equal([season], _filterUsed!.SeasonIds);
        }

        [Fact]
        public async Task Autocomplete_with_no_locations_returns_empty_suggestions()
        {
            var controller = CreateController();

            var result = await controller.Autocomplete();

            Assert.Empty(result.suggestions);
        }

        [Fact]
        public async Task Autocomplete_suggests_location_name_with_town_and_location_id_as_data()
        {
            var location = CreateLocation();
            SetupLocations(location);
            var controller = CreateController();

            var result = await controller.Autocomplete();

            var suggestion = Assert.Single(result.suggestions);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), suggestion.value);
            Assert.Contains("Exampleton", suggestion.value);
            Assert.Equal(location.MatchLocationId.ToString(), suggestion.data);
        }

        [Fact]
        public async Task Map_with_no_locations_returns_empty_result()
        {
            var controller = CreateController();

            var result = await controller.Map();

            Assert.Empty(result);
        }

        [Fact]
        public async Task Map_returns_location_name_and_coordinates()
        {
            var location = CreateLocation();
            SetupLocations(location);
            var controller = CreateController();

            var result = await controller.Map();

            var mapped = Assert.Single(result);
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), mapped.name);
            Assert.Equal(51.5, mapped.latitude);
            Assert.Equal(-0.1, mapped.longitude);
            Assert.Empty(mapped.teams);
        }

        [Fact]
        public async Task Map_returns_teams_marking_those_with_an_end_year_as_inactive()
        {
            var location = CreateLocation();
            var active = new Team { TeamName = "Active team", PlayerType = PlayerType.Mixed, TeamRoute = "/teams/active" };
            var inactive = new Team { TeamName = "Inactive team", PlayerType = PlayerType.Mixed, TeamRoute = "/teams/inactive", UntilYear = 2020 };
            location.Teams.AddRange([active, inactive]);
            SetupLocations(location);
            var controller = CreateController();

            var result = await controller.Map();

            var teams = Assert.Single(result).teams.ToList();
            Assert.Equal(2, teams.Count);
            Assert.Equal(active.TeamNameAndPlayerType(), teams[0].name);
            Assert.Equal("/teams/active", teams[0].route);
            Assert.True(teams[0].active);
            Assert.Equal(inactive.TeamNameAndPlayerType(), teams[1].name);
            Assert.Equal("/teams/inactive", teams[1].route);
            Assert.False(teams[1].active);
        }
    }
}
