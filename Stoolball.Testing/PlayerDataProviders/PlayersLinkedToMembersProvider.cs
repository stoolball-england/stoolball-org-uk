using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;

namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersLinkedToMembersProvider(IFakerFactory<Team> teamFakerFactory, IFakerFactory<Player> playerFakerFactory, IFakerFactory<PlayerIdentity> playerIdentityFakerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = teamFakerFactory.Create();
        private readonly Faker<Player> _playerFaker = playerFakerFactory.Create();
        private readonly Faker<PlayerIdentity> _playerIdentityFaker = playerIdentityFakerFactory.Create();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate(1).Single();

            // two players with only one identity, on the same team, and both linked to a member
            var players = _playerFaker.Generate(2);
            var identities = _playerIdentityFaker.Generate(3);

            for (var i = 0; i < 2; i++)
            {
                players[i].PlayerIdentities.Add(identities[i]);
                identities[i].Player = players[i];
                identities[i].Team = team;
                identities[i].LinkedBy = PlayerIdentityLinkedBy.Member;
                players[i].MemberKey = Guid.NewGuid();
            }

            // another player on the same team, with two identities both linked by member
            var playerWithTwoIdentitiesLinkedByMember = _playerFaker.Generate(1).Single();
            playerWithTwoIdentitiesLinkedByMember.PlayerIdentities.AddRange(_playerIdentityFaker.Generate(2));
            playerWithTwoIdentitiesLinkedByMember.MemberKey = Guid.NewGuid();

            foreach (var identity in playerWithTwoIdentitiesLinkedByMember.PlayerIdentities)
            {
                identity.Player = playerWithTwoIdentitiesLinkedByMember;
                identity.Team = team;
                identity.LinkedBy = PlayerIdentityLinkedBy.Member;
            }
            players.Add(playerWithTwoIdentitiesLinkedByMember);

            // another player on the same team, not linked to a member
            var playerWithoutMember = _playerFaker.Generate(1).Single();
            playerWithoutMember.PlayerIdentities.Add(identities[2]);
            identities[2].Player = playerWithoutMember;
            identities[2].Team = team;
            players.Add(playerWithoutMember);

            // another player on the same team, with two identities but only one linked by member
            var playerWithTwoIdentitiesOneLinkedByMember = _playerFaker.Generate(1).Single();
            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities.AddRange(_playerIdentityFaker.Generate(2));
            playerWithTwoIdentitiesOneLinkedByMember.MemberKey = Guid.NewGuid();

            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities[0].Player = playerWithTwoIdentitiesOneLinkedByMember;
            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities[0].Team = team;
            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities[0].LinkedBy = PlayerIdentityLinkedBy.Member;

            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities[1].Player = playerWithTwoIdentitiesOneLinkedByMember;
            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities[1].Team = team;
            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities[1].LinkedBy = PlayerIdentityLinkedBy.Team;

            players.Add(playerWithTwoIdentitiesOneLinkedByMember);

            return players;
        }
    }
}
