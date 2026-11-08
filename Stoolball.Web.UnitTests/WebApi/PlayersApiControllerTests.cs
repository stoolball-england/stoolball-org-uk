using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.WebApi;
using Xunit;

namespace Stoolball.Web.UnitTests.WebApi
{
    public class PlayersApiControllerTests
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private PlayerFilter? _filterUsed;

        public PlayersApiControllerTests()
        {
            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>()))
                .Callback<PlayerFilter>(f => _filterUsed = f)
                .ReturnsAsync(new List<PlayerIdentity>());
        }

        private PlayersApiController CreateController() => new(_playerDataSource.Object);

        private static PlayerIdentity CreateIdentity(Team? team = null) => new()
        {
            PlayerIdentityId = Guid.NewGuid(),
            PlayerIdentityName = "John Smith",
            Player = new Player { PlayerId = Guid.NewGuid() },
            Team = team ?? new Team { TeamId = Guid.NewGuid(), TeamName = "Example team" },
            TotalMatches = 5,
            FirstPlayed = new DateTimeOffset(2020, 5, 1, 0, 0, 0, TimeSpan.Zero),
            LastPlayed = new DateTimeOffset(2023, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        private void SetupIdentities(params PlayerIdentity[] identities)
        {
            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>()))
                .Callback<PlayerFilter>(f => _filterUsed = f)
                .ReturnsAsync(identities.ToList());
        }

        private static string? Property(object? data, string name) => data?.GetType().GetProperty(name)?.GetValue(data)?.ToString();

        [Fact]
        public void Null_player_data_source_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayersApiController(null!));
        }

        [Fact]
        public async Task Null_not_throws_ArgumentNullException()
        {
            var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Autocomplete("query", null!, []));
        }

        [Fact]
        public async Task Null_teams_throws_ArgumentNullException()
        {
            var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Autocomplete("query", [], null!));
        }

        [Fact]
        public async Task Query_is_passed_to_data_source()
        {
            var controller = CreateController();

            await controller.Autocomplete("john", [], []);

            Assert.Equal("john", _filterUsed!.Query);
        }

        [Fact]
        public async Task Players_linked_to_a_member_and_with_multiple_identities_are_included_by_default()
        {
            var controller = CreateController();

            await controller.Autocomplete("john", [], []);

            Assert.True(_filterUsed!.IncludePlayersAndIdentitiesLinkedToAMember);
            Assert.True(_filterUsed.IncludePlayersAndIdentitiesWithMultipleIdentities);
        }

        [Fact]
        public async Task Players_linked_to_a_member_and_with_multiple_identities_can_be_excluded()
        {
            var controller = CreateController();

            await controller.Autocomplete("john", [], [], includeLinkedToMember: false, includeMultipleIdentities: false);

            Assert.False(_filterUsed!.IncludePlayersAndIdentitiesLinkedToAMember);
            Assert.False(_filterUsed.IncludePlayersAndIdentitiesWithMultipleIdentities);
        }

        [Fact]
        public async Task Valid_not_ids_are_excluded_and_invalid_or_null_ids_are_ignored()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var controller = CreateController();

            await controller.Autocomplete("john", [first.ToString(), "not-a-guid", null!, second.ToString()], []);

            Assert.Equal([first, second], _filterUsed!.ExcludePlayerIdentityIds);
        }

        [Fact]
        public async Task Valid_team_ids_are_filtered_and_invalid_or_null_ids_are_ignored()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var controller = CreateController();

            await controller.Autocomplete("john", [], [first.ToString(), "not-a-guid", null!, second.ToString()]);

            Assert.Equal([first, second], _filterUsed!.TeamIds);
        }

        [Fact]
        public async Task No_players_returns_empty_suggestions()
        {
            var controller = CreateController();

            var result = await controller.Autocomplete("john", [], []);

            Assert.Empty(result.suggestions);
        }

        [Fact]
        public async Task Suggestion_has_identity_name_as_value_and_player_team_and_identity_details_as_data()
        {
            var identity = CreateIdentity();
            SetupIdentities(identity);
            var controller = CreateController();

            var result = await controller.Autocomplete("john", [], []);

            var suggestion = Assert.Single(result.suggestions);
            Assert.Equal("John Smith", suggestion.value);
            Assert.Equal(identity.Player!.PlayerId.ToString(), Property(suggestion.data, "playerId"));
            Assert.Equal(identity.PlayerIdentityId.ToString(), Property(suggestion.data, "playerIdentityId"));
            Assert.Equal("John Smith", Property(suggestion.data, "playerIdentityName"));
            Assert.Equal(identity.Team!.TeamId.ToString(), Property(suggestion.data, "teamId"));
            Assert.Equal("Example team", Property(suggestion.data, "teamName"));
        }

        [Fact]
        public async Task Player_record_includes_team_name_unless_searching_a_single_team()
        {
            var identity = CreateIdentity();
            SetupIdentities(identity);
            var controller = CreateController();

            var noTeam = await controller.Autocomplete("john", [], []);
            var oneTeam = await controller.Autocomplete("john", [], [Guid.NewGuid().ToString()]);
            var twoTeams = await controller.Autocomplete("john", [], [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()]);

            Assert.Equal("5 matches for Example team 2020–23", Property(noTeam.suggestions.Single().data, "playerRecord"));
            Assert.Equal("5 matches 2020–23", Property(oneTeam.suggestions.Single().data, "playerRecord"));
            Assert.Equal("5 matches for Example team 2020–23", Property(twoTeams.suggestions.Single().data, "playerRecord"));
        }

        [Fact]
        public async Task Player_record_shows_a_single_year_if_first_and_last_played_in_the_same_year()
        {
            var identity = CreateIdentity();
            identity.TotalMatches = 1;
            identity.LastPlayed = identity.FirstPlayed;
            SetupIdentities(identity);
            var controller = CreateController();

            var result = await controller.Autocomplete("john", [], [Guid.NewGuid().ToString()]);

            Assert.Equal("1 match 2020", Property(result.suggestions.Single().data, "playerRecord"));
        }

        [Fact]
        public async Task Player_record_has_no_years_if_the_player_has_not_played()
        {
            var identity = CreateIdentity();
            identity.TotalMatches = null;
            identity.FirstPlayed = null;
            identity.LastPlayed = null;
            SetupIdentities(identity);
            var controller = CreateController();

            var result = await controller.Autocomplete("john", [], [Guid.NewGuid().ToString()]);

            Assert.Equal("0 matches", Property(result.suggestions.Single().data, "playerRecord"));
        }

        [Fact]
        public async Task Player_record_has_open_ended_range_if_last_played_is_unknown()
        {
            var identity = CreateIdentity();
            identity.LastPlayed = null;
            SetupIdentities(identity);
            var controller = CreateController();

            var result = await controller.Autocomplete("john", [], [Guid.NewGuid().ToString()]);

            Assert.Equal("5 matches 2020–", Property(result.suggestions.Single().data, "playerRecord"));
        }
    }
}
