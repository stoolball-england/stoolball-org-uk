using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Teams;
using Stoolball.Web.WebApi;
using Xunit;

namespace Stoolball.Web.UnitTests.WebApi
{
    public class TeamsApiControllerTests
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private TeamFilter? _filterUsed;

        public TeamsApiControllerTests()
        {
            _teamDataSource.Setup(x => x.ReadTeams(It.IsAny<TeamFilter>()))
                .Callback<TeamFilter>(f => _filterUsed = f)
                .ReturnsAsync(new List<Team>());
        }

        private TeamsApiController CreateController() => new(_teamDataSource.Object);

        [Fact]
        public void Null_team_data_source_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TeamsApiController(null!));
        }

        [Fact]
        public async Task Null_not_throws_ArgumentNullException()
        {
            var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Autocomplete("query", null!, []));
        }

        [Fact]
        public async Task Null_teamType_throws_ArgumentNullException()
        {
            var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Autocomplete("query", [], null!));
        }

        [Fact]
        public async Task Query_is_passed_to_data_source()
        {
            var controller = CreateController();

            await controller.Autocomplete("example", [], []);

            Assert.Equal("example", _filterUsed!.Query);
        }

        [Fact]
        public async Task Club_teams_are_included_by_default()
        {
            var controller = CreateController();

            await controller.Autocomplete("example", [], []);

            Assert.True(_filterUsed!.IncludeClubTeams);
        }

        [Fact]
        public async Task Club_teams_can_be_excluded()
        {
            var controller = CreateController();

            await controller.Autocomplete("example", [], [], includeClubTeams: false);

            Assert.False(_filterUsed!.IncludeClubTeams);
        }

        [Fact]
        public async Task Valid_not_ids_are_excluded_and_invalid_or_null_ids_are_ignored()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            var controller = CreateController();

            await controller.Autocomplete("example", [first.ToString(), "not-a-guid", null!, second.ToString()], []);

            Assert.Equal([first, second], _filterUsed!.ExcludeTeamIds);
        }

        [Fact]
        public async Task Valid_team_types_are_filtered_and_invalid_types_are_ignored()
        {
            var controller = CreateController();

            await controller.Autocomplete("example", [], ["Regular", "NotATeamType", "SchoolClub"]);

            Assert.Equal([TeamType.Regular, TeamType.SchoolClub], _filterUsed!.TeamTypes);
        }

        [Fact]
        public async Task No_teams_returns_empty_suggestions()
        {
            var controller = CreateController();

            var result = await controller.Autocomplete("example", [], []);

            Assert.Empty(result.suggestions);
        }

        [Fact]
        public async Task Id_data_type_returns_team_id_as_data_by_default()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example-team" };
            _teamDataSource.Setup(x => x.ReadTeams(It.IsAny<TeamFilter>())).ReturnsAsync([team]);

            var controller = CreateController();

            var result = await controller.Autocomplete("example", [], []);

            var suggestion = Assert.Single(result.suggestions);
            Assert.Equal("Example team", suggestion.value);
            Assert.Equal(team.TeamId.ToString(), suggestion.data);
        }

        [Fact]
        public async Task Route_data_type_returns_team_route_without_teams_prefix_as_data()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example-team" };
            _teamDataSource.Setup(x => x.ReadTeams(It.IsAny<TeamFilter>())).ReturnsAsync([team]);

            var controller = CreateController();

            var result = await controller.Autocomplete("example", [], [], data: AutoCompleteDataType.Route);

            var suggestion = Assert.Single(result.suggestions);
            Assert.Equal("example-team", suggestion.data);
        }

        [Fact]
        public async Task Team_that_is_no_longer_active_is_labelled_as_such()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example-team", UntilYear = 2020 };
            _teamDataSource.Setup(x => x.ReadTeams(It.IsAny<TeamFilter>())).ReturnsAsync([team]);

            var controller = CreateController();

            var result = await controller.Autocomplete("example", [], []);

            Assert.Equal("Example team (no longer active)", result.suggestions.Single().value);
        }

        [Fact]
        public async Task Each_team_returned_by_data_source_becomes_a_suggestion_in_order()
        {
            _teamDataSource.Setup(x => x.ReadTeams(It.IsAny<TeamFilter>())).ReturnsAsync([
                new Team { TeamId = Guid.NewGuid(), TeamName = "Team A", TeamRoute = "/teams/a" },
                new Team { TeamId = Guid.NewGuid(), TeamName = "Team B", TeamRoute = "/teams/b" }]);

            var controller = CreateController();

            var result = await controller.Autocomplete("team", [], []);

            Assert.Equal(["Team A", "Team B"], result.suggestions.Select(x => x.value));
        }
    }
}
