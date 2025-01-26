using System;
using System.Collections.Generic;
using Bogus;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;

namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersLinkedToMembersOnSameTeamAsPlayersNotLinkedToMembersProvider(IFakerFactory<Team> teamFakerFactory, IFakerFactory<Player> playerFakerFactory, IFakerFactory<PlayerIdentity> playerIdentityFakerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = teamFakerFactory.Create();
        private readonly Faker<Player> _playerFaker = playerFakerFactory.Create();
        private readonly Faker<PlayerIdentity> _playerIdentityFaker = playerIdentityFakerFactory.Create();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();

            // range of players on the same team, with differing number of identities, some linked by member and some not
            var playerWithOneIdentityLinkedByMember = CreatePlayer(1, team, true);
            var playerWithOneIdentityNotLinkedByMember = CreatePlayer(1, team, false);
            var playerWithTwoIdentitiesLinkedByMember = CreatePlayer(2, team, true);
            var playerWithTwoIdentitiesNotLinkedByMember = CreatePlayer(2, team, false);

            var playerWithTwoIdentitiesOnDifferentTeamsLinkedByMember = CreatePlayer(2, team, true);
            playerWithTwoIdentitiesOnDifferentTeamsLinkedByMember.PlayerIdentities[1].Team = _teamFaker.Generate();

            return [playerWithOneIdentityLinkedByMember,
                    playerWithOneIdentityNotLinkedByMember,
                    playerWithTwoIdentitiesLinkedByMember,
                    playerWithTwoIdentitiesNotLinkedByMember,
                    playerWithTwoIdentitiesOnDifferentTeamsLinkedByMember];
        }

        private Player CreatePlayer(int identities, Team team, bool isLinkedToMember)
        {
            var player = _playerFaker.Generate();
            player.PlayerIdentities.AddRange(_playerIdentityFaker.Generate(identities));
            if (isLinkedToMember)
            {
                player.MemberKey = Guid.NewGuid();
            }

            foreach (var identity in player.PlayerIdentities)
            {
                identity.Player = player;
                identity.Team = team;
                identity.LinkedBy = isLinkedToMember ? PlayerIdentityLinkedBy.Member : PlayerIdentityLinkedBy.Team;
            }

            return player;
        }
    }
}
