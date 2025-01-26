using System.Collections.Generic;
using System.Linq;
using Bogus;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;

namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersNotLinkedToMembersProvider(IFakerFactory<Team> teamFakerFactory, IFakerFactory<Player> playerFakerFactory, IFakerFactory<PlayerIdentity> playerIdentityFakerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = teamFakerFactory.Create();
        private readonly Faker<Player> _playerFaker = playerFakerFactory.Create();
        private readonly Faker<PlayerIdentity> _playerIdentityFaker = playerIdentityFakerFactory.Create();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate(1).Single();

            // player with a single identity
            var playerWithSingleIdentity = _playerFaker.Generate(1).Single();
            playerWithSingleIdentity.PlayerIdentities.Add(_playerIdentityFaker.Generate(1).Single());
            playerWithSingleIdentity.PlayerIdentities[0].Player = playerWithSingleIdentity;
            playerWithSingleIdentity.PlayerIdentities[0].Team = team;
            playerWithSingleIdentity.PlayerIdentities[0].LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity;

            // player with two identities both linked by team, on the same team, not linked to member
            var playerWithTwoIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(2, PlayerIdentityLinkedBy.Team, team);
            var playerWithTwoIdentitiesLinkedByAdmin = CreatePlayerWithMultipleIdentities(2, PlayerIdentityLinkedBy.StoolballEngland, team);
            var playerWithThreeIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(3, PlayerIdentityLinkedBy.Team, team);

            return [playerWithSingleIdentity, playerWithTwoIdentitiesLinkedByTeam, playerWithTwoIdentitiesLinkedByAdmin, playerWithThreeIdentitiesLinkedByTeam];
        }

        private Player CreatePlayerWithMultipleIdentities(int howManyIdentities, PlayerIdentityLinkedBy linkedBy, Team team)
        {
            var player = _playerFaker.Generate(1).Single();
            player.PlayerIdentities.AddRange(_playerIdentityFaker.Generate(howManyIdentities));

            foreach (var identity in player.PlayerIdentities)
            {
                identity.Player = player;
                identity.Team = team;
                identity.LinkedBy = linkedBy;
            }

            return player;
        }
    }
}
